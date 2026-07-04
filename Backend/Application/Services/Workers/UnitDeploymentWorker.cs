using Application.Interfaces;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.User;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Workers
{
    public class UnitDeploymentWorker
    {
        private const int BatchSize = 100;
        private readonly IUnitDeploymentRepository _unitDeploymentRepo;
        private readonly ICityRepository _cityRepo;
        private readonly CombatService _combatService;
        private readonly IBattleReportRepository _reportRepo;
        private readonly ICityStatService _statService;
        private readonly UnitMovementCalculator _movementCalculator;
        private readonly ILogger<UnitDeploymentWorker> _logger;
        private readonly IIdeologyFocusRepository _ideologyFocusRepository;
        private readonly ITransactionManager _transactionManager;
        private readonly IDeploymentPermissionService _permissionService;

        public UnitDeploymentWorker(
            IUnitDeploymentRepository unitDeploymentRepo,
            ICityRepository cityRepo,
            CombatService combatService,
            IBattleReportRepository reportRepo,
            ICityStatService statService,
            UnitMovementCalculator movementCalculator,
            ILogger<UnitDeploymentWorker> logger,
            IIdeologyFocusRepository ideologyFocusRepository,
            ITransactionManager transactionManager,
            IDeploymentPermissionService permissionService)
        {
            _unitDeploymentRepo = unitDeploymentRepo;
            _cityRepo = cityRepo;
            _combatService = combatService;
            _reportRepo = reportRepo;
            _statService = statService;
            _movementCalculator = movementCalculator;
            _logger = logger;
            _ideologyFocusRepository = ideologyFocusRepository;
            _transactionManager = transactionManager;
            _permissionService = permissionService;
        }

        public async Task ProcessMilitaryMovementsAsync()
        {
            await ValidateStationedSupportAsync();
            var dueMovements = await _unitDeploymentRepo.GetDueMovementsAsync(DateTime.UtcNow, BatchSize);
            if (!dueMovements.Any())
            {
                return;
            }

            foreach (var deployment in dueMovements)
            {
                try
                {
                    await _transactionManager.ExecuteAsync(async () =>
                    {
                        var originCity = deployment.OriginCity ?? await _cityRepo.GetByIdAsync(deployment.OriginCityId);
                        if (originCity == null)
                        {
                            _logger.LogWarning("UnitDeployment {DeploymentId} mangler origin city. Fjerner deployment.", deployment.Id);
                            await CleanupUnitDeploymentAsync(deployment);
                            return;
                        }

                        if (deployment.Phase == UnitDeploymentPhaseEnum.Returning)
                        {
                            await CompleteReturnAsync(deployment, originCity);
                            return;
                        }

                        await ResolveOutboundArrivalAsync(deployment, originCity);
                    });
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Fejl ved processering af bevægelse for UnitDeployment: {DeploymentId}", deployment.Id);
                }
            }
        }

        private async Task ValidateStationedSupportAsync()
        {
            var supports = await _unitDeploymentRepo.GetStationedSupportAsync(BatchSize);
            foreach (var support in supports)
            {
                if (support.TargetCity != null && support.OwnerWorldPlayer != null &&
                    await _permissionService.CanSupportAsync(support.OwnerWorldPlayer, support.TargetCity))
                {
                    continue;
                }

                await _transactionManager.ExecuteAsync(() => StartReturnTripAsync(support, support.OriginCity));
            }
        }

        private async Task ResolveOutboundArrivalAsync(UnitDeployment deployment, City originCity)
        {
            var targetCity = deployment.TargetCity ?? await _cityRepo.GetByIdAsync(deployment.TargetCityId ?? Guid.Empty);
            var sourcePlayer = deployment.OwnerWorldPlayer;

            if (deployment.Type == UnitDeploymentTypeEnum.Support)
            {
                if (targetCity == null || sourcePlayer == null || !await _permissionService.CanSupportAsync(sourcePlayer, targetCity))
                {
                    await StartReturnTripAsync(deployment, originCity);
                    return;
                }

                deployment.Phase = UnitDeploymentPhaseEnum.Stationed;
                deployment.UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Stationed;
                deployment.StationedAt = DateTime.UtcNow;
                await _unitDeploymentRepo.UpdateAsync(deployment);
                return;
            }

            if (deployment.Type != UnitDeploymentTypeEnum.Attack || targetCity == null || sourcePlayer == null || !_permissionService.CanAttack(sourcePlayer, targetCity))
            {
                _logger.LogWarning("UnitDeployment {DeploymentId} mistede target city. Starter retur uden kamp.", deployment.Id);
                await StartReturnTripAsync(deployment, originCity);
                return;
            }

            var supports = await _unitDeploymentRepo.GetStationedSupportByTargetCityIdAsync(targetCity.Id);
            var validSupports = new List<UnitDeployment>();
            foreach (var support in supports)
            {
                if (support.OwnerWorldPlayer != null && await _permissionService.CanSupportAsync(support.OwnerWorldPlayer, targetCity))
                {
                    validSupports.Add(support);
                }
                else
                {
                    await StartReturnTripAsync(support, support.OriginCity);
                }
            }

            if (targetCity.UnitStacks.Any(stack => stack.Quantity > 0) || validSupports.Any(s => s.UnitStacks.Any(stack => stack.Quantity > 0)))
            {
                bool attackerDestroyed = await HandleCityCombatAsync(deployment, originCity, targetCity, validSupports);
                if (attackerDestroyed)
                {
                    await CleanupUnitDeploymentAsync(deployment);
                    return;
                }
            }
            else
            {
                await CreateUnopposedAttackReportsAsync(deployment, targetCity);
            }

            await StartReturnTripAsync(deployment, originCity);
        }

        private async Task<bool> HandleCityCombatAsync(UnitDeployment attacker, City originCity, City targetCity, List<UnitDeployment> supports)
        {
            var defenderStacks = targetCity.UnitStacks
                .Concat(supports.SelectMany(support => support.UnitStacks))
                .Where(stack => stack.Quantity > 0)
                .ToList();
            var before = defenderStacks.ToDictionary(stack => stack, stack => stack.Quantity);
            var participatingSupportOwnerIds = supports
                .Where(support => support.UnitStacks.Any(stack => before.TryGetValue(stack, out int quantity) && quantity > 0))
                .Select(support => support.WorldPlayerId)
                .Distinct()
                .Where(id => id != targetCity.WorldPlayerId)
                .ToList();
            var result = _combatService.ResolveBattle(new CombatContext(
                attacker.UnitStacks,
                defenderStacks,
                originCity,
                targetCity,
                attacker.OwnerWorldPlayer,
                targetCity.WorldPlayer));

            RedistributeRevival(defenderStacks, before, result.RevivedDefenders, targetCity, supports);

            if (result.RevivedDefenders.Any())
            {
                var royalMedics = targetCity.ActiveFocuses.FirstOrDefault(x =>
                    x.Name == IdeologyFocusNameEnum.RoyalMedics && x.IsActive);
                if (royalMedics != null)
                {
                    await _ideologyFocusRepository.DeleteAsync(royalMedics);
                }
            }

            await _cityRepo.UpdateAsync(targetCity);
            foreach (var support in supports)
            {
                if (support.UnitStacks.Any(stack => stack.Quantity > 0))
                {
                    await _unitDeploymentRepo.UpdateAsync(support);
                }
                else
                {
                    await _unitDeploymentRepo.DeleteAsync(support);
                }
            }
            var occurredAt = DateTime.UtcNow;
            var attackerLossesJson = JsonSerializer.Serialize(result.AttackerLosses.Select(x => new { x.Type, x.Quantity }));
            var defenderLossesJson = JsonSerializer.Serialize(result.DefenderLosses.Select(x => new { x.Type, x.Quantity }));
            var revivedUnitsJson = JsonSerializer.Serialize(result.RevivedDefenders.Select(x => new { x.Type, x.Quantity }));
            var appliedModifiersJson = JsonSerializer.Serialize(result.AppliedModifiers);

            await _reportRepo.AddAsync(new BattleReport
            {
                Id = Guid.NewGuid(),
                WorldPlayerId = attacker.WorldPlayerId,
                ReportType = ReportTypeEnum.Attack,
                Title = $"Angreb ved {targetCity.Name}",
                Body = $"Attacker losses: {result.AttackerLosses.Sum(x => x.Quantity)}. Defender losses: {result.DefenderLosses.Sum(x => x.Quantity)}. Revived: {result.RevivedDefenders.Sum(x => x.Quantity)}.",
                OccurredAt = occurredAt,
                AttackerLossesJson = attackerLossesJson,
                DefenderLossesJson = defenderLossesJson,
                RevivedUnitsJson = revivedUnitsJson,
                AppliedModifiersJson = appliedModifiersJson
            });

            if (targetCity.WorldPlayerId.HasValue)
            {
                await _reportRepo.AddAsync(new BattleReport
                {
                    Id = Guid.NewGuid(),
                    WorldPlayerId = targetCity.WorldPlayerId.Value,
                    ReportType = ReportTypeEnum.CityAttacked,
                    Title = $"Du blev angrebet ved {targetCity.Name}",
                    Body = $"Defender losses: {result.DefenderLosses.Sum(x => x.Quantity)}. Attacker losses: {result.AttackerLosses.Sum(x => x.Quantity)}. Revived: {result.RevivedDefenders.Sum(x => x.Quantity)}.",
                    OccurredAt = occurredAt,
                    AttackerLossesJson = attackerLossesJson,
                    DefenderLossesJson = defenderLossesJson,
                    RevivedUnitsJson = revivedUnitsJson,
                    AppliedModifiersJson = appliedModifiersJson
                });
            }


            foreach (var supportOwnerId in participatingSupportOwnerIds)
            {
                await _reportRepo.AddAsync(new BattleReport
                {
                    Id = Guid.NewGuid(), WorldPlayerId = supportOwnerId, ReportType = ReportTypeEnum.SupportingUnitsAttacked,
                    Title = $"Dine supporting units i {targetCity.Name} blev angrebet", Body = $"Combined defender losses: {result.DefenderLosses.Sum(x => x.Quantity)}. Revived: {result.RevivedDefenders.Sum(x => x.Quantity)}.",
                    OccurredAt = occurredAt, AttackerLossesJson = attackerLossesJson, DefenderLossesJson = defenderLossesJson,
                    RevivedUnitsJson = revivedUnitsJson, AppliedModifiersJson = appliedModifiersJson
                });
            }

            bool destroyed = !attacker.UnitStacks.Any(x => x.Quantity > 0);
            _logger.LogInformation("Battle resolved for {DeploymentId} at {CityName}. Destroyed: {Destroyed}", attacker.Id, targetCity.Name, destroyed);
            return destroyed;
        }

        private async Task CreateUnopposedAttackReportsAsync(UnitDeployment attacker, City targetCity)
        {
            var occurredAt = DateTime.UtcNow;
            await _reportRepo.AddAsync(new BattleReport
            {
                Id = Guid.NewGuid(), WorldPlayerId = attacker.WorldPlayerId, ReportType = ReportTypeEnum.Attack,
                Title = $"Angreb ved {targetCity.Name}", Body = "The attack met no defending units.", OccurredAt = occurredAt
            });
            if (targetCity.WorldPlayerId.HasValue)
            {
                await _reportRepo.AddAsync(new BattleReport
                {
                    Id = Guid.NewGuid(), WorldPlayerId = targetCity.WorldPlayerId.Value, ReportType = ReportTypeEnum.CityAttacked,
                    Title = $"Du blev angrebet ved {targetCity.Name}", Body = "The attack met no defending units.", OccurredAt = occurredAt
                });
            }
        }

        private async Task StartReturnTripAsync(UnitDeployment deployment, City originCity)
        {
            double travelSeconds = _movementCalculator.CalculateTravelSeconds(
                deployment.LegEndX, deployment.LegEndY, originCity.X, originCity.Y, deployment.Mobility);
            deployment.Phase = UnitDeploymentPhaseEnum.Returning;
            deployment.UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Moving;
            deployment.StationedAt = null;
            deployment.DepartureTime = DateTime.UtcNow;
            deployment.LegStartX = deployment.LegEndX;
            deployment.LegStartY = deployment.LegEndY;
            deployment.LegEndX = originCity.X;
            deployment.LegEndY = originCity.Y;
            deployment.ArrivalTime = deployment.DepartureTime.AddSeconds(travelSeconds);

            await _unitDeploymentRepo.UpdateAsync(deployment);
            _logger.LogInformation("Unit {DeploymentId} starter returrejse mod {CityName}. Forventet ankomst: {ArrivalTime}", deployment.Id, originCity.Name, deployment.ArrivalTime);
        }

        private async Task CompleteReturnAsync(UnitDeployment deployment, City originCity)
        {
            originCity = await _cityRepo.GetByIdAsync(originCity.Id) ?? originCity;
            _logger.LogInformation("Unit {DeploymentId} returneret til {CityName}. Starter afrustning.", deployment.Id, originCity.Name);

            double warehouseCapacity = _statService.GetWarehouseCapacity(originCity);
            originCity.Wood = Math.Min(warehouseCapacity, originCity.Wood + deployment.LootWood);
            originCity.Stone = Math.Min(warehouseCapacity, originCity.Stone + deployment.LootStone);
            originCity.Metal = Math.Min(warehouseCapacity, originCity.Metal + deployment.LootMetal);

            foreach (var deploymentStack in deployment.UnitStacks)
            {
                var existingCityStack = originCity.UnitStacks.FirstOrDefault(stack => stack.Type == deploymentStack.Type);
                if (existingCityStack != null)
                {
                    existingCityStack.Quantity += deploymentStack.Quantity;
                }
                else
                {
                    originCity.UnitStacks.Add(new UnitStack
                    {
                        Id = Guid.NewGuid(),
                        Type = deploymentStack.Type,
                        Quantity = deploymentStack.Quantity,
                        WorldPlayerId = originCity.WorldPlayerId,
                        CityId = originCity.Id
                    });
                }
            }

            await _cityRepo.UpdateAsync(originCity);
            if (deployment.Type == UnitDeploymentTypeEnum.Support)
            {
                await _reportRepo.AddAsync(new BattleReport
                {
                    Id = Guid.NewGuid(),
                    WorldPlayerId = deployment.WorldPlayerId,
                    ReportType = ReportTypeEnum.SupportingUnitsReturned,
                    Title = "Dine supporting units er hjemkommet",
                    Body = $"Dine supporting units er hjemkommet til {originCity.Name}.",
                    OccurredAt = DateTime.UtcNow
                });
            }
            await CleanupUnitDeploymentAsync(deployment);
            _logger.LogInformation("Unit {DeploymentId} færdigbehandlet og fjernet fra kortet.", deployment.Id);
        }

        private async Task CleanupUnitDeploymentAsync(UnitDeployment deployment)
        {
            await _unitDeploymentRepo.DeleteAsync(deployment);
        }

        private static void RedistributeRevival(
            List<UnitStack> stacks,
            Dictionary<UnitStack, int> before,
            List<UnitStack> revived,
            City city,
            List<UnitDeployment> supports)
        {
            foreach (var revival in revived)
            {
                int remainingToRemove = revival.Quantity;
                foreach (var stack in stacks.Where(s => s.Type == revival.Type))
                {
                    int removable = Math.Min(stack.Quantity, remainingToRemove);
                    stack.Quantity -= removable;
                    remainingToRemove -= removable;
                    if (remainingToRemove == 0) break;
                }

                var losses = stacks.Where(s => s.Type == revival.Type)
                    .Select(s => new { Stack = s, Loss = Math.Max(0, before[s] - s.Quantity), Owner = s.CityId == city.Id ? city.WorldPlayerId ?? Guid.Empty : s.WorldPlayerId ?? Guid.Empty, Deployment = s.UnitDeploymentId ?? Guid.Empty })
                    .Where(x => x.Loss > 0)
                    .OrderBy(x => x.Owner).ThenBy(x => x.Deployment).ToList();
                int gross = losses.Sum(x => x.Loss);
                int allocated = 0;
                foreach (var loss in losses)
                {
                    int quantity = gross == 0 ? 0 : (revival.Quantity * loss.Loss) / gross;
                    loss.Stack.Quantity += quantity;
                    allocated += quantity;
                }
                foreach (var loss in losses)
                {
                    if (allocated++ >= revival.Quantity) break;
                    loss.Stack.Quantity++;
                }
            }
        }
    }
}
