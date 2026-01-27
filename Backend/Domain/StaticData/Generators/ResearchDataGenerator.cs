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
                Name = "Better yield I",
                ResearchType = ResearchTypeEnum.Economy,
                Description = "Wood, stone and metal production increased by 1%",
                ResearchPointCost = 10,
                ResearchTimeInSeconds = 300,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.ResourceProduction, Type = ModifierTypeEnum.Increased, Value = 0.01, Source = "Research: Better yield I" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "ECON_PROD_2",
                Name = "Better yield II",
                ParentId = "ECON_PROD_1",
                ResearchType = ResearchTypeEnum.Economy,
                Description = "Wood, stone and metal production increased by 1%",
                ResearchPointCost = 10,
                ResearchTimeInSeconds = 300,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.ResourceProduction, Type = ModifierTypeEnum.Increased, Value = 0.01, Source = "Research: Better yield II" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "ECON_PROD_3",
                Name = "Better yield III",
                ParentId = "ECON_PROD_2",
                ResearchType = ResearchTypeEnum.Economy,
                Description = "Wood, stone and metal production increased by 2%",
                ResearchPointCost = 10,
                ResearchTimeInSeconds = 300,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.ResourceProduction, Type = ModifierTypeEnum.Increased, Value = 0.02, Source = "Research: Better yield III" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "MARKET_SILVER_1",
                Name = "Art of the deal",
                ParentId = "ECON_PROD_1",
                ResearchType = ResearchTypeEnum.Economy,
                Description = "+3% bonus to marketplaces silver generation",
                ResearchPointCost = 25,
                ResearchTimeInSeconds = 1200,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Market, Type = ModifierTypeEnum.Increased, Value = 0.03, Source = "Research: Art of the deal" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "MARKET_MERCHANT_1",
                Name = "Phoenican Inspiration",
                ParentId = "MARKET_SILVER_1",
                ResearchType = ResearchTypeEnum.Economy,
                Description = "+1 to available merchants",
                ResearchPointCost = 30,
                ResearchTimeInSeconds = 900,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Placeholder, Type = ModifierTypeEnum.Flat, Value = 1, Source = "Research: Phoenican Inspiration" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "SILVER_PROD_1",
                Name = "Tax Solidarity",
                ParentId = "MARKET_MERCHANT_1",
                ResearchType = ResearchTypeEnum.Economy,
                Description = "+3% increased global silver income",
                ResearchPointCost = 35,
                ResearchTimeInSeconds = 900,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Silver, Type = ModifierTypeEnum.Increased, Value = 0.03, Source = "Research: Tax Solidarity" } }
            });

            // ============================================================
            // WAR TREE
            // ============================================================
            nodes.Add(new ResearchData
            {
                Id = "SIEGE_UPKEEP_1",
                Name = "Cheaper sieges",
                ResearchType = ResearchTypeEnum.War,
                Description = "Upkeep of siege units is decreased by 5%",
                ResearchPointCost = 25,
                ResearchTimeInSeconds = 600,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Siege, Type = ModifierTypeEnum.Increased, Value = 0.05, Source = "Research: Cheaper sieges" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "UNIT_UPKEEP_1",
                Name = "Cheaper units",
                ResearchType = ResearchTypeEnum.War,
                Description = "Upkeep of all units is decreased by 2%",
                ResearchPointCost = 35,
                ResearchTimeInSeconds = 900,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Upkeep, Type = ModifierTypeEnum.Increased, Value = 0.02, Source = "Research: Cheaper units" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "SIEGE_DMG_1",
                Name = "Sieging Power",
                ParentId = "SIEGE_UPKEEP_1",
                ResearchType = ResearchTypeEnum.War,
                Description = "Siege weapons deal 5% more damage",
                ResearchPointCost = 40,
                ResearchTimeInSeconds = 3600,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Siege, Type = ModifierTypeEnum.Increased, Value = 0.05, Source = "Research: Sieging Power" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "UNIT_UPKEEP_2",
                Name = "Cheaper units",
                ParentId = "MUNIT_UPKEEP_1",
                ResearchType = ResearchTypeEnum.War,
                Description = "Upkeep of all units is decreased by 2%",
                ResearchPointCost = 45,
                ResearchTimeInSeconds = 900,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Upkeep, Type = ModifierTypeEnum.Increased, Value = 0.02, Source = "Research: Cheaper units" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "SIEGE_TT_1",
                Name = "Faster ramming",
                ParentId = "SIEGE_DMG_1",
                ResearchType = ResearchTypeEnum.War,
                Description = "Siege weapons move 2% faster",
                ResearchPointCost = 55,
                ResearchTimeInSeconds = 900,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Placeholder, Type = ModifierTypeEnum.Increased, Value = 0.02, Source = "Research: Faster ramming" } }
            });

            // ============================================================
            // UTILITY TREE
            // ============================================================

            nodes.Add(new ResearchData
            {
                Id = "UTIL_SUBJ_1",
                Name = "City Administration I",
                ResearchType = ResearchTypeEnum.Utility,
                Description = "Subjugation of your cities takes 2.5% more time",
                ResearchPointCost = 20,
                ResearchTimeInSeconds = 600,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Placeholder, Type = ModifierTypeEnum.Increased, Value = 0.025, Source = "Research: City Administration I" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "UTIL_RESIST_1",
                Name = "Civil Resistance",
                ParentId = "UTIL_SUBJ_1",
                ResearchType = ResearchTypeEnum.Utility,
                Description = "Base resistance after conquering a new city is at 60% instead of 50%",
                ResearchPointCost = 30,
                ResearchTimeInSeconds = 1200,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Placeholder, Type = ModifierTypeEnum.Flat, Value = 0.10, Source = "Research: Civil Resistance" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "UTIL_BUILD_1",
                Name = "Efficient Planning",
                ParentId = "UTIL_RESIST_1",
                ResearchType = ResearchTypeEnum.Utility,
                Description = "Building takes 5% less time",
                ResearchPointCost = 40,
                ResearchTimeInSeconds = 1800,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Construction, Type = ModifierTypeEnum.Increased, Value = 0.05, Source = "Research: Efficient Planning" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "UTIL_SUBJ_2",
                Name = "City Administration II",
                ParentId = "UTIL_BUILD_1",
                ResearchType = ResearchTypeEnum.Utility,
                Description = "Subjugation of your cities takes 2.5% more time",
                ResearchPointCost = 50,
                ResearchTimeInSeconds = 2400,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Placeholder, Type = ModifierTypeEnum.Increased, Value = 0.025, Source = "Research: City Administration II" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "UTIL_SILVER_1",
                Name = "Bureaucratic Efficiency",
                ParentId = "UTIL_SUBJ_2",
                ResearchType = ResearchTypeEnum.Utility,
                Description = "1% increased silver income",
                ResearchPointCost = 60,
                ResearchTimeInSeconds = 3000,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Silver, Type = ModifierTypeEnum.Increased, Value = 0.01, Source = "Research: Bureaucratic Efficiency" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "UTIL_POP_1",
                Name = "Urban Expansion",
                ParentId = "UTIL_SILVER_1",
                ResearchType = ResearchTypeEnum.Utility,
                Description = "+3% population in all cities",
                ResearchPointCost = 75,
                ResearchTimeInSeconds = 3600,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Population, Type = ModifierTypeEnum.Increased, Value = 0.03, Source = "Research: Urban Expansion" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "UTIL_ROAD_1",
                Name = "Road Maintenance I",
                ResearchType = ResearchTypeEnum.Utility,
                Description = "Building and repairing roads is 10% faster",
                ResearchPointCost = 20,
                ResearchTimeInSeconds = 600,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Placeholder, Type = ModifierTypeEnum.Increased, Value = 0.10, Source = "Research: Road Maintenance I" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "UTIL_WATCH_1",
                Name = "Intelligence Network",
                ParentId = "UTIL_ROAD_1",
                ResearchType = ResearchTypeEnum.Utility,
                Description = "Watchtowers now list seen units (without exact numbers)",
                ResearchPointCost = 35,
                ResearchTimeInSeconds = 1200,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Placeholder, Type = ModifierTypeEnum.Flat, Value = 1, Source = "Research: Intelligence Network" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "UTIL_ROAD_2",
                Name = "Road Maintenance II",
                ParentId = "UTIL_WATCH_1",
                ResearchType = ResearchTypeEnum.Utility,
                Description = "Building and repairing roads is 10% faster",
                ResearchPointCost = 45,
                ResearchTimeInSeconds = 1800,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Placeholder, Type = ModifierTypeEnum.Increased, Value = 0.10, Source = "Research: Road Maintenance II" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "UTIL_ROAD_COST",
                Name = "Paved Foundations",
                ParentId = "UTIL_ROAD_2",
                ResearchType = ResearchTypeEnum.Utility,
                Description = "Cost of roads is reduced by 5%",
                ResearchPointCost = 55,
                ResearchTimeInSeconds = 2400,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Placeholder, Type = ModifierTypeEnum.Increased, Value = 0.05, Source = "Research: Paved Foundations" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "UTIL_WATCH_RANGE",
                Name = "Eagle Eye",
                ParentId = "UTIL_ROAD_COST",
                ResearchType = ResearchTypeEnum.Utility,
                Description = "Watchtowers gain 10% additional vision range",
                ResearchPointCost = 65,
                ResearchTimeInSeconds = 3000,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Placeholder, Type = ModifierTypeEnum.Increased, Value = 0.10, Source = "Research: Eagle Eye" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "UTIL_ALLIED_SPEED",
                Name = "Imperial Messenger Lines",
                ParentId = "UTIL_WATCH_RANGE",
                ResearchType = ResearchTypeEnum.Utility,
                Description = "Armies are 10% faster from and to allied cities",
                ResearchPointCost = 80,
                ResearchTimeInSeconds = 3600,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.TravelSpeed, Type = ModifierTypeEnum.Increased, Value = 0.10, Source = "Research: Imperial Messenger Lines" } }
            });

            var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
            File.WriteAllText(path, JsonSerializer.Serialize(nodes, options));
        }
    }
}