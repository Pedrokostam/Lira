using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;
using ConsoleMenu;
using Lira.Extensions;
using Lira.Jql;
using LiraPS.Arguments;
using LiraPS.Completers;
using LiraPS.Extensions;
using LiraPS.Transformers;
using static ConsoleMenu.ICompleter;
using static LiraPS.StringFormatter;
namespace LiraPS;

/// <summary>
/// Provides helper methods for generating date completions for JQL arguments in PowerShell.
/// Includes logic for parsing partial date input, matching date patterns, generating keyword and relative date suggestions,
/// and formatting completion results with tooltips for enhanced user experience.
/// </summary>
internal static partial class DateCompletionHelper
{
    public readonly record struct LastLogDateCompletion(int Shift, DateTimeOffset BaseDate, bool IsValid)
    {
        public DateTimeOffset? ParsedDate => BaseDate.AddDays(Shift);
        public string CompletionString
        {
            get
            {
                return $"{DateCompletionHelper.LastLogDateKeyword} {NumberString}";
            }
        }
        private string NumberString => (Shift).ToString("+0;-0;0", null);

        public CompletionResult ToCompletionResult(bool noWrap)
        {
            return CreateCompletion(
                CompletionString,
                listItemText: CompletionString,
                CompletionResultType.ParameterValue,
                $"Date of the last added log shifted by {NumberString} days -> {ParsedDate?.UnambiguousForm() ?? "N/A"}",
                noWrap);
        }
    }
    public const string LastLogDateKeyword = "LastLog";
    public const string NowKeyword = "Now";
    public const string TodayKeyword = "Today";
    public const string YesterdayKeywordStart = "Yester";
    private const string Pattern = @"^((?<year2>\d{2})(?=[-\/\\]|$)|(?<year4>\d{3,4}))(([-\/\\])?(?<month>1[0-2]|0?[1-9])(([-\/\\])?(?<day>3[01]|[12][0-9]|0?[1-9]))?)?";
    [GeneratedRegex(Pattern, RegexOptions.ExplicitCapture, 250)]
    public static partial Regex DateStringChecker();
    [GeneratedRegex(@"[\(\)\[\]\{\}]", RegexOptions.ExplicitCapture, 250)]
    public static partial Regex LastLogSanitizer();
    [GeneratedRegex(@"[^\d+-]", RegexOptions.ExplicitCapture, 250)]
    public static partial Regex LastLogShiftInvalidator();
    [GeneratedRegex(@"^l?a?s?t?l?o?g?[\s\[\]{}()<>+]*(?<shift>[-+]?\d+)?[\s\[\]{}()<>]*$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase, 250)]
    public static partial Regex LastLogParser();
    /// <summary>
    /// Gets a specific date based on a JQL keyword, converted to the local time zone.
    /// </summary>
    private static DateTimeOffset GetSpecificDate(JqlKeywordDate.Keywords keyword)
    {
        return new JqlKeywordDate(keyword).ToAccountDatetime(TimeZoneInfo.Local);
    }

    internal static CompletionResult CreateCompletion(string completionText, string listItemText, CompletionResultType resultType, string toolTip, bool noWrap)
    {
        var completionNoSpace = noWrap || !completionText.Contains(' ') ? completionText : $"'{completionText}'";
        return new CompletionResult(completionNoSpace, listItemText, resultType, toolTip);
    }
    /// <summary>
    /// Adds a date variant to the list based on the start/end of a specified time unit (day, week, month, year).
    /// </summary>
    /// <param name="dates">The list to add the date to.</param>
    private static void AddDateFromStartAndUnit(List<ITooltipDate> dates, DateTimeOffset baseDate, bool isStart, TimeUnit unit)
    {
        string baseString = baseDate.UnambiguousForm();
        int weekNumber = GetWeekNumber(baseDate);
        (JqlKeywordDate.Keywords keyword, string tooltip) = (isStart, unit) switch
        {
            (true, TimeUnit.Day) => (JqlKeywordDate.Keywords.StartOfDay, Format($"Start of day {baseString}")),
            (false, TimeUnit.Day) => (JqlKeywordDate.Keywords.EndOfDay, Format($"End of day {baseString}")),
            (true, TimeUnit.Week) => (JqlKeywordDate.Keywords.StartOfWeek, Format($"First day of week number {weekNumber}")),
            (false, TimeUnit.Week) => (JqlKeywordDate.Keywords.EndOfWeek, Format($"Last day of week number {weekNumber}")),
            (true, TimeUnit.Month) => (JqlKeywordDate.Keywords.StartOfMonth, Format($"First day of month {baseDate:MMMM}/{baseDate:yyyy}")),
            (false, TimeUnit.Month) => (JqlKeywordDate.Keywords.EndOfMonth, Format($"Last day of month {baseDate:MMMM}/{baseDate:yyyy}")),
            (true, TimeUnit.Year) => (JqlKeywordDate.Keywords.StartOfYear, Format($"First day of year {baseDate:yyyy}")),
            (false, TimeUnit.Year) => (JqlKeywordDate.Keywords.EndOfYear, Format($"Last day of year {baseDate:yyyy}")),
            _ => throw new PSNotSupportedException(),
        };
        dates.Add(new TooltipManualDate(keyword.ToDateTimeOffset(baseDate), tooltip));
    }
    /// <summary>
    /// Gets the week number of the year for the specified date, using the current culture's calendar.
    /// </summary>
    private static int GetWeekNumber(DateTimeOffset baseDate)
    {
        return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(baseDate.Date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);
    }

    private static string PluralYear(int yearDiff)
    {
        return yearDiff switch
        {
            1 => $"{yearDiff} year",
            _ => $"{yearDiff} years"
        };
    }
    // TODO tests

    /// <summary>
    /// Returns a string describing the difference between the specified year and the current year.
    /// </summary>
    /// <param name="year">The year to compare.</param>
    private static string GetYearDifferenceString(int year, ref DateTimeOffset now)
    {
        var yearDiff = now.Year - year;
        return yearDiff switch
        {
            0 => Format($"this year"),
            > 0 => Format($"{PluralYear(yearDiff)} ago"),
            < 0 => Format($"{PluralYear(-yearDiff)} in the future")
        };
    }
    /// <summary>
    /// Attempts to add a date variant to the list, handling invalid dates gracefully.
    /// </summary>
    /// <param name="variants">The list to add the date to.</param>
    private static void TryAddDateVariant(List<ITooltipDate> variants, int year, int month, int day, int hour, int minute, string tooltip)
    {
        try
        {
            variants.Add(new TooltipManualDate(new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Local), tooltip));
        }
        catch (ArgumentOutOfRangeException)
        {
            // if it is impossible to add (e.g. day 30 of february
            // dont do anything
        }
    }
    /// <summary>
    /// Sets an integer value from a named regex group if present.
    /// </summary>
    private static int SetNumber(Match match, string name, ref int value)
    {
        if (GetGroup(match, name) is string numberStr)
        {
            value = int.Parse(numberStr);
            return numberStr.Length;
        }
        return 0;
    }
    /// <summary>
    /// Gets the value of a named regex group, or null if not present or empty.
    /// </summary>
    private static string? GetGroup(Match match, string group)
    {
        var s = match.Groups[group]?.Value;
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static readonly JqlKeywordDate.Keywords[] _periods = Enum.GetValues(typeof(JqlKeywordDate.Keywords)).Cast<JqlKeywordDate.Keywords>().ToArray();
    /// <summary>
    /// Matches a partial or complete date string and generates completion results for possible date values.
    /// </summary>
    public static IEnumerable<CompletionResult> MatchDate(string wordToComplete, DateMode mode, bool noWrap)
    {
        wordToComplete = wordToComplete.Trim();
        if (string.IsNullOrEmpty(wordToComplete))
        {
            wordToComplete = DateTimeOffset.Now.NumericalForm();
        }

        var match = DateStringChecker().Match(wordToComplete.Trim());
        int year = -1, month = -1, day = -1;

        var yearDigits = SetNumber(match, "year4", ref year);
        if (yearDigits == 0)
        {
            if (SetNumber(match, "year2", ref year) >= 0)
            {
                year += 2000;
            }
        }
        else if (yearDigits == 3)
        {
            year *= 10;
        }
        SetNumber(match, "month", ref month);
        SetNumber(match, "day", ref day);
        var now = DateTimeOffset.Now;
        var time = mode switch
        {
            DateMode.Current => now.TimeOfDay,
            DateMode.Start => TimeSpan.Zero,
            DateMode.End => TimeSpan.FromHours(24).Subtract(TimeSpan.FromTicks(1)),
            _ => throw new PSNotImplementedException(),
        };
        List<ITooltipDate> variants = [];
        var isStart = mode != DateMode.End;
        if (year > 0)
        {
            var yearDate = new DateTime(year, 1, 1, time.Hours, time.Minutes, 0, DateTimeKind.Local);
            if (month > 0)
            {
                var monthDate = new DateTime(year, month, 1, time.Hours, time.Minutes, 0, DateTimeKind.Local);
                if (day > 0)
                {
                    variants.Add(new TooltipManualDate(new DateTime(year, month, day, time.Hours, time.Minutes, 0), "Today's date"));
                }
                else
                {
                    AddDateFromStartAndUnit(variants, monthDate, isStart, TimeUnit.Month);
                    TryAddDateVariant(variants, year, month, now.Day, time.Hours, time.Minutes, "Exact parsed day");
                }
            }
            else
            {
                AddDateFromStartAndUnit(variants, yearDate, isStart, TimeUnit.Year);
            }
            string yearDiff = GetYearDifferenceString(year, ref now);
            TryAddDateVariant(variants, year, now.Month, 1, time.Hours, time.Minutes, Format($"First day of {now:MMMM}, {yearDiff}"));
            TryAddDateVariant(variants, year, now.Month, now.Day, time.Hours, time.Minutes, "Today's date, " + yearDiff);
        }
        else
        {
            variants.Add(new TooltipManualDate(now, "Today's date"));
            AddDateFromStartAndUnit(variants, now, isStart, TimeUnit.Day);
            AddDateFromStartAndUnit(variants, now, isStart, TimeUnit.Month);
            AddDateFromStartAndUnit(variants, now, isStart, TimeUnit.Year);
        }
        var uniques = variants.Distinct();
        foreach (var item in uniques)
        {
            var str = item.NumericalForm();
            yield return CreateCompletion(str, str, CompletionResultType.ParameterValue, item.Tooltip, noWrap);
        }
    }
    /// <summary>
    /// Attempts to parse a non-positive integer as a relative date (e.g., "0" for today, "-1" for yesterday).
    /// </summary>
    public static bool GetDateFromNonPositiveInt(string s, DateMode mode, out DateTimeOffset date, out int number)
    {
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out number) && number <= 0)
        {
            IJqlDate? todayo = null;
            try
            {
                todayo = mode switch
                {
                    DateMode.Current => new JqlManualDate(DateTimeOffset.Now.AddDays(number)),
                    DateMode.Start => new JqlKeywordDate(JqlKeywordDate.Keywords.StartOfDay, number),
                    DateMode.End => new JqlKeywordDate(JqlKeywordDate.Keywords.EndOfDay, number),
                    _ => null,
                };
            }
            catch (Exception)
            {
                todayo = null;
            }
            date = todayo?.ToAccountDatetime(TimeZoneInfo.Local) ?? default;
            return todayo is not null;
        }
        number = default;
        date = default;
        return false;
    }
    /// <summary>
    /// Attempts to generate a completion result for a non-positive integer date input.
    /// </summary>
    public static bool GetIntCompletions(string wordToComplete, DateMode mode, [NotNullWhen(true)] out CompletionResult? completionResult, bool noWrap)
    {
        completionResult = null;
        if (GetDateFromNonPositiveInt(wordToComplete, mode, out var desiredDate, out int number))
        {
            var unambiguous = desiredDate.UnambiguousForm();
            var completion = desiredDate.NumericalForm();
            var tooltip = number == 0 ? "Today" : $"{-number} days ago";
            completionResult = CreateCompletion(completion, unambiguous, CompletionResultType.ParameterValue, tooltip, noWrap);
            return true;
        }
        return false;
    }
    /// <summary>
    /// Return null only if 
    /// </summary>
    /// <param name="wordToComplete"></param>
    /// <param name="baseDate"></param>
    /// <param name="noWrap"></param>
    /// <returns></returns>
    internal static LastLogDateCompletion? GetLastLogCompletion(string wordToComplete, DateTimeOffset baseDate, bool noWrap)
    {
        wordToComplete = wordToComplete.Trim();
        var parserMatch = LastLogParser().Match(wordToComplete);
        if (!parserMatch.Success)
        {
            return null;
        }

        int shift = 0;
        if (int.TryParse(parserMatch.Groups["shift"].Value, out int maybeShift))
        {
            shift = maybeShift;
        }
        try
        {

            var ret = new LastLogDateCompletion(shift, baseDate, wordToComplete.StartsWith(LastLogDateKeyword, StringComparison.OrdinalIgnoreCase));
            _ = ret.ParsedDate; // Get the parsed date to check if it is still in range
            return ret;
        }
        catch (Exception)
        {
            return null;
        }
    }
    private static bool Fuzzy(string input, string keyword)
    {
        return keyword.StartsWith(input, StringComparison.OrdinalIgnoreCase);
    }
    /// <summary>
    /// Generates completion results for JQL date keyword enums and the "today" keyword, matching the input string.
    /// </summary>
    public static IEnumerable<CompletionResult> GetEnumCompletions(string wordToComplete, DateMode mode, bool noWrap, bool useLastLogDate)
    {
        wordToComplete = (wordToComplete ?? "").Trim();
        bool canUseLastDate = useLastLogDate && LiraSession.LastAddedLogDate is not null;
        if (canUseLastDate
            && (Fuzzy(wordToComplete, LastLogDateKeyword) || wordToComplete.Contains(LastLogDateKeyword, StringComparison.OrdinalIgnoreCase)))
        {
            if (GetLastLogCompletion(wordToComplete, LiraSession.LastAddedLogDate!.Value, noWrap) is LastLogDateCompletion llc)
            {
                yield return llc.ToCompletionResult(noWrap);
                if (llc.Shift == 0)
                {
                    yield return (llc with { Shift = +1 }).ToCompletionResult(noWrap);
                    yield return (llc with { Shift = -1 }).ToCompletionResult(noWrap);
                }
            }
        }
        if (Fuzzy(wordToComplete, TodayKeyword))
        {
            (IJqlDate date, string tooltip) datetip = mode switch
            {
                DateMode.Current => (new JqlManualDate(DateTimeOffset.Now), "Current time"),
                DateMode.Start => (JqlKeywordDate.StartOfDay, "Start of today"),
                DateMode.End => (JqlKeywordDate.EndOfDay, "End of today"),
                _ => throw new PSNotImplementedException(),
            };

            yield return CreateCompletion(TodayKeyword,
                TodayKeyword,
                CompletionResultType.ParameterValue,
                datetip.tooltip, noWrap
                );
        }
        if (Fuzzy(wordToComplete, NowKeyword))
        {
            yield return CreateCompletion(NowKeyword,
                NowKeyword,
                CompletionResultType.ParameterValue,
                "Current time", noWrap
                );
        }
        foreach (var value in _periods)
        {
            string stringValue = value.ToString();
            if (stringValue.Contains(wordToComplete, StringComparison.OrdinalIgnoreCase))
            {
                // the enum value contains the word, yield it.
                // Empty word is considered contained in every value.
                string tooltip = Format($"Jira function returning: {GetSpecificDate(value).UnambiguousForm()}");
                yield return CreateCompletion(stringValue,
                                                  stringValue,
                                                  CompletionResultType.ParameterValue,
                                                  tooltip, noWrap);
            }
        }
    }
}
