using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using System.Linq;

namespace Application.Services
{
    public class CombatSimulatorService
    {
        private readonly IPlayerAccessService _playerAccessService;
        private readonly ICityRepository _cityRepository;
        private readonly CombatService _combatService;
        private readonly DeploymentTransportCapacityService _transportCapacityService;

        public CombatSimulatorService(
            IPlayerAccessService playerAccessService,
            ICityRepository cityRepository,
            CombatService combatService,
            DeploymentTransportCapacityService transportCapacityService)
        {
            _playerAccessService = playerAccessService;
            _cityRepository = cityRepository;
            _combatService = combatService;
            _transportCapacityService = transportCapacityService;
        }

        public async Task<CombatSimulationResultDTO> SimulateBattleAsync(Guid userId, CombatSimulationRequestDTO request)
        {
            ValidateSelections(request.AttackerUnits, "attacker");
            ValidateSelections(request.DefenderUnits, "defender");

            var originCity = await _playerAccessService.RequireOwnedCityAsync(request.OriginCityId);
            var targetCity = await _cityRepository.GetByIdAsync(request.TargetCityId);
            if (targetCity == null || targetCity.WorldId != originCity.WorldId)
            {
                throw new KeyNotFoundException($"City {request.TargetCityId} was not found.");
            }

            var attackerStacks = MapStacks(request.AttackerUnits);
            var defenderStacks = MapStacks(request.DefenderUnits);
            var transportAssessment = await _transportCapacityService.EvaluateAsync(originCity, targetCity, request.AttackerUnits);
            var result = _combatService.ResolveBattle(new CombatContext(
                attackerStacks,
                defenderStacks,
                originCity,
                targetCity,
                originCity.WorldPlayer,
                targetCity.WorldPlayer));

            return new CombatSimulationResultDTO(
                result.RemainingAttackers.Select(MapStack).ToList(),
                result.RemainingDefenders.Select(MapStack).ToList(),
                result.AttackerLosses.Select(MapStack).ToList(),
                result.DefenderLosses.Select(MapStack).ToList(),
                result.RevivedDefenders.Select(MapStack).ToList(),
                result.LuckModifier,
                result.AppliedModifiers,
                transportAssessment.RequiresTransport,
                transportAssessment.RequiredTransportCapacity,
                transportAssessment.AvailableTransportCapacity,
                transportAssessment.TransportCapacityMargin,
                transportAssessment.HasSufficientTransportCapacity);
        }

        private static List<UnitStack> MapStacks(IEnumerable<UnitSelectionDTO> selections)
        {
            return selections.Select(selection => new UnitStack
            {
                Id = Guid.NewGuid(),
                Type = selection.Type,
                Quantity = selection.Quantity
            }).ToList();
        }

        private static UnitStackDTO MapStack(UnitStack stack) => new(stack.Type, stack.Quantity);

        private static void ValidateSelections(List<UnitSelectionDTO>? selections, string role)
        {
            if (selections == null || selections.Count == 0)
            {
                throw new InvalidOperationException($"Select at least one {role} unit.");
            }

            if (selections.Any(selection => selection.Quantity <= 0))
            {
                throw new InvalidOperationException($"Each {role} unit quantity must be positive.");
            }

            if (selections.Select(selection => selection.Type).Distinct().Count() != selections.Count)
            {
                throw new InvalidOperationException($"Each {role} unit type can only be selected once.");
            }
        }
    }
}
