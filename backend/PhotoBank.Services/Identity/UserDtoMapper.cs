using PhotoBank.DbContext.Models;
using PhotoBank.ViewModel.Dto;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PhotoBank.Services.Identity;

internal static class UserDtoMapper
{
    public static UserDto Map(ApplicationUser user, IEnumerable<string> roles)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            TelegramUserId = user.TelegramUserId?.ToString(CultureInfo.InvariantCulture),
            TelegramSendTimeUtc = user.TelegramSendTimeUtc,
            Roles = roles.ToArray()
        };
    }
}
