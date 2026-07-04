using Domain.Enums;
using Domain.StaticData.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Domain.StaticData.Generators
{
    public static class ExoticResourceDataGenerator
    {
        public static void GenerateDefaultJson(string targetStoragePath)
        {
            var balanceFile = new ExoticResourceBalanceFile
            {
                Tiers = GenerateTiers(),
                Resources = Enum.GetValues<ExoticResourceTypeEnum>()
                    .Select(resourceType => new ExoticResourceDefinitionEntry
                    {
                        ResourceType = resourceType,
                        IconKey = resourceType.ToString().ToLowerInvariant()
                    })
                    .ToList()
            };

            var serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            File.WriteAllText(targetStoragePath, JsonSerializer.Serialize(balanceFile, serializerOptions));
        }

        private static List<ExoticResourceTierData> GenerateTiers()
        {
            const int tier1Cost = 100_000;
            const int tier10Cost = 10_000_000;
            double growth = Math.Pow((double)tier10Cost / tier1Cost, 1.0 / 9.0);

            return Enumerable.Range(1, 10)
                .Select(tier => new ExoticResourceTierData
                {
                    Tier = tier,
                    WoodCost = (int)Math.Round(tier1Cost * Math.Pow(growth, tier - 1)),
                    StoneCost = (int)Math.Round(tier1Cost * Math.Pow(growth, tier - 1)),
                    MetalCost = (int)Math.Round(tier1Cost * Math.Pow(growth, tier - 1)),
                    CoinsCost = (int)Math.Round(tier1Cost * Math.Pow(growth, tier - 1)),
                    OutputPerHour = Math.Round(tier * 2.64, 2)
                })
                .ToList();
        }
    }
}
