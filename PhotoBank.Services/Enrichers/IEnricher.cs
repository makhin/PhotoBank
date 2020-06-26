using PhotoBank.DbContext.Models;

namespace PhotoBank.Services.Enrichers
{
    public interface IEnricher<in TSource>
    {
        void Enrich(Photo photo, TSource path);
    }
}