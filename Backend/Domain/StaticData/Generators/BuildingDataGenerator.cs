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
            buildingDataDictionary[BuildingTypeEnum.Senate] = GenerateSenateData();
            buildingDataDictionary[BuildingTypeEnum.Academy] = GenerateAcademyData();
            buildingDataDictionary[BuildingTypeEnum.Warehouse] = GenerateWarehouseData();
            buildingDataDictionary[BuildingTypeEnum.Wall] = GenerateWallData();

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

        private static List<object> GenerateSenateData()
        {
            var progressionLevels = new List<object>();
            double resourceMultiplier = 1.21;
            int initialBaseCost = 200;

            for (int currentLvl = 1; currentLvl <= 30; currentLvl++)
            {
                int totalCalculatedResourceCost = (int)(initialBaseCost * Math.Pow(resourceMultiplier, currentLvl - 1));
                int calculatedPopulationCost = (currentLvl <= 20) ? currentLvl * 2 : 40 + (int)(Math.Pow(currentLvl - 20, 2) * 2);

                var senateEntry = new SenateLevelData
                {
                    Level = currentLvl,
                    Points = CalculatePointValueForLevel(currentLvl, 1.15), // Senatet er vigtigt og giver flere point
                    BuildTime = TimeSpan.FromSeconds(Math.Pow(currentLvl, 1.9) + 60),
                    PopulationCost = calculatedPopulationCost,
                    WoodCost = (int)(totalCalculatedResourceCost * 0.4),
                    StoneCost = (int)(totalCalculatedResourceCost * 0.4),
                    MetalCost = (int)(totalCalculatedResourceCost * 0.2)
                };

                senateEntry.ModifiersThatAffects.Add(ModifierTagEnum.Construction);
                senateEntry.ModifiersInternal.Add(new Modifier
                {
                    Tag = ModifierTagEnum.Construction,
                    Type = ModifierTypeEnum.Increased,
                    Value = (currentLvl * 0.10),
                    Source = $"Senate Level {currentLvl}"
                });

                progressionLevels.Add(senateEntry);
            }
            return progressionLevels;
        }

        private static List<object> GenerateRessourceData<T>(BuildingTypeEnum buildingType) where T : BuildingLevelData, new()
        {
            var progressionLevels = new List<object>();
            double resourceMultiplier = 1.21;
            int initialBaseCost = 150;

            for (int currentLvl = 1; currentLvl <= 30; currentLvl++)
            {
                int totalCalculatedResourceCost = (int)(initialBaseCost * Math.Pow(resourceMultiplier, currentLvl - 1));
                int calculatedPopulationCost = (currentLvl <= 20) ? currentLvl * 3 : 60 + (int)(Math.Pow(currentLvl - 20, 2) * 2.4);

                var resourceEntry = new T
                {
                    Level = currentLvl,
                    Points = CalculatePointValueForLevel(currentLvl, 0.9), // Ressourcebygninger giver lidt færre point
                    BuildTime = TimeSpan.FromSeconds(Math.Pow(currentLvl, 1.8) + 30),
                    PopulationCost = calculatedPopulationCost,
                    WoodCost = (int)(totalCalculatedResourceCost * 0.35),
                    StoneCost = (int)(totalCalculatedResourceCost * 0.35),
                    MetalCost = (int)(totalCalculatedResourceCost * 0.30)
                };

                resourceEntry.ModifiersThatAffects.Add(ModifierTagEnum.ResourceProduction);
                int calculatedHourlyProduction = (int)(28.5 * Math.Pow(currentLvl, 1.1));

                if (resourceEntry is TimberCampLevelData timberData)
                {
                    resourceEntry.ModifiersThatAffects.Add(ModifierTagEnum.Wood);
                    timberData.ProductionPerHour = calculatedHourlyProduction;
                }
                else if (resourceEntry is StoneQuarryLevelData stoneData)
                {
                    resourceEntry.ModifiersThatAffects.Add(ModifierTagEnum.Stone);
                    stoneData.ProductionPerHour = calculatedHourlyProduction;
                }
                else if (resourceEntry is MetalMineLevelData metalData)
                {
                    resourceEntry.ModifiersThatAffects.Add(ModifierTagEnum.Metal);
                    metalData.ProductionPerHour = calculatedHourlyProduction;
                }

                progressionLevels.Add(resourceEntry);
            }
            return progressionLevels;
        }

        private static List<object> GenerateHousingData()
        {
            var progressionLevels = new List<object>();
            for (int currentLvl = 1; currentLvl <= 30; currentLvl++)
            {
                progressionLevels.Add(new HousingLevelData
                {
                    Level = currentLvl,
                    Points = CalculatePointValueForLevel(currentLvl, 0.85),
                    WoodCost = (int)(80 * Math.Pow(1.4, currentLvl - 1)),
                    StoneCost = (int)(60 * Math.Pow(1.4, currentLvl - 1)),
                    Population = (int)(150 * Math.Pow(currentLvl, 1.05)),
                    BuildTime = TimeSpan.FromMinutes(currentLvl * 2),
                    PopulationCost = 0,
                    ModifiersThatAffects = { ModifierTagEnum.Population }
                });
            }
            return progressionLevels;
        }

        private static List<object> GenerateRecruitmentData<T>(BuildingTypeEnum recruitmentType) where T : BuildingLevelData, new()
        {
            var progressionLevels = new List<object>();
            double resourceMultiplier = 1.21;
            int initialBaseCost = 180;

            for (int currentLvl = 1; currentLvl <= 30; currentLvl++)
            {
                int totalCalculatedResourceCost = (int)(initialBaseCost * Math.Pow(resourceMultiplier, currentLvl - 1));
                int calculatedPopulationCost = (currentLvl <= 20) ? currentLvl * 3 : 60 + (int)(Math.Pow(currentLvl - 20, 2) * 2.4);

                double woodPercentage = 0.2, stonePercentage = 0.2, metalPercentage = 0.2;
                switch (recruitmentType)
                {
                    case BuildingTypeEnum.Barracks: woodPercentage = 0.4; stonePercentage = 0.4; metalPercentage = 0.2; break;
                    case BuildingTypeEnum.Stable: woodPercentage = 0.2; stonePercentage = 0.4; metalPercentage = 0.4; break;
                    case BuildingTypeEnum.Workshop: woodPercentage = 0.4; stonePercentage = 0.2; metalPercentage = 0.4; break;
                }

                var recruitmentEntry = new T
                {
                    Level = currentLvl,
                    Points = CalculatePointValueForLevel(currentLvl, 1.0),
                    BuildTime = TimeSpan.FromSeconds(Math.Pow(currentLvl, 1.9) + 45),
                    PopulationCost = calculatedPopulationCost,
                    WoodCost = (int)(totalCalculatedResourceCost * woodPercentage),
                    StoneCost = (int)(totalCalculatedResourceCost * stonePercentage),
                    MetalCost = (int)(totalCalculatedResourceCost * metalPercentage),
                    ModifiersThatAffects = { ModifierTagEnum.Recruitment }
                };

                if (recruitmentEntry is BarracksLevelData) recruitmentEntry.ModifiersThatAffects.Add(ModifierTagEnum.Infantry);
                else if (recruitmentEntry is StableLevelData) recruitmentEntry.ModifiersThatAffects.Add(ModifierTagEnum.Cavalry);
                else if (recruitmentEntry is WorkshopLevelData) recruitmentEntry.ModifiersThatAffects.Add(ModifierTagEnum.Siege);

                double calculatedModifierValue = Math.Pow(currentLvl / 30.0, 1.7);
                recruitmentEntry.ModifiersInternal.Add(new Modifier
                {
                    Tag = ModifierTagEnum.Recruitment,
                    Type = ModifierTypeEnum.Increased,
                    Value = calculatedModifierValue,
                    Source = $"{recruitmentType} Level {currentLvl}"
                });

                progressionLevels.Add(recruitmentEntry);
            }
            return progressionLevels;
        }

        private static List<object> GenerateAcademyData()
        {
            var progressionLevels = new List<object>();
            for (int currentLvl = 1; currentLvl <= 30; currentLvl++)
            {
                int totalCalculatedResourceCost = (int)(200 * Math.Pow(1.22, currentLvl - 1));
                var academyEntry = new AcademyLevelData
                {
                    Level = currentLvl,
                    Points = CalculatePointValueForLevel(currentLvl, 1.1),
                    WoodCost = totalCalculatedResourceCost / 3,
                    StoneCost = totalCalculatedResourceCost / 3,
                    MetalCost = totalCalculatedResourceCost / 3,
                    BuildTime = TimeSpan.FromMinutes(currentLvl * 1.5),
                    PopulationCost = (currentLvl <= 20) ? currentLvl * 3 : 60 + (int)(Math.Pow(currentLvl - 20, 2) * 2.4),
                    ModifiersThatAffects = { ModifierTagEnum.Research }
                };

                academyEntry.ModifiersInternal.Add(new Modifier
                {
                    Tag = ModifierTagEnum.Research,
                    Type = ModifierTypeEnum.Increased,
                    Value = (currentLvl / 30.0),
                    Source = $"Academy Level {currentLvl}"
                });
                progressionLevels.Add(academyEntry);
            }
            return progressionLevels;
        }

        private static List<object> GenerateWarehouseData()
        {
            var progressionLevels = new List<object>();
            for (int currentLvl = 1; currentLvl <= 30; currentLvl++)
            {
                int totalCalculatedResourceCost = (int)(150 * Math.Pow(1.20, currentLvl - 1));
                progressionLevels.Add(new WarehouseLevelData
                {
                    Level = currentLvl,
                    Points = CalculatePointValueForLevel(currentLvl, 0.9),
                    WoodCost = (int)(totalCalculatedResourceCost * 0.5),
                    StoneCost = (int)(totalCalculatedResourceCost * 0.5),
                    PopulationCost = (currentLvl <= 20) ? currentLvl * 2 : 40 + (int)(Math.Pow(currentLvl - 20, 2) * 2.6),
                    BuildTime = TimeSpan.FromMinutes(currentLvl),
                    Capacity = 1500 + (int)(Math.Pow(currentLvl, 2) * 37.22),
                    ModifiersThatAffects = { ModifierTagEnum.Storage }
                });
            }
            return progressionLevels;
        }

        private static List<object> GenerateWallData()
        {
            var progressionLevels = new List<object>();
            for (int currentLvl = 1; currentLvl <= 30; currentLvl++)
            {
                int totalCalculatedResourceCost = (int)(250 * Math.Pow(1.23, currentLvl - 1));
                var wallEntry = new WallLevelData
                {
                    Level = currentLvl,
                    Points = CalculatePointValueForLevel(currentLvl, 1.1),
                    BuildTime = TimeSpan.FromMinutes(currentLvl * 2),
                    PopulationCost = currentLvl * 2,
                    ModifiersThatAffects = { ModifierTagEnum.Wall }
                };

                wallEntry.ModifiersInternal.Add(new Modifier
                {
                    Tag = ModifierTagEnum.Wall,
                    Type = ModifierTypeEnum.Increased,
                    Value = (currentLvl / 30.0) * 0.55,
                    Source = $"Wall Level {currentLvl}"
                });

                if (currentLvl <= 15)
                {
                    wallEntry.WoodCost = (int)(totalCalculatedResourceCost * 0.7);
                    wallEntry.StoneCost = (int)(totalCalculatedResourceCost * 0.2);
                    wallEntry.MetalCost = (int)(totalCalculatedResourceCost * 0.1);
                }
                else
                {
                    wallEntry.WoodCost = (int)(totalCalculatedResourceCost * 0.2);
                    wallEntry.StoneCost = (int)(totalCalculatedResourceCost * 0.45);
                    wallEntry.MetalCost = (int)(totalCalculatedResourceCost * 0.35);
                }

                progressionLevels.Add(wallEntry);
            }
            return progressionLevels;
        }
    }
}