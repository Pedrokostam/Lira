using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;
using Lira.Jql;
using Lira.Extensions;
using static LiraPS.StringFormatter;
using System.Diagnostics.CodeAnalysis;
using LiraPS.Extensions;
namespace LiraPS;

internal static partial class DateCompletionHelper
{
    private const int Timeout = 250;
    private const string Pattern = @"^((?<year2>\d{2})(?=[-\/\\]|$)|(?<year4>\d{3,4}))(([-\/\\])?(?<month>1[0-2]|0?[1-9])(([-\/\\])?(?<day>3[01]|[12][0-9]|0?[1-9]))?)?";
#if !NET8_0

    private static readonly Regex _DateStringChecker = new Regex(
        Pattern,
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(Timeout));
    public static Regex DateStringChecker() => _DateStringChecker;
#else
    [GeneratedRegex(Pattern, RegexOptions.ExplicitCapture, Timeout)]
    public static partial Regex DateStringChecker();
#endif
    private static DateTimeOffset GetSpecificDate(JqlKeywordDate.JqlDateKeywords keyword)
    {
        return new JqlKeywordDate(keyword).ToAccountDatetime(TimeZoneInfo.Local);
    }
    private static void AddDateFromStartAndUnit(List<ITooltipDate> dates, DateTimeOffset baseDate, bool isStart, TimeUnit unit)
    {
        string baseString = baseDate.UnambiguousForm();
        int weekNumber = GetWeekNumber(baseDate);
        (JqlKeywordDate.JqlDateKeywords keyword, string tooltip) = (isStart, unit) switch
        {
            (true, TimeUnit.Day) => (JqlKeywordDate.JqlDateKeywords.StartOfDay, Format($"Start of day {baseString}")),
            (false, TimeUnit.Day) => (JqlKeywordDate.JqlDateKeywords.EndOfDay, Format($"End of day {baseString}")),
            (true, TimeUnit.Week) => (JqlKeywordDate.JqlDateKeywords.StartOfWeek, Format($"First day of week number {weekNumber}")),
            (false, TimeUnit.Week) => (JqlKeywordDate.JqlDateKeywords.EndOfWeek, Format($"Last day of week number {weekNumber}")),
            (true, TimeUnit.Month) => (JqlKeywordDate.JqlDateKeywords.StartOfMonth, Format($"First day of month {baseDate:MMMM}/{baseDate:yyyy}")),
            (false, TimeUnit.Month) => (JqlKeywordDate.JqlDateKeywords.EndOfMonth, Format($"Last day of month {baseDate:MMMM}/{baseDate:yyyy}")),
            (true, TimeUnit.Year) => (JqlKeywordDate.JqlDateKeywords.StartOfYear, Format($"First day of year {baseDate:Year}")),
            (false, TimeUnit.Year) => (JqlKeywordDate.JqlDateKeywords.EndOfYear, Format($"Last day of year {baseDate:Year}")),
            _ => throw new NotSupportedException(),
        };
        dates.Add(new TooltipManualDate(keyword.ToDateTimeOffset(baseDate), tooltip));
    }

    private static int GetWeekNumber(DateTimeOffset baseDate)
    {
        return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(baseDate.Date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);
    }

    // TODO tests

    private static string GetYearDifferenceString(int year, ref DateTimeOffset now)
    {
        var yearDiff = now.Year - year;
        return yearDiff switch
        {
            0 => Format($"this year"),
            > 0 => Format($"{yearDiff} years ago"),
            < 0 => Format($"{yearDiff} years in the future")
        };
    }

