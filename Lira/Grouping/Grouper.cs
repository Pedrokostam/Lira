namespace Lira.Grouping;

public abstract class Grouper<TObject, TProperty> : IGrouper<TObject?, TProperty?>
{
    public abstract string Name { get; }

    public abstract TProperty? GetGenericPropertyValue(TObject? obj);
    public abstract int CompareProperties(TProperty? x, TProperty? y);
    public virtual int Compare(TObject? x, TObject? y)
    {
        return CompareProperties(GetGenericPropertyValue(x), GetGenericPropertyValue(y));
    }
    public abstract string GetDisplay(TObject? obj);

    object? IGrouper<TObject?>.GetPropertyValue(TObject? obj) => GetGenericPropertyValue(obj);
}
