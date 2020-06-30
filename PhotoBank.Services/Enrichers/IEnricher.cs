using System;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;

namespace PhotoBank.Services.Enrichers
{
    public interface IEnricher: IOrderDependant
    {
        void Enrich(Photo photo, SourceDataDto path);
    }
}