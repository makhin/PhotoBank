using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichment;
using PhotoBank.Services.Enrichers;

namespace PhotoBank.UnitTests.Enrichment;

[TestFixture]
public class ActiveEnricherProviderTests
{
    [Test]
    public void GetActiveEnricherTypes_ReturnsOnlyActiveEnrichers()
    {
        var repository = new FakeEnricherRepository(new[]
        {
            new Enricher { Name = nameof(MetadataEnricher), IsActive = true },
            new Enricher { Name = nameof(TagEnricher), IsActive = false }
        });
        var provider = new ActiveEnricherProvider();

        var types = provider.GetActiveEnricherTypes(repository);

        types.Should().Contain(typeof(MetadataEnricher));
        types.Should().NotContain(typeof(TagEnricher));
    }

    [Test]
    public void GetActiveEnricherTypes_ThrowsForUnknownEnricher()
    {
        var repository = new FakeEnricherRepository(new[]
        {
            new Enricher { Name = "UnknownEnricher", IsActive = true }
        });
        var provider = new ActiveEnricherProvider();

        Action act = () => provider.GetActiveEnricherTypes(repository);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*UnknownEnricher*");
    }

    private sealed class FakeEnricherRepository : IRepository<Enricher>
    {
        private readonly IQueryable<Enricher> _items;

        public FakeEnricherRepository(IEnumerable<Enricher> items)
        {
            _items = items.AsQueryable();
        }

        public IQueryable<Enricher> GetAll() => _items;
        public IQueryable<Enricher> GetByCondition(Expression<Func<Enricher, bool>> predicate) => _items.Where(predicate);
        public Task<Enricher> GetAsync(int id, Func<IQueryable<Enricher>, IQueryable<Enricher>> queryable) => throw new NotSupportedException();
        public Enricher Get(int id, Func<IQueryable<Enricher>, IQueryable<Enricher>> queryable) => throw new NotSupportedException();
        public Task<Enricher> GetAsync(int id) => throw new NotSupportedException();
        public Enricher Get(int id) => throw new NotSupportedException();
        public Task<Enricher> InsertAsync(Enricher entity) => throw new NotSupportedException();
        public Task InsertRangeAsync(List<Enricher> entities) => throw new NotSupportedException();
        public Task<Enricher> UpdateAsync(Enricher entity) => throw new NotSupportedException();
        public Task<int> UpdateAsync(Enricher entity, params Expression<Func<Enricher, object>>[] properties) => throw new NotSupportedException();
        public Task<int> DeleteAsync(int id) => throw new NotSupportedException();
    }
}
