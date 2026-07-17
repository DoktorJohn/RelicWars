using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class UnitDeploymentService : IUnitDeploymentService
    {
        private readonly ILogger<UnitDeploymentService> _logger;
        private readonly IUnitDeploymentRepository _unitDeploymentRepo;
        private readonly IPlayerAccessService _playerAccessService;
        private readonly ICityRepository _cityRepo;
        private readonly UnitDataReader _unitDataReader;
        private readonly DeploymentModifierSnapshotService _snapshotService;
        private readonly UnitMovementCalculator _movementCalculator;
        private readonly DeploymentTransportCapacityService _transportCapacityService;
        private readonly ITransactionManager _transactionManager;
        private readonly IDeploymentPermissionService _permissionService;
        private readonly IBattleReportRepository? _battleReportRepository;

        public UnitDeploymentService(
            ILogger<UnitDeploymentService> logger,
            IUnitDeploymentRepository unitDeploymentRepo,
            IPlayerAccessService playerAccessService,
            ICityRepository cityRepo,
            UnitDataReader unitDataReader,
            DeploymentModifierSnapshotService snapshotService,
            UnitMovementCalculator movementCalculator,
            DeploymentTransportCapacityService transportCapacityService,
            ITransactionManager transactionManager,
            IDeploymentPermissionService permissionService,
            IBattleReportRepository? battleReportRepository = null)
        {
            _logger = logger;
            _unitDeploymentRepo = unitDeploymentRepo;
            _playerAccessService = playerAccessService;
            _cityRepo = cityRepo;
            _unitDataReader = unitDataReader;
            _snapshotService = snapshotService;
            _movementCalculator = movementCalculator;
            _transportCapacityService = transportCapacityService;
            _transactionManager = transactionManager;
            _permissionService = permissionService;
            _battleReportRepository = battleReportRepository;
        }

        public Task<OwnedUnitDeploymentDTO> AttackCityDeploymentAsync(AttackCityDeploymentRequestDTO dto) =>
            CreateDeploymentAsync(dto.OriginCityId, dto.TargetCityId, dto.UnitsToDeploy, UnitDeploymentTypeEnum.Attack);

        public Task<OwnedUnitDeploymentDTO> SupportCityDeploymentAsync(SupportCityDeploymentRequestDTO dto) =>
            CreateDeploymentAsync(dto.OriginCityId, dto.TargetCityId, dto.UnitsToDeploy, UnitDeploymentTypeEnum.Support);

        public async Task<DeploymentTravelEstimateDTO> EstimateTravelAsync(DeploymentTravelEstimateRequestDTO dto)
        {
            var sourceCity = await _playerAccessService.RequireOwnedCityAsync(dto.OriginCityId);
            var targetCity = await _cityRepo.GetByIdAsync(dto.TargetCityId);
            if (targetCity == null || targetCity.WorldId != sourceCity.WorldId)
            {
                throw new KeyNotFoundException($"City {dto.TargetCityId} was not found.");
            }

            var selections = dto.UnitsToDeploy ?? new List<UnitSelectionDTO>();
            if (selections.Count == 0 || selections.Any(selection => selection.Quantity <= 0)
                || selections.Select(selection => selection.Type).Distinct().Count() != selections.Count)
            {
                throw new InvalidOperationException("Select at least one available unit.");
            }

            int mobility = int.MaxValue;
            foreach (var selection in selections)
            {
                var stack = sourceCity.UnitStacks.FirstOrDefault(candidate => candidate.Type == selection.Type);
                if (stack == null || stack.Quantity < selection.Quantity)
                {
                    throw new InvalidOperationException($"Not enough {selection.Type} units.");
                }

                mobility = Math.Min(mobility, _unitDataReader.GetUnit(selection.Type).Mobility);
            }

            var transportAssessment = await _transportCapacityService.EvaluateAsync(sourceCity, targetCity, selections);

            mobility = _movementCalculator.CalculateMobilitySnapshot(sourceCity, Math.Max(1, mobility));
            double seconds = _movementCalculator.CalculateTravelSeconds(sourceCity.X, sourceCity.Y, targetCity.X, targetCity.Y, mobility);
            long durationSeconds = Math.Max(0, (long)Math.Ceiling(seconds));
            return new DeploymentTravelEstimateDTO(
                durationSeconds,
                DateTime.UtcNow.AddSeconds(durationSeconds),
                transportAssessment.RequiresTransport,
                transportAssessment.RequiredTransportCapacity,
                transportAssessment.AvailableTransportCapacity,
                transportAssessment.TransportCapacityMargin,
                transportAssessment.HasSufficientTransportCapacity);
        }

        private async Task<OwnedUnitDeploymentDTO> CreateDeploymentAsync(
            Guid originCityId,
            Guid targetCityId,
            List<UnitSelectionDTO>? requestedSelections,
            UnitDeploymentTypeEnum type)
        {
            if (targetCityId == Guid.Empty)
            {
                throw new InvalidOperationException("Målet for angrebet blev ikke angivet.");
            }

            var sourceCity = await _playerAccessService.RequireOwnedCityAsync(originCityId);
            var targetCity = await _cityRepo.GetByIdAsync(targetCityId);

            if (targetCity == null || targetCity.WorldId != sourceCity.WorldId)
            {
                throw new KeyNotFoundException($"Byen med ID {targetCityId} blev ikke fundet.");
            }

            var sourcePlayer = sourceCity.WorldPlayer ?? throw new InvalidOperationException("The origin city owner was not loaded.");
            bool permitted = type == UnitDeploymentTypeEnum.Attack
                ? _permissionService.CanAttack(sourcePlayer, targetCity)
                : await _permissionService.CanSupportAsync(sourcePlayer, targetCity);
            if (!permitted)
            {
                throw new InvalidOperationException(type == UnitDeploymentTypeEnum.Attack
                    ? "The target city cannot be attacked."
                    : "The target city cannot be supported while the alliances are at war.");
            }

            var now = DateTime.UtcNow;
            var worldPlayerId = sourceCity.WorldPlayerId ?? throw new InvalidOperationException("Kilden tilhører ikke en world player.");

            var unitsToMove = new List<UnitStack>();
            var validatedSelections = new List<(UnitSelectionDTO Selection, UnitStack CityStack)>();
            var selections = requestedSelections ?? new List<UnitSelectionDTO>();

            if (selections.Count == 0)
            {
                throw new InvalidOperationException("No units were selected for the attack.");
            }

            if (selections.Select(selection => selection.Type).Distinct().Count() != selections.Count)
            {
                throw new InvalidOperationException("Each unit type can only be selected once.");
            }

            int slowestMobility = int.MaxValue;
            foreach (var selection in selections)
            {
                if (selection.Quantity <= 0)
                {
                    throw new InvalidOperationException("Enheder til angreb skal have en positiv mængde.");
                }

                var staticUnitData = _unitDataReader.GetUnit(selection.Type);
                if (staticUnitData.Mobility < slowestMobility)
                {
                    slowestMobility = staticUnitData.Mobility;
                }

                var cityStack = sourceCity.UnitStacks.FirstOrDefault(stack => stack.Type == selection.Type);
                if (cityStack == null || cityStack.Quantity < selection.Quantity)
                {
                    throw new InvalidOperationException($"Ikke nok enheder af typen {selection.Type} i byen.");
                }

                validatedSelections.Add((selection, cityStack));
            }

            var transportAssessment = await _transportCapacityService.EvaluateAsync(sourceCity, targetCity, selections);
            if (!transportAssessment.HasSufficientTransportCapacity)
            {
                throw new InvalidOperationException(
                    $"Insufficient transport capacity. Required {transportAssessment.RequiredTransportCapacity}, available {transportAssessment.AvailableTransportCapacity}.");
            }

            foreach (var (selection, cityStack) in validatedSelections)
            {
                cityStack.Quantity -= selection.Quantity;
                if (cityStack.Quantity <= 0)
                {
                    cityStack.IsDeleted = true;
                }

                unitsToMove.Add(new UnitStack
                {
                    Type = selection.Type,
                    Quantity = selection.Quantity,
                    WorldPlayerId = worldPlayerId
                });
            }

            if (slowestMobility <= 0)
            {
                slowestMobility = 1;
            }

            slowestMobility = _movementCalculator.CalculateMobilitySnapshot(sourceCity, slowestMobility);
            double travelSeconds = _movementCalculator.CalculateTravelSeconds(
                sourceCity.X,
                sourceCity.Y,
                targetCity.X,
                targetCity.Y,
                slowestMobility);

            var deployment = new UnitDeployment
            {
                Id = Guid.NewGuid(),
                Name = $"Retinue of {sourceCity.Name}",
                WorldPlayerId = worldPlayerId,
                WorldId = sourceCity.WorldId,
                Mobility = slowestMobility,
                Type = type,
                UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Moving,
                Phase = UnitDeploymentPhaseEnum.Outbound,
                DepartureTime = now,
                ArrivalTime = now.AddSeconds(travelSeconds),
                LegStartX = sourceCity.X,
                LegStartY = sourceCity.Y,
                LegEndX = targetCity.X,
                LegEndY = targetCity.Y,
                UnitStacks = unitsToMove,
                OriginCityId = sourceCity.Id,
                OriginCity = sourceCity,
                TargetCity = targetCity,
                TargetCityId = targetCity.Id,
            };

            _snapshotService.ApplyOutgoingModifiers(sourceCity, deployment);

            await _transactionManager.ExecuteAsync(async () =>
            {
                await _unitDeploymentRepo.AddAsync(deployment);
                await _cityRepo.UpdateAsync(sourceCity);
            });

            _logger.LogInformation(
                "Deployment sendt fra {OriginCity} mod {TargetCity} med {UnitCount} enheder.",
                sourceCity.Name,
                targetCity.Name,
                unitsToMove.Sum(unit => unit.Quantity));

            return MapToDto(deployment);
        }

        public async Task<OwnedUnitDeploymentDTO> RecallAsync(Guid deploymentId)
        {
            var deployment = await _playerAccessService.RequireOwnedUnitDeploymentAsync(deploymentId);
            if (deployment.Type != UnitDeploymentTypeEnum.Support || deployment.Phase == UnitDeploymentPhaseEnum.Returning)
            {
                throw new InvalidOperationException("Only outbound or stationed support can be recalled.");
            }

            var now = DateTime.UtcNow;
            var (currentX, currentY) = GetCurrentPosition(deployment, now);
            deployment.Phase = UnitDeploymentPhaseEnum.Returning;
            deployment.UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Moving;
            deployment.StationedAt = null;
            deployment.DepartureTime = now;
            deployment.LegStartX = currentX;
            deployment.LegStartY = currentY;
            deployment.LegEndX = deployment.OriginCity.X;
            deployment.LegEndY = deployment.OriginCity.Y;
            var seconds = _movementCalculator.CalculateTravelSeconds(currentX, currentY, deployment.LegEndX, deployment.LegEndY, deployment.Mobility);
            deployment.ArrivalTime = now.AddSeconds(seconds);
            await _transactionManager.ExecuteAsync(async () =>
            {
                await _unitDeploymentRepo.UpdateAsync(deployment);
                if (_battleReportRepository != null)
                {
                    await _battleReportRepository.AddAsync(new BattleReport
                    {
                        Id = Guid.NewGuid(),
                        WorldPlayerId = deployment.WorldPlayerId,
                        ReportType = ReportTypeEnum.SupportingUnitsRecalled,
                        Title = "Dine supporting units er blevet recalled",
                        Body = $"Dine supporting units returnerer til {deployment.OriginCity.Name}.",
                        OccurredAt = now
                    });
                }
            });
            return MapToDto(deployment);
        }

        public async Task<List<OwnedUnitDeploymentDTO>> GetDeploymentsAsync(Guid worldPlayerId)
        {
            var worldPlayer = await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);
            var deployments = await _unitDeploymentRepo.GetActiveDeploymentsByWorldPlayerIdAsync(worldPlayer.Id);

            return deployments
                .OrderBy(deployment => deployment.Phase == UnitDeploymentPhaseEnum.Stationed ? 1 : 0)
                .ThenBy(deployment => deployment.Phase == UnitDeploymentPhaseEnum.Stationed ? DateTime.MaxValue : deployment.ArrivalTime)
                .ThenByDescending(deployment => deployment.Phase == UnitDeploymentPhaseEnum.Stationed ? deployment.StationedAt : null)
                .ThenBy(deployment => deployment.DepartureTime)
                .ThenBy(deployment => deployment.DateCreated)
                .ThenBy(deployment => deployment.Id)
                .Select(MapToDto)
                .ToList();
        }

        public Task<List<OwnedUnitDeploymentDTO>> GetActiveDeploymentsAsync(Guid worldPlayerId) => GetDeploymentsAsync(worldPlayerId);

        public async Task<List<IncomingAttackDTO>> GetIncomingAttacksAsync(Guid worldPlayerId)
        {
            var worldPlayer = await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);
            var deployments = await _unitDeploymentRepo.GetIncomingAttacksByTargetOwnerIdAsync(worldPlayer.Id);

            return deployments.Select(deployment => new IncomingAttackDTO(
                deployment.Id,
                deployment.WorldPlayerId,
                deployment.OwnerWorldPlayer?.PlayerProfile?.UserName ?? "Unknown player",
                deployment.TargetCityId!.Value,
                deployment.TargetCity!.Name,
                deployment.TargetCity.X,
                deployment.TargetCity.Y,
                deployment.ArrivalTime)).ToList();
        }

        private static (int X, int Y) GetCurrentPosition(UnitDeployment deployment, DateTime now)
        {
            double duration = (deployment.ArrivalTime - deployment.DepartureTime).TotalMilliseconds;
            double progress = duration <= 0 ? 1 : Math.Clamp((now - deployment.DepartureTime).TotalMilliseconds / duration, 0, 1);
            return ((int)Math.Round(deployment.LegStartX + ((deployment.LegEndX - deployment.LegStartX) * progress)),
                (int)Math.Round(deployment.LegStartY + ((deployment.LegEndY - deployment.LegStartY) * progress)));
        }

        private OwnedUnitDeploymentDTO MapToDto(UnitDeployment deployment)
        {
            CityDTO originCity = new CityDTO(
                deployment.OriginCityId,
                deployment.OriginCity.Name,
                deployment.OriginCity.X,
                deployment.OriginCity.Y,
                deployment.OriginCity.Points,
                deployment.OriginCity.IsNPC);

            CityDTO? targetCity = deployment.TargetCity == null
                ? null
                : new CityDTO(
                    deployment.TargetCityId ?? Guid.Empty,
                    deployment.TargetCity.Name,
                    deployment.TargetCity.X,
                    deployment.TargetCity.Y,
                    deployment.TargetCity.Points,
                    deployment.TargetCity.IsNPC);

            return new OwnedUnitDeploymentDTO(
                deployment.Id,
                deployment.Name,
                deployment.WorldPlayerId,
                deployment.OriginCityId,
                originCity,
                deployment.TargetCityId,
                targetCity,
                deployment.UnitDeploymentMovementStatus,
                deployment.Phase,
                deployment.DepartureTime,
                deployment.ArrivalTime,
                deployment.LegStartX,
                deployment.LegStartY,
                deployment.LegEndX,
                deployment.LegEndY,
                deployment.StationedAt,
                deployment.Mobility,
                deployment.Type,
                deployment.UnitStacks.Select(stack => new UnitStackDTO(stack.Type, stack.Quantity)).ToList(),
                deployment.OwnerWorldPlayer?.PlayerProfile?.UserName ?? "Ukendt Spiller",
                MapLocation(deployment.OriginCity),
                deployment.TargetCity == null ? null : MapLocation(deployment.TargetCity));
        }

        private static DeploymentLocationDTO MapLocation(City city)
        {
            var owner = city.WorldPlayer;
            return new DeploymentLocationDTO(
                city.Id,
                city.Name,
                city.X,
                city.Y,
                city.IsNPC,
                owner?.Id,
                owner?.PlayerProfile?.UserName,
                owner?.AllianceId,
                owner?.Alliance?.Name,
                owner?.Alliance?.Tag);
        }
    }
}
