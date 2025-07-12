using System;
using System.Collections.Generic;

namespace Lira.Grouping;

public class DynamicGroupingCalculator<TObject, TCalculated> : GroupingCalculator<TObject, TCalculated>
{
    private Func<IEnumerable<TObject?>, TCalculated> CalculingFunc { get; }
    public DynamicGroupingCalculator(Func<IEnumerable<TObject?>,TCalculated> calculingFunc)
    {
        CalculingFunc = calculingFunc;
    }

    public override TCalculated Calculate(IEnumerable<TObject?> objects) => CalculingFunc(objects);
}
