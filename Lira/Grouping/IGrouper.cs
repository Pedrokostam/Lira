using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Lira.Grouping;

public interface IGrouper<in TObject> : IComparer<TObject>
{
    /// <summary>
    /// Descriptive name for the Grouper, e.g. the name of the property.
    /// </summary>
    string Name { get; }
    object? GetPropertyValue(TObject? obj);
    /// <summary>
    /// Provides the string 
    /// </summary>
    string GetDisplay(TObject? obj);
}
public interface IGrouper<in TObject, TProperty> : IGrouper<TObject>
{
    /// <summary>
    /// Accesing function which extracts the value from the Worklog?, which will be used for grouping
    /// </summary>
    TProperty? GetGenericPropertyValue(TObject? obj);
    /// <summary>
    /// Used to group <typeparamref name="TProperty"/> based on the extracted value.
    /// <para/>
    /// Should be part of <see cref="Grouper"/>.
    /// </summary>
    int CompareProperties(TProperty? x, TProperty? y);
}
