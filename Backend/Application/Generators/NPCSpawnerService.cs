using Application.Interfaces.IRepositories;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Generators;

namespace Application.Generators
{
    public class NPCSpawnerService
    {
        private static readonly string[] NamePrefixes =
        {
            "Ruins of", "Old", "Lost", "Shadow", "Iron", "Grim"
        };

        private static readonly string[] NameSuffixes =
        {
            "Crest", "Watch", "Keep", "Falls", "Mine", "Grave"
        };

        private readonly ICityRepository _cityRepository;
        private readonly IWorldRepository _worldRepository;
        private readonly CityPointCalculator _cityPointCalculator;

        public NPCSpawnerService(
            ICityRepository cityRepository,
            IWorldRepository worldRepository,
            CityPointCalculator cityPointCalculator)
        {
            _cityRepository = cityRepository;
            _worldRepository = worldRepository;
            _cityPointCalculator = cityPointCalculator;
        }

        public async Task<int> EnsureNPCVillagesAsync()
        {
            int createdCount = 0;
            var worlds = await _worldRepository.GetAllAsync();
            var allCities = await _cityRepository.GetCitiesForNPCBackfillAsync();

            foreach (var world in worlds)
            {
                var worldCities = allCities.Where(city => city.WorldId == world.Id).ToList();
                var occupiedSites = worldCities.Select(city => (city.X, city.Y)).ToHashSet();
                var existingNPCCountsByIsland = worldCities
                    .Where(city => city.IsNPC)
                    .Select(city => WorldGenerationService.TryGetIslandCoordinates(
                        city.X,
                        city.Y,
                        world.MapSeed,
                        out int cityIslandX,
                        out int cityIslandY)
                            ? ((int X, int Y)?)(cityIslandX, cityIslandY)
                            : null)
                    .Where(island => island.HasValue)
                    .GroupBy(island => island!.Value)
                    .ToDictionary(group => group.Key, group => group.Count());
                var villagesToCreate = new List<City>();

                foreach (var island in GetActiveIslands(world))
                {
                    var canonicalSites = PlayerCitySiteGenerator.GenerateCanonicalSites(
                        island,
                        world.MapSeed,
                        -world.Width / 2,
                        -world.Width / 2 + world.Width - 1,
                        -world.Height / 2,
                        -world.Height / 2 + world.Height - 1);
                    if (canonicalSites.Count == 0)
                    {
                        continue;
                    }

                    int existingNPCCount = existingNPCCountsByIsland.GetValueOrDefault((island.CellX, island.CellY));
                    int missingNPCCount = Math.Max(
                        0,
                        CalculateTargetCount(world.MapSeed, island.CellX, island.CellY, canonicalSites.Count)
                            - existingNPCCount);

                    var selectedSites = canonicalSites
                        .Where(site => !occupiedSites.Contains(site))
                        .OrderBy(site => GetStableHash(
                            world.MapSeed,
                            island.CellX,
                            island.CellY,
                            site.X,
                            site.Y,
                            101))
                        .ThenBy(site => site.X)
                        .ThenBy(site => site.Y)
                        .Take(missingNPCCount)
                        .ToList();

                    foreach (var site in selectedSites)
                    {
                        var village = CreateVillage(world, island, site.X, site.Y);
                        villagesToCreate.Add(village);
                        worldCities.Add(village);
                        if (!allCities.Contains(village))
                        {
                            allCities.Add(village);
                        }
                        occupiedSites.Add(site);
                        createdCount++;
                    }
                }

                await _cityRepository.AddNPCVillagesWithMapObjectsAsync(villagesToCreate);
            }

            return createdCount;
        }

        public static int CalculateTargetPercentage(int mapSeed, int cellX, int cellY)
        {
            return 15 + (int)(GetStableHash(mapSeed, cellX, cellY, 0, 0, 17) % 11);
        }

        public static int CalculateTargetCount(int mapSeed, int cellX, int cellY, int siteCount)
        {
            int percentage = CalculateTargetPercentage(mapSeed, cellX, cellY);
            return (int)Math.Round(siteCount * percentage / 100d, MidpointRounding.AwayFromZero);
        }

