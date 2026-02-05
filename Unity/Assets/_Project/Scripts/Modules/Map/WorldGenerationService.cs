using System;
using System.Collections.Generic;
using Project.Scripts.Domain.Enums;

namespace Domain.StaticData.Generators
{
    public static class WorldGenerationService
    {
        private const float BiomeGroupFrequency = 0.18f;
        private const float MountainPassFrequency = 0.09f;

        private static readonly Dictionary<BiomeGroup, int> BiomeVariantCounts = new Dictionary<BiomeGroup, int>
    {
        { BiomeGroup.Plains, 9 },
        { BiomeGroup.PlainHills, 5 },
        { BiomeGroup.Forest, 5 },
        { BiomeGroup.Desert, 12 },
        { BiomeGroup.DesertHills, 4 },
        { BiomeGroup.Tundra, 2 },
        { BiomeGroup.Wetland, 8 },
        { BiomeGroup.IceMountains, 6 },
        { BiomeGroup.IceMountainTundraTransition, 2 },
        { BiomeGroup.Sand, 11 },
        { BiomeGroup.SandHills, 3 },
        { BiomeGroup.SandMountains, 4 },
        { BiomeGroup.Jungle, 7 }
    };

        // Static readonly for at undgå array-allokering ved hver klynge-check
        private static readonly int[][] NeighborOffsets = new int[][] {
        new int[] { 0, -1 }, new int[] { 1, -1 },
        new int[] { -1, 0 }, new int[] { 1, 0 },
        new int[] { -1, 1 }, new int[] { 0, 1 }
    };

        public static WorldBiomeVariantType CalculateWorldMapBiomeVariant(short x, short y, int mapSeed)
        {
            // Lokal cache eliminerer 6/7 Perlin-beregninger per tile
            var cache = new Dictionary<(int, int), BiomeGroup>(7);

            // 1-3. Beregn basis biome med caching (samme logik som før)
            BiomeGroup coreGroup = GetCachedBaseBiome(x, y, mapSeed, cache);

            // 4. Benhård geografisk regulering (samme logik, men med cache)
            coreGroup = SolidifyMountainClustersAndGenerateBorders(coreGroup, x, y, mapSeed, cache);

            // 5. Resolve variant (zero-allocation)
            float randomVariantValue = PseudoRandomHash(x, y, mapSeed);
            return ResolveVariant(coreGroup, randomVariantValue);
        }

        private static BiomeGroup DetermineCoreBiomeFromNoise(float noise)
        {
            if (noise < 0.10f) return BiomeGroup.IceMountains;
            if (noise < 0.28f) return BiomeGroup.Wetland;
            if (noise < 0.38f) return BiomeGroup.Forest;
            if (noise < 0.60f) return BiomeGroup.Plains;
            if (noise < 0.69f) return BiomeGroup.PlainHills;
            if (noise < 0.72f) return BiomeGroup.Jungle;
            if (noise < 0.76f) return BiomeGroup.SandHills;
            if (noise < 0.85f) return BiomeGroup.Sand;
            if (noise < 0.89f) return BiomeGroup.DesertHills;
            if (noise < 0.94f) return BiomeGroup.Desert;
            return BiomeGroup.SandMountains;
        }

        private static BiomeGroup InjectMountainPasses(BiomeGroup currentGroup, int x, int y, int seed)
        {
            if (currentGroup != BiomeGroup.IceMountains && currentGroup != BiomeGroup.SandMountains)
                return currentGroup;

            float passNoise = NoiseGenerator.Perlin(x * MountainPassFrequency, y * MountainPassFrequency, seed * 0.938f);

            if (Math.Abs(passNoise) < 0.06f)
                return currentGroup == BiomeGroup.IceMountains ? BiomeGroup.Tundra : BiomeGroup.DesertHills;

            return currentGroup;
        }

