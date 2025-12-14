using System;
using System.Management.Automation;
using Lira.Jql;
namespace LiraPS.Transformers;

public sealed class DateTimeOffsetDateTransformerAttribute(DateMode mode, bool passScriptBlock=false) : DateTransformer<DateTimeOffset>(mode, passScriptBlock)
{
    public static DateTimeOffsetDateTransformerAttribute Create(DateMode mode, bool passScriptBlock = false) => new(mode, passScriptBlock) { CheckForBoundary=false };
    protected override DateTimeOffset WrapUnwrap(object? dateObject, JqlDateBoundary? boundary)
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
