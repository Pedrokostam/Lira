using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Lira;
using Lira.Authorization;

/* Unmerged change from project 'LiraPS (net8.0)'
Before:
using Serilog.Core;
After:
using LiraPS;
using LiraPS;
using LiraPS.Transformers;
using Serilog.Core;
*/
using Serilog.Core;

namespace LiraPS;

public class Configuration
{

    private static HashSet<string> InvalidConfigurationNames = new(StringComparer.OrdinalIgnoreCase);
    private string profileName = DefaultConfigName;
    private const string Extension = ".lbs";
    private static string DefaultConfigName => "DefaultConfig";
    private static string DefaultConfigPath => CreateConfigPathFromName(DefaultConfigName);

    [JsonIgnore]
    public bool IsInitialized => !string.IsNullOrWhiteSpace(ServerAddress);

    public IAuthorization Authorization { get; set; }
    [JsonInclude]
    public string AuthorizationType => Authorization.TypeIdentifier;
    public string ServerAddress { get; set; }

    [JsonIgnore]
    public string SelfPath => Path.Combine(GetConfigFolderPath(), Name + Extension);

    [AllowNull]
    public required string Name
    {
        get => profileName;
        init
        {
            profileName = !string.IsNullOrWhiteSpace(value) ? value! : DefaultConfigName;
        }
    }

    public static string[] GetAvailableProfiles()
    {
        return Directory.GetFiles(GetConfigFolderPath(), "*" + Extension);
    }

    public static string? GetProfilePath_Null(string? profileName = null)
    {
        var present = GetAvailableProfiles();
        var matching = profileName switch
        {
            string name => present.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).Equals(profileName, StringComparison.OrdinalIgnoreCase)),
            null => null,
        };
        return matching;
    }
    public static string? GetProfilePath(string? profileName = null)
    {
        var forbiddenPaths = InvalidConfigurationNames.Select(x => CreateConfigPathFromName(x)).ToList();
        profileName = profileName?.Trim();
        var configFolder = GetConfigFolderPath();
        var lastProfileTxtPath = GetLastProfileTxtPath(configFolder);
        string? lastProfilePath = null;
        if (File.Exists(lastProfileTxtPath))
        {
            var name = File.ReadAllText(lastProfileTxtPath);
            lastProfilePath = CreateConfigPathFromName(name);
            lastProfilePath = File.Exists(lastProfilePath) ? lastProfilePath : null;
        }
        var present = GetAvailableProfiles();
        string?[] candidates = [
            lastProfilePath,
            .. present ,
            DefaultConfigPath
            ];
        var allowedCandidates = candidates.OfType<string>().Except(forbiddenPaths, StringComparer.OrdinalIgnoreCase).ToList();
        var matching = allowedCandidates.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).Equals(profileName, StringComparison.OrdinalIgnoreCase));
        return matching ?? allowedCandidates.FirstOrDefault();
    }

    private static string GetLastProfileTxtPath(string configFolder)
    {
        return Path.Combine(configFolder, "LastProfile.txt");
    }
    private static string CreateConfigPathFromName(string name)
    {
        return Path.ChangeExtension(Path.Combine(GetConfigFolderPath(), name),Extension);
    }
    private static string GetConfigFolderPath()
    {
        string folder;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var localAppdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            folder = Path.Combine(localAppdata, nameof(LiraPS));
        }
        else
        {
            var home = Environment.GetEnvironmentVariable("HOME")!;
            folder = Path.Combine(home, ".config", nameof(LiraPS));
        }
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        return folder;
    }

    public static void MarkLast(Configuration conf)
    {
        File.WriteAllText(GetLastProfileTxtPath(GetConfigFolderPath()), conf.profileName);
    }

    public static void MarkWrong(Configuration conf)
    {
        InvalidConfigurationNames.Add(conf.Name);
    }

    public static Configuration Create(IAuthorization auth, string server, string? profile = null)
    {
        var conf = new Configuration(profile, auth, server);
        InvalidConfigurationNames.Remove(conf.Name);
        conf.Save();
        return conf;
    }

    public static Configuration Load(string? profileName = null)
    {
        var path = GetProfilePath(profileName);
        if (!File.Exists(path))
        {
            return new Configuration() { Name = profileName };
        }
        var confBytes = Storage.DeobfuscateBytes(File.ReadAllBytes(path));
        var confJson = Encoding.UTF8.GetString(confBytes);

        var dto = JsonHelper.Deserialize<Dto>(confJson);

        if (!TypeRegistry.TryGetValue(dto.AuthorizationType, out var authType))
        {
            throw new JsonException($"Unknown TypeIdentifier: {dto.AuthorizationType}");
        }

        var auth = JsonHelper.Deserialize(confJson, authType, nameof(Authorization)) as IAuthorization;

        if (auth is null)
        {
            throw new JsonException($"Could not deserialize Authorization");
        }

        var conf = new Configuration(dto.Name, auth, dto.ServerAddress);

        return conf;
    }

    [SetsRequiredMembers]
    private Configuration(string? profileName = null, IAuthorization? auth = null, string? address = null)
    {
        Name = profileName;
        ServerAddress = address ?? "";
        Authorization = auth ?? NoAuthorization.Instance;
    }

    [SetsRequiredMembers]
    public Configuration() : this(null, null, null) {}

    private static Type GetType(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new ArgumentNullException(nameof(typeName));
        }
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        assemblies.Reverse();
        foreach (var assembly in assemblies)
        {
            var type = assembly.GetType(typeName);
            if (type is not null)
            {
                return type;
            }
        }
        throw new TypeAccessException($"Could not find the type {typeName}");
    }

    public void Save()
    {
        Storage.ObfuscateToFile(this, SelfPath);
    }

    public void SaveAs(string profileName)
    {
        var updated = new Configuration(profileName, this.Authorization, this.ServerAddress);
        updated.Save();
    }


    internal static readonly Dictionary<string, Type> TypeRegistry = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
    public static void RegisterType<T>(string key) where T : IAuthorization
    {
        TypeRegistry[key] = typeof(T);
    }
    public static void RegisterType<T>(T instance) where T : IAuthorization
    {
        TypeRegistry[instance.TypeIdentifier] = typeof(T);
    }

    static Configuration()
    {
        RegisterType<PersonalAccessToken>(PersonalAccessToken.Type);
        RegisterType<CookieProvider>(CookieProvider.Type);
        RegisterType<NoAuthorization>(NoAuthorization.Type);
        RegisterType<AtlassianApiKey>(AtlassianApiKey.Type);
    }


    public Information ToInformation()
    {
        return new Information(SelfPath, Authorization.TypeIdentifier, ServerAddress, Name);
    }

    public readonly record struct Information
    {
        public string Name { get; }
        public string Type { get; }
        public string ServerAddress { get; }
        public string Location { get; }
        public bool IsActive => LiraSession.IsActiveSession(this);
        internal Information(string location, string type, string serverAddress, string name)
        {
            Location = location;
            Type = type;
            ServerAddress = serverAddress;
            Name = name;
        }
    }

    private readonly record struct Dto
    {
        public required string ServerAddress { get; init; }
        public required string AuthorizationType { get; init; }
        public required string Name { get; init; }
    }
}
