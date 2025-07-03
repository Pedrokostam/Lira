using Lira.Jql;
namespace LiraPS.Completers;

internal interface ITooltipDate
{
    IJqlDate Date { get; }
    string Tooltip { get; }
    public string NumericalForm();
}
