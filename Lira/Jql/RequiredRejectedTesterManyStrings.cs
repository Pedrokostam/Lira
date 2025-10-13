using System;
using System.Collections.Generic;
using System.Linq;

namespace Lira.Jql;

public class RequiredRejectedTesterManyStrings<TObject>(string fieldName, Func<TObject, IEnumerable<string>> accessor) : RequiredRejectedTesterBase<TObject, IEnumerable<string>>(fieldName, accessor)
{
    protected override bool Test(IEnumerable<string> property, string item)
    {
        return property.Any(x=>x.Equals(item, StringComparison.OrdinalIgnoreCase));
    }
}