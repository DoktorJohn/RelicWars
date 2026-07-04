using Domain.Enums;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Application.Generators;
using Domain.StaticData.Generators;

namespace Infrastructure.Context
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(GameContext context, NPCSpawnerService spawner)
        {
            if (!await context.World.AnyAsync())
            {
                Console.WriteLine("--- Seeding World & Initial Data ---");

                context.World.Add(new World
                {
                    Id = Guid.NewGuid(),
                    Name = "Alpha World 0.0.1",
                    Abbrevation = "ALFA",
                    Width = 1000,
                    Height = 1000,
                    MapSeed = 42069,
                    PlayerCount = 0
                });
                await context.SaveChangesAsync();
            }

            await EnsureWorldIslandsAsync(context);
        }

        private static async Task EnsureWorldIslandsAsync(GameContext context)
        {
            var worlds = await context.World.AsNoTracking().ToListAsync();
            foreach (var world in worlds)
            {
                var existingIslands = (await context.WorldIslands
                    .Where(island => island.WorldId == world.Id)
                    .ToListAsync())
                    .ToDictionary(island => (island.CellX, island.CellY));
                var existingIslandIds = existingIslands.Values.Select(island => island.Id).ToList();
                var existingResources = await context.WorldIslandExoticResources
                    .Where(resource => existingIslandIds.Contains(resource.WorldIslandId))
                    .ToListAsync();
                var existingResourcesByIsland = existingResources
                    .GroupBy(resource => resource.WorldIslandId)
                    .ToDictionary(group => group.Key, group => group.ToList());

                int minimumX = -world.Width / 2;
                int maximumX = minimumX + world.Width - 1;
                int minimumY = -world.Height / 2;
                int maximumY = minimumY + world.Height - 1;
                int minimumCellX = FloorDivide(minimumX, WorldGenerationService.IslandCellSize) - 1;
                int maximumCellX = FloorDivide(maximumX, WorldGenerationService.IslandCellSize) + 1;
                int minimumCellY = FloorDivide(minimumY, WorldGenerationService.IslandRowHeight) - 1;
                int maximumCellY = FloorDivide(maximumY, WorldGenerationService.IslandRowHeight) + 1;
                var activeCells = new HashSet<(int CellX, int CellY)>();

                for (int cellX = minimumCellX; cellX <= maximumCellX; cellX++)
                for (int cellY = minimumCellY; cellY <= maximumCellY; cellY++)
                {
                    if (!WorldGenerationService.IsIslandCellActive(cellX, cellY, world.MapSeed))
                        continue;

                    var definition = WorldGenerationService.GetIslandDefinition(cellX, cellY, world.MapSeed);
                    activeCells.Add((cellX, cellY));
                    if (!existingIslands.TryGetValue((cellX, cellY), out var island))
                    {
                        island = new WorldIsland
                        {
                            Id = Guid.NewGuid(),
                            WorldId = world.Id,
                            CellX = cellX,
                            CellY = cellY
                        };
                        await context.WorldIslands.AddAsync(island);
                    }

                    island.CenterX = definition.CenterX;
                    island.CenterY = definition.CenterY;
                    island.Shape = (IslandShapeEnum)definition.Shape;
                    island.MajorRadius = definition.MajorRadius;
                    island.MinorRadius = definition.MinorRadius;
                    island.RotationDegrees = definition.RotationDegrees;
                    island.EdgeRoughness = definition.EdgeRoughness;

                    if (!existingResourcesByIsland.TryGetValue(island.Id, out var islandResources) || islandResources.Count == 0)
                    {
                        await SeedIslandExoticResourcesAsync(context, island.Id, world.MapSeed, cellX, cellY);
                    }
                    else if (islandResources.Count < 3)
                    {
                        var existingTypes = islandResources.Select(resource => resource.ResourceType).ToHashSet();
                        var assignedResources = GetAssignedExoticResources(world.MapSeed, cellX, cellY);

                        for (int slotIndex = 0; slotIndex < assignedResources.Count; slotIndex++)
                        {
                            var resourceType = assignedResources[slotIndex];
                            if (existingTypes.Contains(resourceType))
                                continue;

                            await context.WorldIslandExoticResources.AddAsync(new WorldIslandExoticResource
                            {
                                Id = Guid.NewGuid(),
                                WorldIslandId = island.Id,
                                SlotIndex = slotIndex,
                                ResourceType = resourceType,
                                Tier = 1
                            });
                        }
                    }
                }

                context.WorldIslands.RemoveRange(existingIslands
                    .Where(pair => !activeCells.Contains(pair.Key))
                    .Select(pair => pair.Value));
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedIslandExoticResourcesAsync(GameContext context, Guid islandId, int mapSeed, int cellX, int cellY)
        {
            var assignedResources = GetAssignedExoticResources(mapSeed, cellX, cellY);
            for (int slotIndex = 0; slotIndex < assignedResources.Count; slotIndex++)
            {
                await context.WorldIslandExoticResources.AddAsync(new WorldIslandExoticResource
                {
                    Id = Guid.NewGuid(),
                    WorldIslandId = islandId,
                    SlotIndex = slotIndex,
                    ResourceType = assignedResources[slotIndex],
                    Tier = 1
                });
            }
        }

        private static List<ExoticResourceTypeEnum> GetAssignedExoticResources(int mapSeed, int cellX, int cellY)
        {
            return Enum.GetValues<ExoticResourceTypeEnum>()
                .OrderBy(resource => GetResourceSortKey(mapSeed, cellX, cellY, resource))
                .Take(3)
                .ToList();
        }

        private static int GetResourceSortKey(int mapSeed, int cellX, int cellY, ExoticResourceTypeEnum resourceType)
        {
            unchecked
            {
                int hash = mapSeed;
                hash = hash * 397 ^ cellX;
                hash = hash * 397 ^ cellY;
                hash = hash * 397 ^ (int)resourceType;
                hash ^= hash >> 16;
                return hash & int.MaxValue;
            }
        }

        private static int FloorDivide(int value, int divisor)
        {
            int quotient = value / divisor;
            return value % divisor < 0 ? quotient - 1 : quotient;
        }
    }
}
