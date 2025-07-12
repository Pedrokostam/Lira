using System;
using Lira.Objects;

namespace Lira.Grouping;

public abstract class WorklogDateGrouperBase<TProperty>(DateSelector date) : Grouper<Worklog?, TProperty?>
{
    public DateSelector DateType { get; } = date;
    protected DateTimeOffset GetDate(Worklog? obj)
    {
        return DateType switch
        {
            DateSelector.Started => obj?.Started ?? default,
            DateSelector.Created => obj?.Created ?? default,
            DateSelector.Updated => obj?.Updated ?? default,
            _ => obj?.Started ?? default
        };
    }
}
