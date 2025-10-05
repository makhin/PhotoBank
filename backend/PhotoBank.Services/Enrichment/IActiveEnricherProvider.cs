using System;
using System.Collections.Generic;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;

namespace PhotoBank.Services.Enrichment;

public interface IActiveEnricherProvider
{
    IReadOnlyCollection<Type> GetActiveEnricherTypes(IRepository<Enricher> repository);
}
