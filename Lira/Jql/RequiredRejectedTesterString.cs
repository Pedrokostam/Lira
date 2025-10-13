using System;

namespace Lira.Jql;

public class RequiredRejectedTesterString<TObject>(string fieldName, Func<TObject, string?> accessor) : RequiredRejectedTesterBase<TObject, string?>(fieldName, accessor)
{
    protected override bool Test(string? property, string item)
    {
        return string.Equals(property, item, StringComparison.OrdinalIgnoreCase);
    }
}
