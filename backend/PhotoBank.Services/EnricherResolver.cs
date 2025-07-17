using System.Collections.Generic;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichers;

namespace PhotoBank.Services;

public delegate IEnumerable<IEnricher> EnricherResolver(IRepository<Enricher> enricherRepository);