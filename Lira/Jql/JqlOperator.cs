using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Lira.Jql;

[DebuggerDisplay("{Name}")]
public readonly record struct JqlOperator
{
    public string? Symbol { get; }
    public string Name { get; }
    public JqlOperators Operator { get; }
    public static bool TryGetFromString(string symbol, [NotNullWhen(true)] out JqlOperator? jqlOperator)
    {
        jqlOperator = symbol.ToUpperInvariant().AsSpan().Trim() switch
        {
            "=" => new(JqlOperators.Equals),
            "!=" => new(JqlOperators.NotEquals),
            "<" => new(JqlOperators.LessThan),
            "<=" => new(JqlOperators.LessThanEquals),
            ">" => new(JqlOperators.GreaterThan),
            ">=" => new(JqlOperators.GreaterThanEquals),
            "~" => new(JqlOperators.Contains),
            "!~" => new(JqlOperators.DoesNotContains),

            "EQUALS" => new(JqlOperators.Equals),
            "NOT EQUALS" => new(JqlOperators.NotEquals),
            "LESS THAN" => new(JqlOperators.LessThan),
            "LESS THAN OR EQUALS" => new(JqlOperators.LessThanEquals),
            "GREATER THAN" => new(JqlOperators.GreaterThan),
            "GREATER THAN OR EQUALS" => new(JqlOperators.GreaterThanEquals),
            "IN" => new(JqlOperators.In),
            "NOT IN" => new(JqlOperators.NotIn),
            "CONTAINS" => new(JqlOperators.Contains),
            "DOES NOT CONTAIN" => new(JqlOperators.DoesNotContains),
            "IS" => new(JqlOperators.Is),
            "IS NOT" => new(JqlOperators.IsNot),
            "WAS" => new(JqlOperators.Was),
            "WAS NOT" => new(JqlOperators.WasNot),
            "WAS IN" => new(JqlOperators.WasIn),
            "WAS NOT IN" => new(JqlOperators.WasNotIn),
            "CHANGED" => new(JqlOperators.Changed),

            _ => null
        };
        return jqlOperator is not null;
    }
    public JqlOperator(JqlOperators operatorName)
    {
        Operator = operatorName;
        Symbol = Operator switch
        {
            JqlOperators.Equals => "=",
            JqlOperators.NotEquals => "!=",
            JqlOperators.LessThan => "<",
            JqlOperators.LessThanEquals => "<=",
            JqlOperators.GreaterThan => ">",
            JqlOperators.GreaterThanEquals => ">=",
            JqlOperators.Contains => "~",
            JqlOperators.DoesNotContains => "!~",
            _ => null,
        };
        Name = Operator switch
        {
            JqlOperators.Equals => "Equals",
            JqlOperators.NotEquals => "Not equals",
            JqlOperators.LessThan => "Less than",
            JqlOperators.LessThanEquals => "Less than or equals",
            JqlOperators.GreaterThan => "Greater than",
            JqlOperators.GreaterThanEquals => "Greater than or equals",
            JqlOperators.In => "IN",
            JqlOperators.NotIn => "NOT IN",
            JqlOperators.Contains => "CONTAINS",
            JqlOperators.DoesNotContains => "DOES NOT CONTAIN",
            JqlOperators.Is => "IS",
            JqlOperators.IsNot => "IS NOT",
            JqlOperators.Was => "WAS",
            JqlOperators.WasNot => "WAS NOT",
            JqlOperators.WasIn => "WAS IN",
            JqlOperators.WasNotIn => "WAS NOT IN",
            JqlOperators.Changed => "CHANGED",
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