    private static void TryAddDateVariant(List<ITooltipDate> variants, int year, int month, int day, string tooltip)
    {
        try
        {
            variants.Add(new TooltipManualDate(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local), tooltip));
        }
        catch (ArgumentOutOfRangeException)
        {
            // if it is impossible to add (e.g. day 30 of february
            // dont do anything
        }
    }

    private static int SetNumber(Match match, string name, ref int value)
    {
        if (GetGroup(match, name) is string numberStr)
        {
            value = int.Parse(numberStr);
            return numberStr.Length;
        }
        return 0;
    }

    private static string? GetGroup(Match match, string group)
    {
        var s = match.Groups[group]?.Value;
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static readonly JqlKeywordDate.JqlDateKeywords[] _periods = Enum.GetValues(typeof(JqlKeywordDate.JqlDateKeywords)).Cast<JqlKeywordDate.JqlDateKeywords>().ToArray();
    public static IEnumerable<CompletionResult> MatchDate(string wordToComplete, bool start)
    {
        wordToComplete.Trim();
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
        List<ITooltipDate> variants = [];
        if (year > 0)
        {
            var yearDate = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Local);
            if (month > 0)
            {
                var monthDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Local);
                if (day > 0)
                {
                    variants.Add(new TooltipManualDate(new DateTime(year, month, day), "Today's date"));
                }
                else
                {
                    AddDateFromStartAndUnit(variants, monthDate, start, TimeUnit.Month);
                    TryAddDateVariant(variants, year, month, now.Day, "Exact parsed day");
                }
            }
            else
            {
                AddDateFromStartAndUnit(variants, yearDate, start, TimeUnit.Year);
            }
            string yearDiff = GetYearDifferenceString(year, ref now);
            TryAddDateVariant(variants, year, now.Month, 1, Format($"First day of {now:MMMM}, {yearDiff}"));
            TryAddDateVariant(variants, year, now.Month, now.Day, "Today's date, " + yearDiff);
        }
        else
        {
            variants.Add(new TooltipManualDate(now, "Today's date"));
            AddDateFromStartAndUnit(variants, now, start, TimeUnit.Day);
            AddDateFromStartAndUnit(variants, now, start, TimeUnit.Month);
            AddDateFromStartAndUnit(variants, now, start, TimeUnit.Year);
        }
        var uniques = variants.Distinct();
        foreach (var item in uniques)
        {
            var str = item.NumericalForm();
            yield return new CompletionResult(str, str, CompletionResultType.ParameterValue, item.Tooltip);
        }
    }
    public static bool GetDateFromNonPositiveInt(string s, out DateTimeOffset date, out int number)
    {
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out  number) && number <= 0)
        {
            var today = DateTimeOffset.Now;
            date = today.AddDays(number);
            return true;
        }
        number = default;
        date = default;
        return false;
    }
    public static bool GetIntCompletions(string wordToComplete, [NotNullWhen(true)] out CompletionResult? completionResult)
    {
        completionResult = null;
        if (GetDateFromNonPositiveInt(wordToComplete,out var desiredDate, out int number))
        {
            var unambiguous = desiredDate.UnambiguousForm();
            var completion = desiredDate.NumericalForm();
            var tooltip = number == 0 ? "Today" : $"{-number} days ago";
            completionResult = new CompletionResult(completion, unambiguous, CompletionResultType.ParameterValue, tooltip);
            return true;
        }
        return false;
    }
    public static IEnumerable<CompletionResult> GetEnumCompletions(string wordToComplete)
    {
        wordToComplete = (wordToComplete ?? "").Trim();
        foreach (var value in _periods)
        {
            string stringValue = value.ToString();
            if (stringValue.Contains(wordToComplete, StringComparison.OrdinalIgnoreCase))
            {
                // the enum value contains the word, yield it.
                // Empty word is considered contained in every value.
                string tooltip = Format($"Jira function returning: {GetSpecificDate(value).UnambiguousForm()}");
                yield return new CompletionResult(stringValue,
                                                  stringValue,
                                                  CompletionResultType.ParameterValue,
                                                  tooltip);
            }
        }
    }
}
internal class DateArgumentCompletionAttribute : IArgumentCompleter
{
    public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
    {
        throw new NotImplementedException();
    }
}
internal interface ITooltipDate
{
    IJqlDate Date { get; }
    string Tooltip { get; }
    public string NumericalForm();
}
internal readonly record struct TooltipManualDate : ITooltipDate, IEquatable<ITooltipDate>
{
    public TooltipManualDate(IJqlDate date, string tooltip)
    {
        Date = date;
        Tooltip = tooltip;
    }
    public TooltipManualDate(DateTimeOffset date, string tooltip) : this(new JqlManualDate(date), tooltip)
    {

    }
    public bool Equals(TooltipManualDate other) => Date.Equals(other.Date);
    public bool Equals(ITooltipDate? other) => Date.Equals(other?.Date);
    public override int GetHashCode() => Date.GetHashCode();
    public IJqlDate Date { get; init; }
    public string Tooltip { get; init; }
    public string NumericalForm() => Date.ToAccountDatetime(TimeZoneInfo.Local).NumericalForm();


    public static implicit operator TooltipManualDate((DateTimeOffset date, string tooltip) tuple)
        => new(tuple.date, tuple.tooltip);
}
internal readonly record struct TooltipKeywordDate : ITooltipDate, IEquatable<ITooltipDate>
{
    public TooltipKeywordDate(IJqlDate date, string tooltip)
    {
        Date = date;
        Tooltip = tooltip;
    }
    public TooltipKeywordDate(JqlKeywordDate.JqlDateKeywords keyword, string tooltip) : this(new JqlKeywordDate(keyword), tooltip)
    {

    }
    public bool Equals(TooltipKeywordDate other) => Date.Equals(other.Date);
    public bool Equals(ITooltipDate? other) => Date.Equals(other?.Date);
    public override int GetHashCode() => Date.GetHashCode();
    public IJqlDate Date { get; init; }
    public string Tooltip { get; init; }
    public string NumericalForm() => Date.ToAccountDatetime(TimeZoneInfo.Local).NumericalForm();

    public static implicit operator TooltipKeywordDate((JqlKeywordDate.JqlDateKeywords keyword, string tooltip) tuple)
        => new(tuple.keyword, tuple.tooltip);
}
internal enum TimeUnit
{
    Day, Week, Month, Year
}
internal class JqlDateArgumentCompletionAttribute : IArgumentCompleter
{
    public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
    {
        wordToComplete = (wordToComplete ?? "").Trim();
        if (wordToComplete.Length == 0 || char.IsLetter(wordToComplete[0]))
        {
            foreach (var item in DateCompletionHelper.GetEnumCompletions(wordToComplete))
            {
                yield return item;
            }
        }
        if (DateCompletionHelper.GetIntCompletions(wordToComplete, out var intCompletion))
        {
            yield return intCompletion;
        }
        bool isStartDate = parameterName.Contains("start", StringComparison.OrdinalIgnoreCase);
        if (wordToComplete.Length == 0 || char.IsDigit(wordToComplete[0]))
        {
            var completions = DateCompletionHelper.MatchDate(wordToComplete, isStartDate);
            foreach (var item in completions)
            {
                yield return item;
            }
        }
    }

}
