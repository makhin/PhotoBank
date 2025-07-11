namespace PhotoBank.ViewModel.Dto;

public class RoleDto
{
    public required string Name { get; set; } = string.Empty;
    public IEnumerable<ClaimDto> Claims { get; set; } = new List<ClaimDto>();
}
