namespace PhotoBank.Api.AccessControl;

public interface ICurrentUser
{
    bool IsAdmin { get; }
    IReadOnlySet<int> AllowedStorageIds { get; }
    IReadOnlySet<int> AllowedPersonGroupIds { get; }
    IReadOnlyList<(DateOnly From, DateOnly To)> AllowedDateRanges { get; }
    bool NsfwOnly { get; } // сейчас в модели поля NSFW нет — оставлен на будущее
}
