using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    private string profileName = DefaultConfigName;

    /// <summary>
    /// Checks if <see cref="ServerAddress"/> is valid.
    /// </summary>
    [JsonIgnore]
    public bool IsInitialized => !string.IsNullOrWhiteSpace(ServerAddress);
    private const string Extension = ".lbs";
    private static string DefaultConfigName => "DefaultConfig";
    private static string DefaultConfigPath => Path.Combine(GetConfigFolderPath(), DefaultConfigName);
    public static string[] GetAvailableProfiles()
    {
        return Directory.GetFiles(GetConfigFolderPath(), "*" + Extension);
    }
    public static string GetProfilePath(string? profileName = null)
    {
        profileName = profileName?.Trim();
        var configFolder = GetConfigFolderPath();
        var lastProfileTxtPath = GetLastProfileTxtPath(configFolder);
        string? lastProfilePath = null;
        if (File.Exists(lastProfileTxtPath))
        {
            var name = File.ReadAllText(lastProfileTxtPath);
            lastProfilePath = Path.Combine(configFolder, name);
            lastProfilePath = Path.ChangeExtension(lastProfilePath, Extension);
            lastProfilePath = File.Exists(lastProfilePath) ? lastProfilePath : null;
        }
        profileName = string.IsNullOrWhiteSpace(profileName) ? null : profileName!.Trim();
        var present = GetAvailableProfiles();
        var matching = profileName switch
        {
            string name => present.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).Equals(profileName, StringComparison.OrdinalIgnoreCase)),
            null => null,
        };
        return matching ?? lastProfilePath ?? present.FirstOrDefault() ?? DefaultConfigPath;
    }

    private static string GetLastProfileTxtPath(string configFolder)
    {
        return Path.Combine(configFolder, "LastProfile.txt");
    }

    private static string GetConfigFolderPath()
    {
        string folder;
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
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
    public static Configuration Create(IAuthorization auth, string server, string? profile = null)
    {
        var conf = new Configuration(profile, auth, server);
        conf.Save();
        return conf;
    }
    public IAuthorization Authorization { get; set; }
    public string ServerAddress { get; set; }
    [JsonIgnore]
    public string SelfPath => Path.Combine(GetConfigFolderPath(), Name+Extension);

    [AllowNull]
    public required string Name
    {
        get => profileName;
        init
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                profileName = value!;
            }
            else
            {
                profileName = DefaultConfigName;

            }
        }
    }

    public static Configuration Load(string? profileName = null)
    {
        var path = GetProfilePath(profileName);
        if (!File.Exists(path))
        {
            return new Configuration() { Name = profileName };
        }
        var empty = new Configuration(profileName);
        var conf = Storage.DeobfuscateFromFile<Configuration>(path) ?? empty;
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
    public Configuration() : this(null, null, null)
    {

    }
    private static Type GetType(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new ArgumentNullException(nameof(typeName));
        }
        Type? type;
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies.Reverse())
        {
            type = assembly.GetType(typeName);
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
    public Information ToInformation()
    {
        return new Information(SelfPath, Authorization.TypeIdentifier, ServerAddress);
    }
    public readonly record struct Information
    {
        public string Location { get; }
        public string Type { get; }
        public string ServerAddress { get; }
        internal Information(string location, string type, string serverAddress)
        {
            Location = location;
            Type = type;
            ServerAddress = serverAddress;
        }
    }
    private readonly record struct Dto
    {
        public string ServerAddress { get; init; }
        public string AuthorizationType { get; init; }
        public string Name { get; init; }
    }
}
