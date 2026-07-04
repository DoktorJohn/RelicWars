using Domain.Enums;
using Domain.StaticData.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Domain.StaticData.Readers
{
    public class ExoticResourceDataReader
    {
        private Dictionary<ExoticResourceTypeEnum, ExoticResourceDefinitionData> _definitions = new();

        public void Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Filen {path} blev ikke fundet!");

            string json = File.ReadAllText(path);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var balanceFile = JsonSerializer.Deserialize<ExoticResourceBalanceFile>(json, options)
                ?? new ExoticResourceBalanceFile();

            _definitions = balanceFile.Resources.ToDictionary(
                definition => definition.ResourceType,
                definition => new ExoticResourceDefinitionData
                {
                    ResourceType = definition.ResourceType,
                    IconKey = definition.IconKey,
                    Tiers = balanceFile.Tiers.Select(tier => new ExoticResourceTierData
                    {
                        Tier = tier.Tier,
                        WoodCost = tier.WoodCost,
                        StoneCost = tier.StoneCost,
                        MetalCost = tier.MetalCost,
                        CoinsCost = tier.CoinsCost,
                        OutputPerHour = tier.OutputPerHour
                    }).ToList()
                });
        }

        public ExoticResourceDefinitionData GetDefinition(ExoticResourceTypeEnum type)
        {
            if (_definitions.TryGetValue(type, out var definition))
                return definition;

            throw new Exception($"Exotic resource {type} blev ikke fundet i data!");
        }

        public ExoticResourceTierData GetTierData(ExoticResourceTypeEnum type, int tier)
        {
            var definition = GetDefinition(type);
            var tierData = definition.Tiers.FirstOrDefault(t => t.Tier == tier);
            if (tierData != null)
                return tierData;

            throw new Exception($"Tier {tier} for exotic resource {type} blev ikke fundet i data!");
        }

        public IReadOnlyCollection<ExoticResourceDefinitionData> GetAll() => _definitions.Values.ToList();
    }
}
