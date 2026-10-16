using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Lira.Jql;

[DebuggerDisplay("{Name}")]
public readonly record struct JqlOperator
{
    public enum Operators
    {
        Equals,
        NotEquals,
        LessThan, LessThanEquals,
        GreaterThan, GreaterThanEquals,
        In,
        NotIn,
        Contains,
        DoesNotContain,
        Is,
        IsNot,
        Was,
        WasNot,
        WasIn,
        WasNotIn,
        Changed,
    }
    public string? Symbol { get; }
    public string Name { get; }
    public Operators Operator { get; }
    public static bool TryGetFromString(string symbol, [NotNullWhen(true)] out JqlOperator? jqlOperator)
    {
        jqlOperator = symbol.ToUpperInvariant().AsSpan().Trim() switch
        {
            "=" => new(Operators.Equals),
            "!=" => new(Operators.NotEquals),
            "<" => new(Operators.LessThan),
            "<=" => new(Operators.LessThanEquals),
            ">" => new(Operators.GreaterThan),
            ">=" => new(Operators.GreaterThanEquals),
            "~" => new(Operators.Contains),
            "!~" => new(Operators.DoesNotContain),

            "EQUALS" => new(Operators.Equals),
            "NOT EQUALS" => new(Operators.NotEquals),
            "LESS THAN" => new(Operators.LessThan),
            "LESS THAN OR EQUALS" => new(Operators.LessThanEquals),
            "GREATER THAN" => new(Operators.GreaterThan),
            "GREATER THAN OR EQUALS" => new(Operators.GreaterThanEquals),
            "IN" => new(Operators.In),
            "NOT IN" => new(Operators.NotIn),
            "CONTAINS" => new(Operators.Contains),
            "DOES NOT CONTAIN" => new(Operators.DoesNotContain),
            "IS" => new(Operators.Is),
            "IS NOT" => new(Operators.IsNot),
            "WAS" => new(Operators.Was),
            "WAS NOT" => new(Operators.WasNot),
            "WAS IN" => new(Operators.WasIn),
            "WAS NOT IN" => new(Operators.WasNotIn),
            "CHANGED" => new(Operators.Changed),

            _ => null,
        };
        return jqlOperator is not null;
    }
    public JqlOperator(Operators operatorName)
    {
        Operator = operatorName;
        Symbol = Operator switch
        {
            Operators.Equals => "=",
            Operators.NotEquals => "!=",
            Operators.LessThan => "<",
            Operators.LessThanEquals => "<=",
            Operators.GreaterThan => ">",
            Operators.GreaterThanEquals => ">=",
            Operators.Contains => "~",
            Operators.DoesNotContain => "!~",
            _ => null,
        };
        Name = Operator switch
        {
            Operators.Equals => "Equals",
            Operators.NotEquals => "Not equals",
            Operators.LessThan => "Less than",
            Operators.LessThanEquals => "Less than or equals",
            Operators.GreaterThan => "Greater than",
            Operators.GreaterThanEquals => "Greater than or equals",
            Operators.In => "IN",
            Operators.NotIn => "NOT IN",
            Operators.Contains => "CONTAINS",
            Operators.DoesNotContain => "DOES NOT CONTAIN",
            Operators.Is => "IS",
            Operators.IsNot => "IS NOT",
            Operators.Was => "WAS",
            Operators.WasNot => "WAS NOT",
            Operators.WasIn => "WAS IN",
            Operators.WasNotIn => "WAS NOT IN",
            Operators.Changed => "CHANGED",
            _ => throw new NotSupportedException(),
        };
    }
    public bool Equals(JqlOperator other)
    {
        return Operator == other.Operator;
    }
    public override int GetHashCode()
    {
        return Operator.GetHashCode();
    }
    public override string ToString() => QueryForm;
    public string QueryForm => Symbol ?? Name;
}
