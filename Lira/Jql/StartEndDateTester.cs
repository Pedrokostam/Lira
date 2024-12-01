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
public class StartEndDateTester<TObject> : JqlQueryItem<TObject,DateTimeOffset>
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
    public IJqlDate? StartDate { get; set; }
    public IJqlDate? EndDate { get; set; }

    public override string? GetJqlQuery(LiraClient client)
    {
        var start = StartDate switch
        {
            IJqlDate date => $"{FieldName} >= {date.GetJqlValue(client.AccountTimezone)}",
            _ => null,
        };
        var end = EndDate switch
        {
            IJqlDate date => $"{FieldName} <= {date.GetJqlValue(client.AccountTimezone)}",
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
