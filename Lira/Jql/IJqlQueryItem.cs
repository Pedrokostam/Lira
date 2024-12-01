namespace Lira.Jql;

/// <summary>
/// Represents an object that can create a JQL query from multiple conditions and can filter out relevant objects based on those conditions.
/// </summary>
public interface IJqlQueryItem
{
    /// <summary>
    /// Determines whether the input object passes or not.
    /// This method should only filter out objects of matching type - types that are not relevant to its conditions should be let through.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="client"></param>
    /// <returns></returns>
    bool Filter(object? item, LiraClient client);
    /// <summary>
    /// Create the string representation of the query.
    /// </summary>
    /// <param name="client"></param>
    /// <returns></returns>
    string? GetJqlQuery(LiraClient client);
}
public interface IJqlQueryItem<TObject> : IJqlQueryItem
{
    bool Filter(TObject? item, LiraClient client);
}
