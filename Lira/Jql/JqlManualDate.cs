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

    public DateTimeOffset Date { get; }

    public string GetJqlValue(TimeZoneInfo accountTimezone)
    {
        return ToAccountDatetime(accountTimezone).ToString(@"yyyy-MM-dd",formatProvider:null);
    }

    public DateTimeOffset ToAccountDatetime(TimeZoneInfo accountTimezone)
    {
        return TimeZoneInfo.ConvertTime(Date, accountTimezone);
    }

    public static implicit operator JqlManualDate(DateTimeOffset date) => new JqlManualDate(date:date);


}