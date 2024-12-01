using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Lira.Cryptography;
public static  class ObfuscatedStorage
{
#if WINDOWS
    private static byte[] Obfuscate_Win<T>(T item)
    {
        var json = JsonHelper.Serialize(item);
        var data = Encoding.UTF8.GetBytes(json);
        return ProtectedData.Protect(data, [2, 1, 3, 7], DataProtectionScope.CurrentUser);
    }
#else
private static byte[] Obfuscate_Lin<T>(T item)
    {
        return [];
    }
#endif
}
