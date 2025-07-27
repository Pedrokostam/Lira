namespace ConsoleMenu;

public interface ITransform<T>
{
    T Transform(string item);
    string? DescriptiveTransform(string item);
}
