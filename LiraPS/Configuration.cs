using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
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
    public bool IsInitialized => !string.IsNullOrWhiteSpace(BaseAddress);
    internal static string GetPath()
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
        return Path.Combine(folder, "Config.json");
    }
    public IAuthorization Authorization { get; set; } = NoAuthorization.Instance;
    public string BaseAddress { get; set; } = default!;

    public static Configuration Load()
    {
        var path = GetPath();
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        IAuthorization auth;
        string address = "";
        try
        {
            var typeElem = doc.RootElement.GetProperty(nameof(Authorization) + "Type");
            var typeName = typeElem.GetString();
            var authorizationType = GetType(typeName);
            var authDataProp = doc.RootElement.GetProperty(nameof(Authorization));
            auth = authDataProp.Deserialize(authorizationType) as IAuthorization ?? throw new ArgumentNullException();
            address = doc.RootElement.GetProperty(nameof(BaseAddress)).GetString() ?? throw new ArgumentNullException();
        }
        catch
        {
            auth = NoAuthorization.Instance;
            address = "";
        }
        return new Configuration() { BaseAddress = address, Authorization = auth };
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
        var dto = new Dictionary<string, object>()
        {
            {"BaseAddress",BaseAddress },
            {"AuthorizationType",Authorization.GetType().FullName! },
            {"Authorization",Authorization },
        };
        var json = JsonSerializer.Serialize(dto);
        File.WriteAllText(GetPath(), json);
    }
}
