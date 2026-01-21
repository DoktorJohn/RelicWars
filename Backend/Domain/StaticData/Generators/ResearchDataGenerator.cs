using Domain.StaticData.Data;
using Domain.Entities;
using Domain.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Domain.StaticData.Generators
{
    public static class ResearchDataGenerator
    {
        public static void GenerateDefaultJson(string path)
        {
            var nodes = new List<ResearchData>();

            // ============================================================
            // ECONOMY TREE
            // ============================================================
            nodes.Add(new ResearchData
            {
                Id = "ECON_PROD_1",
                Name = "Effektiv Minedrift I",
                ResearchType = ResearchTypeEnum.Economy,
                Description = "Øger produktionen af alle råmaterialer med 1%",
                ResearchPointCost = 100,
                ResearchTimeInSeconds = 300,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.ResourceProduction, Type = ModifierTypeEnum.Increased, Value = 0.01, Source = "Research: Minedrift I" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "ECON_SILVER_1",
                Name = "Handelsaftaler",
                ParentId = "ECON_PROD_1",
                ResearchType = ResearchTypeEnum.Economy,
                Description = "+3% bonus til markedspladsens sølv-generering",
                ResearchPointCost = 250,
                ResearchTimeInSeconds = 1200,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Silver, Type = ModifierTypeEnum.Increased, Value = 0.03, Source = "Research: Handelsaftaler" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "ECON_WAREHOUSE_1",
                Name = "Struktureret Lager",
                ParentId = "ECON_PROD_1",
                ResearchType = ResearchTypeEnum.Economy,
                Description = "Øger lagerkapaciteten med 5%",
                ResearchPointCost = 300,
                ResearchTimeInSeconds = 900,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.WarehouseCapacity, Type = ModifierTypeEnum.Increased, Value = 0.05, Source = "Research: Lager" } }
            });

            // ============================================================
            // WAR TREE
            // ============================================================
            nodes.Add(new ResearchData
            {
                Id = "MIL_INF_1",
                Name = "Skarpe Klinger",
                ResearchType = ResearchTypeEnum.War,
                Description = "Infanteri giver 5% mere skade",
                ResearchPointCost = 150,
                ResearchTimeInSeconds = 600,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Infantry, Type = ModifierTypeEnum.Increased, Value = 0.05, Source = "Research: Klinger" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "MIL_SIEGE_1",
                Name = "Belejringslogistik",
                ResearchType = ResearchTypeEnum.War,
                Description = "Nedsætter upkeep af belejringsenheder med 5%",
                ResearchPointCost = 400,
                ResearchTimeInSeconds = 3600,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Siege, Type = ModifierTypeEnum.Decreased, Value = 0.05, Source = "Research: Belejringslogistik" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "DEF_WALL_1",
                Name = "Forstærket Murværk",
                ResearchType = ResearchTypeEnum.War,
                Description = "Øger murens forsvarsevne med 10%",
                ResearchPointCost = 300,
                ResearchTimeInSeconds = 1800,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Wall, Type = ModifierTypeEnum.Increased, Value = 0.10, Source = "Research: Murværk" } }
            });

            // ============================================================
            // UTILITY TREE
            // ============================================================
            nodes.Add(new ResearchData
            {
                Id = "UTIL_ROADS_1",
                Name = "Vejbygning I",
                ResearchType = ResearchTypeEnum.Utility,
                Description = "Bygning af veje er 10% hurtigere",
                ResearchPointCost = 150,
                ResearchTimeInSeconds = 600,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Construction, Type = ModifierTypeEnum.Increased, Value = 0.10, Source = "Research: Vejbygning I" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "UTIL_STUDY_1",
                Name = "Videnskabelig Metode",
                ResearchType = ResearchTypeEnum.Utility,
                Description = "Øger RP generering med 5%",
                ResearchPointCost = 500,
                ResearchTimeInSeconds = 1500,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Research, Type = ModifierTypeEnum.Increased, Value = 0.05, Source = "Research: Metode" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "UTIL_POP_1",
                Name = "Byplanlægning",
                ResearchType = ResearchTypeEnum.Utility,
                Description = "Øger max befolkning med 100",
                ResearchPointCost = 800,
                ResearchTimeInSeconds = 3600,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Population, Type = ModifierTypeEnum.Flat, Value = 100, Source = "Research: Byplanlægning" } }
            });

            var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
            File.WriteAllText(path, JsonSerializer.Serialize(nodes, options));
        }
    }
}