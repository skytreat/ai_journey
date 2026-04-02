using System.Linq.Expressions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using FundRecommendationAPI.Models;
using FundRecommendationAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Xunit;

namespace FundRecommendationAPI.Tests
{
    public class RepositoryTests
    {
        private FundDbContext CreateInMemoryContext(string dbName = null)
        {
            var options = new DbContextOptionsBuilder<FundDbContext>()
                .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
                .Options;
            return new FundDbContext(options);
        }

        [Fact]
        public void Repository_Constructor_ShouldInitializeCorrectly()
        {
            using var context = CreateInMemoryContext();
            var repository = new Repository<FundBasicInfo>(context);
            Assert.NotNull(repository);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllEntities()
        {
            using var context = CreateInMemoryContext();
            context.FundBasicInfo.Add(new FundBasicInfo { Code = "000001", Name = "Test Fund 1" });
            context.FundBasicInfo.Add(new FundBasicInfo { Code = "000002", Name = "Test Fund 2" });
            await context.SaveChangesAsync();

            var repository = new Repository<FundBasicInfo>(context);

            var result = await repository.GetAllAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyListWhenNoEntities()
        {
            using var context = CreateInMemoryContext();
            var repository = new Repository<FundBasicInfo>(context);

            var result = await repository.GetAllAsync();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnEntityById()
        {
            using var context = CreateInMemoryContext();
            var entity = new FundBasicInfo { Code = "000001", Name = "Test Fund" };
            context.FundBasicInfo.Add(entity);
            await context.SaveChangesAsync();

            var repository = new Repository<FundBasicInfo>(context);

            var result = await repository.GetByIdAsync("000001");

            Assert.NotNull(result);
            Assert.Equal("000001", result.Code);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNullWhenEntityNotFound()
        {
            using var context = CreateInMemoryContext();
            var repository = new Repository<FundBasicInfo>(context);

            var result = await repository.GetByIdAsync("999999");

            Assert.Null(result);
        }

        [Fact]
        public async Task AddAsync_ShouldAddEntity()
        {
            using var context = CreateInMemoryContext();
            var repository = new Repository<FundBasicInfo>(context);
            var entity = new FundBasicInfo { Code = "000001", Name = "Test Fund" };

            await repository.AddAsync(entity);

            var savedEntity = await context.FundBasicInfo.FindAsync("000001");
            Assert.NotNull(savedEntity);
            Assert.Equal("Test Fund", savedEntity.Name);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateEntity()
        {
            using var context = CreateInMemoryContext();
            var entity = new FundBasicInfo { Code = "000001", Name = "Test Fund" };
            context.FundBasicInfo.Add(entity);
            await context.SaveChangesAsync();

            var repository = new Repository<FundBasicInfo>(context);
            entity.Name = "Updated Fund";

            await repository.UpdateAsync(entity);

            var updatedEntity = await context.FundBasicInfo.FindAsync("000001");
            Assert.Equal("Updated Fund", updatedEntity.Name);
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteEntity()
        {
            using var context = CreateInMemoryContext();
            var entity = new FundBasicInfo { Code = "000001", Name = "Test Fund" };
            context.FundBasicInfo.Add(entity);
            await context.SaveChangesAsync();

            var repository = new Repository<FundBasicInfo>(context);

            await repository.DeleteAsync(entity);

            var deletedEntity = await context.FundBasicInfo.FindAsync("000001");
            Assert.Null(deletedEntity);
        }

        [Fact]
        public async Task CountAsync_ShouldReturnEntityCount()
        {
            using var context = CreateInMemoryContext();
            context.FundBasicInfo.Add(new FundBasicInfo { Code = "000001", Name = "Test Fund 1" });
            context.FundBasicInfo.Add(new FundBasicInfo { Code = "000002", Name = "Test Fund 2" });
            context.FundBasicInfo.Add(new FundBasicInfo { Code = "000003", Name = "Test Fund 3" });
            await context.SaveChangesAsync();

            var repository = new Repository<FundBasicInfo>(context);

            var result = await repository.CountAsync();

            Assert.Equal(3, result);
        }

        [Fact]
        public async Task CountAsync_ShouldReturnZeroWhenNoEntities()
        {
            using var context = CreateInMemoryContext();
            var repository = new Repository<FundBasicInfo>(context);

            var result = await repository.CountAsync();

            Assert.Equal(0, result);
        }

        [Fact]
        public void Query_ShouldReturnQueryable()
        {
            using var context = CreateInMemoryContext();
            var repository = new Repository<FundBasicInfo>(context);

            var result = repository.Query();

            Assert.NotNull(result);
            Assert.IsAssignableFrom<IQueryable<FundBasicInfo>>(result);
        }

        [Fact]
        public async Task Query_WithFilter_ShouldReturnFilteredResults()
        {
            using var context = CreateInMemoryContext();
            context.FundBasicInfo.Add(new FundBasicInfo { Code = "000001", Name = "Test Fund 1", FundType = "股票型" });
            context.FundBasicInfo.Add(new FundBasicInfo { Code = "000002", Name = "Test Fund 2", FundType = "债券型" });
            context.FundBasicInfo.Add(new FundBasicInfo { Code = "000003", Name = "Test Fund 3", FundType = "股票型" });
            await context.SaveChangesAsync();

            var repository = new Repository<FundBasicInfo>(context);

            var result = repository.Query().Where(f => f.FundType == "股票型").ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task Query_WithOrderBy_ShouldReturnSortedResults()
        {
            using var context = CreateInMemoryContext();
            context.FundBasicInfo.Add(new FundBasicInfo { Code = "000001", Name = "AAA Fund" });
            context.FundBasicInfo.Add(new FundBasicInfo { Code = "000002", Name = "ZZZ Fund" });
            context.FundBasicInfo.Add(new FundBasicInfo { Code = "000003", Name = "MMM Fund" });
            await context.SaveChangesAsync();

            var repository = new Repository<FundBasicInfo>(context);

            var result = repository.Query().OrderBy(f => f.Name).ToList();

            Assert.Equal("AAA Fund", result[0].Name);
            Assert.Equal("MMM Fund", result[1].Name);
            Assert.Equal("ZZZ Fund", result[2].Name);
        }

        [Fact]
        public async Task Query_WithSkipAndTake_ShouldReturnPaginatedResults()
        {
            using var context = CreateInMemoryContext();
            for (int i = 1; i <= 10; i++)
            {
                context.FundBasicInfo.Add(new FundBasicInfo { Code = $"00000{i}", Name = $"Fund {i}" });
            }
            await context.SaveChangesAsync();

            var repository = new Repository<FundBasicInfo>(context);

            var result = repository.Query().OrderBy(f => f.Code).Skip(3).Take(3).ToList();

            Assert.Equal(3, result.Count);
            Assert.Equal("000004", result[0].Code);
        }

        [Fact]
        public async Task MultipleEntities_CanBeAddedAndRetrieved()
        {
            using var context = CreateInMemoryContext();
            var repository = new Repository<FundNavHistory>(context);

            var nav1 = new FundNavHistory { Code = "000001", Date = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)), Nav = 1.0m };
            var nav2 = new FundNavHistory { Code = "000001", Date = DateOnly.FromDateTime(DateTime.Now), Nav = 1.1m };

            await repository.AddAsync(nav1);
            await repository.AddAsync(nav2);

            var count = await repository.CountAsync();
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task Update_NonExistentEntity_DoesNotThrow()
        {
            using var context = CreateInMemoryContext();
            var repository = new Repository<FundBasicInfo>(context);
            var entity = new FundBasicInfo { Code = "999999", Name = "Non Existent" };

            await repository.UpdateAsync(entity);

            var result = await repository.GetByIdAsync("999999");
            Assert.Null(result);
        }
    }
}
