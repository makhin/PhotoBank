using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.AccessControl;

/// <summary>
/// Simple implementation of ICurrentUserAccessor that returns a fixed ICurrentUser instance.
/// Used in console applications where there's no HTTP context or request-scoped users.
/// </summary>
public sealed class SimpleCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly ICurrentUser _user;

    public SimpleCurrentUserAccessor(ICurrentUser user)
    {
        _user = user;
    }

    public ValueTask<ICurrentUser> GetCurrentUserAsync(CancellationToken ct = default)
        => ValueTask.FromResult(_user);

    public ICurrentUser CurrentUser => _user;
}
