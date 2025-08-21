using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;

namespace PhotoBank.UnitTests
{
    [TestFixture]
    public class RepositoryTests
    {
        private static Repository<Storage> CreateRepository(out PhotoBankDbContext context)
        {
            var services = new ServiceCollection();
            services.AddDbContext<PhotoBankDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            var provider = services.BuildServiceProvider();
            context = provider.GetRequiredService<PhotoBankDbContext>();
            return new Repository<Storage>(provider);
        }

        [Test]
        public void GetAll_ReturnsAllEntities()
        {
            var repo = CreateRepository(out var context);
            context.Storages.AddRange(
                new Storage { Name = "S1", Folder = "F1" },
                new Storage { Name = "S2", Folder = "F2" });
            context.SaveChanges();

            var result = repo.GetAll().ToList();

            result.Should().HaveCount(2);
        }

        [Test]
        public void GetByCondition_FiltersEntities()
        {
            var repo = CreateRepository(out var context);
            context.Storages.AddRange(
                new Storage { Name = "A", Folder = "F1" },
                new Storage { Name = "B", Folder = "F2" });
            context.SaveChanges();

            var result = repo.GetByCondition(s => s.Name == "B").ToList();

            result.Should().ContainSingle()
                  .Which.Name.Should().Be("B");
        }

        [Test]
        public async Task GetAsync_WithQueryable_ReturnsEntity()
        {
            var repo = CreateRepository(out var context);
            var storage = new Storage { Name = "S1", Folder = "F1" };
            context.Storages.Add(storage);
            context.SaveChanges();

            var result = await repo.GetAsync(storage.Id, q => q);

            result.Should().NotBeNull();
            result.Name.Should().Be("S1");
        }

        [Test]
        public void Get_WithQueryable_ReturnsEntity()
        {
            var repo = CreateRepository(out var context);
            var storage = new Storage { Name = "S1", Folder = "F1" };
            context.Storages.Add(storage);
            context.SaveChanges();

            var result = repo.Get(storage.Id, q => q);

            result.Should().NotBeNull();
            result.Name.Should().Be("S1");
        }

        [Test]
        public async Task GetAsync_ReturnsEntity()
        {
            var repo = CreateRepository(out var context);
            var storage = new Storage { Name = "S1", Folder = "F1" };
            context.Storages.Add(storage);
            context.SaveChanges();

            var result = await repo.GetAsync(storage.Id);

            result.Should().NotBeNull();
            result.Name.Should().Be("S1");
        }

        [Test]
        public void Get_ReturnsEntity()
        {
            var repo = CreateRepository(out var context);
            var storage = new Storage { Name = "S1", Folder = "F1" };
            context.Storages.Add(storage);
            context.SaveChanges();

            var result = repo.Get(storage.Id);

            result.Should().NotBeNull();
            result.Name.Should().Be("S1");
        }

        [Test]
        public async Task InsertAsync_AddsEntity()
        {
            var repo = CreateRepository(out var context);
            var storage = new Storage { Name = "S1", Folder = "F1" };

            var result = await repo.InsertAsync(storage);

            result.Id.Should().BeGreaterThan(0);
            context.Storages.Count().Should().Be(1);
        }

        [Test]
        public async Task InsertRangeAsync_AddsEntities()
        {
            var repo = CreateRepository(out var context);
            var storages = new List<Storage>
            {
                new Storage { Name = "S1", Folder = "F1" },
                new Storage { Name = "S2", Folder = "F2" }
            };

            await repo.InsertRangeAsync(storages);

            context.Storages.Count().Should().Be(2);
        }

        [Test]
        public async Task UpdateAsync_UpdatesEntity()
        {
            var repo = CreateRepository(out var context);
            var storage = await repo.InsertAsync(new Storage { Name = "S1", Folder = "F1" });

            storage.Name = "Updated";
            var result = await repo.UpdateAsync(storage);

            result.Name.Should().Be("Updated");
            context.Storages.Single().Name.Should().Be("Updated");
        }

        [Test]
        public async Task UpdateAsync_WithProperties_UpdatesSpecifiedFields()
        {
            var repo = CreateRepository(out var context);
            var inserted = await repo.InsertAsync(new Storage { Name = "S1", Folder = "F1" });
            context.Entry(inserted).State = EntityState.Detached;

            var entity = new Storage { Id = inserted.Id, Name = "S2" };
            await repo.UpdateAsync(entity, new Expression<Func<Storage, object>>[] { s => s.Name });

            var updated = context.Storages.AsNoTracking().Single(s => s.Id == inserted.Id);
            updated.Name.Should().Be("S2");
            updated.Folder.Should().Be("F1");
        }

        [Test]
        public async Task DeleteAsync_RemovesEntity()
        {
            var repo = CreateRepository(out var context);
            var storage = await repo.InsertAsync(new Storage { Name = "S1", Folder = "F1" });

            var result = await repo.DeleteAsync(storage.Id);

            result.Should().Be(1);
            context.Storages.Count().Should().Be(0);
        }

        [Test]
        public async Task DeleteAsync_NonExisting_Throws()
        {
            var repo = CreateRepository(out _);

            var act = async () => await repo.DeleteAsync(1);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Record not found; not deleted");
        }
    }
}

