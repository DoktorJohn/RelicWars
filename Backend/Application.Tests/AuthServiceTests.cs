using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services.Authentication;
using Domain.User;
using Infrastructure.Context;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterNormalizesIdentityAndRejectsCaseInsensitiveDuplicates()
    {
        using var provider = CreateServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var created = await service.RegisterAsync(new RegisterRequest("  Peter123  ", "  Peter@example.test  ", "password"));
        var duplicateName = await service.RegisterAsync(new RegisterRequest("peter123", "other@example.test", "password"));
        var duplicateEmail = await service.RegisterAsync(new RegisterRequest("OtherPlayer", "PETER@EXAMPLE.TEST", "password"));

        Assert.True(created.IsAuthenticated);
        Assert.Equal("Peter123", created.Profile!.UserName);
        Assert.False(duplicateName.IsAuthenticated);
        Assert.Equal("Username is already in use.", duplicateName.FeedbackMessage);
        Assert.False(duplicateEmail.IsAuthenticated);
        Assert.Equal("Email is already in use.", duplicateEmail.FeedbackMessage);

        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        var stored = await context.PlayerProfiles.SingleAsync();
        Assert.Equal("PETER123", stored.NormalizedUserName);
        Assert.Equal("PETER@EXAMPLE.TEST", stored.NormalizedEmail);
        Assert.False(stored.PasswordHash!.StartsWith("$2", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("ab", "password")]
    [InlineData("Invalid Name", "password")]
    [InlineData("ValidName", "short")]
    public async Task RegisterRejectsInvalidUserNameOrPassword(string userName, string password)
    {
        using var provider = CreateServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var response = await service.RegisterAsync(new RegisterRequest(userName, "player@example.test", password));

        Assert.False(response.IsAuthenticated);
        Assert.Null(response.JwtToken);
    }

    [Fact]
    public async Task LoginLocksAccountAfterFiveFailedAttempts()
    {
        using var provider = CreateServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthService>();
        await service.RegisterAsync(new RegisterRequest("LockPlayer", "lock@example.test", "password"));

        for (int attempt = 0; attempt < 5; attempt++)
        {
            var failure = await service.LoginAsync(new LoginRequest("lock@example.test", "incorrect"));
            Assert.False(failure.IsAuthenticated);
        }

        var lockedResponse = await service.LoginAsync(new LoginRequest("lock@example.test", "password"));
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PlayerProfile>>();
        var profile = await userManager.FindByEmailAsync("lock@example.test");

        Assert.False(lockedResponse.IsAuthenticated);
        Assert.NotNull(profile);
        Assert.True(await userManager.IsLockedOutAsync(profile!));
        Assert.True(profile!.LockoutEnd > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task SuccessfulLoginResetsFailedAttemptCount()
    {
        using var provider = CreateServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthService>();
        await service.RegisterAsync(new RegisterRequest("ResetPlayer", "reset@example.test", "password"));
        await service.LoginAsync(new LoginRequest("reset@example.test", "incorrect"));
        await service.LoginAsync(new LoginRequest("reset@example.test", "incorrect"));

        var response = await service.LoginAsync(new LoginRequest("RESET@example.test", "password"));
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PlayerProfile>>();
        var profile = await userManager.FindByEmailAsync("reset@example.test");

        Assert.True(response.IsAuthenticated);
        Assert.NotNull(profile);
        Assert.Equal(0, profile!.AccessFailedCount);
    }

    [Fact]
    public async Task LoginUpgradesLegacyBcryptHash()
    {
        using var provider = CreateServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        var profile = new PlayerProfile
        {
            Id = Guid.NewGuid(),
            UserName = "Legacy Player!",
            NormalizedUserName = "LEGACY PLAYER!",
            Email = "legacy@example.test",
            NormalizedEmail = "LEGACY@EXAMPLE.TEST",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true
        };
        context.PlayerProfiles.Add(profile);
        await context.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var response = await service.LoginAsync(new LoginRequest("LEGACY@example.test", "password"));
        await context.Entry(profile).ReloadAsync();

        Assert.True(response.IsAuthenticated);
        Assert.False(profile.PasswordHash!.StartsWith("$2", StringComparison.Ordinal));
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<GameContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services
            .AddIdentityCore<PlayerProfile>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = null!;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<GameContext>();
        services.AddScoped<IPasswordHasher<PlayerProfile>, LegacyCompatiblePasswordHasher>();
        services.AddScoped<IPlayerProfileRepository, PlayerProfileRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IJwtService, FixedJwtService>();
        return services.BuildServiceProvider();
    }

    private sealed class FixedJwtService : IJwtService
    {
        public string GenerateToken(PlayerProfile user) => "test-token";
    }
}
