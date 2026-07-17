using Application.DTOs;
using Application.DTOs;
using Application.Interfaces.IRepositories;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Generators;
using Domain.StaticData.Readers;

namespace Application.Utility
{
    public record DeploymentTransportCapacityAssessment(
        bool RequiresTransport,
        int RequiredTransportCapacity,
        int AvailableTransportCapacity,
        int TransportCapacityMargin,
        bool HasSufficientTransportCapacity);

    public class DeploymentTransportCapacityService
    {
        private readonly IWorldRepository _worldRepository;
        private readonly UnitDataReader _unitDataReader;

        public DeploymentTransportCapacityService(IWorldRepository worldRepository, UnitDataReader unitDataReader)
        {
            _worldRepository = worldRepository;
            _unitDataReader = unitDataReader;
        }

        public async Task<DeploymentTransportCapacityAssessment> EvaluateAsync(
            City originCity,
            City targetCity,
            IReadOnlyCollection<UnitSelectionDTO> selections)
        {
            if (originCity == null) throw new ArgumentNullException(nameof(originCity));
            if (targetCity == null) throw new ArgumentNullException(nameof(targetCity));
            if (selections == null || selections.Count == 0) throw new ArgumentException("At least one unit must be selected.", nameof(selections));

            int? worldSeed = await _worldRepository.GetWorldSeedAsync(originCity.WorldId);
            if (!worldSeed.HasValue)
            {
                throw new KeyNotFoundException($"World {originCity.WorldId} was not found.");
            }

            bool requiresTransport = RequiresTransport(originCity, targetCity, worldSeed.Value);
            if (!requiresTransport)
            {
                return new DeploymentTransportCapacityAssessment(false, 0, 0, 0, true);
            }

            int requiredCapacity = 0;
            int availableCapacity = 0;
            foreach (var selection in selections)
            {
                var unitData = _unitDataReader.GetUnit(selection.Type);
                if (unitData.Category == UnitCategoryEnum.Naval)
                {
                    availableCapacity += unitData.UnitCapacity * selection.Quantity;
                    continue;
                }

                requiredCapacity += unitData.PopulationCost * selection.Quantity;
            }

            int margin = availableCapacity - requiredCapacity;
            return new DeploymentTransportCapacityAssessment(true, requiredCapacity, availableCapacity, margin, margin >= 0);
        }

        private static bool RequiresTransport(City originCity, City targetCity, int worldSeed)
        {
            bool originResolved = WorldGenerationService.TryGetIslandCoordinates(originCity.X, originCity.Y, worldSeed, out int originIslandX, out int originIslandY);
            bool targetResolved = WorldGenerationService.TryGetIslandCoordinates(targetCity.X, targetCity.Y, worldSeed, out int targetIslandX, out int targetIslandY);

            if (!originResolved || !targetResolved)
            {
                throw new InvalidOperationException("Unable to resolve island coordinates for the transport check.");
            }

            return originIslandX != targetIslandX || originIslandY != targetIslandY;
        }
    }
}
