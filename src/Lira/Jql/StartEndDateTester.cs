using System;

namespace Lira.Jql;

/// <summary>
/// Represents up to 2 conditions for a given <see cref="DateTime"/> or <see cref="DateTimeOffset"/> property.
/// <para/>
/// The property has to be later than <see cref="StartDate"/> (automatically passed if <see cref="StartDate"/> is <see langword="null"/>.
/// <para/>
/// The property has to be before than <see cref="EndDate"/> (automatically passed if <see cref="EndDate"/> is <see langword="null"/>.
/// </summary>
/// <typeparam name="TObject"></typeparam>
public class StartEndDateTester<TObject> : JqlQueryItem<TObject, DateTimeOffset>
{
    private readonly Func<TObject, DateTimeOffset> _accessor;

    public StartEndDateTester(string fieldName, Func<TObject, DateTimeOffset> accessor) : base(fieldName)
    {
        _accessor = accessor;
    }
    public override bool Filter(TObject? item, LiraClient client)
    {
        if (item == null)
        {
            return false;
        }
        var value = _accessor(item);
        var isAfterStart = StartDate is null || value >= StartDate.ToAccountDatetime(client.AccountTimezone);
        var isBeforeEnd = EndDate is null || value <= EndDate.ToAccountDatetime(client.AccountTimezone);
        return isAfterStart && isBeforeEnd;

    }
    /// <summary>
    /// Gets or sets the start date used to filter results. Respect date boundary if <see cref="IBoundedJqlDate"/> is used.
    /// </summary>
    public IJqlDate? StartDate { get; set; }
    /// <summary>
    /// Gets or sets the end date used to filter results. Respect date boundary if <see cref="IBoundedJqlDate"/> is used.
    /// </summary>
    public IJqlDate? EndDate { get; set; }
    private static string DateBoundaryString(JqlDateBoundary dateBoundary, bool isStart)
    {
        return dateBoundary switch
        {
            JqlDateBoundary.Inclusive => isStart ? ">=" : "<=",
            JqlDateBoundary.Exclusive => isStart ? ">" : "<",
            JqlDateBoundary.Exact => "=",
            _ => throw new ArgumentOutOfRangeException(nameof(dateBoundary), dateBoundary, null)
        };
    }
    private static string DateBoundedString(IJqlDate date, LiraClient client, bool isStart) { 
    var boundary = date is IBoundedJqlDate boundedDate ? boundedDate.DateBoundary : JqlDateBoundary.Inclusive;
        return $"{DateBoundaryString(boundary, isStart)} {date.GetJqlValue(client.AccountTimezone)}";
    }
    public override string? GetJqlQuery(LiraClient client)
    {
        var start = StartDate switch
        {
            IJqlDate date => $"{FieldName} {DateBoundedString(date,client,isStart:true)}",
            _ => null,
        };
        var end = EndDate switch
        {
            IJqlDate date => $"{FieldName} {DateBoundedString(date, client, isStart: false)}",
            _ => null,
        };
        return (start, end) switch
        {
            (null, null) => null,
            (not null, not null) => $"{start} AND {end}",
            (not null, null) => start,
            (null, not null) => end,
        };
    }
}
