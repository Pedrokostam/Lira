using System;
using System.Collections.Generic;

namespace Lira.Grouping;

public class DynamicGrouper<TObject, TProperty> : Grouper<TObject?, TProperty?>
{
    public DynamicGrouper(string name, Func<TObject?, TProperty> accessor, Func<TObject?, string>? displayGetter=null, IComparer<TProperty>? propertyGrouper=null)
    {
        Name = name;
        Accessor = accessor;
        DisplayGetter = displayGetter ?? (x=>Accessor(x)?.ToString() ?? "");
        PropertyComparer = propertyGrouper ?? Comparer<TProperty>.Default;
    }

    public Func<TObject?, TProperty> Accessor { get; }
    public Func<TObject?, string> DisplayGetter { get; }
    public IComparer<TProperty> PropertyComparer { get; }

    public override string Name { get; }
    public override TProperty? GetGenericPropertyValue(TObject? obj)=>Accessor(obj);
    public override int CompareProperties(TProperty? x, TProperty? y)=>PropertyComparer.Compare(x, y); 
    public override string GetDisplay(TObject? obj)=>DisplayGetter(obj);
}