        private City CreateVillage(
            World world,
            WorldGenerationService.IslandDefinition island,
            int x,
            int y)
        {
            var village = new City
            {
                Id = Guid.NewGuid(),
                Name = GenerateNPCName(world.MapSeed, island.CellX, island.CellY, x, y),
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
                Buildings =
                [
                    new Building
                    {
                        Id = Guid.NewGuid(),
                        Type = BuildingTypeEnum.TimberCamp,
                        Level = GetDeterministicLevel(world.MapSeed, island.CellX, island.CellY, x, y, 31)
                    },
                    new Building
                    {
                        Id = Guid.NewGuid(),
                        Type = BuildingTypeEnum.StoneQuarry,
                        Level = GetDeterministicLevel(world.MapSeed, island.CellX, island.CellY, x, y, 47)
                    }
                ],
                UnitStacks =
                [
                    new UnitStack
                    {
                        Id = Guid.NewGuid(),
                        Type = UnitTypeEnum.Militia,
                        Quantity = 5 + (int)(GetStableHash(
                            world.MapSeed,
                            island.CellX,
                            island.CellY,
                            x,
                            y,
                            59) % 20)
                    }
                ]
            };

            village.Points = _cityPointCalculator.CalculateTotalPointsForCity(village);
            return village;
        }

        private static IEnumerable<WorldGenerationService.IslandDefinition> GetActiveIslands(World world)
        {
            int minimumX = -world.Width / 2;
            int maximumX = minimumX + world.Width - 1;
            int minimumY = -world.Height / 2;
            int maximumY = minimumY + world.Height - 1;
            int minimumCellX = FloorDivide(minimumX, WorldGenerationService.IslandCellSize) - 1;
            int maximumCellX = FloorDivide(maximumX, WorldGenerationService.IslandCellSize) + 1;
            int minimumCellY = FloorDivide(minimumY, WorldGenerationService.IslandRowHeight) - 1;
            int maximumCellY = FloorDivide(maximumY, WorldGenerationService.IslandRowHeight) + 1;

            for (int cellX = minimumCellX; cellX <= maximumCellX; cellX++)
            for (int cellY = minimumCellY; cellY <= maximumCellY; cellY++)
            {
                if (WorldGenerationService.IsIslandCellActive(cellX, cellY, world.MapSeed))
                {
                    yield return WorldGenerationService.GetIslandDefinition(cellX, cellY, world.MapSeed);
                }
            }
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

        private static int GetDeterministicLevel(
            int mapSeed,
            int cellX,
            int cellY,
            int x,
            int y,
            int salt)
        {
            return 1 + (int)(GetStableHash(mapSeed, cellX, cellY, x, y, salt) % 2);
        }

        private static string GenerateNPCName(int mapSeed, int cellX, int cellY, int x, int y)
        {
            int prefixIndex = (int)(GetStableHash(mapSeed, cellX, cellY, x, y, 71) % NamePrefixes.Length);
            int suffixIndex = (int)(GetStableHash(mapSeed, cellX, cellY, x, y, 89) % NameSuffixes.Length);
            return $"{NamePrefixes[prefixIndex]} {NameSuffixes[suffixIndex]}";
        }

        private static uint GetStableHash(
            int mapSeed,
            int cellX,
            int cellY,
            int x,
            int y,
            int salt)
        {
            unchecked
            {
                uint hash = 2166136261;
                hash = (hash ^ (uint)mapSeed) * 16777619;
                hash = (hash ^ (uint)cellX) * 16777619;
                hash = (hash ^ (uint)cellY) * 16777619;
                hash = (hash ^ (uint)x) * 16777619;
                hash = (hash ^ (uint)y) * 16777619;
                hash = (hash ^ (uint)salt) * 16777619;
                hash ^= hash >> 16;
                hash *= 0x7feb352d;
                hash ^= hash >> 15;
                hash *= 0x846ca68b;
                hash ^= hash >> 16;
                return hash;
            }
        }

        private static int FloorDivide(int value, int divisor)
        {
            int quotient = value / divisor;
            return value % divisor < 0 ? quotient - 1 : quotient;
        }
    }
}
