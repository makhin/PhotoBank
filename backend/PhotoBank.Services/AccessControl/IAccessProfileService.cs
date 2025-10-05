using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.AccessControl;

public interface IAccessProfileService
{
    Task<IReadOnlyList<AccessProfileDto>> ListAsync(CancellationToken ct);
    Task<AccessProfileDto?> GetAsync(int id, CancellationToken ct);
    Task<AccessProfileDto> CreateAsync(AccessProfileDto dto, CancellationToken ct);
    Task<bool> UpdateAsync(int id, AccessProfileDto dto, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> AssignUserAsync(int profileId, string userId, CancellationToken ct);
    Task<bool> UnassignUserAsync(int profileId, string userId, CancellationToken ct);
    Task<bool> AssignRoleAsync(int profileId, string roleId, CancellationToken ct);
    Task<bool> UnassignRoleAsync(int profileId, string roleId, CancellationToken ct);
}
