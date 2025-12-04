using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lira.Jql;

public abstract class RequiredRejectedTesterBase<TObject, TProperty> : JqlQueryItem<TObject, TProperty>, IEquatable<RequiredRejectedTesterBase<TObject, TProperty>>
{
    private readonly Func<TObject, TProperty> _accessor;

    protected RequiredRejectedTesterBase(string fieldName, Func<TObject, TProperty> accessor) : base(fieldName)
    {
        _accessor = accessor;
    }
    protected abstract bool Test(TProperty property, string item);
    public override bool Filter(TObject? item, LiraClient client)
    {
        if (item == null)
        {
            return false;
        }
        var property = _accessor(item);
        bool isInGood = false;
        bool isInBad = false;
        foreach (var requested in Good)
        {
            isInGood |= Test(property, requested);
            if (isInGood)
            {
                break;
            }
        }
        foreach (var requested in Bad)
        {
            isInBad |= Test(property, requested);
            if (isInBad)
            {
                break;
            }
        }
        return isInGood && !isInBad;
    }
    public IList<string> Good { get; set; } = [];
    public IList<string> Bad { get; set; } = [];

    private static string CreateArray(ICollection<string> values)
    {
        return "(" + string.Join(", ", values.Select(x => $"\"{x}\"")) + ")";
    }
    public override string? GetJqlQuery(LiraClient client)
    {
        var requested = Good switch
        {
            [] => null,
            [string user] => $"{FieldName} = \"{user}\"",
            [..] => $"{FieldName} IN {CreateArray(Good)}",
        };
        var denied = Bad switch
        {
            [] => null,
            [string user] => $"{FieldName} != \"{user}\"",
            [..] => $"{FieldName} NOT IN {CreateArray(Bad)}",
        };
        return (requested, denied) switch
        {
            (null, null) => null,
            (not null, not null) => $"{requested} AND {denied}",
            (not null, null) => requested,
            (null, not null) => denied,
        };
    }

    public bool Equals(RequiredRejectedTesterBase<TObject, TProperty>? other)
    {
        if (other is null)
        {
            return false;
        }
        return FieldName.Equals(other.FieldName, StringComparison.OrdinalIgnoreCase)
            && Bad.SequenceEqual(other.Bad, StringComparer.OrdinalIgnoreCase)
            && Good.SequenceEqual(other.Good, StringComparer.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)=>Equals(obj as RequiredRejectedTesterBase<TObject, TProperty>);

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
