namespace Lira.Jql;

public interface IBoundedJqlDate : IJqlDate
{
    JqlDateBoundary DateBoundary { get; }
}