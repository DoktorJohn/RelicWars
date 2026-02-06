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

            var manualData = new (double minutes, int wood, int stone, int metal)[]
            {
                (10, 6, 14, 0),          // Lvl 1
                (15, 6, 21, 6),          // Lvl 2
                (20, 26, 26, 0),         // Lvl 3
                (30, 24, 57, 0),         // Lvl 4
                (40, 24, 73, 24),        // Lvl 5
                (60, 52, 71, 52),        // Lvl 6
                (90, 60, 120, 60),       // Lvl 7
                (135, 68, 205, 68),      // Lvl 8
                (180, 138, 184, 138),    // Lvl 9
                (270, 149, 298, 149),    // Lvl 10
                (360, 150, 450, 150),    // Lvl 11
                (540, 305, 407, 305),    // Lvl 12
                (720, 396, 528, 396),    // Lvl 13
                (990, 557, 576, 557),    // Lvl 14
                (1260, 630, 840, 630),   // Lvl 15
                (1710, 843, 869, 843),   // Lvl 16
                (2160, 912, 1216, 912),  // Lvl 17
                (2700, 1184, 1220, 1184),// Lvl 18
                (3240, 1248, 1664, 1248),// Lvl 19
                (4320, 1584, 1632, 1584) // Lvl 20
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

                // 1. Construction Modifier (Eksisterende)
                townHallEntry.ModifiersThatAffectsThis.Add(ModifierTagEnum.Construction);
                townHallEntry.ModifiersInternal.Add(new Modifier
                {
                    Tag = ModifierTagEnum.Construction,
                    Type = ModifierTypeEnum.Increased,
                    Value = (currentLvl * 0.10),
                    Source = $"TownHall Level {currentLvl}"
                });

                // 2. Silver Modifier (Ny)
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

            // Produktionsværdier pr. time (Lvl 1 - 20) fra din tabel
            var hourlyProductionData = new int[]
            {
        20, 27, 35, 45, 55, 67, 80, 97, 115, 132,
        150, 185, 220, 260, 300, 350, 400, 460, 520, 600
            };

            // Statisk data (BuildTime i minutter, Wood/Timber, Stone, Metal/Ore)
            // Rækkefølge: Minutes, Wood, Stone, Metal
            var costAndTimeManualData = new (double minutes, int wood, int stone, int metal)[]
            {
                (5, 6, 14, 0),           // Lvl 1
                (7.5, 6, 21, 6),         // Lvl 2
                (10, 26, 26, 0),         // Lvl 3
                (15, 24, 57, 0),         // Lvl 4
                (20, 24, 73, 24),        // Lvl 5
                (40, 52, 71, 52),        // Lvl 6
                (60, 60, 120, 60),       // Lvl 7
                (90, 68, 205, 68),       // Lvl 8
                (120, 138, 184, 138),    // Lvl 9
                (180, 149, 298, 149),    // Lvl 10
                (240, 150, 450, 150),    // Lvl 11
                (360, 305, 407, 305),    // Lvl 12
                (480, 396, 528, 396),    // Lvl 13
                (660, 557, 576, 557),    // Lvl 14
                (840, 630, 840, 630),    // Lvl 15
                (1140, 843, 869, 843),   // Lvl 16
                (1440, 912, 1216, 912),  // Lvl 17
                (1800, 1184, 1220, 1184),// Lvl 18
                (2160, 1248, 1664, 1248),// Lvl 19
                (2880, 1584, 1632, 1584) // Lvl 20
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

                // Mapping af produktion baseret på bygningstype
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

            var manualData = new (double minutes, int wood, int stone, int metal)[]
            {
                (3, 6, 14, 0),           // Lvl 1
                (5, 6, 21, 6),           // Lvl 2
                (6, 26, 26, 0),          // Lvl 3
                (9, 24, 57, 0),          // Lvl 4
                (12, 24, 73, 24),        // Lvl 5
                (30, 52, 71, 52),        // Lvl 6
                (60, 60, 120, 60),       // Lvl 7
                (90, 68, 205, 68),       // Lvl 8
                (120, 138, 184, 138),    // Lvl 9
                (180, 149, 298, 149),    // Lvl 10
                (240, 150, 450, 150),    // Lvl 11
                (360, 305, 407, 305),    // Lvl 12
                (480, 396, 528, 396),    // Lvl 13
                (660, 557, 576, 557),    // Lvl 14
                (840, 630, 840, 630),    // Lvl 15
                (1140, 843, 869, 843),   // Lvl 16
                (1440, 912, 1216, 912),  // Lvl 17
                (1800, 1184, 1220, 1184),// Lvl 18
                (2160, 1248, 1664, 1248),// Lvl 19
                (2880, 1584, 1632, 1584) // Lvl 20
            };

            for (int currentLvl = 1; currentLvl <= 20; currentLvl++)
            {
                var data = manualData[currentLvl - 1];

                // Population logik: 3000 i lvl 20
                int calculatedPopulation = (currentLvl == 20) ? 3000 : (int)(80 * Math.Pow(currentLvl, 1.21));

                var housingEntry = new HousingLevelData
                {
                    Level = currentLvl,
                    Points = CalculatePointValueForLevel(currentLvl, 0.85),
                    WoodCost = data.wood,
                    StoneCost = data.stone,
                    MetalCost = data.metal,
                    Population = calculatedPopulation,
                    BuildTime = TimeSpan.FromMinutes(data.minutes),
                    ModifiersThatAffectsThis = { ModifierTagEnum.Population, ModifierTagEnum.Silver }
                };

                // Tilføjelse af Silver Modifier
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

        private static List<object> GenerateRecruitmentData<T>(BuildingTypeEnum recruitmentType) where T : BuildingLevelData, new()
        {
            var progressionLevels = new List<object>();

            var sharedCostData = new (int wood, int stone, int metal)[]
            {
                (6, 14, 0),        // Lvl 1
                (6, 21, 6),        // Lvl 2
                (26, 26, 0),       // Lvl 3
                (24, 57, 0),       // Lvl 4
                (24, 73, 24),      // Lvl 5
                (52, 71, 52),      // Lvl 6
                (60, 120, 60),     // Lvl 7
                (68, 205, 68),     // Lvl 8
                (138, 184, 138),   // Lvl 9
                (149, 298, 149),   // Lvl 10
                (150, 450, 150),   // Lvl 11
                (305, 407, 305),   // Lvl 12
                (396, 528, 396),   // Lvl 13
                (557, 576, 557),   // Lvl 14
                (630, 840, 630),   // Lvl 15
                (843, 869, 843),   // Lvl 16
                (912, 1216, 912),  // Lvl 17
                (1184, 1220, 1184),// Lvl 18
                (1248, 1664, 1248),// Lvl 19
                (1584, 1632, 1584) // Lvl 20
            };

            var barracksStablesTimeData = new double[]
            {
                10, 15, 20, 30, 40, 60, 90, 135, 180, 270,
                360, 540, 720, 990, 1200, 1710, 2160, 2700, 2880, 3600
            };

            var workshopTimeData = new double[]
            {
                15, 22, 30, 45, 60, 120, 180, 270, 360, 480,
                600, 840, 1080, 1440, 2160, 2880, 3600, 4320, 5040, 5760
            };

            for (int currentLvl = 1; currentLvl <= 20; currentLvl++)
            {
                var costs = sharedCostData[currentLvl - 1];
                double minutes = (recruitmentType == BuildingTypeEnum.Workshop)
                    ? workshopTimeData[currentLvl - 1]
                    : barracksStablesTimeData[currentLvl - 1];

                var recruitmentEntry = new T
                {
                    Level = currentLvl,
                    Points = CalculatePointValueForLevel(currentLvl, 1.0),
                    BuildTime = TimeSpan.FromMinutes(minutes),
                    WoodCost = costs.wood,
                    StoneCost = costs.stone,
                    MetalCost = costs.metal,
                    ModifiersThatAffectsThis = { ModifierTagEnum.RecruitmentSpeed }
                };

                if (recruitmentEntry is BarracksLevelData) recruitmentEntry.ModifiersThatAffectsThis.Add(ModifierTagEnum.Infantry);
                else if (recruitmentEntry is StableLevelData) recruitmentEntry.ModifiersThatAffectsThis.Add(ModifierTagEnum.Cavalry);
                else if (recruitmentEntry is WorkshopLevelData) recruitmentEntry.ModifiersThatAffectsThis.Add(ModifierTagEnum.Siege);

                double calculatedModifierValue = Math.Pow(currentLvl / 20.0, 1.7);
                recruitmentEntry.ModifiersInternal.Add(new Modifier
                {
                    Tag = ModifierTagEnum.RecruitmentSpeed,
                    Type = ModifierTypeEnum.Increased,
                    Value = calculatedModifierValue,
                    Source = $"{recruitmentType} Level {currentLvl}"
                });

                progressionLevels.Add(recruitmentEntry);
            }

            return progressionLevels;
        }

        private static List<object> GenerateUniversityData()
        {
            var progressionLevels = new List<object>();

            // Statisk data (BuildTime i minutter, Wood/Timber, Stone, Metal/Ore)
            // Rækkefølge: Minutes, Wood, Stone, Metal
            var manualData = new (double minutes, int wood, int stone, int metal)[]
            {
                (15, 6, 14, 0),          // Lvl 1
                (22, 6, 21, 6),          // Lvl 2
                (30, 26, 26, 0),         // Lvl 3
                (45, 24, 57, 0),         // Lvl 4
                (60, 24, 73, 24),        // Lvl 5
                (120, 52, 71, 52),       // Lvl 6
                (180, 60, 120, 60),      // Lvl 7
                (270, 68, 205, 68),      // Lvl 8
                (360, 138, 184, 138),    // Lvl 9
                (480, 149, 298, 149),    // Lvl 10
                (600, 150, 450, 150),    // Lvl 11
                (840, 305, 407, 305),    // Lvl 12
                (1080, 396, 528, 396),   // Lvl 13
                (1440, 557, 576, 557),   // Lvl 14
                (2160, 630, 840, 630),   // Lvl 15
                (2880, 843, 869, 843),   // Lvl 16
                (3600, 912, 1216, 912),  // Lvl 17
                (4320, 1184, 1220, 1184),// Lvl 18
                (5040, 1248, 1664, 1248),// Lvl 19
                (5760, 1584, 1632, 1584) // Lvl 20
            };

            for (int currentLvl = 1; currentLvl <= 20; currentLvl++)
            {
                var data = manualData[currentLvl - 1];

                var universityEntry = new UniversityLevelData
                {
                    Level = currentLvl,
                    Points = CalculatePointValueForLevel(currentLvl, 1.1),
                    BuildTime = TimeSpan.FromMinutes(data.minutes),
                    WoodCost = data.wood,
                    StoneCost = data.stone,
                    MetalCost = data.metal,

                    ProductionPerHour = currentLvl * 2,

                    ModifiersThatAffectsThis = { ModifierTagEnum.Research }
                };

                progressionLevels.Add(universityEntry);
            }

            return progressionLevels;
        }

        private static List<object> GenerateMarketPlaceData()
        {
            var progressionLevels = new List<object>();

            // Silver produktion (Flat rate) fra din tabel (Lvl 1 - 20)
            var silverProductionData = new double[]
            {
                400, 600, 800, 1000, 1250, 1500, 1800, 2150, 2450, 2825,
                3200, 3600, 4000, 4450, 4900, 5400, 5900, 6550, 7000, 7500
            };

            // Statisk data (BuildTime i minutter, Wood/Timber, Stone, Metal/Ore)
            // Rækkefølge: Minutes, Wood, Stone, Metal
            var costAndTimeManualData = new (double minutes, int wood, int stone, int metal)[]
            {
                (5, 6, 14, 0),           // Lvl 1
                (7.5, 6, 21, 6),         // Lvl 2
                (10, 26, 26, 0),         // Lvl 3
                (15, 24, 57, 0),         // Lvl 4
                (20, 24, 73, 24),        // Lvl 5
                (40, 52, 71, 52),        // Lvl 6
                (60, 60, 120, 60),       // Lvl 7
                (90, 68, 205, 68),       // Lvl 8
                (120, 138, 184, 138),    // Lvl 9
                (180, 149, 298, 149),    // Lvl 10
                (240, 150, 450, 150),    // Lvl 11
                (360, 305, 407, 305),    // Lvl 12
                (480, 396, 528, 396),    // Lvl 13
                (660, 557, 576, 557),    // Lvl 14
                (840, 630, 840, 630),    // Lvl 15
                (1140, 843, 869, 843),   // Lvl 16
                (1440, 912, 1216, 912),  // Lvl 17
                (1800, 1184, 1220, 1184),// Lvl 18
                (2160, 1248, 1664, 1248),// Lvl 19
                (2880, 1584, 1632, 1584) // Lvl 20
            };

            for (int currentLvl = 1; currentLvl <= 20; currentLvl++)
            {
                var data = costAndTimeManualData[currentLvl - 1];
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
                            Source = "MarketPlace Building"
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

            // Statisk data (BuildTime i minutter, Wood/Timber, Stone, Metal/Ore)
            // Rækkefølge i manualData: Minutes, Wood, Stone, Metal
            var manualData = new (double minutes, int wood, int stone, int metal)[]
            {
                (5, 6, 14, 0),           // Lvl 1
                (7.5, 6, 21, 6),         // Lvl 2
                (10, 26, 26, 0),         // Lvl 3
                (15, 24, 57, 0),         // Lvl 4
                (20, 24, 73, 24),        // Lvl 5
                (40, 52, 71, 52),        // Lvl 6
                (60, 60, 120, 60),       // Lvl 7
                (90, 68, 205, 68),       // Lvl 8
                (120, 138, 184, 138),    // Lvl 9
                (180, 149, 298, 149),    // Lvl 10
                (240, 150, 450, 150),    // Lvl 11
                (360, 305, 407, 305),    // Lvl 12
                (480, 396, 528, 396),    // Lvl 13
                (660, 557, 576, 557),    // Lvl 14
                (840, 630, 840, 630),    // Lvl 15
                (1140, 843, 869, 843),   // Lvl 16
                (1440, 912, 1216, 912),  // Lvl 17
                (1800, 1184, 1220, 1184),// Lvl 18
                (2160, 1248, 1664, 1248),// Lvl 19
                (2880, 1584, 1632, 1584) // Lvl 20
            };

            // Beregning af den nye kapacitets-kurve:
            // For at starte på ~250 og ende på 15.974 bruger vi formlen:
            // Capacity = BaseCapacity * Level^Exponent
            double baseStorageCapacity = 250.0;
            double capacityProgressionExponent = 1.3875;

            for (int currentLvl = 1; currentLvl <= 20; currentLvl++)
            {
                var data = manualData[currentLvl - 1];

                // Beregn kapacitet. Vi tvinger den præcise værdi i lvl 20.
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

            var manualData = new (double minutes, int wood, int stone, int metal)[]
            {
                (15, 6, 14, 0),          // Lvl 1
                (22, 6, 21, 6),          // Lvl 2
                (30, 26, 26, 0),         // Lvl 3
                (45, 24, 57, 0),         // Lvl 4
                (60, 24, 73, 24),        // Lvl 5
                (90, 52, 71, 52),        // Lvl 6 (1.5h)
                (120, 60, 120, 60),      // Lvl 7 (2h)
                (180, 68, 205, 68),      // Lvl 8 (3h)
                (240, 138, 184, 138),    // Lvl 9 (4h)
                (360, 149, 298, 149),    // Lvl 10 (6h)
                (480, 150, 450, 150),    // Lvl 11 (8h)
                (660, 305, 407, 305),    // Lvl 12 (11h)
                (840, 396, 528, 396),    // Lvl 13 (14h)
                (1140, 557, 576, 557),   // Lvl 14 (19h)
                (1440, 630, 840, 630),   // Lvl 15 (24h)
                (1800, 843, 869, 843),   // Lvl 16 (30h)
                (2160, 912, 1216, 912),  // Lvl 17 (36h)
                (2880, 1184, 1220, 1184),// Lvl 18 (48h)
                (3600, 1248, 1664, 1248),// Lvl 19 (60h)
                (4320, 1584, 1632, 1584) // Lvl 20 (72h)
            };

            for (int currentLvl = 1; currentLvl <= 20; currentLvl++)
            {
                var data = manualData[currentLvl - 1];

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