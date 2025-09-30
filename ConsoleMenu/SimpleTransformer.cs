using System.Diagnostics.CodeAnalysis;

namespace ConsoleMenu;

public class SimpleTransformer<T> : ITransformer<T>
{
    private readonly Func<string, T> _transformFunc ;

    public SimpleTransformer(Func<string, T> transformFunc) 
    {
        ArgumentNullException.ThrowIfNull(transformFunc, nameof(transformFunc));
        _transformFunc = transformFunc;
    }
    public string? DescriptiveTransform(string? item)
    {
        if (item is null)
        {
            return null;
        }
        return Transform(item)?.ToString();
    }

    public T Transform(string item)
    {
        return _transformFunc(item);
    }

    public bool TryTransform(string item, [NotNullWhen(true)] out T value)
    {
        try
        {
            value = Transform(item)!;
            return true;
        }
        catch (Exception)
        {
            value = default!;
            return false;
        }
    }

    public static implicit operator SimpleTransformer<T>(Func<string, T> transformFunc)
    {
        return new SimpleTransformer<T>(transformFunc);
    }

}