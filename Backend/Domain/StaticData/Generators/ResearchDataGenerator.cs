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
                Id = "MARKET_COINS_1",
                Name = "Art of the deal",
                ParentId = "ECON_PROD_1",
                ResearchType = ResearchTypeEnum.Economy,
                Description = "+3% bonus to marketplaces coins generation",
                ResearchPointCost = 25,
                ResearchTimeInSeconds = 1200,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Market, Type = ModifierTypeEnum.Increased, Value = 0.03, Source = "Research: Art of the deal" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "MARKET_MERCHANT_1",
                Name = "Phoenican Inspiration",
                ParentId = "MARKET_COINS_1",
                ResearchType = ResearchTypeEnum.Economy,
                Description = "+1 to available merchants",
                ResearchPointCost = 30,
                ResearchTimeInSeconds = 900,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Placeholder, Type = ModifierTypeEnum.Flat, Value = 1, Source = "Research: Phoenican Inspiration" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "COINS_PROD_1",
                Name = "Tax Solidarity",
                ParentId = "MARKET_MERCHANT_1",
                ResearchType = ResearchTypeEnum.Economy,
                Description = "+3% increased global coins income",
                ResearchPointCost = 35,
                ResearchTimeInSeconds = 900,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Coins, Type = ModifierTypeEnum.Increased, Value = 0.03, Source = "Research: Tax Solidarity" } }
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
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.SiegeUpkeep, Type = ModifierTypeEnum.Increased, Value = 0.05, Source = "Research: Cheaper sieges" } }
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
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.SiegeStats, Type = ModifierTypeEnum.Increased, Value = 0.05, Source = "Research: Sieging Power" } }
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
                Id = "UTIL_COINS_1",
                Name = "Efficient Bureaucracy",
                ParentId = "UTIL_SUBJ_2",
                ResearchType = ResearchTypeEnum.Utility,
                Description = "1% increased coins income",
                ResearchPointCost = 60,
                ResearchTimeInSeconds = 3000,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.Coins, Type = ModifierTypeEnum.Increased, Value = 0.01, Source = "Research: Efficient Bureaucracy" } }
            });

            nodes.Add(new ResearchData
            {
                Id = "UTIL_POP_1",
                Name = "Urban Expansion",
                ParentId = "UTIL_COINS_1",
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
                Name = "Imperial Messengers",
                ParentId = "UTIL_WATCH_RANGE",
                ResearchType = ResearchTypeEnum.Utility,
                Description = "Armies are 10% faster from and to allied cities",
                ResearchPointCost = 80,
                ResearchTimeInSeconds = 3600,
                ModifiersInternal = { new Modifier { Tag = ModifierTagEnum.TravelSpeed, Type = ModifierTypeEnum.Increased, Value = 0.10, Source = "Research: Imperial Messengers" } }
            });

            AddUnlockBranch(nodes, new[]
            {
                (UnitTypeEnum.Bowmen, "UNLOCK_UNIT_BOWMEN", "Bowmen", BuildingTypeEnum.Barracks, 2),
                (UnitTypeEnum.Spearmen, "UNLOCK_UNIT_SPEARMEN", "Spearmen", BuildingTypeEnum.Barracks, 5),
                (UnitTypeEnum.Axemen, "UNLOCK_UNIT_AXEMEN", "Axemen", BuildingTypeEnum.Barracks, 8),
                (UnitTypeEnum.Swordsmen, "UNLOCK_UNIT_SWORDSMEN", "Swordsmen", BuildingTypeEnum.Barracks, 10),
                (UnitTypeEnum.Crossbowmen, "UNLOCK_UNIT_CROSSBOWMEN", "Crossbowmen", BuildingTypeEnum.Barracks, 12),
                (UnitTypeEnum.MenAtArms, "UNLOCK_UNIT_MEN_AT_ARMS", "Men At Arms", BuildingTypeEnum.Barracks, 15)
            });
            AddUnlockBranch(nodes, new[]
            {
                (UnitTypeEnum.Knights, "UNLOCK_UNIT_KNIGHTS", "Knights", BuildingTypeEnum.Stable, 15),
                (UnitTypeEnum.Cataphracts, "UNLOCK_UNIT_CATAPHRACTS", "Cataphracts", BuildingTypeEnum.Stable, 20)
            });
            AddUnlockBranch(nodes, new[]
            {
                (UnitTypeEnum.Catapult, "UNLOCK_UNIT_CATAPULT", "Catapult", BuildingTypeEnum.Workshop, 10),
                (UnitTypeEnum.Trebuchet, "UNLOCK_UNIT_TREBUCHET", "Trebuchet", BuildingTypeEnum.Workshop, 15),
                (UnitTypeEnum.Engineers, "UNLOCK_UNIT_ENGINEERS", "Engineers", BuildingTypeEnum.Workshop, 20),
                (UnitTypeEnum.Cannon, "UNLOCK_UNIT_CANNON", "Cannon", BuildingTypeEnum.Workshop, 25)
            });
            AddUnlockBranch(nodes, new[]
            {
                (UnitTypeEnum.Transport, "UNLOCK_UNIT_TRANSPORT", "Transport", BuildingTypeEnum.Harbor, 3),
                (UnitTypeEnum.WarGalley, "UNLOCK_UNIT_WAR_GALLEY", "War Galley", BuildingTypeEnum.Harbor, 5),
                (UnitTypeEnum.GrandTransport, "UNLOCK_UNIT_GRAND_TRANSPORT", "Grand Transport", BuildingTypeEnum.Harbor, 12)
            });

            nodes.Add(new ResearchData
            {
                Id = "UNLOCK_SUBJUGATION",
                Name = "Right of Subjugation",
                Description = "Unlocks the authority required to subjugate conquered cities.",
                ResearchType = ResearchTypeEnum.Unlocks,
                ResearchPointCost = 100,
                ResearchTimeInSeconds = 7200,
                Effects = { new ResearchEffectData { Type = ResearchEffectType.Subjugation } }
            });

            var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
            File.WriteAllText(path, JsonSerializer.Serialize(nodes, options));
        }

        private static void AddUnlockBranch(
            ICollection<ResearchData> nodes,
            IReadOnlyList<(UnitTypeEnum UnitType, string Id, string Name, BuildingTypeEnum BuildingType, int BuildingLevel)> branch)
        {
            string? parentId = null;
            foreach (var unlock in branch)
            {
                var (cost, seconds) = GetUnlockBalance(unlock.BuildingLevel);
                nodes.Add(new ResearchData
                {
                    Id = unlock.Id,
                    Name = unlock.Name,
                    Description = $"Unlocks {unlock.Name} recruitment.",
                    ResearchType = ResearchTypeEnum.Unlocks,
                    ParentId = parentId,
                    ResearchPointCost = cost,
                    ResearchTimeInSeconds = seconds,
                    Effects = { new ResearchEffectData { Type = ResearchEffectType.UnitRecruitment, UnitType = unlock.UnitType } }
                });
                parentId = unlock.Id;
            }
        }

        private static (double Cost, int Seconds) GetUnlockBalance(int requiredLevel) => requiredLevel switch
        {
            <= 5 => (10, 300),
            <= 10 => (25, 900),
            <= 15 => (40, 1800),
            <= 20 => (60, 2700),
            <= 25 => (80, 3600),
            _ => throw new ArgumentOutOfRangeException(nameof(requiredLevel))
        };
    }
}
