using System.Globalization;

namespace PhotoBank.Api.Validation;

public static class TelegramUserIdParser
{
    public static bool TryParse(string? raw, out long? value, out string? error)
    {
        value = null;
        error = null;

        if (raw is null)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        if (!long.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
        {
            error = "Telegram user ID must contain only digits.";
            return false;
        }

        if (parsed <= 0)
        {
            error = "Telegram user ID must be greater than zero.";
            return false;
        }

        value = parsed;
        return true;
    }
}
