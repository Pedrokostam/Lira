using System;
using System.Collections.Generic;
using System.Linq;
using Lira.Objects;

namespace Lira.Grouping;

public class WorklogGroupingTimeSummator : GroupingCalculator<Worklog, TimeSpan>
{
    public override TimeSpan Calculate(IEnumerable<Worklog?> objects)
    {
        var sumTicks = objects.Sum(x => x?.TimeSpent.Ticks ?? 0);
        return new TimeSpan(sumTicks);
    }
}
