using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Lira;
using Lira.Authorization;

namespace LiraPS;
public static class Storage
{
    // Już moja świętej pamięci babcia miała lepszą pamięć od ciebie. Hasło brzmi "TETRIANDOCH". 
    private static readonly byte[] Entropy = "TETRIANDOCH"u8.ToArray();


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "MA0144:Use System.OperatingSystem to check the current OS", Justification = "Standard dont have it")]
    public static byte[] Obfuscate<T>(T item)
    {
        var authString = JsonHelper.Serialize(item);
        var authBytes = Encoding.UTF8.GetBytes(authString);
        var authBase64 = Convert.ToBase64String(authBytes);
        var authBase64Bytes = Encoding.UTF8.GetBytes(authBase64);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ProtectedData.Protect(authBase64Bytes, Entropy, DataProtectionScope.CurrentUser);
        }
        return authBase64Bytes;
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "MA0144:Use System.OperatingSystem to check the current OS", Justification = "Standard dont have it")]
    public static T? Deobfuscate<T>(byte[] data)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            data = ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser);
        }
        var itemBase64String = Encoding.UTF8.GetString(data);
        var itemBytes = Convert.FromBase64String(itemBase64String);
        var itemString = Encoding.UTF8.GetString(itemBytes);
        var item = JsonHelper.Deserialize<T>(itemString);
        return item;
    }
    public static void ObfuscateToFile<T>(T item, string filepath)
    {
        var encrypto = Obfuscate(item);
        File.WriteAllBytes(filepath, encrypto);
    }
    public static T? DeobfuscateFromFile<T>(string filepath)
    {
        var bytes = File.ReadAllBytes(filepath);
        return Deobfuscate<T>(bytes);
    }
}