        private static BiomeGroup SolidifyMountainClustersAndGenerateBorders(
            BiomeGroup currentGroup, int x, int y, int mapSeed,
            Dictionary<(int, int), BiomeGroup> cache)
        {
            int iceMountainNeighbors = 0;
            int sandMountainNeighbors = 0;  // ADD: Track sand mountains too
            bool touchesIceMountain = false;
            bool touchesSandMountain = false;  // ADD

            foreach (var offset in NeighborOffsets)  // Use the static readonly array
            {
                BiomeGroup neighbor = GetCachedBaseBiome(x + offset[0], y + offset[1], mapSeed, cache);

                if (neighbor == BiomeGroup.IceMountains)
                {
                    iceMountainNeighbors++;
                    touchesIceMountain = true;
                }
                else if (neighbor == BiomeGroup.SandMountains)  // ADD
                {
                    sandMountainNeighbors++;
                    touchesSandMountain = true;
                }
            }

            // --- 25% MORE HEXAGONS: Relaxed survival rules ---

            // IceMountains: Allow single-neighbor mountains to survive (dendritic growth)
            if (currentGroup == BiomeGroup.IceMountains && iceMountainNeighbors < 1)  // Was < 2
                return BiomeGroup.Plains;

            // ADD: Same clustering for SandMountains (was missing entirely!)
            if (currentGroup == BiomeGroup.SandMountains && sandMountainNeighbors < 1)
                return BiomeGroup.Desert;  // Or SandHills if you prefer

            // --- Borders remain the same ---
            if (currentGroup != BiomeGroup.IceMountains &&
                currentGroup != BiomeGroup.Tundra &&
                touchesIceMountain)
            {
                return BiomeGroup.IceMountainTundraTransition;
            }

            // ADD: SandMountain borders (optional but helps visual cohesion)
            if (currentGroup != BiomeGroup.SandMountains &&
                currentGroup != BiomeGroup.DesertHills &&
                touchesSandMountain)
            {
                return BiomeGroup.DesertHills;
            }

            return currentGroup;
        }

        /// <summary>
        /// Henter cached biome eller beregner den (DetermineCore + InjectPasses).
        /// </summary>
        private static BiomeGroup GetCachedBaseBiome(int x, int y, int mapSeed, Dictionary<(int, int), BiomeGroup> cache)
        {
            var key = (x, y);
            if (cache.TryGetValue(key, out var cached))
                return cached;

            float rawNoise = NoiseGenerator.Perlin(x * BiomeGroupFrequency, y * BiomeGroupFrequency, mapSeed * 0.5f);
            float biomeNoise = Smoothstep((rawNoise + 1f) / 2f);

            BiomeGroup group = DetermineCoreBiomeFromNoise(biomeNoise);
            group = InjectMountainPasses(group, x, y, mapSeed);

            cache[key] = group;
            return group;
        }

