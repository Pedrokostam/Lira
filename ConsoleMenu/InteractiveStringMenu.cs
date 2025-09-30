namespace ConsoleMenu;

public static class InteractiveStringMenu
{
    public class NonEmptyStringValidator : IReasonableValidator
    {
        public bool Validate(string value)
        {
           return ValidateWithReason(value).valid;
        }

        public (bool valid, string? reason) ValidateWithReason(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return (false, "Value cannot be an empty string");
            }
            return (true, null);
        }
    }
    public static InteractiveMenu<string> Create(string prompt) => Create(prompt,null, validator: null);
    public static InteractiveMenu<string> CreateNonWhitespace(string prompt,string? placeholder=null) => Create(prompt,placeholder, validator: new NonEmptyStringValidator());
    public static InteractiveMenu<string> Create(string prompt, string? placeholderValue) => Create(prompt, placeholderValue, validator: null);
    public static InteractiveMenu<string> Create(string prompt, Func<string, bool> validatorFunction) => Create(prompt,null, new SimpleValidator(validatorFunction));
    public static InteractiveMenu<string> Create(string prompt, string? placeholderValue, Func<string, bool> validatorFunction) => Create(prompt, placeholderValue, new SimpleValidator(validatorFunction));
    public static InteractiveMenu<string> Create(string prompt, IValidator? validator) => Create(prompt,null, validator);
    public static InteractiveMenu<string> Create(string prompt, string? placeholderValue, IValidator? validator)
    {
        var menu = new InteractiveMenu<string>(new SimpleTransformer<string>(x => x), prompt)
        {
            Validator = validator,
            Hints = validator is null ? Hint.None : Hint.Validation,
            PlaceholderValue = placeholderValue,
        };
        return menu;
    }
}
