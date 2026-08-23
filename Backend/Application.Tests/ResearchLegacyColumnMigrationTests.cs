using Domain.Entities;
using Domain.User;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Data;

namespace Application.Tests;

public class ResearchLegacyColumnMigrationTests
{
    private const string MigrationBeforeRepair = "20260718135143_PreventDuplicateWorldParticipationAndCityCoordinates";
    private const string RepairMigration = "20260719182549_RepairResearchLegacyUserIdColumn";

    [Fact]
    public async Task RepairMigration_RemovesLegacyUserIdAndAllowsResearchCompletionInsert()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string databaseName = $"RelicWarsResearchMigration_{Guid.NewGuid():N}";
        string connectionString =
            $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true";
        var options = new DbContextOptionsBuilder<GameContext>()
            .UseSqlServer(connectionString)
            .Options;

        try
        {
            Guid worldPlayerId = await CreatePlayerAndLegacyResearchAsync(options);

            await using (var migrationContext = new GameContext(options))
            {
                await migrationContext.Database.MigrateAsync();
                Assert.Null(await GetColumnLengthAsync(migrationContext, "UserId"));

                await migrationContext.GetService<IMigrator>().MigrateAsync(MigrationBeforeRepair);
                Assert.Equal(16, await GetColumnLengthAsync(migrationContext, "UserId"));

                await migrationContext.Database.MigrateAsync();
                Assert.Null(await GetColumnLengthAsync(migrationContext, "UserId"));
            }

            await using var verificationContext = new GameContext(options);
            var existingResearch = await verificationContext.Researches
                .SingleAsync(research => research.WorldPlayerId == worldPlayerId);
            Assert.Equal("LEGACY_RESEARCH", existingResearch.ResearchId);

            verificationContext.Researches.Add(new Research
            {
                Id = Guid.NewGuid(),
                WorldPlayerId = worldPlayerId,
                ResearchId = "WORKER_COMPLETION_RESEARCH",
                CompletedAt = DateTime.UtcNow
            });
            await verificationContext.SaveChangesAsync();

            Assert.Equal(2, await verificationContext.Researches
                .CountAsync(research => research.WorldPlayerId == worldPlayerId));
            Assert.Contains(RepairMigration, await verificationContext.Database.GetAppliedMigrationsAsync());
        }
        finally
        {
            await using var cleanupContext = new GameContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    private static async Task<Guid> CreatePlayerAndLegacyResearchAsync(DbContextOptions<GameContext> options)
    {
        await using var context = new GameContext(options);
        await context.GetService<IMigrator>().MigrateAsync(MigrationBeforeRepair);

        // The current model no longer maps ResearchPoints, while this historical
        // schema still requires it. A temporary default keeps this migration fixture
        // writable and is removed automatically when the column is later dropped.
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE [dbo].[WorldPlayers]
            ADD CONSTRAINT [DF_Test_WorldPlayers_ResearchPoints] DEFAULT (0.0) FOR [ResearchPoints]
            """);

        var profile = new PlayerProfile
        {
            Id = Guid.NewGuid(),
            UserName = "research-migration-test",
            Email = "research-migration@example.test",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
        var world = new World
        {
            Id = Guid.NewGuid(),
            Name = "Research migration test world",
            Abbrevation = "RMT",
            Width = 100,
            Height = 100,
            MapSeed = 1234
        };
        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            PlayerProfile = profile,
            PlayerProfileId = profile.Id,
            World = world,
            WorldId = world.Id
        };

        context.WorldPlayers.Add(player);
        await context.SaveChangesAsync();

        Guid researchId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [dbo].[Researches]
                ([Id], [UserId], [ResearchId], [CompletedAt], [WorldPlayerId], [DateCreated], [DateLastModified], [IsDeleted])
            VALUES
                ({researchId}, {player.Id}, {"LEGACY_RESEARCH"}, {now}, {player.Id}, {now}, {now}, {false})
            """);

        return player.Id;
    }

    private static async Task<int?> GetColumnLengthAsync(GameContext context, string columnName)
    {
        var connection = context.Database.GetDbConnection();
        bool closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COL_LENGTH(N'dbo.Researches', @columnName)";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@columnName";
            parameter.Value = columnName;
            command.Parameters.Add(parameter);

            object? value = await command.ExecuteScalarAsync();
            return value == DBNull.Value ? null : Convert.ToInt32(value);
        }
        finally
        {
            if (closeConnection)
            {
                await connection.CloseAsync();
            }
        }
    }
}
