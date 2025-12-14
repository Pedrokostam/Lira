using System;
using System.Management.Automation;
using Lira.Extensions;
using Lira.Jql;
namespace LiraPS.Transformers;

public sealed class JqlDateTransformerAttribute(DateMode mode) : DateTransformer<IBoundedJqlDate>(mode)
{
    public static JqlDateTransformerAttribute Create(DateMode mode) => new(mode);
    protected override IBoundedJqlDate WrapUnwrap(object? dateObject, JqlDateBoundary? boundary)
    {
        var _boundary = boundary.GetValueOrDefault(JqlDateBoundary.Inclusive);
        return dateObject switch
        {
            IBoundedJqlDate boundedJql => boundedJql,
            IJqlDate ijql => new BoundedJqlDate(ijql, _boundary),
            DateTimeOffset dto => new JqlManualDate(dto).ToBounded(_boundary),
            DateTime dt => new JqlManualDate(dt).ToBounded(_boundary),
            _ => throw new ArgumentTransformationMetadataException(),
        };
    }
}
