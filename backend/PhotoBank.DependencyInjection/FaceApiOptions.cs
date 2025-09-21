using System.ComponentModel.DataAnnotations;

namespace PhotoBank.DependencyInjection;

public sealed class FaceApiOptions
{
    [Required(AllowEmptyStrings = false)]
    public string Endpoint { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Key { get; init; } = string.Empty;
}
