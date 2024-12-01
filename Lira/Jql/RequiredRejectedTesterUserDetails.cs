using System;
using Lira.Objects;

namespace Lira.Jql;

public class RequiredRejectedTesterUserDetails<TObject> : RequiredRejectedTesterBase<TObject, UserDetails>
{
    public RequiredRejectedTesterUserDetails(string fieldName, Func<TObject, UserDetails> accessor) : base(fieldName, accessor)
    {
    }

    protected override bool Test(UserDetails property, string item)
    {
       return property.NameMatches(item);
    }
}

public class RequiredRejectedTesterString<TObject> : RequiredRejectedTesterBase<TObject, string>
{
    public RequiredRejectedTesterString(string fieldName, Func<TObject, string> accessor) : base(fieldName, accessor)
    {
    }

    protected override bool Test(string property, string item)
    {
        return property.Equals(item,StringComparison.OrdinalIgnoreCase);
    }
}
