namespace PhotoBank.Services.Identity;

public interface ITelegramServiceKeyValidator
{
    ServiceKeyValidationResult Validate(string? presentedKey);
}
