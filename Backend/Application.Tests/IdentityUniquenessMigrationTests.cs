using Domain.User;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Application.Tests;

public class IdentityUniquenessMigrationTests
{
    private const string MigrationBeforeUniqueness = "20260719182549_RepairResearchLegacyUserIdColumn";

    [Fact]
    public async Task MigrationRejectsExistingCaseInsensitiveUserNameDuplicates()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string databaseName = $"RelicWarsIdentityUniquenessMigration_{Guid.NewGuid():N}";
        string connectionString =
            $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true";
        var options = new DbContextOptionsBuilder<GameContext>()
            .UseSqlServer(connectionString)
            .Options;

        try
        {
            await using (var setupContext = new GameContext(options))
            {
                await setupContext.GetService<IMigrator>().MigrateAsync(MigrationBeforeUniqueness);
                setupContext.PlayerProfiles.AddRange(
                    CreateLegacyProfile("Peter123", "first@example.test"),
                    CreateLegacyProfile("peter123", "second@example.test"));
                await setupContext.SaveChangesAsync();
            }

            await using var migrationContext = new GameContext(options);
            var exception = await Assert.ThrowsAnyAsync<Exception>(() => migrationContext.Database.MigrateAsync());

            Assert.Contains("case-insensitive duplicate usernames remain", exception.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await using var cleanupContext = new GameContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    private static PlayerProfile CreateLegacyProfile(string userName, string email) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
}
