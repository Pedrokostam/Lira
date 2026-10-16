using System;
using System.Globalization;
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
    public enum Keywords
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
    public static readonly JqlKeywordDate StartOfDay = new(Keywords.StartOfDay);
    public static readonly JqlKeywordDate EndOfDay = new(Keywords.EndOfDay);
    public static readonly JqlKeywordDate StartOfWeek = new(Keywords.StartOfWeek);
    public static readonly JqlKeywordDate EndOfWeek = new(Keywords.EndOfWeek);
    public static readonly JqlKeywordDate StartOfMonth = new(Keywords.StartOfMonth);
    public static readonly JqlKeywordDate EndOfMonth = new(Keywords.EndOfMonth);
    public static readonly JqlKeywordDate StartOfYear = new(Keywords.StartOfYear);
    public static readonly JqlKeywordDate EndOfYear = new(Keywords.EndOfYear);
    public Keywords Keyword { get; init; }
    /// <summary>
    /// Offset expressed in the default unit represented by the <paramref name="keyword"/> (so, weeks for <see cref="Keywords.EndOfWeek"/>, years for <see cref="Keywords.StartOfYear"/>).
    /// </summary>
    public int Offset { get; init; } = 0;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="keyword">One of the JQL date keywords.</param>
    /// <param name="offset">Offset expressed in the default unit represented by the <paramref name="keyword"/> (so, weeks for <see cref="Keywords.EndOfWeek"/>, years for <see cref="Keywords.StartOfYear"/>).</param>
    public JqlKeywordDate(Keywords keyword, int offset)
    {
        Keyword = keyword;
        Offset = offset;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <inheritdoc cref="JqlKeywordDate(Keywords, int)"/>
    public JqlKeywordDate(Keywords keyword) : this(keyword, 0)
    {
    }
    /// <summary>
    /// Return a new item with the given <paramref name="offset"/>.
    /// </summary>
    /// <inheritdoc cref="JqlKeywordDate(Keywords, int)"/>
    /// <returns>New item with given <paramref name="offset"/>.</returns>
    public JqlKeywordDate WithOffset(int offset) => new(Keyword, offset);

    public string GetJqlValue(TimeZoneInfo accountTimezone)
    {
        string offsetString = Offset.ToString(CultureInfo.InvariantCulture);
        return $"{Keyword}({offsetString})";
    }
    private DateTimeOffset ApplyOffset(DateTimeOffset baseDate)
    {
        if (Offset == 0)
            return baseDate;
        return Keyword switch
        {
            Keywords.StartOfDay => baseDate.AddDays(Offset),
            Keywords.EndOfDay => baseDate.AddDays(Offset),
            Keywords.StartOfWeek => baseDate.AddDays(Offset * 7),
            Keywords.EndOfWeek => baseDate.AddDays(Offset * 7),
            Keywords.StartOfMonth => baseDate.AddMonths(Offset),
            Keywords.EndOfMonth => baseDate.AddMonths(Offset),
            Keywords.StartOfYear => baseDate.AddYears(Offset),
            Keywords.EndOfYear => baseDate.AddYears(Offset),
            _ => throw new NotSupportedException(),
        };
    }
    public DateTimeOffset ToAccountDatetime(TimeZoneInfo accountTimezone)
    {
        var currentTime = DateTimeOffset.UtcNow;
        var accountTime = TimeZoneInfo.ConvertTime(currentTime, accountTimezone);
        accountTime = ApplyOffset(accountTime);
        return Keyword switch
        {
            Keywords.StartOfDay => accountTime.StartOfDay(),
            Keywords.EndOfDay => accountTime.EndOfDay(),
            Keywords.StartOfWeek => accountTime.StartOfWeek(),
            Keywords.EndOfWeek => accountTime.EndOfWeek(),
            Keywords.StartOfMonth => accountTime.StartOfMonth(),
            Keywords.EndOfMonth => accountTime.EndOfMonth(),
            Keywords.StartOfYear => accountTime.StartOfYear(),
            Keywords.EndOfYear => accountTime.EndOfYear(),
            _ => throw new NotSupportedException(),
        };
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
        if (!Enum.TryParse<Keywords>(keywordPart, ignoreCase: true, out var keyword))
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

    [GeneratedRegex(@"(?<keyword>[A-Z]+)(\(?['""]?(?<offset>[\+-]?\d*)['""]?\)?)?", RegexOptions.IgnoreCase|RegexOptions.ExplicitCapture,250)]
    private static partial Regex KeywordOffsetDetector();
}
