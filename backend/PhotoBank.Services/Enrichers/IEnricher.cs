using System;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers
{
    public interface IEnricher : IOrderDependent
    {
        Task EnrichAsync(Photo photo, SourceDataDto path);
        EnricherType EnricherType { get; }
    }
}