using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Lira.Extensions;
using Dtx = Lira.Extensions.DateTimeExtensions;
namespace Lira.Jql;

/// <summary>
/// Implementation of <see cref="IJqlDate"/> based on date keywords available in JQL.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly partial record struct JqlKeywordDate : IJqlDate
{
    public enum JqlDateKeywords
    {
        StartOfDay,
        EndOfDay,
        /// <summary>
        /// Date for the first day of the current week, i.e. Sunday.
        /// </summary>
        StartOfWeek,
        /// <summary>
        /// Date for the last day of the current week, i.e. Saturday.
        /// </summary>
        EndOfWeek,
        StartOfMonth,
        EndOfMonth,
        StartOfYear,
        EndOfYear,
    }
    public static readonly JqlKeywordDate StartOfDay = new(JqlDateKeywords.StartOfDay);
    public static readonly JqlKeywordDate EndOfDay = new(JqlDateKeywords.EndOfDay);
    public static readonly JqlKeywordDate StartOfWeek = new(JqlDateKeywords.StartOfWeek);
    public static readonly JqlKeywordDate EndOfWeek = new(JqlDateKeywords.EndOfWeek);
    public static readonly JqlKeywordDate StartOfMonth = new(JqlDateKeywords.StartOfMonth);
    public static readonly JqlKeywordDate EndOfMonth = new(JqlDateKeywords.EndOfMonth);
    public static readonly JqlKeywordDate StartOfYear = new(JqlDateKeywords.StartOfYear);
    public static readonly JqlKeywordDate EndOfYear = new(JqlDateKeywords.EndOfYear);
    public JqlDateKeywords Keyword { get; }
    /// <summary>
    /// Offset expressed in the default unit represented by the <paramref name="keyword"/> (so, weeks for <see cref="JqlDateKeywords.EndOfWeek"/>, years for <see cref="JqlDateKeywords.StartOfYear"/>).
    /// </summary>
    public int Offset { get; } = 0;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="keyword">One of the JQL date keywords.</param>
    /// <param name="offset">Offset expressed in the default unit represented by the <paramref name="keyword"/> (so, weeks for <see cref="JqlDateKeywords.EndOfWeek"/>, years for <see cref="JqlDateKeywords.StartOfYear"/>).</param>
    public JqlKeywordDate(JqlDateKeywords keyword, int offset)
    {
        Keyword = keyword;
        Offset = offset;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <inheritdoc cref="JqlKeywordDate(JqlDateKeywords, int)"/>
    public JqlKeywordDate(JqlDateKeywords keyword) : this(keyword, 0)
    {
    }
    /// <summary>
    /// Return a new item with the given <paramref name="offset"/>.
    /// </summary>
    /// <inheritdoc cref="JqlKeywordDate(JqlDateKeywords, int)"/>
    /// <returns>New item with given <paramref name="offset"/>.</returns>
    public JqlKeywordDate WithOffset(int offset) => new JqlKeywordDate(Keyword, offset);

    public string GetJqlValue(TimeZoneInfo accountTimezone)
    {
        return $"{Keyword}({Offset})";
    }
    private DateTimeOffset ApplyOffset(DateTimeOffset baseDate)
    {
        if (Offset == 0)
            return baseDate;
        return Keyword switch
        {
            JqlDateKeywords.StartOfDay => baseDate.AddDays(Offset),
            JqlDateKeywords.EndOfDay => baseDate.AddDays(Offset),
            JqlDateKeywords.StartOfWeek => baseDate.AddDays(Offset * 7),
            JqlDateKeywords.EndOfWeek => baseDate.AddDays(Offset * 7),
            JqlDateKeywords.StartOfMonth => baseDate.AddMonths(Offset),
            JqlDateKeywords.EndOfMonth => baseDate.AddMonths(Offset),
            JqlDateKeywords.StartOfYear => baseDate.AddYears(Offset),
            JqlDateKeywords.EndOfYear => baseDate.AddYears(Offset),
            _ => throw new NotSupportedException()
        };
    }
    public DateTimeOffset ToAccountDatetime(TimeZoneInfo accountTimezone)
    {
        var currentTime = DateTimeOffset.UtcNow;
        var accountTime = TimeZoneInfo.ConvertTime(currentTime, accountTimezone);
        accountTime = ApplyOffset(accountTime);
        return Keyword switch
        {
            JqlDateKeywords.StartOfDay => accountTime.StartOfDay(),
            JqlDateKeywords.EndOfDay => accountTime.EndOfDay(),
            JqlDateKeywords.StartOfWeek => accountTime.StartOfWeek(),
            JqlDateKeywords.EndOfWeek => accountTime.EndOfWeek(),
            JqlDateKeywords.StartOfMonth => accountTime.StartOfMonth(),
            JqlDateKeywords.EndOfMonth => accountTime.EndOfMonth(),
            JqlDateKeywords.StartOfYear => accountTime.StartOfYear(),
            JqlDateKeywords.EndOfYear => accountTime.EndOfYear(),
            _ => throw new NotSupportedException(),
        };
    }

    public JqlManualDate ToManualDate(TimeZoneInfo timezone)
    {
        return new JqlManualDate(ToAccountDatetime(timezone));
    }

    public static bool TryParse(string value, out JqlKeywordDate keywordDate)
    {
        keywordDate = default;
        var match = KeywordOffsetDetector().Match(value);
        if (!match.Success)
        {
            return false;
        }
        var keywordPart = match.Groups["keyword"].Value;
        if (!Enum.TryParse<JqlDateKeywords>(keywordPart, ignoreCase: true, out var keyword))
        {
            return false;
        }
        int offset = 0;
        if (match.Groups.Count > 1)
        {
            if (!int.TryParse(match.Groups["offset"].Value, System.Globalization.NumberStyles.Integer, provider: null, out offset))
            {
                offset = 0;
            }
        }
        keywordDate = new JqlKeywordDate(keyword, offset);
        return true;
    }
    private const int Timeout = 250;
    private const string Pattern = @"(?<keyword>[A-Z]+)(\(?['""]?(?<offset>[\+-]?\d*)['""]?\)?)?";
#if NETSTANDARD2_0
    private static readonly Regex _KeywordOffsetDetector = new Regex(
        Pattern,
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(Timeout));
    private static Regex KeywordOffsetDetector() => _KeywordOffsetDetector;
#else
    [GeneratedRegex(Pattern, RegexOptions.IgnoreCase|RegexOptions.ExplicitCapture,Timeout)]
    private static partial Regex KeywordOffsetDetector();
#endif
}
