using System;
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

    private static ServiceProvider BuildProviderWithPhotos()
    {
        var services = new ServiceCollection();
        services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
        services.AddHttpContextAccessor();

        var provider = services.BuildServiceProvider();

        var context = provider.GetRequiredService<PhotoBankDbContext>();

        var group1 = new PersonGroup { Id = 1, Name = "Group1" };
        var group2 = new PersonGroup { Id = 2, Name = "Group2" };

        var allowed = new Person { Id = 1, Name = "Alice" };
        var denied = new Person { Id = 2, Name = "Bob" };

        allowed.PersonGroups = new List<PersonGroup> { group2 };
        denied.PersonGroups = new List<PersonGroup> { group1 };
        group2.Persons = new List<Person> { allowed };
        group1.Persons = new List<Person> { denied };

        var storage = new Storage { Id = 1, Name = "s" };

        var photoAllowed = new Photo { Id = 1, Name = "Allowed", StorageId = storage.Id, Faces = new List<Face>() };
        var photoDenied = new Photo { Id = 2, Name = "Denied", StorageId = storage.Id, Faces = new List<Face>() };
        var photoNoPersons = new Photo { Id = 3, Name = "None", StorageId = storage.Id, Faces = new List<Face>() };

        var faceAllowed = new Face { Id = 1, Photo = photoAllowed, PhotoId = photoAllowed.Id, Person = allowed, PersonId = allowed.Id };
        var faceDenied = new Face { Id = 2, Photo = photoDenied, PhotoId = photoDenied.Id, Person = denied, PersonId = denied.Id };

        photoAllowed.Faces.Add(faceAllowed);
        photoDenied.Faces.Add(faceDenied);

        context.PersonGroups.AddRange(group1, group2);
        context.Persons.AddRange(allowed, denied);
        context.Storages.Add(storage);
        context.Photos.AddRange(photoAllowed, photoDenied, photoNoPersons);
        context.Faces.AddRange(faceAllowed, faceDenied);
        context.SaveChanges();

        return provider;
    }

    [Test]
    public void GetAllPhotos_RespectsAllowPersonGroupClaims()
    {
        var provider = BuildProviderWithPhotos();

        var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
        var context = provider.GetRequiredService<PhotoBankDbContext>();
        context.Persons.Include(p => p.PersonGroups).Load();
        httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("AllowPersonGroup", "2")
            }))
        };

        var repository = new Repository<Photo>(provider, httpContextAccessor);

        var photos = repository.GetAll().ToList();

        Assert.That(photos.Select(p => p.Id), Is.EquivalentTo(new[] { 1, 3 }));
    }

    [Test]
    public void GetAllPhotos_NoGroupsAllowed_ReturnsOnlyPhotosWithoutPersons()
    {
        var provider = BuildProviderWithPhotos();

        var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
        var context = provider.GetRequiredService<PhotoBankDbContext>();
        context.Persons.Include(p => p.PersonGroups).Load();
        httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("AllowPersonGroup", "-1")
            }))
        };

        var repository = new Repository<Photo>(provider, httpContextAccessor);

        var photos = repository.GetAll().ToList();

        Assert.That(photos.Select(p => p.Id), Is.EquivalentTo(new[] { 3 }));
    }
}

