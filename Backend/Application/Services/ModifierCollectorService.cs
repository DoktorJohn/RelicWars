using Application.Interfaces.IServices;
using Domain.Abstraction;
using Domain.Entities;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ModifierCollectorService : IModifierCollectorService
    {
        private readonly BuildingDataReader _buildingDataReader;
        private readonly ResearchDataReader _researchDataReader;
        private readonly IdeologyDataReader _ideologyDataReader;
        private readonly IdeologyFocusDataReader _ideologyFocusDataReader;
        private readonly TimeProvider _timeProvider;

        public ModifierCollectorService(
            BuildingDataReader buildingDataReader,
            ResearchDataReader researchDataReader,
            IdeologyDataReader ideologyDataReader,
            IdeologyFocusDataReader ideologyFocusDataReader,
            TimeProvider timeProvider)
        {
            _buildingDataReader = buildingDataReader;
            _researchDataReader = researchDataReader;
            _ideologyDataReader = ideologyDataReader;
            _ideologyFocusDataReader = ideologyFocusDataReader;
            _timeProvider = timeProvider;
        }

        public List<IModifierProvider> CollectAllProvidersForPlayer(WorldPlayer playerEntity)
        {
            var providers = new List<IModifierProvider>();
            if (playerEntity == null) return providers;

            // 1. Spilleren selv
            providers.Add(playerEntity);

            // 2. Alliance
            if (playerEntity.Alliance != null)
            {
                providers.Add(playerEntity.Alliance);
            }

            // 3. Forskning
            foreach (var research in playerEntity.CompletedResearches)
            {
                var researchData = _researchDataReader.GetNode(research.ResearchId);
                if (researchData != null) providers.Add(researchData);
            }

            // 4. Ideologi (Core)
            var ideology = _ideologyDataReader.GetIdeology(playerEntity.Ideology);
            if (ideology != null) providers.Add(ideology);

            return providers;
        }

        public List<IModifierProvider> CollectAllProvidersForCity(City cityEntity)
        {
            var providers = new List<IModifierProvider>();
            if (cityEntity == null) return providers;

            if (cityEntity.WorldPlayer != null)
            {
                providers.AddRange(CollectAllProvidersForPlayer(cityEntity.WorldPlayer));
            }

            providers.Add(cityEntity);

            foreach (var cityBuilding in cityEntity.Buildings.Where(b => b.Level > 0))
            {
                var levelConfig = _buildingDataReader.GetConfig<BuildingLevelData>(cityBuilding.Type, cityBuilding.Level);
                if (levelConfig != null) providers.Add(levelConfig);
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            foreach (var ideologyFocus in cityEntity.ActiveFocuses.Where(f =>
                f.TimeOfIdeologyStarted <= now && (!f.TimeOfIdeologyFinished.HasValue || f.TimeOfIdeologyFinished > now)))
            {
                var focusData = _ideologyFocusDataReader.GetIdeology(ideologyFocus.Name);
                if (focusData != null) providers.Add(focusData);
            }

            return providers;
        }
    }
}
