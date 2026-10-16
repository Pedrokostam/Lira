using Lira.Jql;
namespace LiraPS.Arguments;

internal interface ITooltipDate
{
    IJqlDate Date { get; }
    string Tooltip { get; }
    public string NumericalForm();
}
