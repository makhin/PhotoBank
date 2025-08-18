using System.Security.Claims;

namespace PhotoBank.AccessControl;

public sealed class CurrentUser : ICurrentUser
{
    public bool IsAdmin { get; }
    public IReadOnlySet<int> AllowedStorageIds { get; } = new HashSet<int>();
    public IReadOnlySet<int> AllowedPersonGroupIds { get; } = new HashSet<int>();
    public IReadOnlyList<(DateOnly From, DateOnly To)> AllowedDateRanges { get; } = new List<(DateOnly, DateOnly)>();
    public bool CanSeeNsfw { get; } = false; // по умолчанию запрещено

    public CurrentUser(IHttpContextAccessor accessor)
    {
        var principal = accessor.HttpContext?.User ?? throw new InvalidOperationException("No HttpContext.User");
        IsAdmin = principal.IsInRole("Admin");
        // если у вас уже есть реальный провайдер разрешений — тут подхватите флаг
        // CanSeeNsfw = ...;
    }
}