        /// <summary>
        /// Zero-allocation variant resolver uden string-konkatinering.
        /// </summary>
        private static WorldBiomeVariantType ResolveVariant(BiomeGroup group, float randomValue)
        {
            if (!BiomeVariantCounts.TryGetValue(group, out int count))
                return WorldBiomeVariantType.Plains_1;

            int index = (int)(randomValue * count) + 1;
            if (index > count) index = count;
            if (index < 1) index = 1;

            // Direkte mapping - ingen string-allokering
            return (group, index) switch
            {
                (BiomeGroup.Plains, 1) => WorldBiomeVariantType.Plains_1,
                (BiomeGroup.Plains, 2) => WorldBiomeVariantType.Plains_2,
                (BiomeGroup.Plains, 3) => WorldBiomeVariantType.Plains_3,
                (BiomeGroup.Plains, 4) => WorldBiomeVariantType.Plains_4,
                (BiomeGroup.Plains, 5) => WorldBiomeVariantType.Plains_5,
                (BiomeGroup.Plains, 6) => WorldBiomeVariantType.Plains_6,
                (BiomeGroup.Plains, 7) => WorldBiomeVariantType.Plains_7,
                (BiomeGroup.Plains, 8) => WorldBiomeVariantType.Plains_8,
                (BiomeGroup.Plains, 9) => WorldBiomeVariantType.Plains_9,

                (BiomeGroup.PlainHills, 1) => WorldBiomeVariantType.PlainHills_1,
                (BiomeGroup.PlainHills, 2) => WorldBiomeVariantType.PlainHills_2,
                (BiomeGroup.PlainHills, 3) => WorldBiomeVariantType.PlainHills_3,
                (BiomeGroup.PlainHills, 4) => WorldBiomeVariantType.PlainHills_4,
                (BiomeGroup.PlainHills, 5) => WorldBiomeVariantType.PlainHills_5,

                (BiomeGroup.Forest, 1) => WorldBiomeVariantType.Forest_1,
                (BiomeGroup.Forest, 2) => WorldBiomeVariantType.Forest_2,
                (BiomeGroup.Forest, 3) => WorldBiomeVariantType.Forest_3,
                (BiomeGroup.Forest, 4) => WorldBiomeVariantType.Forest_4,
                (BiomeGroup.Forest, 5) => WorldBiomeVariantType.Forest_5,

                (BiomeGroup.Desert, 1) => WorldBiomeVariantType.Desert_1,
                (BiomeGroup.Desert, 2) => WorldBiomeVariantType.Desert_2,
                (BiomeGroup.Desert, 3) => WorldBiomeVariantType.Desert_3,
                (BiomeGroup.Desert, 4) => WorldBiomeVariantType.Desert_4,
                (BiomeGroup.Desert, 5) => WorldBiomeVariantType.Desert_5,
                (BiomeGroup.Desert, 6) => WorldBiomeVariantType.Desert_6,
                (BiomeGroup.Desert, 7) => WorldBiomeVariantType.Desert_7,
                (BiomeGroup.Desert, 8) => WorldBiomeVariantType.Desert_8,
                (BiomeGroup.Desert, 9) => WorldBiomeVariantType.Desert_9,
                (BiomeGroup.Desert, 10) => WorldBiomeVariantType.Desert_10,
                (BiomeGroup.Desert, 11) => WorldBiomeVariantType.Desert_11,
                (BiomeGroup.Desert, 12) => WorldBiomeVariantType.Desert_12,

                (BiomeGroup.DesertHills, 1) => WorldBiomeVariantType.DesertHills_1,
                (BiomeGroup.DesertHills, 2) => WorldBiomeVariantType.DesertHills_2,
                (BiomeGroup.DesertHills, 3) => WorldBiomeVariantType.DesertHills_3,
                (BiomeGroup.DesertHills, 4) => WorldBiomeVariantType.DesertHills_4,

                (BiomeGroup.Tundra, 1) => WorldBiomeVariantType.Tundra_1,
                (BiomeGroup.Tundra, 2) => WorldBiomeVariantType.Tundra_2,

                (BiomeGroup.Wetland, 1) => WorldBiomeVariantType.Wetland_1,
                (BiomeGroup.Wetland, 2) => WorldBiomeVariantType.Wetland_2,
                (BiomeGroup.Wetland, 3) => WorldBiomeVariantType.Wetland_3,
                (BiomeGroup.Wetland, 4) => WorldBiomeVariantType.Wetland_4,
                (BiomeGroup.Wetland, 5) => WorldBiomeVariantType.Wetland_5,
                (BiomeGroup.Wetland, 6) => WorldBiomeVariantType.Wetland_6,
                (BiomeGroup.Wetland, 7) => WorldBiomeVariantType.Wetland_7,
                (BiomeGroup.Wetland, 8) => WorldBiomeVariantType.Wetland_8,

                (BiomeGroup.IceMountains, 1) => WorldBiomeVariantType.IceMountains_1,
                (BiomeGroup.IceMountains, 2) => WorldBiomeVariantType.IceMountains_2,
                (BiomeGroup.IceMountains, 3) => WorldBiomeVariantType.IceMountains_3,
                (BiomeGroup.IceMountains, 4) => WorldBiomeVariantType.IceMountains_4,
                (BiomeGroup.IceMountains, 5) => WorldBiomeVariantType.IceMountains_5,
                (BiomeGroup.IceMountains, 6) => WorldBiomeVariantType.IceMountains_6,

                (BiomeGroup.IceMountainTundraTransition, 1) => WorldBiomeVariantType.IceMountainTundraTransition_1,
                (BiomeGroup.IceMountainTundraTransition, 2) => WorldBiomeVariantType.IceMountainTundraTransition_2,

                (BiomeGroup.Sand, 1) => WorldBiomeVariantType.Sand_1,
                (BiomeGroup.Sand, 2) => WorldBiomeVariantType.Sand_2,
                (BiomeGroup.Sand, 3) => WorldBiomeVariantType.Sand_3,
                (BiomeGroup.Sand, 4) => WorldBiomeVariantType.Sand_4,
                (BiomeGroup.Sand, 5) => WorldBiomeVariantType.Sand_5,
                (BiomeGroup.Sand, 6) => WorldBiomeVariantType.Sand_6,
                (BiomeGroup.Sand, 7) => WorldBiomeVariantType.Sand_7,
                (BiomeGroup.Sand, 8) => WorldBiomeVariantType.Sand_8,
                (BiomeGroup.Sand, 9) => WorldBiomeVariantType.Sand_9,
                (BiomeGroup.Sand, 10) => WorldBiomeVariantType.Sand_10,
                (BiomeGroup.Sand, 11) => WorldBiomeVariantType.Sand_11,

                (BiomeGroup.SandHills, 1) => WorldBiomeVariantType.SandHills_1,
                (BiomeGroup.SandHills, 2) => WorldBiomeVariantType.SandHills_2,
                (BiomeGroup.SandHills, 3) => WorldBiomeVariantType.SandHills_3,

                (BiomeGroup.SandMountains, 1) => WorldBiomeVariantType.SandMountains_1,
                (BiomeGroup.SandMountains, 2) => WorldBiomeVariantType.SandMountains_2,
                (BiomeGroup.SandMountains, 3) => WorldBiomeVariantType.SandMountains_3,
                (BiomeGroup.SandMountains, 4) => WorldBiomeVariantType.SandMountains_4,

                (BiomeGroup.Jungle, 1) => WorldBiomeVariantType.Jungle_1,
                (BiomeGroup.Jungle, 2) => WorldBiomeVariantType.Jungle_2,
                (BiomeGroup.Jungle, 3) => WorldBiomeVariantType.Jungle_3,
                (BiomeGroup.Jungle, 4) => WorldBiomeVariantType.Jungle_4,
                (BiomeGroup.Jungle, 5) => WorldBiomeVariantType.Jungle_5,
                (BiomeGroup.Jungle, 6) => WorldBiomeVariantType.Jungle_6,
                (BiomeGroup.Jungle, 7) => WorldBiomeVariantType.Jungle_7,

                _ => WorldBiomeVariantType.Plains_1
            };
        }

