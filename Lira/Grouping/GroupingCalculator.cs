using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Lira.Extensions;

namespace Lira.Grouping;
public abstract class GroupingCalculator<TObject, TCalculated>
    : ICalculator<TObject, TCalculated>
    , IComparer<TObject?>
    , IEnumerable<IGrouper<TObject>>
{
    public virtual string PropertySeparator { get; set; } = " > ";
    public IList<IGrouper<TObject>> Groupers { get; } = [];
    public void Add(IGrouper<TObject> grouper)
    {
        Remove(grouper);
        Groupers.Add(grouper);
    }
    public void Remove(IGrouper<TObject> grouper)
    {
        Groupers.Remove(grouper);
    }
    public void Clear() => Groupers.Clear();
    public IEnumerable<CalculatedGroup<TObject?, TCalculated>> Group(IEnumerable<TObject?> enumerable)
    {
        var instanceGroupers = Groupers.Where(x => x is not NullGrouper<TObject?>).ToList();
        if (instanceGroupers.Count == 0)
        {
            Groupers.Add(new NullGrouper<TObject>());
        }
        var columns = instanceGroupers.Select(x => x.Name).ToImmutableArray(instanceGroupers.Count);
        var ordered = enumerable.Order(this);
        List<TObject?> currentBatch = [];
        foreach (var ord in ordered)
        {
            if (currentBatch.Count == 0 || Compare(currentBatch[^1], ord) == 0)
            {
                currentBatch.Add(ord);
                continue;
            }
            var last = currentBatch[^1];
            var grupa = Create(currentBatch, columns, instanceGroupers);
            currentBatch = [ord];
            yield return grupa;
        }
        if (currentBatch.Count > 0)
        {
            yield return Create(currentBatch, columns, instanceGroupers);
        }
    }
    private CalculatedGroup<TObject?, TCalculated> Create(IList<TObject?> batch, ImmutableArray<string> columns, IList<IGrouper<TObject>> instanceGroupers)
    {
        if (batch is null || batch.Count == 0)
        {
            throw new ArgumentException("Batch contains no elements or is null", nameof(batch));
        }
        var last = batch[^1];
        var propStrings = instanceGroupers.Select(x => x.GetDisplay(last)).ToImmutableArray(instanceGroupers.Count);
        var propValues = instanceGroupers.Select(x => x.GetPropertyValue(last)).ToImmutableArray(instanceGroupers.Count);
        return new CalculatedGroup<TObject?, TCalculated>(
                GetGroupHeader(instanceGroupers, PropertySeparator, last),
                columns,
                propValues,
                propStrings,
                Calculate(batch),
                [.. batch]);
    }

    public static string GetGroupHeader(IList<IGrouper<TObject>> groupers, string separator, TObject? o)
    {
        return string.Join(separator, groupers.Select(x => x.GetDisplay(o)));
    }

    public int Compare(TObject? x, TObject? y)
    {
        foreach (var grouper in Groupers)
        {
            var comparison = grouper.Compare(x, y);
            if (comparison != 0)
            {
                return comparison;
            }
        }
        return 0;
    }

    public IEnumerator<IGrouper<TObject>> GetEnumerator() => Groupers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public abstract TCalculated Calculate(IEnumerable<TObject?> objects);
}
