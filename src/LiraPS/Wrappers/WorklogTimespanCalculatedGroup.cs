using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Management.Automation;
using Lira.Grouping;
using Lira.Objects;
using LiraPS.Arguments;

namespace LiraPS.Wrappers;

// Couldn't really find a simple way to specify formatting for a generic type (I'd rather avoid specifying assembly version, tyvm)
// Also, since the group implements GetEnumerator, Powershell would rather display the inner collection than the object...
// So, here is a simple wrapper, which exposes all properties of the Group and does NOT implement IEnumerable (nor IGrouping)
public sealed record WorklogTimespanCalculatedGroup
{
    public CalculatedGroup<Worklog?, TimeSpan> Base { get; }
    public ImmutableArray<Worklog?> Items => Base.Items;
    public string Header => Base.Header;
    public ImmutableArray<string> Columns => Base.Columns;
    public ImmutableArray<object?> ColumnValues => Base.ColumnValues;
    public ImmutableArray<string> ColumnValuesTexts => Base.ColumnValuesTexts;
    public TimeSpan CalculatedValue => Base.CalculatedValue;
    public WorklogTimespanCalculatedGroup(CalculatedGroup<Worklog?, TimeSpan> donor)
    {
        Base = donor;
    }
    public static WorklogTimespanCalculatedGroup Wrap(CalculatedGroup<Worklog?, TimeSpan> donor) => new(donor);

}