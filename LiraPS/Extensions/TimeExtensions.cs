using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace LiraPS.Extensions;
public static partial class TimeExtensions
{
    public const string DateFormatString = "yyyy-MM-dd HH:mm zzz";
    private const string UnambiguousFormat = "d MMMM yyyy HH:mm";
    public static string NumericalForm(this DateTimeOffset date) => date.ToString(DateFormatString);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string UnambiguousForm(this DateTimeOffset date) => date.ToString(UnambiguousFormat);


    public static readonly string[] ParseFormatters = [
       // Date only
       "yyyy-M-d",
        "d-M-yyyy",
        "d-M-yy",

        // Date + time
        "yyyy-M-d H:m",
        "d-M-yy H:m",
        "d-M-yyyy H:m",

        // Date + time + offset
        "yyyy-M-d H:m zzz",
        "d-M-yy H:m zzz",
        "d-M-yyyy H:m zzz",

        // Date+ offset
        "yyyy-M-d zzz",
        "d-M-yy zzz",
        "d-M-yyyy zzz",

        // Today + time
        "H:m",

        // Today + time + offset
        "H:m zzz",
    ];
    public static bool TryParseDateTimeOffset(string value, out DateTimeOffset dto)
    {
        value = DateTimeCorrecter().Replace(value, "-");
        return DateTimeOffset.TryParseExact(value, ParseFormatters, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dto);
    }
    private static string Pad(int pad)
    {
        return "".PadLeft(pad, ' ');
    }
    public static string PrettyTime(this TimeSpan ts, int pad)
    {

        return $"{Pad(pad)}{(int)ts.TotalHours}h {ts.Minutes}m{Pad(pad)}";
    }
    public static string PrettyTime(this TimeSpan ts) => PrettyTime(ts, 0);
    public static string PrettyDate(this DateTimeOffset dto, int pad)
    {
        var s = dto.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        return $"{Pad(pad)}{s}{Pad(pad)}";
    }
    public static string PrettyDate(this DateTimeOffset dto) => PrettyDate(dto, 0);
    [GeneratedRegex(@"[\/\\\.]", RegexOptions.ExplicitCapture, 250)]
    private static partial Regex DateTimeCorrecter();
}
