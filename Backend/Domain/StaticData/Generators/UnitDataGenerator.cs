using Domain.Enums;
using Domain.StaticData.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.StaticData.Generators
{
    public static class UnitDataGenerator
    {
        public static void GenerateDefaultJson(string path)
        {
            var units = new List<UnitData>
        {
            // --- BARRACKS UNITS (Infantry/Archers) ---
            new UnitData {
                Type = UnitTypeEnum.Militia, Category = UnitCategoryEnum.Infantry,
                Power = 10, Armor = 5, Reach = 1, Discipline = 2, Mobility = 3,
                WoodCost = 40, MetalCost = 10, PopulationCost = 1, RecruitmentTimeInSeconds = 20,
                Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 1) },
                ModifiersThatAffects = { ModifierTagEnum.Infantry, ModifierTagEnum.Upkeep },
                LootCapacity = 10
            },
            new UnitData {
                Type = UnitTypeEnum.Bowmen, Category = UnitCategoryEnum.Infantry,
                Power = 12, Armor = 2, Reach = 4, Discipline = 3, Mobility = 4,
                WoodCost = 70, MetalCost = 5, PopulationCost = 1, RecruitmentTimeInSeconds = 35,
                Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 2) },
                ModifiersThatAffects = { ModifierTagEnum.Infantry, ModifierTagEnum.Upkeep },
                LootCapacity = 20
            },
            new UnitData {
                Type = UnitTypeEnum.Spearmen, Category = UnitCategoryEnum.Infantry,
                Power = 14, Armor = 8, Reach = 2, Discipline = 5, Mobility = 2,
                WoodCost = 50, MetalCost = 40, PopulationCost = 1, RecruitmentTimeInSeconds = 45,
                Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 5) },
                ModifiersThatAffects = { ModifierTagEnum.Infantry, ModifierTagEnum.Upkeep },
                LootCapacity = 30
            },
            new UnitData {
                Type = UnitTypeEnum.Axemen, Category = UnitCategoryEnum.Infantry,
                Power = 18, Armor = 6, Reach = 1, Discipline = 4, Mobility = 4,
                WoodCost = 40, MetalCost = 80, PopulationCost = 1, RecruitmentTimeInSeconds = 55,
                Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 8) },
                ModifiersThatAffects = { ModifierTagEnum.Infantry, ModifierTagEnum.Upkeep },
                LootCapacity = 5
            },
            new UnitData {
                Type = UnitTypeEnum.Swordsmen, Category = UnitCategoryEnum.Infantry,
                Power = 20, Armor = 12, Reach = 1, Discipline = 7, Mobility = 3,
                WoodCost = 30, MetalCost = 120, PopulationCost = 1, RecruitmentTimeInSeconds = 70,
                Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 10) },
                ModifiersThatAffects = { ModifierTagEnum.Infantry, ModifierTagEnum.Upkeep },
                LootCapacity = 30
            },
            new UnitData {
                Type = UnitTypeEnum.Crossbowmen, Category = UnitCategoryEnum.Infantry,
                Power = 24, Armor = 8, Reach = 4, Discipline = 6, Mobility = 2,
                WoodCost = 90, MetalCost = 100, PopulationCost = 1, RecruitmentTimeInSeconds = 90,
                Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 12) },
                ModifiersThatAffects = { ModifierTagEnum.Infantry, ModifierTagEnum.Upkeep },
                LootCapacity = 30
            },
            new UnitData {
                Type = UnitTypeEnum.MenAtArms, Category = UnitCategoryEnum.Infantry,
                Power = 26, Armor = 18, Reach = 1, Discipline = 9, Mobility = 2,
                WoodCost = 60, MetalCost = 200, PopulationCost = 2, RecruitmentTimeInSeconds = 110,
                Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 15) },
                ModifiersThatAffects = { ModifierTagEnum.Infantry, ModifierTagEnum.Upkeep },
                LootCapacity = 0
            },

            // --- STABLE UNITS (Cavalry) ---
            new UnitData {
                Type = UnitTypeEnum.LightCavalry, Category = UnitCategoryEnum.Cavalry,
                Power = 18, Armor = 8, Reach = 1, Discipline = 4, Mobility = 12,
                WoodCost = 100, MetalCost = 60, PopulationCost = 2, RecruitmentTimeInSeconds = 80,
                Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Stable, 1) },
                ModifiersThatAffects = { ModifierTagEnum.Cavalry, ModifierTagEnum.Upkeep },
                LootCapacity = 80
            },
            new UnitData {
                Type = UnitTypeEnum.Knights, Category = UnitCategoryEnum.Cavalry,
                Power = 35, Armor = 20, Reach = 2, Discipline = 9, Mobility = 9,
                WoodCost = 150, MetalCost = 350, PopulationCost = 3, RecruitmentTimeInSeconds = 180,
                Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Stable, 15) },
                ModifiersThatAffects = { ModifierTagEnum.Cavalry, ModifierTagEnum.Upkeep },
                LootCapacity = 30
            },
            new UnitData {
                Type = UnitTypeEnum.Cataphracts, Category = UnitCategoryEnum.Cavalry,
                Power = 45, Armor = 30, Reach = 2, Discipline = 10, Mobility = 7,
                WoodCost = 200, MetalCost = 500, PopulationCost = 4, RecruitmentTimeInSeconds = 240,
                Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Stable, 20) },
                ModifiersThatAffects = { ModifierTagEnum.Cavalry, ModifierTagEnum.Upkeep },
                LootCapacity = 40
            },

            // --- WORKSHOP UNITS (Siege/Engineers) ---
            new UnitData {
                Type = UnitTypeEnum.Ballista, Category = UnitCategoryEnum.Siege,
                Power = 50, Armor = 5, Reach = 6, Discipline = 5, Mobility = 1,
                WoodCost = 400, MetalCost = 150, PopulationCost = 3, RecruitmentTimeInSeconds = 300,
                Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Workshop, 1) },
                ModifiersThatAffects = { ModifierTagEnum.Siege, ModifierTagEnum.Upkeep },
                LootCapacity = 0
            },
            new UnitData {
                Type = UnitTypeEnum.Catapult, Category = UnitCategoryEnum.Siege,
                Power = 80, Armor = 5, Reach = 5, Discipline = 5, Mobility = 1,
                WoodCost = 600, MetalCost = 250, PopulationCost = 5, RecruitmentTimeInSeconds = 450,
                Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Workshop, 10) },
                ModifiersThatAffects = { ModifierTagEnum.Siege, ModifierTagEnum.Upkeep },
                LootCapacity = 0
            },
            new UnitData {
                Type = UnitTypeEnum.Trebuchet, Category = UnitCategoryEnum.Siege,
                Power = 120, Armor = 5, Reach = 8, Discipline = 5, Mobility = 0,
                WoodCost = 1000, MetalCost = 400, PopulationCost = 8, RecruitmentTimeInSeconds = 600,
                Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Workshop, 15) },
                ModifiersThatAffects = { ModifierTagEnum.Siege, ModifierTagEnum.Upkeep },
                LootCapacity = 0
            },
            new UnitData {
                Type = UnitTypeEnum.Engineers, Category = UnitCategoryEnum.Siege, // Eller special
                Power = 5, Armor = 5, Reach = 1, Discipline = 10, Mobility = 5,
                WoodCost = 200, MetalCost = 100, PopulationCost = 2, RecruitmentTimeInSeconds = 150,
                Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Workshop, 20) },
                ModifiersThatAffects = { ModifierTagEnum.Siege, ModifierTagEnum.Upkeep },
                LootCapacity = 0
            },
            new UnitData {
                Type = UnitTypeEnum.Cannon, Category = UnitCategoryEnum.Siege,
                Power = 200, Armor = 10, Reach = 6, Discipline = 8, Mobility = 1,
                WoodCost = 800, MetalCost = 1500, PopulationCost = 10, RecruitmentTimeInSeconds = 900,
                Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Workshop, 25) },
                ModifiersThatAffects = { ModifierTagEnum.Siege, ModifierTagEnum.Upkeep },
                LootCapacity = 0
            }
        };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            string json = JsonSerializer.Serialize(units, options);
            File.WriteAllText(path, json);
        }
    }
}