namespace ConsoleMenu;

public interface IReasonableValidator : IValidator
{
    (bool valid, string? reason) ValidateWithReason(string value);
}
