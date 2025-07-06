using System;
using Lira.Jql;
using LiraPS.Arguments;
using LiraPS.Extensions;
namespace LiraPS.Completers;

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
