using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Lira.Grouping;

public sealed record CalculatedGroup<TObject, TCalculated> : IGrouping<string, TObject?>
{
    public ImmutableArray<TObject?> Items { get; }
    public string Header { get; }
    public ImmutableArray<string> Columns { get; }
    public ImmutableArray<object?> ColumnValues { get; }
    public ImmutableArray<string> ColumnValuesTexts { get; }
    public TCalculated CalculatedValue { get; }
    string IGrouping<string, TObject?>.Key => Header;

    public CalculatedGroup(string header, ImmutableArray<string> columns, ImmutableArray<object?> columnValues, ImmutableArray<string> columnTexts, TCalculated calculatedValue, ImmutableArray<TObject?> items)
    {
        Items = items;
        Header = header;
        Columns = columns;
        CalculatedValue = calculatedValue;
        ColumnValues = columnValues;
        ColumnValuesTexts = columnTexts;
    }

    public IEnumerator<TObject?> GetEnumerator()
    {
        foreach (var item in Items)
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
