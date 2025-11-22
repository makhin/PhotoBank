using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PhotoBank.Services.Enrichment;

namespace PhotoBank.UnitTests.Enrichment;

[TestFixture]
public class EnricherDependencyResolverTests
{
    [Test]
    public void Sort_ShouldUseInstancePropertyDependencies()
    {
        using var provider = BuildProvider(services => services.AddSingleton<DependencyHolder>());
        var holder = provider.GetRequiredService<DependencyHolder>();

        var types = new[]
        {
            typeof(InstanceDependentEnricher),
            typeof(BaseEnricher)
        };

        var result = InvokeSort(types, provider);

        result.Should().Equal(typeof(BaseEnricher), typeof(InstanceDependentEnricher));
        holder.WasAccessed.Should().BeTrue();
    }

    [Test]
    public void Sort_ShouldUseNameFallbackWhenDependencyProvidedByName()
    {
        using var provider = BuildProvider();
        var types = new[] { typeof(NameFallbackEnricher), typeof(BaseEnricher) };

        var result = InvokeSort(types, provider);

        result.Should().Equal(typeof(BaseEnricher), typeof(NameFallbackEnricher));
    }

    [Test]
    public void Sort_ShouldThrowWhenDependencyNameIsUnknown()
    {
        using var provider = BuildProvider();
        var types = new[] { typeof(UnknownNameEnricher) };

        Action act = () => InvokeSort(types, provider);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Enricher UnknownNameEnricher declares unknown dependency by name: 'Ghost'.");
    }

    [Test]
    public void Sort_ShouldThrowWhenDependencyTypeIsNotRegistered()
    {
        using var provider = BuildProvider();
        var types = new[] { typeof(UnknownTypeEnricher) };

        Action act = () => InvokeSort(types, provider);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Enricher UnknownTypeEnricher depends on ExternalDependency, which is not registered.");
    }

    [Test]
    public void Sort_ShouldThrowWhenCycleDetected()
    {
        using var provider = BuildProvider();
        var types = new[] { typeof(CycleAEnricher), typeof(CycleBEnricher) };

        Action act = () => InvokeSort(types, provider);

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().StartWith("A dependency cycle was detected among enrichers:");
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    private static Type[] InvokeSort(IReadOnlyList<Type> types, IServiceProvider provider)
    {
        var servicesAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "PhotoBank.Services")
            ?? throw new InvalidOperationException("PhotoBank.Services assembly not found.");

        var resolverType = servicesAssembly.GetType("PhotoBank.Services.Enrichment.EnricherDependencyResolver")
            ?? throw new InvalidOperationException("Resolver type not found.");

        var method = resolverType.GetMethod("Sort", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Sort method not found.");

        try
        {
            return (Type[])method.Invoke(null, new object[] { types, provider })!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private sealed class BaseEnricher
    {
    }

    private sealed class InstanceDependentEnricher
    {
        private readonly DependencyHolder _holder;

        public InstanceDependentEnricher(DependencyHolder holder)
        {
            _holder = holder;
        }

        public IEnumerable<Type> Dependencies
        {
            get
            {
                _holder.WasAccessed = true;
                return new[] { typeof(BaseEnricher) };
            }
        }
    }

    private sealed class DependencyHolder
    {
        public bool WasAccessed { get; set; }
    }

    private sealed class NameFallbackEnricher
    {
        public static IEnumerable<string> Dependencies => new[] { "baseenricher" };
    }

    private sealed class UnknownNameEnricher
    {
        public static IEnumerable<string> Dependencies => new[] { "Ghost" };
    }

    private sealed class UnknownTypeEnricher
    {
        public static IEnumerable<Type> Dependencies => new[] { typeof(ExternalDependency) };
    }

    private sealed class ExternalDependency
    {
    }

    private sealed class CycleAEnricher
    {
        public static IEnumerable<Type> Dependencies => new[] { typeof(CycleBEnricher) };
    }

    private sealed class CycleBEnricher
    {
        public static IEnumerable<Type> Dependencies => new[] { typeof(CycleAEnricher) };
    }
}
