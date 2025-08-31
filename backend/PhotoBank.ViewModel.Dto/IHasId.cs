namespace PhotoBank.ViewModel.Dto;

public interface IHasId<TId>
{
    TId Id { get; set; }
}

