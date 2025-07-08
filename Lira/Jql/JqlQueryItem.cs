namespace Lira.Jql;

/// <summary>
/// Implementation of <see cref="IJqlQueryItem"/> for the given type <typeparamref name="TObject"/>.
/// 
/// Represents a related set of conditions for one property of <typeparamref name="TObject"/> or one condition.
/// </summary>
/// <typeparam name="TObject">Type of object this query applies to.</typeparam>
/// <typeparam name="TObject">Type of the property of <typeparamref name="TObject"/> it applies to.</typeparam>
public abstract class JqlQueryItem<TObject,TProperty>(string fieldName) : IJqlQueryItem<TObject>
{
    /// <summary>
    /// Name of property of <typeparamref name="TObject"/> that the query applies to.
    /// </summary>
    public string FieldName { get; } = fieldName;

    public abstract string? GetJqlQuery(LiraClient client);
    public bool Filter(object? item, LiraClient client)
    {
        if (item?.GetType() == typeof(TObject))
        {
            return Filter((TObject)item, client);
        }
        return true;
    }
    /// <summary>
    /// Applies the filter to a concrete generic type <typeparamref name="TObject"/>
    /// </summary>
    /// <param name="item"></param>
    /// <param name="client"></param>
    /// <returns></returns>

    public abstract bool Filter(TObject? item, LiraClient client);
  
}
