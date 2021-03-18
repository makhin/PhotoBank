using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto.Load;

namespace PhotoBank.Services.Enrichers
{
    public interface IEnricher: IOrderDependant
    {
        Task Enrich(Photo photo, SourceDataDto path);
    }
}