namespace Lira.Grouping;

public sealed class NullGrouper<TObject> : Grouper<TObject?, NullProperty>
{
    public override string Name { get; } = "None";

    public override int CompareProperties(NullProperty? x, NullProperty? y) => 0;

    public override string GetDisplay(TObject? obj) => "All";

    public override NullProperty? GetGenericPropertyValue(TObject? obj) => NullProperty.Get;
    public override int Compare(TObject? x, TObject? y) => 0;
}