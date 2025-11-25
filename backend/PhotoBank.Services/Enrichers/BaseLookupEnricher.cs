using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        private readonly Expression<Func<TModel, string>> _nameSelector;
        private readonly Func<TModel, string> _nameAccessor;
        private readonly Func<string, TModel> _modelFactory;
        private readonly Func<Photo, string, TModel, SourceDataDto, TLink> _linkFactory;

        protected BaseLookupEnricher(
            IRepository<TModel> repository,
            Func<SourceDataDto, IEnumerable<string>> namesSelector,
            Expression<Func<TModel, string>> nameSelector,
            Func<string, TModel> modelFactory,
            Func<Photo, string, TModel, SourceDataDto, TLink> linkFactory,
            Func<TModel, string>? nameAccessor = null)
        {
            _repository = repository;
            _namesSelector = namesSelector;
            _nameSelector = nameSelector;
            _nameAccessor = nameAccessor ?? nameSelector.Compile();
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
                .Select(n => n!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (incoming.Length == 0)
            {
                return;
            }

            var query = _repository.GetByCondition(BuildContainsExpression(incoming));
            List<TModel> existing;
            try
            {
                existing = await query.AsNoTracking().ToListAsync(cancellationToken);
            }
            catch (InvalidOperationException)
            {
                existing = query.AsNoTracking().ToList();
            }

            // Attach existing entities so EF knows they already exist in the database
            foreach (var entity in existing)
            {
                _repository.Attach(entity);
            }

            var map = existing.ToDictionary(_nameAccessor, StringComparer.OrdinalIgnoreCase);

            foreach (var name in incoming)
            {
                if (!map.TryGetValue(name, out var model))
                {
                    model = _modelFactory(name);
                    await _repository.InsertAsync(model);
                    // Attach newly inserted entity to ensure it's marked as Unchanged
                    _repository.Attach(model);
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

        private Expression<Func<TModel, bool>> BuildContainsExpression(IEnumerable<string> values)
        {
            var parameter = Expression.Parameter(typeof(TModel), "model");
            var valueExpression = new ParameterReplaceVisitor(_nameSelector.Parameters[0], parameter)
                .Visit(_nameSelector.Body);

            var containsCall = Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Contains),
                new[] { typeof(string) },
                Expression.Constant(values, typeof(IEnumerable<string>)),
                valueExpression);

            return Expression.Lambda<Func<TModel, bool>>(containsCall, parameter);
        }

        private sealed class ParameterReplaceVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _source;
            private readonly ParameterExpression _target;

            public ParameterReplaceVisitor(ParameterExpression source, ParameterExpression target)
            {
                _source = source;
                _target = target;
            }

            protected override Expression VisitParameter(ParameterExpression node)
                => node == _source ? _target : base.VisitParameter(node);
        }
    }
}
