using PhotoBank.DbContext.Models;

namespace PhotoBank.ViewModel.Dto;

public class FaceIdentityDto : IHasId<int>
{
    public int Id { get; set; }
    public IdentityStatus IdentityStatus { get; set; }
    public PersonDto? Person { get; set; }
}
