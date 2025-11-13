using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PhotoBank.DbContext.Models;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Services.Identity;

internal static class UserDtoMapper
{
    public static UserDto Map(
        ApplicationUser user,
        IEnumerable<string> roles,
        IEnumerable<int>? accessProfileIds = null)
    {
        var assignments = accessProfileIds?.Select(id => new UserAccessProfileAssignmentDto
        {
            ProfileId = id
        }).ToArray() ?? Array.Empty<UserAccessProfileAssignmentDto>();

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            TelegramUserId = user.TelegramUserId?.ToString(CultureInfo.InvariantCulture),
            TelegramSendTimeUtc = user.TelegramSendTimeUtc,
            Roles = roles.ToArray(),
            AccessProfiles = assignments
        };
    }
}