        private static float PseudoRandomHash(int x, int y, int seed)
        {
            int h = seed + x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return (h & 0x7FFFFFFF) / (float)0x7FFFFFFF;
        }

        private static float Smoothstep(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }

    public static class NoiseGenerator
    {
        private static readonly int[] Permutation;

        static NoiseGenerator()
        {
            int[] p = { 151,160,137,91,90,15,131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
                190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,88,237,149,56,87,174,20,125,
                136,171,168, 68,175,74,165,71,134,139,48,27,166,77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,
                46,245,40,244,102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,135,130,116,188,159,
                86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,
                58,17,182,189,28,42,223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,129,22,39,253,
                19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,
                235,249,14,239,107,49,192,214,31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,138,236,205,93,222,114,
                67,29,24,72,243,141,128,195,78,66,215,61,156,180 };

            Permutation = new int[512];
            for (int i = 0; i < 256; i++)
                Permutation[i] = Permutation[i + 256] = p[i];
        }

        public static float Perlin(float x, float y, float z)
        {
            int X = (int)Math.Floor(x) & 255;
            int Y = (int)Math.Floor(y) & 255;
            int Z = (int)Math.Floor(z) & 255;

            x -= (float)Math.Floor(x);
            y -= (float)Math.Floor(y);
            z -= (float)Math.Floor(z);

            float u = Fade(x);
            float v = Fade(y);
            float w = Fade(z);

            int A = Permutation[X] + Y, AA = Permutation[A] + Z, AB = Permutation[A + 1] + Z;
            int B = Permutation[X + 1] + Y, BA = Permutation[B] + Z, BB = Permutation[B + 1] + Z;

            return Lerp(w, Lerp(v, Lerp(u, Grad(Permutation[AA], x, y, z),
                                           Grad(Permutation[BA], x - 1, y, z)),
                                   Lerp(u, Grad(Permutation[AB], x, y - 1, z),
                                           Grad(Permutation[BB], x - 1, y - 1, z))),
                           Lerp(v, Lerp(u, Grad(Permutation[AA + 1], x, y, z - 1),
                                           Grad(Permutation[BA + 1], x - 1, y, z - 1)),
                                   Lerp(u, Grad(Permutation[AB + 1], x, y - 1, z - 1),
                                           Grad(Permutation[BB + 1], x - 1, y - 1, z - 1))));
        }

        private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
        private static float Lerp(float t, float a, float b) => a + t * (b - a);
        private static float Grad(int hash, float x, float y, float z)
        {
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : h == 12 || h == 14 ? x : z;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }
}