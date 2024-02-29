using System;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto.Load;

namespace PhotoBank.Services.Enrichers
{
    public interface IEnricher: IOrderDependant
    {
        new Type[] Dependencies { get; }
        Task EnrichAsync(Photo photo, SourceDataDto path);
        EnricherType EnricherType { get; }
        bool IsActive { get; set; }
    }
}