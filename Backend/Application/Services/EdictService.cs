using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Readers;

namespace Application.Services;

public sealed class EdictService : IEdictService
{
    public static readonly TimeSpan ReenactCooldown = TimeSpan.FromHours(24);
    private readonly ICityRepository _cities;
    private readonly IPlayerAccessService _access;
    private readonly ITransactionManager _transactions;
    private readonly IResourceService _resources;
    private readonly IExoticResourceService _exotics;
    private readonly IResistanceService _resistance;
    private readonly EdictDataReader _definitions;
    private readonly TimeProvider _time;

    public EdictService(ICityRepository cities, IPlayerAccessService access, ITransactionManager transactions,
        IResourceService resources, IExoticResourceService exotics, IResistanceService resistance,
        EdictDataReader definitions, TimeProvider time)
    { _cities = cities; _access = access; _transactions = transactions; _resources = resources; _exotics = exotics; _resistance = resistance; _definitions = definitions; _time = time; }

    public async Task<EdictOverviewDTO> GetOverviewAsync(Guid cityId)
    {
        await _access.RequireOwnedCityForTownHallAsync(cityId);
        var city = await _cities.GetForEdictAsync(cityId) ?? throw new KeyNotFoundException("City was not found.");
        return BuildOverview(city, UtcNow());
    }

    public async Task<EdictOverviewDTO> EnactAsync(Guid cityId, EdictTypeEnum edictType)
    {
        if (!Enum.IsDefined(edictType)) throw new ArgumentOutOfRangeException(nameof(edictType));
        var authorized = await _access.RequireOwnedCityForTownHallAsync(cityId);
        if (!authorized.WorldPlayerId.HasValue) throw new KeyNotFoundException("City owner was not found.");
        return await _transactions.ExecuteAsync(async () =>
        {
            await _cities.AcquireEdictPlayerLockAsync(authorized.WorldPlayerId.Value);
            var city = await _cities.GetForEdictAsync(cityId) ?? throw new KeyNotFoundException("City was not found.");
            var now = UtcNow();
            Validate(city, edictType, now);
            await SynchronizeOldEdictAsync(city, now);
            city.ActiveEdict = edictType;
            city.EdictEnactedAtUtc = now;
            await _transactions.SaveChangesAsync();
            return BuildOverview(city, now);
        });
    }

    private void Validate(City city, EdictTypeEnum requested, DateTime now)
    {
        if (city.ActiveEdict == requested) throw new EdictConflictException("edict.already_active", "This edict is already active in the city.");
        if (city.EdictEnactedAtUtc is DateTime enacted && now < enacted + ReenactCooldown)
            throw new EdictConflictException("edict.cooldown", "The city cannot change edict until its cooldown ends.");
        var player = city.WorldPlayer ?? throw new KeyNotFoundException("City owner was not found.");
        var limit = UsageLimit(player.Cities.Count);
        if (player.Cities.Count(c => c.ActiveEdict == requested) >= limit)
            throw new EdictConflictException("edict.usage_limit", "This edict has reached its city usage limit.");
    }

    private async Task SynchronizeOldEdictAsync(City city, DateTime now)
    {
        var player = city.WorldPlayer ?? throw new KeyNotFoundException("City owner was not found.");
        var global = _resources.CalculateGlobalResources(player, now);
        player.Coins = global.CoinsAmount; player.IdeologyFocusPoints = global.IdeologyFocusPoints; player.LastResourceUpdate = now;
        var local = _resources.CalculateCityResources(city, now);
        city.Wood = local.Wood; city.Stone = local.Stone; city.Metal = local.Metal; city.LastResourceUpdate = now;
        await _exotics.SyncCityExoticResourcesAsync(city, now);
        _resistance.UpdateResistance(city, now);
    }

    private EdictOverviewDTO BuildOverview(City city, DateTime now)
    {
        var player = city.WorldPlayer ?? throw new KeyNotFoundException("City owner was not found.");
        var limit = UsageLimit(player.Cities.Count);
        var cooldown = city.EdictEnactedAtUtc?.Add(ReenactCooldown);
        var options = _definitions.GetAll().OrderBy(x => (int)x.EdictType).Select(def =>
        {
            var usage = player.Cities.Count(c => c.ActiveEdict == def.EdictType);
            var reason = city.ActiveEdict == def.EdictType ? EdictAvailabilityReasonEnum.AlreadyActive
                : cooldown > now ? EdictAvailabilityReasonEnum.Cooldown
                : usage >= limit ? EdictAvailabilityReasonEnum.UsageLimitReached
                : EdictAvailabilityReasonEnum.Available;
            return new EdictOptionDTO(def.EdictType, def.Name, def.BenefitDescription, def.DownsideDescription,
                def.BenefitImplemented, def.DownsideImplemented, usage, limit, reason == EdictAvailabilityReasonEnum.Available, reason);
        }).ToList();
        return new EdictOverviewDTO(city.Id, city.ActiveEdict, city.EdictEnactedAtUtc, cooldown, now, options);
    }

    private DateTime UtcNow() => _time.GetUtcNow().UtcDateTime;
    private static int UsageLimit(int cityCount) => Math.Max(1, (cityCount + 1) / 2);
}
