using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lira.Objects;

namespace LiraPS.Extensions;
public static partial class FormattingExtensions
{
    public static string TrimString(this string s,int length = 24)
    {
        var trimmed = s.Trim();
        if (trimmed.Length > length)
        {
            var oneless = length - 1;
            return trimmed[..oneless] + "…";
        }
        return trimmed;
    }

    public static string OnelineUserDetails(this UserDetails ud)
    {
        var tz = ud.TimeZone is null ? "" : $" ({ud.TimeZone.DisplayName})";
        return $"${ud.Name} - {ud.DisplayName}{tz}";
    }

    //[GeneratedRegex(@"^\s*\[.*\]\s*")]
    //private static partial Regex BracketRemover();
}
