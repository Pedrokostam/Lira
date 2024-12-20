﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Lira;
using Lira.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Serilog.Core;

namespace LiraPS;
public static class LiraSession
{
    private static Configuration? _config;

    public static IEnumerable<Log> LogQueue { get; } = (Logger as IEnumerable<Log>) ?? [];
    public static LoggingLevelSwitch LogSwitch { get; } = new(Serilog.Events.LogEventLevel.Verbose);
    internal static Configuration Config
    {
        get
        {
            if (_config is null)
            {
                _config = Configuration.Load();
                Logger.LogDebug("Loaded configuration");

            }
            return _config;
        }
        set
        {
            if (_config != value)
            {
                CloseSession();
                _config = value;
            }
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
        Logger = new PSLogger<LiraClient>("log.log");
        //Logger = new SerilogLoggerFactory(serilogger).CreateLogger<LiraClient>();
    }
    public static LiraClient Client { get; private set; } = default!;
    internal static async Task<LiraClient> StartSession()
    {
        if (Client is not null)
        {
            Logger.LogDebug("Reusing existing session.");
            return Client;
        }
        Config ??= Configuration.Load();
        if (!Config.IsInitialized)
        {
            throw new InvalidOperationException("Attempted to load unitialized configuration. Call Set-Configuration to initiialize it.");
        }
        Client = await LiraSessionFactory.Create(Config.BaseAddress)
             .WithLogger(Logger)
             .AuthorizedBy(Config.Authorization)
             .Initialize();
        if (Client.Authorization is NoAuthorization)
        {
            Logger.LogWarning("Created session with no authorization.");
        }
        Config.BaseAddress = Client.ServerAddress.ToString();
        Config.Save();
        return Client;
    }
    public static bool TestSessionDateAvailable()
    {
        if (Config is not null)
        {
            return Config.IsInitialized;
        }
        if (File.Exists(Configuration.GetPath()))
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
