using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers
{
    public abstract class BaseLookupEnricher<TModel, TLink> : IEnricher
        where TModel : class, IEntityBase, new()
    {
        private readonly IRepository<TModel> _repository;
        private readonly Func<SourceDataDto, IEnumerable<string>> _namesSelector;
        private readonly Func<string, TModel> _modelFactory;
        private readonly Func<Photo, string, TModel, SourceDataDto, TLink> _linkFactory;

        protected BaseLookupEnricher(
            IRepository<TModel> repository,
            Func<SourceDataDto, IEnumerable<string>> namesSelector,
            Func<string, TModel> modelFactory,
            Func<Photo, string, TModel, SourceDataDto, TLink> linkFactory)
        {
            _repository = repository;
            _namesSelector = namesSelector;
            _modelFactory = modelFactory;
            _linkFactory = linkFactory;
        }

        public virtual Type[] Dependencies => new[] { typeof(AnalyzeEnricher) };

        public abstract EnricherType EnricherType { get; }

        protected abstract ICollection<TLink> GetCollection(Photo photo);

        public async Task EnrichAsync(Photo photo, SourceDataDto sourceData, CancellationToken cancellationToken = default)
        {
            var incoming = _namesSelector(sourceData)
                .Select(n => n?.Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (incoming.Length == 0)
            {
                return;
            }

            var query = _repository.GetByCondition(m => incoming.Contains(EF.Property<string>(m, "Name")));
            List<TModel> existing;
            try
            {
                existing = await query.AsNoTracking().ToListAsync(cancellationToken);
            }
            catch (InvalidOperationException)
            {
                existing = query.AsNoTracking().ToList();
            }

            var nameProp = typeof(TModel).GetProperty("Name");
            var map = existing.ToDictionary(e => (string)nameProp.GetValue(e), StringComparer.OrdinalIgnoreCase);

            foreach (var name in incoming)
            {
                if (!map.TryGetValue(name, out var model))
                {
                    model = _modelFactory(name);
                    await _repository.InsertAsync(model);
                    map[name] = model;
                }
            }

            var collection = GetCollection(photo);

            foreach (var name in incoming)
            {
                var model = map[name];
                var link = _linkFactory(photo, name, model, sourceData);
                collection.Add(link);
            }
        }
    }
}
