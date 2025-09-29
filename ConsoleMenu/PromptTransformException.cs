namespace ConsoleMenu;

public class PromptTransformException(string value, Type targetType, Exception inner) : InvalidCastException($"Cannot convert string {value} into an instance of {targetType}", inner)
{
    public string Value { get; } = value;
    public Type TargetType { get; } = targetType;
    public static PromptTransformException WrapException(string value, Type targetType, Exception inner)
    {
        return new PromptTransformException(value, targetType, inner);
    }
    public static PromptTransformException WrapException<T>(string value, Exception inner)
    {
        return new PromptTransformException(value, typeof(T), inner);
    }
}
