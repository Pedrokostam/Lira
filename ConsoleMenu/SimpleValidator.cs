namespace ConsoleMenu;

public class SimpleValidator(Func<string, bool> function) : IValidator
{
    private readonly Func<string, bool> _function = function;

    public bool Validate(string value) => _function(value);
}
