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
            Power = 2, Armor = 1, Reach = 1, Discipline = 2, Mobility = 2,
            WoodCost = 5, StoneCost = 5, MetalCost = 0, PopulationCost = 3, RecruitmentTimeInSeconds = 20,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 1) },
            ModifiersThatAffectsThis = { ModifierTagEnum.Infantry, ModifierTagEnum.Upkeep },
            LootCapacity = 10
        },
           new UnitData {
            Type = UnitTypeEnum.MenAtArms, Category = UnitCategoryEnum.Infantry,
            Power = 5, Armor = 3, Reach = 1, Discipline = 5, Mobility = 2,
            WoodCost = 10, StoneCost = 0, MetalCost = 5, PopulationCost = 5, RecruitmentTimeInSeconds = 110,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 15) },
            ModifiersThatAffectsThis = { ModifierTagEnum.Infantry, ModifierTagEnum.Upkeep },
            LootCapacity = 25
        },
        new UnitData {
            Type = UnitTypeEnum.Spearmen, Category = UnitCategoryEnum.Infantry,
            Power = 4, Armor = 3, Reach = 3, Discipline = 7, Mobility = 1,
            WoodCost = 8, StoneCost = 0, MetalCost = 6, PopulationCost = 6, RecruitmentTimeInSeconds = 45,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 5) },
            ModifiersThatAffectsThis = { ModifierTagEnum.Infantry, ModifierTagEnum.Upkeep },
            LootCapacity = 15
        },
        new UnitData {
            Type = UnitTypeEnum.Axemen, Category = UnitCategoryEnum.Infantry,
            Power = 6, Armor = 2, Reach = 1, Discipline = 4, Mobility = 2,
            WoodCost = 0, StoneCost = 10, MetalCost = 5, PopulationCost = 7, RecruitmentTimeInSeconds = 55,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 8) },
            ModifiersThatAffectsThis = { ModifierTagEnum.Infantry, ModifierTagEnum.Upkeep },
            LootCapacity = 20
        },
        new UnitData {
            Type = UnitTypeEnum.Swordsmen, Category = UnitCategoryEnum.Infantry,
            Power = 7, Armor = 5, Reach = 1, Discipline = 6, Mobility = 2,
            WoodCost = 15, StoneCost = 5, MetalCost = 20, PopulationCost = 9, RecruitmentTimeInSeconds = 70,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 10) },
            ModifiersThatAffectsThis = { ModifierTagEnum.Infantry, ModifierTagEnum.Upkeep },
            LootCapacity = 20
        },

        // --- RANGED (Barracks/Archery Range) ---
        new UnitData {
            Type = UnitTypeEnum.Bowmen, Category = UnitCategoryEnum.Ranged,
            Power = 4, Armor = 1, Reach = 4, Discipline = 3, Mobility = 3,
            WoodCost = 12, StoneCost = 0, MetalCost = 0, PopulationCost = 5, RecruitmentTimeInSeconds = 35,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 2) },
            ModifiersThatAffectsThis = { ModifierTagEnum.Infantry, ModifierTagEnum.Upkeep },
            LootCapacity = 10
        },
        new UnitData {
            Type = UnitTypeEnum.Crossbowmen, Category = UnitCategoryEnum.Ranged,
            Power = 7, Armor = 2, Reach = 4, Discipline = 4, Mobility = 2,
            WoodCost = 15, StoneCost = 0, MetalCost = 15, PopulationCost = 8, RecruitmentTimeInSeconds = 90,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 12) },
            ModifiersThatAffectsThis = { ModifierTagEnum.Infantry, ModifierTagEnum.Upkeep },
            LootCapacity = 15
        },

        // --- CAVALRY (Stables) ---
        new UnitData {
            Type = UnitTypeEnum.LightCavalry, Category = UnitCategoryEnum.Cavalry,
            Power = 6, Armor = 3, Reach = 2, Discipline = 4, Mobility = 6,
            WoodCost = 10, StoneCost = 2, MetalCost = 6, PopulationCost = 8, RecruitmentTimeInSeconds = 80,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Stable, 1) },
            ModifiersThatAffectsThis = { ModifierTagEnum.Cavalry, ModifierTagEnum.Upkeep },
            LootCapacity = 50
        },
        new UnitData {
            Type = UnitTypeEnum.Knights, Category = UnitCategoryEnum.Cavalry,
            Power = 9, Armor = 6, Reach = 2, Discipline = 7, Mobility = 5,
            WoodCost = 5, StoneCost = 15, MetalCost = 20, PopulationCost = 12, RecruitmentTimeInSeconds = 180,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Stable, 15) },
            ModifiersThatAffectsThis = { ModifierTagEnum.Cavalry, ModifierTagEnum.Upkeep },
            LootCapacity = 40
        },
        new UnitData {
            Type = UnitTypeEnum.Cataphracts, Category = UnitCategoryEnum.Cavalry,
            Power = 10, Armor = 8, Reach = 2, Discipline = 9, Mobility = 3,
            WoodCost = 15, StoneCost = 20, MetalCost = 30, PopulationCost = 18, RecruitmentTimeInSeconds = 240,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Stable, 20) },
            ModifiersThatAffectsThis = { ModifierTagEnum.Cavalry, ModifierTagEnum.Upkeep },
            LootCapacity = 30
        },

        // --- SIEGE (Workshop) ---
        new UnitData {
            Type = UnitTypeEnum.Ballista, Category = UnitCategoryEnum.Siege,
            Power = 5, Armor = 1, Reach = 6, Discipline = 2, Mobility = 1,
            WoodCost = 35, StoneCost = 30, MetalCost = 10, PopulationCost = 4, RecruitmentTimeInSeconds = 300,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Workshop, 1) },
            ModifiersThatAffectsThis = { ModifierTagEnum.Siege, ModifierTagEnum.Upkeep },
            LootCapacity = 0
        },
        new UnitData {
            Type = UnitTypeEnum.Catapult, Category = UnitCategoryEnum.Siege,
            Power = 6, Armor = 2, Reach = 6, Discipline = 3, Mobility = 1,
            WoodCost = 40, StoneCost = 20, MetalCost = 5, PopulationCost = 5, RecruitmentTimeInSeconds = 450,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Workshop, 10) },
            ModifiersThatAffectsThis = { ModifierTagEnum.Siege, ModifierTagEnum.Upkeep },
            LootCapacity = 0
        },
        new UnitData {
            Type = UnitTypeEnum.Trebuchet, Category = UnitCategoryEnum.Siege,
            Power = 8, Armor = 3, Reach = 7, Discipline = 4, Mobility = 1,
            WoodCost = 60, StoneCost = 10, MetalCost = 0, PopulationCost = 7, RecruitmentTimeInSeconds = 600,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Workshop, 15) },
            ModifiersThatAffectsThis = { ModifierTagEnum.Siege, ModifierTagEnum.Upkeep },
            LootCapacity = 0
        },
        new UnitData {
            Type = UnitTypeEnum.Cannon, Category = UnitCategoryEnum.Siege,
            Power = 10, Armor = 4, Reach = 7, Discipline = 5, Mobility = 1,
            WoodCost = 10, StoneCost = 10, MetalCost = 60, PopulationCost = 9, RecruitmentTimeInSeconds = 900,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Workshop, 25) },
            ModifiersThatAffectsThis = { ModifierTagEnum.Siege, ModifierTagEnum.Upkeep },
            LootCapacity = 0
        },
        new UnitData {
            Type = UnitTypeEnum.Engineers, Category = UnitCategoryEnum.Siege,
            Power = 2, Armor = 1, Reach = 6, Discipline = 3, Mobility = 2,
            WoodCost = 12, StoneCost = 12, MetalCost = 70, PopulationCost = 10, RecruitmentTimeInSeconds = 150,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Workshop, 20) },
            ModifiersThatAffectsThis = { ModifierTagEnum.Siege, ModifierTagEnum.Upkeep },
            LootCapacity = 10
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