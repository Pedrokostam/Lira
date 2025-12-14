using System;

namespace Lira.Jql;

/// <summary>
/// Implementation of <see cref="IJqlDate"/> based actual <see cref="DateTimeOffset"/> values.
/// </summary>
public record JqlManualDate : IJqlDate
{
    public JqlManualDate(DateTimeOffset date)
    {
        Date = date;
    }

    public DateTimeOffset Date { get; init; }

    public string GetJqlValue(TimeZoneInfo accountTimezone)
    {
        return "\""+ToAccountDatetime(accountTimezone).ToString("yyyy-MM-dd HH:mm",formatProvider:System.Globalization.CultureInfo.InvariantCulture)+"\"";
    }

    public DateTimeOffset ToAccountDatetime(TimeZoneInfo accountTimezone)
    {
        return TimeZoneInfo.ConvertTime(Date, accountTimezone);
    }

    public static implicit operator JqlManualDate(DateTimeOffset date) => new(date);


}