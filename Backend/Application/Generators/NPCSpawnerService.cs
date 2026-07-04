using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Generators
{
    public class NPCSpawnerService
    {
        private readonly ICityRepository _cityRepo;
        private readonly IWorldRepository _worldRepository;
        private readonly IWorldMapObjectService _worldMapObjectService;
        private readonly Random _random = new();

        public NPCSpawnerService(ICityRepository cityRepo, IWorldRepository worldRepository, IWorldMapObjectService worldMapObjectService)
        {
            _cityRepo = cityRepo;
            _worldRepository = worldRepository;
            _worldMapObjectService = worldMapObjectService;
        }

        public async Task SpawnInitialNPCsAsync(int count, int mapRange)
        {
            const int targetCitiesPerIsland = 25;
            var world = (await _worldRepository.GetAllAsync()).FirstOrDefault();
            if (world == null) return;

            var existingCities = (await _cityRepo.GetAllAsync())
                .Where(city => city.WorldId == world.Id)
                .ToList();

            // Vi bruger en HashSet til hurtigt at tjekke om koordinaterne er optaget
            var occupied = existingCities.Select(c => (c.X, c.Y)).ToHashSet();

            for (int i = 0; i < count; i++)
            {
                var position = FindCoastalPosition(
                    existingCities, occupied, world.MapSeed, mapRange, targetCitiesPerIsland);
                if (!position.HasValue) continue;

                var (x, y) = position.Value;

                var npcCity = new City
                {
                    Id = Guid.NewGuid(),
                    Name = GenerateNPCName(),
                    WorldId = world.Id,
                    X = x,
                    Y = y,
                    IsNPC = true,
                    Wood = 500,
                    Stone = 500,
                    Metal = 500,
                    LastResourceUpdate = DateTime.UtcNow,
                    LastExoticResourceUpdate = DateTime.UtcNow,
                    ExoticResources = CreateInitialExoticResources(),
                    Buildings = new List<Building>
                    {
                        new Building { Type = BuildingTypeEnum.TimberCamp, Level = _random.Next(1, 3) },
                        new Building { Type = BuildingTypeEnum.StoneQuarry, Level = _random.Next(1, 3) }
                    },
                    UnitStacks = new List<UnitStack>
                    {
                        new UnitStack { Type = UnitTypeEnum.Militia, Quantity = _random.Next(5, 25) }
                    }
                };

                await _cityRepo.AddAsync(npcCity);
                await _worldMapObjectService.AddEntityToWorldMapAsync(npcCity);
                occupied.Add((x, y));
                existingCities.Add(npcCity);
            }
        }

        private (int X, int Y)? FindCoastalPosition(
            List<City> cities,
            HashSet<(int X, int Y)> occupied,
            int mapSeed,
            int mapRange,
            int targetCitiesPerIsland)
        {
            const int islandCellSize = WorldGenerationService.IslandCellSize;
            const int islandSearchRadius = WorldGenerationService.MaximumIslandRadius + 2;

            var cityCounts = cities
                .Select(city => WorldGenerationService.TryGetIslandCoordinates(
                    city.X, city.Y, mapSeed, out int islandX, out int islandY)
                    ? ((int X, int Y)?)(islandX, islandY)
                    : null)
                .Where(island => island.HasValue)
                .GroupBy(island => island!.Value)
                .ToDictionary(group => group.Key, group => group.Count());

            var activeIsland = cityCounts
                .Where(pair => pair.Value < targetCitiesPerIsland)
                .OrderByDescending(pair => pair.Value)
                .Select(pair => ((int X, int Y)?)pair.Key)
                .FirstOrDefault();

            for (int attempt = 0; attempt < 100; attempt++)
            {
                var island = activeIsland ?? (
                    _random.Next(-mapRange / islandCellSize, mapRange / islandCellSize + 1),
                    _random.Next(-mapRange / WorldGenerationService.IslandRowHeight, mapRange / WorldGenerationService.IslandRowHeight + 1));
                if (!WorldGenerationService.IsIslandCellActive(island.X, island.Y, mapSeed))
                {
                    activeIsland = null;
                    continue;
                }

                var islandDefinition = WorldGenerationService.GetIslandDefinition(island.X, island.Y, mapSeed);

                var candidates = (
                    from x in Enumerable.Range(islandDefinition.CenterX - islandSearchRadius, islandSearchRadius * 2 + 1)
                    from y in Enumerable.Range(islandDefinition.CenterY - islandSearchRadius, islandSearchRadius * 2 + 1)
                    where x >= -mapRange && x < mapRange && y >= -mapRange && y < mapRange
                    where !occupied.Contains((x, y))
                    where WorldGenerationService.IsCoastal(x, y, mapSeed)
                    where WorldGenerationService.TryGetIslandCoordinates(x, y, mapSeed, out int islandX, out int islandY)
                        && islandX == island.X && islandY == island.Y
                    select (x, y)).ToList();

                if (candidates.Count > 0)
                    return candidates[_random.Next(candidates.Count)];

                activeIsland = null;
            }

            return null;
        }

        private static List<CityExoticResource> CreateInitialExoticResources()
        {
            return Enum.GetValues<ExoticResourceTypeEnum>()
                .Select(resourceType => new CityExoticResource
                {
                    Id = Guid.NewGuid(),
                    ResourceType = resourceType,
                    Amount = 0
                })
                .ToList();
        }

        private string GenerateNPCName()
        {
            string[] names = { "Ruins of", "Old", "Lost", "Shadow", "Iron", "Grim" };
            string[] sites = { "Crest", "Watch", "Keep", "Falls", "Mine", "Grave" };
            return $"{names[_random.Next(names.Length)]} {sites[_random.Next(sites.Length)]}";
        }
    }
}
