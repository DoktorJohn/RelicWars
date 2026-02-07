using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Domain.StaticData.Generators
{
    public static class BuildingDataGenerator
    {
        /// <summary>
        /// Genererer standard JSON-konfiguration for alle bygningstyper og niveauer.
        /// Ved alle bygninger i niveau 30 vil en by have ca. 10.000 point totalt.
        /// </summary>
        public static void GenerateDefaultJson(string targetStoragePath)
        {
            var buildingDataDictionary = new Dictionary<BuildingTypeEnum, List<object>>();

            // Ressource-bygninger (3 typer)
            buildingDataDictionary[BuildingTypeEnum.TimberCamp] = GenerateRessourceData<TimberCampLevelData>(BuildingTypeEnum.TimberCamp);
            buildingDataDictionary[BuildingTypeEnum.StoneQuarry] = GenerateRessourceData<StoneQuarryLevelData>(BuildingTypeEnum.StoneQuarry);
            buildingDataDictionary[BuildingTypeEnum.MetalMine] = GenerateRessourceData<MetalMineLevelData>(BuildingTypeEnum.MetalMine);

            // Boliger og befolkning
            buildingDataDictionary[BuildingTypeEnum.Housing] = GenerateHousingData();

            // Militær og rekruttering (3 typer)
            buildingDataDictionary[BuildingTypeEnum.Barracks] = GenerateRecruitmentData<BarracksLevelData>(BuildingTypeEnum.Barracks);
            buildingDataDictionary[BuildingTypeEnum.Stable] = GenerateRecruitmentData<StableLevelData>(BuildingTypeEnum.Stable);
            buildingDataDictionary[BuildingTypeEnum.Workshop] = GenerateRecruitmentData<WorkshopLevelData>(BuildingTypeEnum.Workshop);

            // Infrastruktur og specialbygninger
            buildingDataDictionary[BuildingTypeEnum.TownHall] = GenerateTownHallData();
            buildingDataDictionary[BuildingTypeEnum.University] = GenerateUniversityData();
            buildingDataDictionary[BuildingTypeEnum.Warehouse] = GenerateWarehouseData();
            buildingDataDictionary[BuildingTypeEnum.Wall] = GenerateWallData();
            buildingDataDictionary[BuildingTypeEnum.MarketPlace] = GenerateMarketPlaceData();

            var serializerOptions = new JsonSerializerOptions { WriteIndented = true };
            string serializedContent = JsonSerializer.Serialize(buildingDataDictionary, serializerOptions);

            File.WriteAllText(targetStoragePath, serializedContent);
        }

        /// <summary>
        /// Beregner point baseret på niveau. 
        /// Gennemsnittet for en bygning i lvl 30 er ca. 910 point (910 * 11 bygninger ≈ 10.000).
        /// </summary>
        private static int CalculatePointValueForLevel(int buildingLevel, double buildingWeight)
        {
            // Vi bruger en kurve der starter lavt og stiger til ca. 900-1000 i lvl 30
            // Formel: Vægt * 1.22^(Level - 1) * Level
            double pointCalculation = buildingWeight * Math.Pow(1.18, buildingLevel - 1) * buildingLevel;
            return (int)Math.Round(pointCalculation);
        }

        private static List<object> GenerateTownHallData()
        {
            var progressionLevels = new List<object>();

            // Silver bonus data fra din tabel (omregnet til decimalværdier)
            var silverBonusData = new double[]
            {
                0.00, 0.02, 0.04, 0.06, 0.08, 0.10, 0.12, 0.15, 0.18, 0.21,
                0.25, 0.28, 0.33, 0.37, 0.42, 0.47, 0.52, 0.56, 0.60, 0.66
            };

            // Statisk data baseret på dine nye værdier. 
            // Format: (BuildTime i minutter, Wood/Timber, Stone, Metal/Ore)
            var manualData = new (double minutes, int wood, int stone, int metal)[]
            {
                (1.25, 6, 14, 0),          // Lvl 1
                (1.875, 6, 21, 6),         // Lvl 2
                (2.5, 26, 26, 0),          // Lvl 3
                (3.75, 24, 57, 0),         // Lvl 4
                (5, 24, 73, 24),           // Lvl 5
                (7.5, 52, 71, 52),         // Lvl 6 (0.125h)
                (11.25, 60, 120, 60),      // Lvl 7 (0.1875h)
                (16.875, 68, 205, 68),     // Lvl 8 (0.28125h)
                (22.5, 138, 184, 138),     // Lvl 9 (0.375h)
                (33.75, 149, 298, 149),    // Lvl 10 (0.5625h)
                (45, 150, 450, 150),       // Lvl 11 (0.75h)
                (67.5, 305, 407, 305),     // Lvl 12 (1.125h)
                (90, 396, 528, 396),       // Lvl 13 (1.5h)
                (123.75, 557, 576, 557),   // Lvl 14 (2.0625h)
                (157.5, 630, 840, 630),    // Lvl 15 (2.625h)
                (213.75, 843, 869, 843),   // Lvl 16 (3.5625h)
                (270, 912, 1216, 912),     // Lvl 17 (4.5h)
                (337.5, 1184, 1220, 1184), // Lvl 18 (5.625h)
                (405, 1248, 1664, 1248),   // Lvl 19 (6.75h)
                (540, 1584, 1632, 1584)    // Lvl 20 (9h)
            };

            for (int currentLvl = 1; currentLvl <= 20; currentLvl++)
            {
                var data = manualData[currentLvl - 1];

                var townHallEntry = new TownHallLevelData
                {
                    Level = currentLvl,
                    Points = CalculatePointValueForLevel(currentLvl, 1.15),
                    BuildTime = TimeSpan.FromMinutes(data.minutes),
                    WoodCost = data.wood,
                    StoneCost = data.stone,
                    MetalCost = data.metal
                };

                townHallEntry.ModifiersThatAffectsThis.Add(ModifierTagEnum.Construction);
                townHallEntry.ModifiersThatAffectsThis.Add(ModifierTagEnum.Silver);

                townHallEntry.ModifiersInternal.Add(new Modifier
                {
                    Tag = ModifierTagEnum.Silver,
                    Type = ModifierTypeEnum.Increased,
                    Value = silverBonusData[currentLvl - 1],
                    Source = $"TownHall Level {currentLvl}"
                });

                progressionLevels.Add(townHallEntry);
            }

            return progressionLevels;
        }

        private static List<object> GenerateRessourceData<T>(BuildingTypeEnum buildingType) where T : BuildingLevelData, new()
        {
            var progressionLevels = new List<object>();

            // Produktionsværdier pr. time (Lvl 1 - 20)
            var hourlyProductionData = new int[]
            {
                20, 27, 35, 45, 55, 67, 80, 97, 115, 132,
                150, 185, 220, 260, 300, 350, 400, 460, 520, 600
            };

            // Statisk data (BuildTime i minutter, Wood/Timber, Stone, Metal/Ore)
            // Rækkefølge: Minutes, Wood, Stone, Metal
            var costAndTimeManualData = new (double minutes, int wood, int stone, int metal)[]
            {
                (0.625, 6, 14, 0),          // Lvl 1
                (0.9375, 6, 21, 6),         // Lvl 2
                (1.25, 26, 26, 0),          // Lvl 3
                (1.875, 24, 57, 0),         // Lvl 4
                (2.5, 24, 73, 24),          // Lvl 5
                (5.0, 52, 71, 52),          // Lvl 6
                (7.5, 60, 120, 60),         // Lvl 7 (0.125h)
                (11.25, 68, 205, 68),       // Lvl 8 (0.1875h)
                (15.0, 184, 138, 138),      // Lvl 9 (0.25h)
                (22.5, 149, 298, 149),      // Lvl 10 (0.375h)
                (30.0, 150, 450, 150),      // Lvl 11 (0.5h)
                (45.0, 305, 407, 305),      // Lvl 12 (0.75h)
                (60.0, 396, 528, 396),      // Lvl 13 (1h)
                (82.5, 557, 576, 557),      // Lvl 14 (1.375h)
                (105.0, 630, 840, 630),     // Lvl 15 (1.75h)
                (142.5, 843, 869, 843),     // Lvl 16 (2.375h)
                (180.0, 912, 1216, 912),    // Lvl 17 (3h)
                (225.0, 1184, 1220, 1184),  // Lvl 18 (3.75h)
                (270.0, 1248, 1664, 1248),  // Lvl 19 (4.5h)
                (360.0, 1584, 1632, 1584)   // Lvl 20 (6h)
            };

            for (int currentLvl = 1; currentLvl <= 20; currentLvl++)
            {
                var resourceData = costAndTimeManualData[currentLvl - 1];
                int currentLevelProduction = hourlyProductionData[currentLvl - 1];

                var resourceEntry = new T
                {
                    Level = currentLvl,
                    Points = CalculatePointValueForLevel(currentLvl, 0.9),
                    BuildTime = TimeSpan.FromMinutes(resourceData.minutes),
                    WoodCost = resourceData.wood,
                    StoneCost = resourceData.stone,
                    MetalCost = resourceData.metal,
                };

                resourceEntry.ModifiersThatAffectsThis.Add(ModifierTagEnum.ResourceProduction);

                // Mapping af produktion og specifikke tags baseret på bygningstype
                if (resourceEntry is TimberCampLevelData timberData)
                {
                    resourceEntry.ModifiersThatAffectsThis.Add(ModifierTagEnum.Wood);
                    timberData.ProductionPerHour = currentLevelProduction;
                }
                else if (resourceEntry is StoneQuarryLevelData stoneData)
                {
                    resourceEntry.ModifiersThatAffectsThis.Add(ModifierTagEnum.Stone);
                    stoneData.ProductionPerHour = currentLevelProduction;
                }
                else if (resourceEntry is MetalMineLevelData metalData)
                {
                    resourceEntry.ModifiersThatAffectsThis.Add(ModifierTagEnum.Metal);
                    metalData.ProductionPerHour = currentLevelProduction;
                }

                progressionLevels.Add(resourceEntry);
            }

            return progressionLevels;
        }

        private static List<object> GenerateHousingData()
        {
            var progressionLevels = new List<object>();

            // Silver bonus data fra din tabel (omregnet til decimalværdier: 1% = 0.01)
            var silverBonusData = new double[]
            {
                0.00, 0.01, 0.02, 0.03, 0.04, 0.06, 0.07, 0.09, 0.11, 0.13,
                0.16, 0.19, 0.22, 0.25, 0.28, 0.31, 0.34, 0.37, 0.40, 0.43
            };

            // Statisk data baseret på din tabel
            // Format: (BuildTime i minutter, Wood/Timber, Stone, Metal/Ore)
            var manualHousingData = new (double minutes, int wood, int stone, int metal)[]
            {
                (0.375, 6, 14, 0),          // Lvl 1
                (0.625, 6, 21, 6),          // Lvl 2
                (0.75, 26, 26, 0),           // Lvl 3
                (1.125, 24, 57, 0),          // Lvl 4
                (1.5, 24, 73, 24),           // Lvl 5
                (3.75, 52, 71, 52),          // Lvl 6
                (7.5, 60, 120, 60),          // Lvl 7 (0.125h)
                (11.25, 68, 205, 68),        // Lvl 8 (0.1875h)
                (15, 138, 184, 138),         // Lvl 9 (0.25h)
                (22.5, 149, 298, 149),       // Lvl 10 (0.375h)
                (30, 150, 450, 150),         // Lvl 11 (0.5h)
                (45, 305, 407, 305),         // Lvl 12 (0.75h)
                (60, 396, 528, 396),         // Lvl 13 (1h)
                (82.5, 557, 576, 557),       // Lvl 14 (1.375h)
                (105, 630, 840, 630),        // Lvl 15 (1.75h)
                (142.5, 843, 869, 843),      // Lvl 16 (2.375h)
                (180, 912, 1216, 912),       // Lvl 17 (3h)
                (225, 1184, 1220, 1184),     // Lvl 18 (3.75h)
                (270, 1248, 1664, 1248),     // Lvl 19 (4.5h)
                (360, 1584, 1632, 1584)      // Lvl 20 (6h)
            };

            for (int currentLvl = 1; currentLvl <= 20; currentLvl++)
            {
                var data = manualHousingData[currentLvl - 1];

                // Population logik: Giver plads til flere indbyggere
                int calculatedPopulation = (currentLvl == 20) ? 3000 : (int)(80 * Math.Pow(currentLvl, 1.21));

                var housingEntry = new HousingLevelData
                {
                    Level = currentLvl,
                    Points = CalculatePointValueForLevel(currentLvl, 0.85),
                    BuildTime = TimeSpan.FromMinutes(data.minutes),
                    WoodCost = data.wood,
                    StoneCost = data.stone,
                    MetalCost = data.metal,
                    Population = calculatedPopulation,

                    ModifiersThatAffectsThis = { ModifierTagEnum.Population, ModifierTagEnum.Silver }
                };

                // Silver Modifier (Skatteindkomst baseret på befolkningens størrelse)
                housingEntry.ModifiersInternal.Add(new Modifier
                {
                    Tag = ModifierTagEnum.Silver,
                    Type = ModifierTypeEnum.Increased,
                    Value = silverBonusData[currentLvl - 1],
                    Source = $"Housing District Level {currentLvl}"
                });

                progressionLevels.Add(housingEntry);
            }

            return progressionLevels;
        }

        private static List<object> GenerateRecruitmentData<T>(BuildingTypeEnum recruitmentBuildingType) where T : BuildingLevelData, new()
        {
            var progressionLevels = new List<object>();

            // Ressource-omkostninger (Deles af alle tre bygningstyper)
            // Format: Wood/Timber, Stone, Metal/Ore
            var sharedResourceCostData = new (int wood, int stone, int metal)[]
            {
                (6, 14, 0),         // Lvl 1
                (6, 21, 6),         // Lvl 2
                (26, 26, 0),        // Lvl 3
                (24, 57, 0),        // Lvl 4
                (24, 73, 24),       // Lvl 5
                (52, 71, 52),       // Lvl 6
                (60, 120, 60),      // Lvl 7
                (68, 205, 68),      // Lvl 8
                (138, 184, 138),    // Lvl 9
                (149, 298, 149),    // Lvl 10
                (150, 450, 150),    // Lvl 11
                (305, 407, 305),    // Lvl 12
                (396, 528, 396),    // Lvl 13
                (557, 576, 557),    // Lvl 14
                (630, 840, 630),    // Lvl 15
                (843, 869, 843),    // Lvl 16
                (912, 1216, 912),   // Lvl 17
                (1184, 1220, 1184), // Lvl 18
                (1248, 1664, 1248), // Lvl 19
                (1584, 1632, 1584)  // Lvl 20
            };

            // Byggetid i minutter for Barracks og Stables
            var barracksAndStablesTimeData = new double[]
            {
                1.25, 1.875, 2.5, 3.75, 5, 7.5, 11.25, 16.875, 22.5, 33.75,
                45, 67.5, 90, 123.75, 150, 213.75, 270, 337.5, 360, 450
            };

            // Byggetid i minutter for Workshop
            var workshopTimeData = new double[]
            {
                1.875, 2.75, 3.75, 5.625, 7.5, 15, 22.5, 33.75, 45, 60,
                75, 105, 135, 180, 270, 360, 450, 540, 630, 720
            };

            for (int currentLvl = 1; currentLvl <= 20; currentLvl++)
            {
                var costs = sharedResourceCostData[currentLvl - 1];

                // Vælg korrekt byggetid baseret på bygningstype
                double buildMinutes = (recruitmentBuildingType == BuildingTypeEnum.Workshop)
                    ? workshopTimeData[currentLvl - 1]
                    : barracksAndStablesTimeData[currentLvl - 1];

                var recruitmentEntry = new T
                {
                    Level = currentLvl,
                    Points = CalculatePointValueForLevel(currentLvl, 1.0),
                    BuildTime = TimeSpan.FromMinutes(buildMinutes),
                    WoodCost = costs.wood,
                    StoneCost = costs.stone,
                    MetalCost = costs.metal,
                };

                // Standard tags for rekrutterings-funktionalitet
                recruitmentEntry.ModifiersThatAffectsThis.Add(ModifierTagEnum.RecruitmentSpeed);

                // Specifikke tags baseret på enhedskategorier
                if (recruitmentEntry is BarracksLevelData)
                    recruitmentEntry.ModifiersThatAffectsThis.Add(ModifierTagEnum.Infantry);
                else if (recruitmentEntry is StableLevelData)
                    recruitmentEntry.ModifiersThatAffectsThis.Add(ModifierTagEnum.Cavalry);
                else if (recruitmentEntry is WorkshopLevelData)
                    recruitmentEntry.ModifiersThatAffectsThis.Add(ModifierTagEnum.Siege);

                // Beregning af rekrutteringsbonus (eksponentiel vækst mod niveau 20)
                // Formel: Value = (Level / 20.0)^1.7
                double calculatedModifierValue = Math.Pow(currentLvl / 20.0, 1.7);

                recruitmentEntry.ModifiersInternal.Add(new Modifier
                {
                    Tag = ModifierTagEnum.RecruitmentSpeed,
                    Type = ModifierTypeEnum.Increased,
                    Value = calculatedModifierValue,
                    Source = $"{recruitmentBuildingType} Level {currentLvl}"
                });

                progressionLevels.Add(recruitmentEntry);
            }

            return progressionLevels;
        }
        private static List<object> GenerateUniversityData()
        {
            var progressionLevels = new List<object>();

            // Statisk data baseret på dine specifikationer
            // Format: (BuildTime i minutter, Wood/Timber, Stone, Metal/Ore)
            var manualUniversityData = new (double minutes, int wood, int stone, int metal)[]
            {
                (1.875, 6, 14, 0),          // Lvl 1
                (2.75, 6, 21, 6),           // Lvl 2
                (3.75, 26, 26, 0),          // Lvl 3
                (5.625, 24, 57, 0),         // Lvl 4
                (7.5, 24, 73, 24),          // Lvl 5 (0.125h)
                (15, 52, 71, 52),           // Lvl 6 (0.25h)
                (22.5, 60, 120, 60),        // Lvl 7 (0.375h)
                (33.75, 68, 205, 68),       // Lvl 8 (0.5625h)
                (45, 138, 184, 138),        // Lvl 9 (0.75h)
                (60, 149, 298, 149),        // Lvl 10 (1h)
                (75, 150, 450, 150),        // Lvl 11 (1.25h)
                (105, 305, 407, 305),       // Lvl 12 (1.75h)
                (135, 396, 528, 396),       // Lvl 13 (2.25h)
                (180, 557, 576, 557),       // Lvl 14 (3h)
                (270, 630, 840, 630),       // Lvl 15 (4.5h)
                (360, 843, 869, 843),       // Lvl 16 (6h)
                (450, 912, 1216, 912),      // Lvl 17 (7.5h)
                (540, 1184, 1220, 1184),    // Lvl 18 (9h)
                (630, 1248, 1664, 1248),    // Lvl 19 (10.5h)
                (720, 1584, 1632, 1584)     // Lvl 20 (12h)
            };

            for (int currentLvl = 1; currentLvl <= 20; currentLvl++)
            {
                var data = manualUniversityData[currentLvl - 1];

                var universityEntry = new UniversityLevelData
                {
                    Level = currentLvl,
                    Points = CalculatePointValueForLevel(currentLvl, 1.1),
                    BuildTime = TimeSpan.FromMinutes(data.minutes),
                    WoodCost = data.wood,
                    StoneCost = data.stone,
                    MetalCost = data.metal,

                    // Basal forskningsproduktion pr. niveau
                    ProductionPerHour = currentLvl * 2
                };

                // University påvirker forskningshastighed og låser op for teknologier
                universityEntry.ModifiersThatAffectsThis.Add(ModifierTagEnum.Research);

                progressionLevels.Add(universityEntry);
            }

            return progressionLevels;
        }

        private static List<object> GenerateMarketPlaceData()
        {
            var progressionLevels = new List<object>();

            // Silver produktion (Flat rate) pr. time (Lvl 1 - 20)
            var silverProductionData = new double[]
            {
                400, 600, 800, 1000, 1250, 1500, 1800, 2150, 2450, 2825,
                3200, 3600, 4000, 4450, 4900, 5400, 5900, 6550, 7000, 7500
            };

            // Statisk data baseret på din tabel
            // Format: (BuildTime i minutter, Wood/Timber, Stone, Metal/Ore)
            var manualMarketPlaceData = new (double minutes, int wood, int stone, int metal)[]
            {
                (0.625, 6, 14, 0),          // Lvl 1
                (0.9375, 6, 21, 6),         // Lvl 2
                (1.25, 26, 26, 0),          // Lvl 3
                (1.875, 24, 57, 0),         // Lvl 4
                (2.5, 24, 73, 24),          // Lvl 5
                (5.0, 52, 71, 52),          // Lvl 6
                (7.5, 60, 120, 60),         // Lvl 7 (0.125h)
                (11.25, 68, 205, 68),       // Lvl 8 (0.1875h)
                (15.0, 138, 184, 138),      // Lvl 9 (0.25h)
                (22.5, 149, 298, 149),      // Lvl 10 (0.375h)
                (30.0, 150, 450, 150),      // Lvl 11 (0.5h)
                (45.0, 305, 407, 305),      // Lvl 12 (0.75h)
                (60.0, 396, 528, 396),      // Lvl 13 (1h)
                (82.5, 557, 576, 557),      // Lvl 14 (1.375h)
                (105.0, 630, 840, 630),     // Lvl 15 (1.75h)
                (142.5, 843, 869, 843),     // Lvl 16 (2.375h)
                (180.0, 912, 1216, 912),    // Lvl 17 (3h)
                (225.0, 1184, 1220, 1184),  // Lvl 18 (3.75h)
                (270.0, 1248, 1664, 1248),  // Lvl 19 (4.5h)
                (360.0, 1584, 1632, 1584)   // Lvl 20 (6h)
            };

            for (int currentLvl = 1; currentLvl <= 20; currentLvl++)
            {
                var data = manualMarketPlaceData[currentLvl - 1];
                double currentSilverValue = silverProductionData[currentLvl - 1];

                var marketPlaceEntry = new MarketPlaceLevelData
                {
                    Level = currentLvl,
                    Points = CalculatePointValueForLevel(currentLvl, 1.0),
                    WoodCost = data.wood,
                    StoneCost = data.stone,
                    MetalCost = data.metal,
                    BuildTime = TimeSpan.FromMinutes(data.minutes),

                    ModifiersInternal = new List<Modifier>
            {
                new Modifier
                {
                    Tag = ModifierTagEnum.Silver,
                    Type = ModifierTypeEnum.Flat,
                    Value = currentSilverValue,
                    Source = $"MarketPlace Level {currentLvl}"
                }
            },

                    ModifiersThatAffectsThis = { ModifierTagEnum.Silver }
                };

                progressionLevels.Add(marketPlaceEntry);
            }

            return progressionLevels;
        }

        private static List<object> GenerateWarehouseData()
        {
            var progressionLevels = new List<object>();

            // Statisk data baseret på din tabel
            // Format: (BuildTime i minutter, Wood/Timber, Stone, Metal/Ore)
            var manualWarehouseData = new (double minutes, int wood, int stone, int metal)[]
            {
                (0.625, 6, 14, 0),          // Lvl 1
                (0.9375, 6, 21, 6),         // Lvl 2
                (1.25, 26, 26, 0),          // Lvl 3
                (1.875, 24, 57, 0),         // Lvl 4
                (2.5, 24, 73, 24),          // Lvl 5
                (5.0, 52, 71, 52),          // Lvl 6
                (7.5, 60, 120, 60),         // Lvl 7 (0.125h)
                (11.25, 68, 205, 68),       // Lvl 8 (0.1875h)
                (15.0, 138, 184, 138),      // Lvl 9 (0.25h)
                (22.5, 149, 298, 149),      // Lvl 10 (0.375h)
                (30.0, 150, 450, 150),      // Lvl 11 (0.5h)
                (45.0, 305, 407, 305),      // Lvl 12 (0.75h)
                (60.0, 396, 528, 396),      // Lvl 13 (1h)
                (82.5, 557, 576, 557),      // Lvl 14 (1.375h)
                (105.0, 630, 840, 630),     // Lvl 15 (1.75h)
                (142.5, 843, 869, 843),     // Lvl 16 (2.375h)
                (180.0, 912, 1216, 912),    // Lvl 17 (3h)
                (225.0, 1184, 1220, 1184),  // Lvl 18 (3.75h)
                (270.0, 1248, 1664, 1248),  // Lvl 19 (4.5h)
                (360.0, 1584, 1632, 1584)   // Lvl 20 (6h)
            };

            // Beregning af kapacitets-kurve
            double baseStorageCapacity = 250.0;
            double capacityProgressionExponent = 1.3875;

            for (int currentLvl = 1; currentLvl <= 20; currentLvl++)
            {
                var data = manualWarehouseData[currentLvl - 1];

                // Beregn kapacitet med faste ankre i start og slut
                int calculatedCapacity;
                if (currentLvl == 1)
                {
                    calculatedCapacity = (int)baseStorageCapacity;
                }
                else if (currentLvl == 20)
                {
                    calculatedCapacity = 15974;
                }
                else
                {
                    calculatedCapacity = (int)(baseStorageCapacity * Math.Pow(currentLvl, capacityProgressionExponent));
                }

                progressionLevels.Add(new WarehouseLevelData
                {
                    Level = currentLvl,
                    Points = CalculatePointValueForLevel(currentLvl, 0.9),
                    WoodCost = data.wood,
                    StoneCost = data.stone,
                    MetalCost = data.metal,
                    BuildTime = TimeSpan.FromMinutes(data.minutes),
                    Capacity = calculatedCapacity,
                    ModifiersThatAffectsThis = { ModifierTagEnum.WarehouseCapacity }
                });
            }

            return progressionLevels;
        }

        private static List<object> GenerateWallData()
        {
            var progressionLevels = new List<object>();

            // Statisk data baseret på din tabel
            // Format: (BuildTime i minutter, Wood/Timber, Stone, Metal/Ore)
            var manualWallData = new (double minutes, int wood, int stone, int metal)[]
            {
                (1.875, 6, 14, 0),          // Lvl 1
                (2.75, 6, 21, 6),           // Lvl 2
                (3.75, 26, 26, 0),          // Lvl 3
                (5.625, 24, 57, 0),         // Lvl 4
                (7.5, 24, 73, 24),          // Lvl 5 (0.125h)
                (11.25, 52, 71, 52),        // Lvl 6 (0.1875h)
                (15, 60, 120, 60),          // Lvl 7 (0.25h)
                (22.5, 68, 205, 68),        // Lvl 8 (0.375h)
                (30, 138, 184, 138),        // Lvl 9 (0.5h)
                (45, 149, 298, 149),        // Lvl 10 (0.75h)
                (60, 150, 450, 150),        // Lvl 11 (1h)
                (82.5, 305, 407, 305),      // Lvl 12 (1.375h)
                (105, 396, 528, 396),       // Lvl 13 (1.75h)
                (142.5, 557, 576, 557),     // Lvl 14 (2.375h)
                (180, 630, 840, 630),       // Lvl 15 (3h)
                (225, 843, 869, 843),       // Lvl 16 (3.75h)
                (270, 912, 1216, 912),      // Lvl 17 (4.5h)
                (360, 1184, 1220, 1184),    // Lvl 18 (6h)
                (450, 1248, 1664, 1248),    // Lvl 19 (7.5h)
                (540, 1584, 1632, 1584)     // Lvl 20 (9h)
            };

            for (int currentLvl = 1; currentLvl <= 20; currentLvl++)
            {
                var data = manualWallData[currentLvl - 1];

                var wallEntry = new WallLevelData
                {
                    Level = currentLvl,
                    Points = CalculatePointValueForLevel(currentLvl, 1.1),
                    BuildTime = TimeSpan.FromMinutes(data.minutes),
                    WoodCost = data.wood,
                    StoneCost = data.stone,
                    MetalCost = data.metal,

                    ModifiersThatAffectsThis = { ModifierTagEnum.Wall }
                };

                // Forsvarsbonus modifier (Typisk brugt til at øge byens defensive styrke)
                wallEntry.ModifiersInternal.Add(new Modifier
                {
                    Tag = ModifierTagEnum.Wall,
                    Type = ModifierTypeEnum.Increased,
                    Value = currentLvl * 2.2,
                    Source = $"Wall Level {currentLvl}"
                });

                progressionLevels.Add(wallEntry);
            }

            return progressionLevels;
        }
    }
}