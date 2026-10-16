using System.Diagnostics.CodeAnalysis;

namespace ConsoleMenu;
public interface ITransformer<T>
{
    T Transform(string item);

    /// <summary>
    /// Tries to transform <paramref name="item"/> into an instance of <typeparamref name="T"/> and then returns its canonical string representation.
    /// </summary>
    /// <remarks>This method should not throw.</remarks>
    /// <param name="item">The input string to be transformed. Can be <see langword="null"/>.</param>
    /// <returns>A transformed string based on the input, or <see langword="null"/> if <paramref name="item"/> could not be trasnformed to <typeparamref name="T"/>.</returns>
    string? DescriptiveTransform(string? item);
    bool TryTransform(string item, [NotNullWhen(true)] out T value);
   
}
