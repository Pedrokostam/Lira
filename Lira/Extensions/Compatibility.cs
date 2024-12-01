using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
namespace Lira.Extensions;
internal static class Compatibility
{
#if NETSTANDARD2_0
public static bool Contains(this string s,string other,StringComparison comparison)
    {
        return s.IndexOf(other, comparison) >= 0;
    }
    public static bool TryGetValue(this GroupCollection groups, string key, [NotNullWhen(true)] out Group? value)
    {
		try
		{
			value = groups[key];
			return value.Success;
		}
		catch (Exception)
		{
			value = null;
			return false;
		}
    }
#endif
}
