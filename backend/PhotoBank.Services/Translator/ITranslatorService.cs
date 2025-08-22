using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.Services.Translator;

public interface ITranslatorService
{
    Task<TranslateResponse> TranslateAsync(TranslateRequest req, CancellationToken ct = default);
}

