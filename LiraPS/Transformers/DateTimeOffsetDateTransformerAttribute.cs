using System;
using System.Management.Automation;
using Lira.Jql;
namespace LiraPS.Transformers;

public sealed class DateTimeOffsetDateTransformerAttribute(DateMode mode) : DateTransformer<DateTimeOffset>(mode)
{
    public static DateTimeOffsetDateTransformerAttribute Create(DateMode mode) => new(mode);
    protected override DateTimeOffset WrapUnwrap(object? dateObject)
    {
        return dateObject switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => dt,
            IJqlDate ijql => ijql.ToAccountDatetime(TimeZoneInfo.Local),
            _ => throw new ArgumentTransformationMetadataException(),
        };
    }
}
