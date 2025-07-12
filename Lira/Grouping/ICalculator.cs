using System.Collections.Generic;

namespace Lira.Grouping;

public interface ICalculator<in TObject, TCalculated>
{
    TCalculated Calculate(IEnumerable<TObject?> objects);
}
