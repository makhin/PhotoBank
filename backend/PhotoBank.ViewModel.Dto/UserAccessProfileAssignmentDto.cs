using System.ComponentModel.DataAnnotations;

namespace PhotoBank.ViewModel.Dto;

public sealed class UserAccessProfileAssignmentDto
{
    [Required]
    public int ProfileId { get; init; }
}
