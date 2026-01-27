using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Domain.StaticData.Generators
{
    public static class WorldGenerationService
    {
        // 0.18f giver biomer på ca. 4-8 tiles.
        private const float BiomeGroupFrequency = 0.18f;

        // Frekvensen for bjergpas 
        private const float MountainPassFrequency = 0.10f;

        private static readonly Dictionary<BiomeGroup, int> BiomeVariantCounts = new Dictionary<BiomeGroup, int>
        {
            { BiomeGroup.Plains, 9 },
            { BiomeGroup.PlainHills, 5 },
            { BiomeGroup.Forest, 5 },
            { BiomeGroup.Desert, 12 },
            { BiomeGroup.DesertHills, 4 },
            { BiomeGroup.Tundra, 2 },
            { BiomeGroup.Wetland, 8 },
            { BiomeGroup.IceMountains, 7 },
            { BiomeGroup.IceMountainTundraTransition, 2 },
            { BiomeGroup.Sand, 11 },
            { BiomeGroup.SandHills, 3 },
            { BiomeGroup.SandMountains, 4 },
            { BiomeGroup.Jungle, 7 }
        };

        public static WorldBiomeVariantType CalculateWorldMapBiomeVariant(short x, short y, int mapSeed)
        {
            float rawNoise = NoiseGenerator.Perlin(x * BiomeGroupFrequency, y * BiomeGroupFrequency, mapSeed * 0.5f);
            float normalizedNoise = (rawNoise + 1f) / 2f;
            float biomeNoise = Smoothstep(normalizedNoise);

            BiomeGroup group = DetermineGranularBiomeGroup(biomeNoise);

            group = ApplyMountainPasses(group, x, y, mapSeed);

            float randomVariantValue = PseudoRandomHash(x, y, mapSeed);

            return ResolveVariant(group, randomVariantValue);
        }

        private static BiomeGroup DetermineGranularBiomeGroup(float noise)
        {
            if (noise < 0.14f) return BiomeGroup.IceMountains;

            if (noise < 0.20f) return BiomeGroup.IceMountainTundraTransition;


            if (noise < 0.28f) return BiomeGroup.Wetland;
            if (noise < 0.38f) return BiomeGroup.Forest;

            if (noise < 0.55f) return BiomeGroup.Plains;

            if (noise < 0.65f) return BiomeGroup.PlainHills;

            if (noise < 0.70f) return BiomeGroup.Jungle;


            if (noise < 0.75f) return BiomeGroup.PlainHills;

            if (noise < 0.77f) return BiomeGroup.SandHills;

            if (noise < 0.90f) return BiomeGroup.Sand;

            if (noise < 0.92f) return BiomeGroup.DesertHills;

            if (noise < 0.97f) return BiomeGroup.Desert;

            return BiomeGroup.SandMountains;
        }

        private static BiomeGroup ApplyMountainPasses(BiomeGroup currentGroup, int x, int y, int seed)
        {
            bool isMountain = currentGroup == BiomeGroup.SandMountains ||
                              currentGroup == BiomeGroup.IceMountains;

            if (!isMountain) return currentGroup;

            float passNoise = NoiseGenerator.Perlin(x * MountainPassFrequency, y * MountainPassFrequency, seed * 0.938f);

            if (Math.Abs(passNoise) < 0.05f)
            {
                switch (currentGroup)
                {
                    case BiomeGroup.SandMountains:
                        return BiomeGroup.DesertHills;

                    case BiomeGroup.IceMountains:
                        return BiomeGroup.Tundra;
                }
            }

            return currentGroup;
        }

        private static WorldBiomeVariantType ResolveVariant(BiomeGroup group, float randomValue)
        {
            if (!BiomeVariantCounts.TryGetValue(group, out int count))
                return WorldBiomeVariantType.Plains_1;

            int index = (int)(randomValue * count) + 1;
            if (index > count) index = count;
            if (index < 1) index = 1;

            string variantName = $"{group}_{index}";
            if (Enum.TryParse(variantName, out WorldBiomeVariantType result)) return result;
            if (Enum.TryParse($"{group}_1", out WorldBiomeVariantType baseVariant)) return baseVariant;

            return WorldBiomeVariantType.Plains_1;
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
