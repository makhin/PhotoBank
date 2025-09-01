using System;
using FluentAssertions;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers;

namespace PhotoBank.UnitTests.Enrichers
{
    public abstract class EnricherTestsBase<TEnricher>
        where TEnricher : IEnricher, new()
    {
        protected TEnricher _enricher;

        protected abstract EnricherType ExpectedEnricherType { get; }
        protected abstract Type[] ExpectedDependencies { get; }

        [SetUp]
        public void Setup()
        {
            _enricher = new TEnricher();
        }

        [Test]
        public void EnricherType_ShouldReturnExpected()
        {
            var result = _enricher.EnricherType;
            result.Should().Be(ExpectedEnricherType);
        }

        [Test]
        public void Dependencies_ShouldReturnExpected()
        {
            var result = _enricher.Dependencies;
            result.Should().BeEquivalentTo(ExpectedDependencies);
        }
    }
}
