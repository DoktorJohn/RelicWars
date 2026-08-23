using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.User;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public sealed class DailyObjectiveService : IDailyObjectiveService
    {
        private readonly IDailyObjectiveRepository _repository;
        private readonly IPlayerAccessService _playerAccessService;
        private readonly DailyObjectiveDataReader _reader;
        private readonly IRandomService _random;
        private readonly TimeProvider _timeProvider;
        private readonly ITransactionManager _transactionManager;
        private readonly UnitDataReader _unitDataReader;
        private readonly IResourceService _resourceService;
        private readonly ICityStatService _cityStatService;

        public DailyObjectiveService(
            IDailyObjectiveRepository repository,
            IPlayerAccessService playerAccessService,
            DailyObjectiveDataReader reader,
            IRandomService random,
            TimeProvider timeProvider,
            ITransactionManager transactionManager,
            UnitDataReader unitDataReader,
            IResourceService resourceService,
            ICityStatService cityStatService)
        {
            _repository = repository;
            _playerAccessService = playerAccessService;
            _reader = reader;
            _random = random;
            _timeProvider = timeProvider;
            _transactionManager = transactionManager;
            _unitDataReader = unitDataReader;
            _resourceService = resourceService;
            _cityStatService = cityStatService;
        }

        public async Task<DailyObjectivesDTO> GetAsync(Guid worldPlayerId)
        {
            await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);
            var set = await GetOrCreateTodayAsync(worldPlayerId);
            return Map(set);
        }

        public async Task<DailyObjectivesDTO> CollectAsync(Guid worldPlayerId, int definitionId, Guid cityId)
        {
            var player = await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);
            return await _transactionManager.ExecuteAsync(async () =>
            {
                var set = await EnsureTodayInCurrentTransactionAsync(worldPlayerId);
                var assignment = set.Assignments.SingleOrDefault(x => x.DefinitionId == definitionId)
                    ?? throw new KeyNotFoundException($"Daily objective {definitionId} is not assigned today.");
                if (assignment.IsCollected) return Map(set);
                if (!assignment.IsComplete)
                    throw new InvalidOperationException("Only completed daily objectives can be collected.");

                var city = player.Cities.SingleOrDefault(candidate =>
                    candidate.Id == cityId && candidate.WorldPlayerId == worldPlayerId)
                    ?? throw new InvalidOperationException("The selected city does not belong to this world player.");
                DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
                SynchronizeResources(player, city, now);
                AwardRewards(player, city, _reader.GetDefinition(definitionId).Rewards);
                assignment.IsCollected = true;
                assignment.DateLastModified = now;
                await _transactionManager.SaveChangesAsync();
                return Map(set);
            });
        }

        public async Task ApplyProgressAsync(Guid worldPlayerId, DailyObjectiveProgressEvent progressEvent)
        {
            if (progressEvent.Amount <= 0) return;
            bool saveWithinOperation = !_transactionManager.HasActiveTransaction;
            int maximumAttempts = saveWithinOperation ? 2 : 1;

            for (int attempt = 1; attempt <= maximumAttempts; attempt++)
            {
                try
                {
                    await _transactionManager.ExecuteAsync(async () =>
                    {
                        var set = await EnsureTodayInCurrentTransactionAsync(worldPlayerId);
                        DateTime occurredAt = AsUtc(progressEvent.OccurredAtUtc);
                        if (occurredAt < set.DayStartUtc || occurredAt >= set.DayStartUtc.AddDays(1)) return;

                        foreach (var assignment in set.Assignments)
                        {
                            var definition = _reader.GetDefinition(assignment.DefinitionId);
                            if (!definition.IsImplemented || assignment.IsComplete || !Matches(definition, progressEvent)) continue;
                            assignment.Progress = Math.Min(assignment.Target, assignment.Progress + progressEvent.Amount);
                            assignment.IsComplete = assignment.Progress >= assignment.Target;
                            assignment.DateLastModified = _timeProvider.GetUtcNow().UtcDateTime;
                        }

                        if (saveWithinOperation) await _transactionManager.SaveChangesAsync();
                    });
                    return;
                }
                catch (DbUpdateConcurrencyException exception) when (
                    attempt < maximumAttempts && IsDailyObjectiveConcurrency(exception))
                {
                    // The next attempt detaches daily state after reacquiring the player lock.
                }
            }
        }

        public async Task ApplyProductionAsync(
            Guid worldPlayerId,
            DateTime intervalStartUtc,
            DateTime intervalEndUtc,
            double coinsPerHour = 0,
            double exoticResourcesPerHour = 0)
        {
            DateTime dayStart = _timeProvider.GetUtcNow().UtcDateTime.Date;
            DateTime start = AsUtc(intervalStartUtc) > dayStart ? AsUtc(intervalStartUtc) : dayStart;
            DateTime reset = dayStart.AddDays(1);
            DateTime end = AsUtc(intervalEndUtc) < reset ? AsUtc(intervalEndUtc) : reset;
            double hours = Math.Max(0, (end - start).TotalHours);
            if (hours <= 0) return;
            if (coinsPerHour > 0)
                await ApplyProgressAsync(worldPlayerId,
                    new(DailyObjectiveProgressTypeEnum.CoinsProduced, coinsPerHour * hours, end.AddTicks(-1)));
            if (exoticResourcesPerHour > 0)
                await ApplyProgressAsync(worldPlayerId,
                    new(DailyObjectiveProgressTypeEnum.ExoticResourcesProduced, exoticResourcesPerHour * hours, end.AddTicks(-1)));
        }

        public IReadOnlyList<DailyObjectiveDefinitionData> SelectDefinitions()
        {
            var fixedPool = _reader.Catalog.Definitions.Where(x => x.Tier == DailyObjectiveTierEnum.Fixed).ToList();
            var selected = DrawWithoutReplacement(fixedPool, _reader.Catalog.Selection.FixedSlots);
            var weightedPool = _reader.Catalog.Definitions.Where(x => x.Tier != DailyObjectiveTierEnum.Fixed).ToList();

            for (int slot = 0; slot < _reader.Catalog.Selection.WeightedSlots; slot++)
            {
                var availableTiers = weightedPool.Select(x => x.Tier).Distinct().ToList();
                int totalWeight = availableTiers.Sum(tier => _reader.Catalog.Selection.Weights[tier]);
                int roll = _random.Next(totalWeight);
                DailyObjectiveTierEnum selectedTier = availableTiers[0];
                foreach (var tier in availableTiers)
                {
                    int weight = _reader.Catalog.Selection.Weights[tier];
                    if (roll < weight)
                    {
                        selectedTier = tier;
                        break;
                    }
                    roll -= weight;
                }

                var tierPool = weightedPool.Where(x => x.Tier == selectedTier).ToList();
                var definition = tierPool[_random.Next(tierPool.Count)];
                selected.Add(definition);
                weightedPool.Remove(definition);
            }

            return selected;
        }

        private async Task<DailyObjectiveSet> GetOrCreateTodayAsync(Guid worldPlayerId)
        {
            bool saveWithinOperation = !_transactionManager.HasActiveTransaction;
            int maximumAttempts = saveWithinOperation ? 2 : 1;

            for (int attempt = 1; attempt <= maximumAttempts; attempt++)
            {
                try
                {
                    return await _transactionManager.ExecuteAsync(async () =>
                    {
                        var set = await EnsureTodayInCurrentTransactionAsync(worldPlayerId);
                        if (saveWithinOperation) await _transactionManager.SaveChangesAsync();
                        return set;
                    });
                }
                catch (DbUpdateConcurrencyException exception) when (
                    attempt < maximumAttempts && IsDailyObjectiveConcurrency(exception))
                {
                    // The next attempt detaches daily state after reacquiring the player lock.
                }
            }

            throw new InvalidOperationException("Daily objective retry loop completed without a result.");
        }

        private async Task<DailyObjectiveSet> EnsureTodayInCurrentTransactionAsync(Guid worldPlayerId)
        {
            await _repository.AcquirePlayerLockAsync(worldPlayerId);
            _repository.ResetTrackedState(worldPlayerId);
            DateTime dayStart = _timeProvider.GetUtcNow().UtcDateTime.Date;
            var existing = await _repository.GetByWorldPlayerIdAsync(worldPlayerId);
            if (existing?.DayStartUtc == dayStart) return existing;

            var definitions = SelectDefinitions();
            var replacement = new DailyObjectiveSet
            {
                Id = Guid.NewGuid(),
                WorldPlayerId = worldPlayerId,
                DayStartUtc = dayStart,
                Assignments = definitions.Select((definition, index) => new DailyObjectiveAssignment
                {
                    Id = Guid.NewGuid(),
                    DefinitionId = definition.Id,
                    Slot = index + 1,
                    Target = definition.Target
                }).ToList()
            };
            return await _repository.ReplaceAsync(existing, replacement);
        }

        private DailyObjectivesDTO Map(DailyObjectiveSet set) => new(
            DateTime.SpecifyKind(set.DayStartUtc.Date, DateTimeKind.Utc),
            DateTime.SpecifyKind(set.DayStartUtc.Date.AddDays(1), DateTimeKind.Utc),
            set.Assignments.OrderBy(x => x.Slot).Select(assignment =>
            {
                var definition = _reader.GetDefinition(assignment.DefinitionId);
                var state = !definition.IsImplemented
                    ? DailyObjectiveStateEnum.ComingSoon
                    : assignment.IsComplete ? DailyObjectiveStateEnum.Complete : DailyObjectiveStateEnum.InProgress;
                return new DailyObjectiveRowDTO(
                    assignment.Slot,
                    assignment.DefinitionId,
                    definition.Name,
                    definition.CompletionInfo,
                    definition.Tier,
                    definition.Rewards.Select(reward =>
                        new DailyObjectiveRewardDTO(reward.Type, reward.Amount)).ToList(),
                    assignment.Progress,
                    assignment.Target,
                    assignment.IsComplete,
                    assignment.IsCollected,
                    state);
            }).ToList());

        private void SynchronizeResources(WorldPlayer player, City city, DateTime now)
        {
            var global = _resourceService.CalculateGlobalResources(player, now);
            player.Coins = global.CoinsAmount;
            player.IdeologyFocusPoints = global.IdeologyFocusPoints;
            player.LastResourceUpdate = global.Timestamp;

            var local = _resourceService.CalculateCityResources(city, now);
            city.Wood = local.Wood;
            city.Stone = local.Stone;
            city.Metal = local.Metal;
            city.LastResourceUpdate = local.Timestamp;
        }

        private void AwardRewards(
            WorldPlayer player,
            City city,
            IReadOnlyCollection<DailyObjectiveRewardData> rewards)
        {
            double capacity = _cityStatService.GetWarehouseCapacity(city);
            foreach (var reward in rewards)
            {
                switch (reward.Type)
                {
                    case DailyObjectiveRewardTypeEnum.Coins:
                        player.Coins += reward.Amount;
                        break;
                    case DailyObjectiveRewardTypeEnum.Wood:
                        city.Wood = Math.Min(capacity, city.Wood + reward.Amount);
                        break;
                    case DailyObjectiveRewardTypeEnum.Stone:
                        city.Stone = Math.Min(capacity, city.Stone + reward.Amount);
                        break;
                    case DailyObjectiveRewardTypeEnum.Metal:
                        city.Metal = Math.Min(capacity, city.Metal + reward.Amount);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported daily reward type {reward.Type}.");
                }
            }
        }

        private bool Matches(DailyObjectiveDefinitionData definition, DailyObjectiveProgressEvent progressEvent)
        {
            var unitData = progressEvent.UnitType.HasValue ? _unitDataReader.GetUnit(progressEvent.UnitType.Value) : null;
            if (definition.ProgressType == progressEvent.ProgressType)
                return (!definition.UnitType.HasValue || definition.UnitType == progressEvent.UnitType) &&
                       (!definition.UnitCategory.HasValue || definition.UnitCategory == unitData?.Category) &&
                       (!definition.RequiresElite || unitData?.IsElite == true);

            return progressEvent.ProgressType == DailyObjectiveProgressTypeEnum.UnitsRecruited &&
                   ((definition.ProgressType == DailyObjectiveProgressTypeEnum.EliteUnitsRecruited && unitData?.IsElite == true) ||
                    (definition.ProgressType == DailyObjectiveProgressTypeEnum.UnitTypeRecruited && definition.UnitType == progressEvent.UnitType)) ||
                   progressEvent.ProgressType == DailyObjectiveProgressTypeEnum.EnemyUnitsKilled &&
                   definition.ProgressType == DailyObjectiveProgressTypeEnum.NavalUnitsKilled &&
                   unitData?.Category == UnitCategoryEnum.Naval;
        }

        private List<DailyObjectiveDefinitionData> DrawWithoutReplacement(List<DailyObjectiveDefinitionData> pool, int count)
        {
            var selected = new List<DailyObjectiveDefinitionData>(count);
            for (int index = 0; index < count; index++)
            {
                int selectedIndex = _random.Next(pool.Count);
                selected.Add(pool[selectedIndex]);
                pool.RemoveAt(selectedIndex);
            }
            return selected;
        }

        private static DateTime AsUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        private static bool IsDailyObjectiveConcurrency(DbUpdateConcurrencyException exception) =>
            exception.Entries.Count > 0 && exception.Entries.All(entry =>
                entry.Entity is DailyObjectiveSet or DailyObjectiveAssignment);
    }
}
