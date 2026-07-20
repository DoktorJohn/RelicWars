using Domain.Entities;
using Domain.Enums;
using Infrastructure.Context;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Application.Tests;

public class ExoticResourceMigrationTests
{
    private const string MigrationBeforeRepair = "20260717191714_AddDailyObjectives";
    private const string RepairMigration = "20260717195246_RepairCityExoticResourceBalances";
    private const string LatestMigration = "20260719182549_RepairResearchLegacyUserIdColumn";

    [Fact]
    public async Task RepairMigration_BackfillsMissingBalancesAndRepositoryLoadsAllTypes()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string databaseName = $"RelicWarsExoticResourceMigration_{Guid.NewGuid():N}";
        string connectionString =
            $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true";
        var options = new DbContextOptionsBuilder<GameContext>()
            .UseSqlServer(connectionString)
            .Options;

        try
        {
            Guid cityId = await CreateIncompleteNpcCityAsync(options);

            await using (var migrationContext = new GameContext(options))
            {
                await migrationContext.Database.MigrateAsync();
                await migrationContext.GetService<IMigrator>().MigrateAsync(MigrationBeforeRepair);
                await migrationContext.Database.MigrateAsync();
            }

            await using var verificationContext = new GameContext(options);
            var balances = await verificationContext.CityExoticResources
                .AsNoTracking()
                .Where(resource => resource.CityId == cityId)
                .OrderBy(resource => resource.ResourceType)
                .ToListAsync();

            Assert.Equal(Enum.GetValues<ExoticResourceTypeEnum>().Length, balances.Count);
            Assert.Equal(balances.Count, balances.Select(resource => resource.ResourceType).Distinct().Count());
            Assert.Equal(10.5, balances.Single(resource => resource.ResourceType == ExoticResourceTypeEnum.Cloth).Amount);
            Assert.Equal(17.5, balances.Single(resource => resource.ResourceType == ExoticResourceTypeEnum.Gold).Amount);
            Assert.Equal(0, balances.Single(resource => resource.ResourceType == ExoticResourceTypeEnum.Silver).Amount);
            Assert.Equal(0, balances.Single(resource => resource.ResourceType == ExoticResourceTypeEnum.Sulphur).Amount);

            var loadedCity = Assert.Single(await new CityRepository(verificationContext)
                .GetNPCsForBuildingAutomationAsync());
            Assert.Equal(cityId, loadedCity.Id);
            Assert.Equal(Enum.GetValues<ExoticResourceTypeEnum>().Length, loadedCity.ExoticResources.Count);

            var appliedMigrations = await verificationContext.Database.GetAppliedMigrationsAsync();
            Assert.Contains(RepairMigration, appliedMigrations);
            Assert.Equal(LatestMigration, appliedMigrations.Last());
        }
        finally
        {
            await using var cleanupContext = new GameContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    private static async Task<Guid> CreateIncompleteNpcCityAsync(DbContextOptions<GameContext> options)
    {
        await using var context = new GameContext(options);
        await context.GetService<IMigrator>().MigrateAsync(MigrationBeforeRepair);

        var world = new World
        {
            Id = Guid.NewGuid(),
            Name = "Migration test world",
            Abbrevation = "MT",
            Width = 100,
            Height = 100,
            MapSeed = 1234
        };
        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = "Incomplete NPC",
            IsNPC = true,
            World = world,
            WorldId = world.Id,
            X = 1,
            Y = 1,
            ExoticResources = Enum.GetValues<ExoticResourceTypeEnum>()
                .Where(type => type is not ExoticResourceTypeEnum.Silver and not ExoticResourceTypeEnum.Sulphur)
                .Select(type => new CityExoticResource
                {
                    Id = Guid.NewGuid(),
                    ResourceType = type,
                    Amount = type == ExoticResourceTypeEnum.Cloth ? 10.5
                        : type == ExoticResourceTypeEnum.Gold ? 17.5
                        : (int)type + 1
                })
                .ToList()
        };

        context.Cities.Add(city);
        await context.SaveChangesAsync();
        return city.Id;
    }
}
