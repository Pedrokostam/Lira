using System;
using System.Management.Automation;
using Lira.Jql;
namespace LiraPS.Transformers;

public sealed class JqlDateTransformerAttribute(DateMode mode) : DateTransformer<IJqlDate>(mode)
{
    public static JqlDateTransformerAttribute Create(DateMode mode) => new (mode);
    protected override IJqlDate WrapUnwrap(object? dateObject)
    {
        return dateObject switch
        {
            IJqlDate ijql => ijql,
            DateTimeOffset dto => new JqlManualDate(dto),
            DateTime dt => new JqlManualDate(dt),
            _ => throw new ArgumentTransformationMetadataException(),
        };
    }
}
