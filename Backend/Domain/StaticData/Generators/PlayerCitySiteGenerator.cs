using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Domain.StaticData.Generators
{
    public static class PlayerCitySiteGenerator
    {
        public const int MinimumCityDistance = 3;

        // Geometry is deterministic for this key and shared by spawning, NPC backfill and overlapping chunk requests.
        private static readonly ConcurrentDictionary<IslandCacheKey, Lazy<IReadOnlyList<(int X, int Y)>>> CanonicalSitesByIsland = new();

        public static (int X, int Y)? FindNextSite(
            WorldGenerationService.IslandDefinition island,
            int mapSeed,
            int minimumX,
            int maximumX,
            int minimumY,
            int maximumY,
            IReadOnlyCollection<(int X, int Y)> occupiedSites)
        {
            var occupied = occupiedSites.ToHashSet();
            foreach (var candidate in GenerateCanonicalSites(
                island,
                mapSeed,
                minimumX,
                maximumX,
                minimumY,
                maximumY))
            {
                if (occupied.Contains(candidate))
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        public static List<(int X, int Y)> GenerateCanonicalSites(
            WorldGenerationService.IslandDefinition island,
            int mapSeed,
            int minimumX,
            int maximumX,
            int minimumY,
            int maximumY)
        {
            return GetCanonicalSites(island, mapSeed)
                .Where(site => site.X >= minimumX && site.X <= maximumX
                    && site.Y >= minimumY && site.Y <= maximumY)
                .ToList();
        }

        public static List<(int X, int Y)> GenerateFutureSites(
            WorldGenerationService.IslandDefinition island,
            int mapSeed,
            int minimumX,
            int maximumX,
            int minimumY,
            int maximumY,
            IEnumerable<(int X, int Y)> occupiedSites)
        {
            var occupied = occupiedSites.ToHashSet();
            return GenerateCanonicalSites(island, mapSeed, minimumX, maximumX, minimumY, maximumY)
                .Where(site => !occupied.Contains(site))
                .ToList();
        }

        private static IReadOnlyList<(int X, int Y)> GetCanonicalSites(
            WorldGenerationService.IslandDefinition island,
            int mapSeed)
        {
            var key = new IslandCacheKey(mapSeed, island.CellX, island.CellY);
            return CanonicalSitesByIsland.GetOrAdd(
                key,
                _ => new Lazy<IReadOnlyList<(int X, int Y)>>(
                    () => CalculateCanonicalSites(island, mapSeed),
                    LazyThreadSafetyMode.ExecutionAndPublication)).Value;
        }

        private static IReadOnlyList<(int X, int Y)> CalculateCanonicalSites(
            WorldGenerationService.IslandDefinition island,
            int mapSeed)
        {
            int searchRadius = WorldGenerationService.MaximumIslandRadius + 2;
            var sites = new List<(int X, int Y)>();

            for (int x = island.CenterX - searchRadius; x <= island.CenterX + searchRadius; x++)
            {
                for (int y = island.CenterY - searchRadius; y <= island.CenterY + searchRadius; y++)
                {
                    if (!WorldGenerationService.IsCoastalOnIsland(x, y, mapSeed, island))
                    {
                        continue;
                    }

                    sites.Add((x, y));
                }
            }

            var canonicalSites = new List<(int X, int Y)>();
            var candidates = sites
                .Select(site => new SiteCandidate(site, int.MaxValue))
                .ToList();

            while (TryTakeBestCandidate(candidates, out var nextSite))
            {
                canonicalSites.Add(nextSite);
                foreach (var candidate in candidates)
                {
                    candidate.MinimumSpacing = Math.Min(
                        candidate.MinimumSpacing,
                        HexDistance(candidate.Position.X, candidate.Position.Y, nextSite.X, nextSite.Y));
                }
            }

            return canonicalSites;
        }

        private static bool TryTakeBestCandidate(
            List<SiteCandidate> candidates,
            out (int X, int Y) bestSite)
        {
            int bestIndex = -1;
            int bestSpacing = int.MinValue;

            for (int index = 0; index < candidates.Count; index++)
            {
                int spacing = candidates[index].MinimumSpacing;
                if (spacing < MinimumCityDistance || spacing <= bestSpacing)
                {
                    continue;
                }

                bestIndex = index;
                bestSpacing = spacing;
            }

            if (bestIndex < 0)
            {
                bestSite = default;
                return false;
            }

            bestSite = candidates[bestIndex].Position;
            candidates.RemoveAt(bestIndex);
            return true;
        }

        public static int HexDistance(int firstX, int firstY, int secondX, int secondY)
        {
            int firstCubeX = firstX - (firstY - (firstY & 1)) / 2;
            int firstCubeZ = firstY;
            int firstCubeY = -firstCubeX - firstCubeZ;
            int secondCubeX = secondX - (secondY - (secondY & 1)) / 2;
            int secondCubeZ = secondY;
            int secondCubeY = -secondCubeX - secondCubeZ;

            return Math.Max(
                Math.Abs(firstCubeX - secondCubeX),
                Math.Max(Math.Abs(firstCubeY - secondCubeY), Math.Abs(firstCubeZ - secondCubeZ)));
        }

        private readonly record struct IslandCacheKey(int MapSeed, int CellX, int CellY);

        private sealed class SiteCandidate((int X, int Y) position, int minimumSpacing)
        {
            public (int X, int Y) Position { get; } = position;
            public int MinimumSpacing { get; set; } = minimumSpacing;
        }
    }
}
