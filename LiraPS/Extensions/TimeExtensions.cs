using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    public static string UnambiguousForm(this DateTimeOffset date) => date.UnambiguousForm(false);
    public static string UnambiguousForm(this DateTimeOffset date, bool withTimeZone)
    {
        if (withTimeZone)
        {
            return date.ToString(UnambiguousFormat + " zzz", CultureInfo.InvariantCulture);
        }
        else
        {
            return date.ToLocalTime().ToString(UnambiguousFormat, CultureInfo.InvariantCulture);
        }
    }
    private const string TimePartSeparator = " ";
    public static readonly string[] ParseFormatters = [
       // Date only
        "yyyy M d",
        "d M yyyy",
        "d M yy",
        "d MMM yyyy",
        "d MMMM yyyy",
        "MMM d yyyy",
        "MMMM d yyyy",
        "yyyy MMM d",
        "yyyy MMMM d",
        "d MMM yy",
        "d MMMM yy",

        // Date + time
        "yyyy M d H:m",
        "d M yy H:m",
        "d M yyyy H:m",
        "d MMM yyyy H:m",
        "d MMMM yyyy H:m",
        "MMM d yyyy H:m",
        "MMMM d yyyy H:m",
        "yyyy MMM d H:m",
        "yyyy MMMM d H:m",
        "d MMM yy H:m",
        "d MMMM yy H:m",

        // Date + time + offset
        "yyyy-M-d H:m zzz",
        "d-M-yy H:m zzz",
        "d-M-yyyy H:m zzz",
        "d-MMM-yyyy H:m zzz",
        "d-MMMM-yyyy H:m zzz",
        "MMM d yyyy H:m zzz",
        "MMMM d yyyy H:m zzz",
        "yyyy-MMM-d H:m zzz",
        "yyyy-MMMM-d H:m zzz",
        "d-MMM-yy H:m zzz",
        "d-MMMM-yy H:m zzz",


        // Date + offset
        "yyyy-M-d zzz",
        "d-M-yy zzz",
        "d-M-yyyy zzz",
        "d-MMM-yyyy zzz",
        "d-MMMM-yyyy zzz",
        "MMM-d-yyyy zzz",
        "MMMM-d-yyyy zzz",
        "yyyy-MMM-d zzz",
        "yyyy-MMMM-d zzz",
        "d-MMM-yy zzz",
        "d-MMMM-yy zzz",

        // Today + time
        "H:m",

        // Today + time + offset
        "H:m zzz",
    ];
    private static readonly string[] NormalizedParseFormatters = [.. ParseFormatters.Select(x => DateTimeCorrecter().Replace(x, TimePartSeparator)).Concat(
        ParseFormatters.Where(x=>x.Contains("H:m",StringComparison.Ordinal)).Select(x=>x.Replace("H:m","HHmm",StringComparison.Ordinal)))];
    public static bool TryParseDateTimeOffset(string value, out DateTimeOffset dto)
    {
        value = DateTimeCorrecter().Replace(value, TimePartSeparator);
        return DateTimeOffset.TryParseExact(value, NormalizedParseFormatters, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dto);
    }
    private static string Pad(int pad)
    {
        return "".PadLeft(pad, ' ');
    }
    /// <param name="pad">Symmetric padding - how many spaces are added to each side of the string.</param>
    /// <inheritdoc  cref="PrettyTime(TimeSpan)"/>
    public static string PrettyTime(this TimeSpan ts, int pad)
    {

        return $"{Pad(pad)}{(int)ts.TotalHours}h {ts.Minutes}m{Pad(pad)}";
    }
    /// <summary>
    /// HH:mm, where HH is total number of hours, i.e. can exceed 24.
    /// </summary>
    /// <returns></returns>
    public static string PrettyTime(this TimeSpan ts) => PrettyTime(ts, 0);
    public static string PrettyDate(this DateTimeOffset dto, int pad)
    {
        var s = dto.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        return $"{Pad(pad)}{s}{Pad(pad)}";
    }
    public static string PrettyDate(this DateTimeOffset dto) => PrettyDate(dto, 0);
    [GeneratedRegex(@"[\/\\\.\- :](?!\d?\d:?\d\d)$", RegexOptions.ExplicitCapture, 250)]
    private static partial Regex DateTimeCorrecter();
}
