using System;
using System.Linq;

namespace Lira.Jql;

public abstract class RequiredRejectedTesterBase<TObject,TProperty> : JqlQueryItem<TObject,TProperty>
{
    private readonly Func<TObject, TProperty> _accessor;

    protected RequiredRejectedTesterBase(string fieldName, Func<TObject, TProperty> accessor) : base(fieldName)
    {
        _accessor = accessor;
    }
    protected abstract bool Test(TProperty property, string item);
    public override bool Filter(TObject? item, LiraClient client)
    {
        if(item == null)
        {
            return false;
        }
        var property = _accessor(item);
        bool isInGood = false;
        bool isInBad = false;
        foreach (var requested in Good)
        {
            isInGood |= Test(property,requested);
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
    public string[] Good { get; set; } = [];
    public string[] Bad { get; set; } = [];

    private static string CreateArray(string[] values)
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
}
