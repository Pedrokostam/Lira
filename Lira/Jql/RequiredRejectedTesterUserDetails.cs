using System;
using Lira.Objects;

namespace Lira.Jql;

public class RequiredRejectedTesterUserDetails<TObject>(string fieldName, Func<TObject, UserDetails?> accessor) : RequiredRejectedTesterBase<TObject, UserDetails?>(fieldName, accessor)
{
    protected override bool Test(UserDetails? property, string item)
    {
       return property?.NameMatches(item) ?? false;
    }
}
