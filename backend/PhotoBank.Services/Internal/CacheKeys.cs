using PhotoBank.AccessControl;

namespace PhotoBank.Services.Internal;

public static class CacheKeys
{
    public static string Tags => "tags";
    public static string PersonGroups => "persongroups";

    public static string Persons(ICurrentUser u) => u.IsAdmin ? PersonsAll : PersonsOf(u.UserId);
    public static string Paths(ICurrentUser u) => u.IsAdmin ? PathsAll : PathsOf(u.UserId);
    public static string Storages(ICurrentUser u) => u.IsAdmin ? StoragesAll : StoragesOf(u.UserId);

    public static string PersonsAll => "persons_all";
    public static string PathsAll => "paths_all";
    public static string StoragesAll => "storages_all";

    public static string PersonsOf(string userId) => $"persons_{userId}";
    public static string PathsOf(string userId) => $"paths_{userId}";
    public static string StoragesOf(string userId) => $"storages_{userId}";
}