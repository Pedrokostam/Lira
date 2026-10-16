namespace Lira.Grouping;

public sealed record NullProperty
{
    private NullProperty()
    {

    }
    public static NullProperty Get { get; } = new();
}
