using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lira.Extensions;
public static partial class StringExtensions
{
    public static string StripMarkup(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return AntiFormatter().Replace(input, "");

    }

    [GeneratedRegex(@"\{.*?\}|\[.*?\|.*?\]|\*|_|{{|}}|h\d\.\s*|^[-*#]+\s*", RegexOptions.Multiline,500)]
    private static partial Regex AntiFormatter();
}
