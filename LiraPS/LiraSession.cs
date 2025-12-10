using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Lira;
using Lira.Authorization;
using Lira.Objects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Serilog.Core;

namespace LiraPS;

public static class LiraSession
{
    // --- Internal state -----------------------------------------------------------------------

    private static readonly Dictionary<string, Worklog> WorklogCache = new(StringComparer.OrdinalIgnoreCase);
    private static Configuration? Configuration;

    // --- Recent / last-added log helpers ------------------------------------------------------

    /// <summary>
    /// Gets the key of the last added log (via <see cref="RecentIssues"/>)
    /// </summary>
    public static string? LastAddedLogId
    {
        get
        {
            if (RecentIssues.Count > 0)
            {
                return RecentIssues.GetRecentIDs().First().Key;
            }

            return null;
        }
    }       

    /// <summary>
    /// The date/time of the last added log, if known.
    /// </summary>
    public static DateTimeOffset? LastAddedLogDate { get; set; } = null;

    // --- Worklog cache management -------------------------------------------------------------

    /// <summary>
    /// Attempts to get a cached <see cref="Worklog"/> by identifier.
    /// </summary>
    /// <param name="id">Worklog identifier to look up.</param>
    /// <param name="log">When this method returns, contains the cached <see cref="Worklog"/> if found; otherwise null.</param>
    /// <returns>True if a cached worklog was found; otherwise false.</returns>
    public static bool TryGetCachedWorklog(string id, [NotNullWhen(true)] out Worklog? log) => WorklogCache.TryGetValue(id, out log);

    /// <summary>
    /// Adds or updates the given <see cref="Worklog"/> in the session cache.
    /// </summary>
    /// <param name="log">Worklog to cache.</param>
    public static void CacheWorklog(Worklog log)
    {
        WorklogCache[log.ID] = log;
        Logger.LogDebug("Added worklog {id} to session cache", log.ID);
    }

    /// <summary>
    /// Removes the given <see cref="Worklog"/> from the session cache if present.
    /// </summary>
    /// <param name="log">Worklog to remove from cache.</param>
    public static void UncacheWorklog(Worklog log)
    {
        if (WorklogCache.Remove(log.ID))
        {
            Logger.LogDebug("Removed worklog {id} from session cache", log.ID);
        }
    }

    /// <summary>
    /// Validates the session worklog cache and removes entries that are also cached by the client.
    /// </summary>
    public static void ValidateWorklogCache()
    {
        List<Worklog> cached = [.. WorklogCache.Values];
        foreach (var c in cached)
        {
            if (Client.TryGetCachedIssue(c.ID, out _))
            {
                Logger.LogDebug("Removing client-cached worklog {id} from session cache", c.ID);
                UncacheWorklog(c);
            }
        }
    }

    // --- Configuration access and state -------------------------------------------------------

    /// <summary>
    /// Gets or sets the active <see cref="LiraPS.Configuration"/> for the session.
    /// The getter lazily loads the configuration and marks it as the last-used configuration.
    /// The setter will close any active session if the configuration changes and mark the new config as last.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.AllowNull]
    internal static Configuration Config
    {
        get
        {
            if (Configuration is null)
            {
                Configuration = Configuration.Load();
                Configuration.MarkLast(Configuration);
                Logger.LogDebug("Loaded configuration");

            }
            return Configuration;
        }
        set
        {
            if (Configuration != value && Configuration is not null)
            {
                CloseSession();
                Configuration = value;
            }
            if (Configuration is not null)
            {
                Configuration.MarkLast(Configuration);
            }
        }
    }

    /// <summary>
    /// Gets whether a valid, initialized configuration is available in the session.
    /// </summary>
    public static bool HasConfig
    {
        get
        {
            return Configuration is not null && Configuration.IsInitialized;
        }
    }

