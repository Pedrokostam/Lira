using System;
using Lira.Jql;
using LiraPS.Extensions;
namespace LiraPS.Arguments;

internal readonly record struct TooltipKeywordDate : ITooltipDate, IEquatable<ITooltipDate>
{
    public TooltipKeywordDate(IJqlDate date, string tooltip)
    {
        Date = date;
        Tooltip = tooltip;
    }
    public TooltipKeywordDate(JqlKeywordDate.Keywords keyword, string tooltip) : this(new JqlKeywordDate(keyword), tooltip)
    {

    }
    public bool Equals(TooltipKeywordDate other) => Date.Equals(other.Date);
    public bool Equals(ITooltipDate? other) => Date.Equals(other?.Date);
    public override int GetHashCode() => Date.GetHashCode();
    public IJqlDate Date { get; init; }
    public string Tooltip { get; init; }
    public string NumericalForm() => Date.ToAccountDatetime(TimeZoneInfo.Local).NumericalForm();

    public static implicit operator TooltipKeywordDate((JqlKeywordDate.Keywords keyword, string tooltip) tuple)
        => new(tuple.keyword, tuple.tooltip);
}
