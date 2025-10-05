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
    private static readonly Dictionary<string, Worklog> _worklogCache = new Dictionary<string, Worklog>(StringComparer.OrdinalIgnoreCase);
    private static Configuration? _config;

    public static string? LastAddedLogId { get; set; }
    public static IEnumerable<Log> LogQueue => (Logger as IEnumerable<Log>) ?? [];
    public static bool IsActiveSession(Configuration.Information info) => info.Equals(_config?.ToInformation());
    public static LoggingLevelSwitch LogSwitch { get; } = new(Serilog.Events.LogEventLevel.Verbose);
    [System.Diagnostics.CodeAnalysis.AllowNull]
    internal static Configuration Config
    {
        get
        {
            if (_config is null)
            {
                _config = Configuration.Load();
                Configuration.MarkLast(_config);
                Logger.LogDebug("Loaded configuration");

            }
            return _config;
        }
        set
        {
            if (_config != value && _config is not null)
            {
                CloseSession();
                _config = value;
            }
            if (_config is not null)
            {
                Configuration.MarkLast(_config);
            }
        }
    }
    public static bool TryGetCachedWorklog(string id, [NotNullWhen(true)] out Worklog? log) => _worklogCache.TryGetValue(id, out log);
    public static void CacheWorklog(Worklog log)
    {
        _worklogCache[log.ID] = log;
        Logger.LogDebug("Added worklog {id} to session cache", log.ID);
    }
    public static void UncacheWorklog(Worklog log)
    {
        if (_worklogCache.Remove(log.ID))
        {
            Logger.LogDebug("Removed worklog {id} from session cache", log.ID);
        }
    }
    public static void ValidateWorklogCache()
    {
        List<Worklog> cached = [.. _worklogCache.Values];
        foreach (var c in cached)
        {
            if (Client.TryGetCachedIssue(c.ID, out _))
            {
                Logger.LogDebug("Removing client-cached worklog {id} from session cache", c.ID);
                UncacheWorklog(c);
            }
        }
    }

    public static DateTimeOffset? LastAddedLogDate { get; set; } =null;

    public static bool HasConfig
    {
        get
        {
            return _config is not null && _config.IsInitialized;
        }
    }
    public static ILogger<LiraClient> Logger { get; }
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
        Logger = new PSLogger<LiraClient>("log.log");
    }
    public static LiraClient Client { get; private set; } = default!;
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
                Logger.LogInformation("Marked config {name} as invalid",Config.Name);
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
    internal static void CloseSession()
    {
        Client?.Dispose();
        Client = null!;
        Logger.LogDebug("Closed session");
    }
}