    /// <summary>
    /// Determines whether the provided configuration information matches the currently active session configuration.
    /// </summary>
    /// <param name="info">Configuration information to compare against the active session.</param>
    /// <returns>True if the provided information matches the active session configuration; otherwise false.</returns>
    public static bool IsActiveSession(Configuration.Information info) => info.Equals(Configuration?.ToInformation());

    /// <summary>
    /// Tests whether session configuration data is available and initialized.
    /// Attempts to load the profile from disk if necessary.
    /// </summary>
    /// <returns>True if a valid configuration is available; otherwise false.</returns>
    public static bool TestSessionDateAvailable()
    {
        if (Config is not null)
        {
            return Config.IsInitialized;
        }
        if (File.Exists(Configuration.GetProfilePath()))
        {
            try
            {
                Config = Configuration.Load();
                return Config.IsInitialized;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    // --- Logging helpers ---------------------------------------------------------------------

    /// <summary>
    /// Exposes the logger's queued log events as an enumerable, if supported by the logger implementation.
    /// </summary>
    public static IEnumerable<Log> LogQueue => (Logger as IEnumerable<Log>) ?? [];

    /// <summary>
    /// Switch used by Serilog to control the logging level dynamically.
    /// </summary>
    public static LoggingLevelSwitch LogSwitch { get; } = new(Serilog.Events.LogEventLevel.Verbose);

    /// <summary>
    /// Logger instance used by the session and helpers.
    /// </summary>
    public static IPSLogger<LiraClient> Logger { get; }

    /// <summary>
    /// Static constructor. Initializes logging infrastructure for the session.
    /// </summary>
    static LiraSession()
    {
        //        var serilogger = new LoggerConfiguration()
        //             //.WriteTo.File("test.log").MinimumLevel.Verbose()
        //             .WriteTo.Sink(LogSink).MinimumLevel.Verbose()
        //#if DEBUG
        //             //.WriteTo.Debug()
        //#endif
        //             .CreateLogger();
        //Logger = new SerilogLoggerFactory(serilogger).CreateLogger<LiraClient>();
        Logger = new PSLogger<LiraClient>();
    }

    // --- Client lifecycle --------------------------------------------------------------------

    /// <summary>
    /// Active <see cref="LiraClient"/> for the current session.
    /// </summary>
    public static LiraClient Client { get; private set; } = default!;

    /// <summary>
    /// Starts or returns the active <see cref="LiraClient"/> session. If a session exists it's reused.
    /// This method will attempt to create and initialize a client using the session configuration and will retry
    /// after marking a configuration as wrong if initialization fails.
    /// </summary>
    /// <returns>An initialized <see cref="LiraClient"/>.</returns>
    internal static async Task<LiraClient> StartSession()
    {
        if (Client is not null)
        {
            Logger.LogDebug("Reusing existing session.");
            return Client;
        }
        while (true)
        {

            Config ??= Configuration.Load();
            if (!Config.IsInitialized)
            {
                throw new PSInvalidOperationException("Attempted to load unitialized configuration. Call Set-Configuration to initialize it.");
            }
            try
            {
                Client = await LiraSessionFactory.Create(Config.ServerAddress)
                     .WithLogger(Logger)
                     .AuthorizedBy(Config.Authorization)
                     .Online()
                     .Initialize();
                break;
            }
            catch (Exception)
            {
                Configuration.MarkWrong(Config);
                Config = null;
                Logger.LogInformation("Marked config {name} as invalid", Config.Name);
            }
        }
        if (Client.Authorization is NoAuthorization)
        {
            Logger.LogWarning("Created session with no authorization.");
        }
        Config.ServerAddress = Client.ServerAddress.ToString();
        Config.Save();
        return Client;
    }

    /// <summary>
    /// Disposes and clears the current <see cref="LiraClient"/>, closing the session.
    /// </summary>
    internal static void CloseSession()
    {
        Client?.Dispose();
        Client = null!;
        Logger.LogDebug("Closed session");
    }
}
