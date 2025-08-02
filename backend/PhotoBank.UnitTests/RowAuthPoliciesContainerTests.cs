using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;

namespace PhotoBank.UnitTests;

[TestFixture]
public class RowAuthPoliciesContainerTests
{
    [Test]
    public void GetAllPersons_RespectsAllowPersonGroupClaims()
    {
        var services = new ServiceCollection();
        services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase("persons"));
        services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
        services.AddHttpContextAccessor();

        var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PhotoBankDbContext>();

            var group1 = new PersonGroup { Id = 1, Name = "Group1" };
            var group2 = new PersonGroup { Id = 2, Name = "Group2" };

            var allowed = new Person
            {
                Id = 1,
                Name = "Alice",
                PersonGroups = new List<PersonGroup> { group2 }
            };

            var denied = new Person
            {
                Id = 2,
                Name = "Bob",
                PersonGroups = new List<PersonGroup> { group1 }
            };

            context.PersonGroups.AddRange(group1, group2);
            context.Persons.AddRange(allowed, denied);
            context.SaveChanges();
        }

        var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("AllowPersonGroup", "2")
            }))
        };

        var repository = new Repository<Person>(provider, httpContextAccessor);

        var persons = repository.GetAll().ToList();

        Assert.That(persons.Select(p => p.Id), Is.EquivalentTo(new[] { 1 }));
    }
}

