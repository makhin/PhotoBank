using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.AccessControl;

public interface ICurrentUserAccessor
{
    ValueTask<ICurrentUser> GetCurrentUserAsync(CancellationToken ct = default);

    ICurrentUser CurrentUser { get; }
}
