using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lira.Jql;

[method: SetsRequiredMembers]
public readonly record struct BoundedJqlDate(IJqlDate Date, JqlDateBoundary DateBoundary = JqlDateBoundary.Inclusive) : IBoundedJqlDate
{
    public string GetJqlValue(TimeZoneInfo accountTimezone)
    {
        return Date.GetJqlValue(accountTimezone);
    }

    public DateTimeOffset ToAccountDatetime(TimeZoneInfo accountTimezone)
    {
        return Date.ToAccountDatetime(accountTimezone);
    }

    public static explicit operator BoundedJqlDate (JqlKeywordDate date) => new(date);
    public static explicit operator BoundedJqlDate(JqlManualDate date) => new(date);
}
