using System.Security.Claims;

namespace PhotoBank.Api.AccessControl;

public sealed class CurrentUser : ICurrentUser
{
    public bool IsAdmin { get; }
    public IReadOnlySet<int> AllowedStorageIds { get; } = new HashSet<int>();
    public IReadOnlySet<int> AllowedPersonGroupIds { get; } = new HashSet<int>();
    public IReadOnlyList<(DateOnly From, DateOnly To)> AllowedDateRanges { get; } = new List<(DateOnly, DateOnly)>();
    public bool NsfwOnly { get; } = false; // поля NSFW в модели сейчас нет

    public CurrentUser(IHttpContextAccessor accessor)
    {
        var principal = accessor.HttpContext?.User ?? throw new InvalidOperationException("No HttpContext.User");
        IsAdmin = principal.IsInRole("Admin");
    }
}
