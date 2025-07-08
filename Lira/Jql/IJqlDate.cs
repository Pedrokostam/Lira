using System;

namespace Lira.Jql;

/// <summary>
/// Interface for objects that can represent date in a JQL query
/// </summary>
public interface IJqlDate
{
    /// <summary>
    /// Gets the text representation of the date, understood by the JQL parser.
    /// </summary>
    /// <param name="accountTimezone">Timezone associated with the currently logged in account. JQL assumes all date are in account's timezone.</param>
    /// <returns>Text representaiton of the date</returns>
    string GetJqlValue(TimeZoneInfo accountTimezone);
    /// <summary>
    /// Gets the <see cref="DateTimeOffset"/> representation of this date.
    /// </summary>
    /// <param name="accountTimezone">Timezone associated with the currently logged in account. JQL assumes all date are in account's timezone.</param>
    /// <returns>DateTime equivalent expressed in the given time zone</returns>
    DateTimeOffset ToAccountDatetime(TimeZoneInfo accountTimezone);
}
