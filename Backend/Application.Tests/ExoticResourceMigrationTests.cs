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
    private const string LatestMigration = "20260823150859_ReplaceResearchPointsWithResearchRate";

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
        Guid cityId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        context.World.Add(world);
        await context.SaveChangesAsync();

        // Seed against the historical schema without asking the current EF model to
        // write columns that did not exist yet at this migration boundary.
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [dbo].[Cities]
                ([Id], [Name], [Points], [Wood], [Stone], [Metal], [Population], [Resistance], [ResistanceTarget],
                 [LastResistanceUpdate], [LastResourceUpdate], [LastExoticResourceUpdate], [IsNPC], [X], [Y],
                 [WorldId], [WorldPlayerId], [ModifiersThatAffectsThis], [DateCreated], [DateLastModified], [IsDeleted])
            VALUES
                ({cityId}, {"Incomplete NPC"}, {0}, {0d}, {0d}, {0d}, {0}, {100d}, {100d},
                 {now}, {now}, {now}, {true}, {1}, {1}, {world.Id}, {null}, {"[]"}, {now}, {now}, {false})
            """);

        foreach (ExoticResourceTypeEnum type in Enum.GetValues<ExoticResourceTypeEnum>()
                     .Where(type => type is not ExoticResourceTypeEnum.Silver and not ExoticResourceTypeEnum.Sulphur))
        {
            double amount = type == ExoticResourceTypeEnum.Cloth ? 10.5
                : type == ExoticResourceTypeEnum.Gold ? 17.5
                : (int)type + 1;
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [dbo].[CityExoticResources]
                    ([Id], [CityId], [ResourceType], [Amount], [DateCreated], [DateLastModified], [IsDeleted])
                VALUES
                    ({Guid.NewGuid()}, {cityId}, {(int)type}, {amount}, {now}, {now}, {false})
                """);
        }

        return cityId;
    }
}
