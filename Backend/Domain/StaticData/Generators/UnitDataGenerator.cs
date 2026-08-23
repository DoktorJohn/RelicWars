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
            ModifiersThatAffectsThis = { ModifierTagEnum.InfantryCost, ModifierTagEnum.InfantryStats, ModifierTagEnum.InfantryUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.InfantryRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed },
            LootCapacity = 10
        },
           new UnitData {
            Type = UnitTypeEnum.MenAtArms, Category = UnitCategoryEnum.Infantry,
            Power = 5, Armor = 3, Reach = 1, Discipline = 5, Mobility = 2,
            WoodCost = 10, StoneCost = 0, MetalCost = 5, PopulationCost = 5, RecruitmentTimeInSeconds = 110,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 5) },
            ModifiersThatAffectsThis = { ModifierTagEnum.InfantryCost, ModifierTagEnum.InfantryStats, ModifierTagEnum.InfantryUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.InfantryRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed  },
            LootCapacity = 25
        },
        new UnitData {
            Type = UnitTypeEnum.Spearmen, Category = UnitCategoryEnum.Infantry,
            Power = 4, Armor = 3, Reach = 3, Discipline = 7, Mobility = 1,
            WoodCost = 8, StoneCost = 0, MetalCost = 6, PopulationCost = 6, RecruitmentTimeInSeconds = 45,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 3) },
            ModifiersThatAffectsThis = { ModifierTagEnum.InfantryCost, ModifierTagEnum.InfantryStats, ModifierTagEnum.InfantryUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.InfantryRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed },
            LootCapacity = 15
        },
        new UnitData {
            Type = UnitTypeEnum.Axemen, Category = UnitCategoryEnum.Infantry,
            Power = 6, Armor = 2, Reach = 1, Discipline = 4, Mobility = 2,
            WoodCost = 0, StoneCost = 10, MetalCost = 5, PopulationCost = 7, RecruitmentTimeInSeconds = 55,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 7) },
            ModifiersThatAffectsThis = { ModifierTagEnum.InfantryCost, ModifierTagEnum.InfantryStats, ModifierTagEnum.InfantryUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.InfantryRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed },
            LootCapacity = 20
        },
        new UnitData {
            Type = UnitTypeEnum.Swordsmen, Category = UnitCategoryEnum.Infantry,
            Power = 7, Armor = 5, Reach = 1, Discipline = 6, Mobility = 2,
            WoodCost = 15, StoneCost = 5, MetalCost = 20, PopulationCost = 9, RecruitmentTimeInSeconds = 70,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 10) },
            ModifiersThatAffectsThis = { ModifierTagEnum.InfantryCost, ModifierTagEnum.InfantryStats, ModifierTagEnum.InfantryUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.InfantryRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed },
            LootCapacity = 20
        },

        // --- RANGED (Barracks/Archery Range) ---
        new UnitData {
            Type = UnitTypeEnum.Bowmen, Category = UnitCategoryEnum.Ranged,
            Power = 4, Armor = 1, Reach = 4, Discipline = 3, Mobility = 3,
            WoodCost = 12, StoneCost = 0, MetalCost = 0, PopulationCost = 5, RecruitmentTimeInSeconds = 35,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 2) },
            ModifiersThatAffectsThis = { ModifierTagEnum.InfantryCost, ModifierTagEnum.InfantryStats, ModifierTagEnum.InfantryUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.InfantryRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed },
            LootCapacity = 10
        },
        new UnitData {
            Type = UnitTypeEnum.Crossbowmen, Category = UnitCategoryEnum.Ranged,
            Power = 7, Armor = 2, Reach = 4, Discipline = 4, Mobility = 2,
            WoodCost = 15, StoneCost = 0, MetalCost = 15, PopulationCost = 8, RecruitmentTimeInSeconds = 90,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Barracks, 12) },
            ModifiersThatAffectsThis = { ModifierTagEnum.SiegeCost, ModifierTagEnum.SiegeStats, ModifierTagEnum.SiegeUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.SiegeRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed },
            LootCapacity = 15
        },

        // --- CAVALRY (Stables) ---
        new UnitData {
            Type = UnitTypeEnum.LightCavalry, Category = UnitCategoryEnum.Cavalry,
            Power = 6, Armor = 3, Reach = 2, Discipline = 4, Mobility = 6,
            WoodCost = 10, StoneCost = 2, MetalCost = 6, PopulationCost = 8, RecruitmentTimeInSeconds = 80,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Stable, 1) },
            ModifiersThatAffectsThis = { ModifierTagEnum.CavalryStats, ModifierTagEnum.CavalryCost, ModifierTagEnum.CavalryUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.CavalryRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed },
            LootCapacity = 50
        },
        new UnitData {
            Type = UnitTypeEnum.Knights, Category = UnitCategoryEnum.Cavalry,
            Power = 9, Armor = 6, Reach = 2, Discipline = 7, Mobility = 5,
            WoodCost = 5, StoneCost = 15, MetalCost = 20, PopulationCost = 12, RecruitmentTimeInSeconds = 180,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Stable, 10) },
            ModifiersThatAffectsThis = { ModifierTagEnum.CavalryStats, ModifierTagEnum.CavalryCost, ModifierTagEnum.CavalryUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.CavalryRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed },
            LootCapacity = 40
        },
        new UnitData {
            Type = UnitTypeEnum.Cataphracts, Category = UnitCategoryEnum.Cavalry,
            Power = 10, Armor = 8, Reach = 2, Discipline = 9, Mobility = 3,
            WoodCost = 15, StoneCost = 20, MetalCost = 30, PopulationCost = 18, RecruitmentTimeInSeconds = 240,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Stable, 18) },
            ModifiersThatAffectsThis = { ModifierTagEnum.CavalryStats, ModifierTagEnum.CavalryCost, ModifierTagEnum.CavalryUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.CavalryRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed },
            LootCapacity = 30
        },

        // --- SIEGE (Workshop) ---
        new UnitData {
            Type = UnitTypeEnum.Ballista, Category = UnitCategoryEnum.Siege,
            Power = 5, Armor = 1, Reach = 6, Discipline = 2, Mobility = 1,
            WoodCost = 35, StoneCost = 30, MetalCost = 10, PopulationCost = 4, RecruitmentTimeInSeconds = 300,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Workshop, 1) },
            ModifiersThatAffectsThis = { ModifierTagEnum.SiegeCost, ModifierTagEnum.SiegeStats, ModifierTagEnum.SiegeUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.SiegeRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed },
            LootCapacity = 0
        },
        new UnitData {
            Type = UnitTypeEnum.Catapult, Category = UnitCategoryEnum.Siege,
            Power = 6, Armor = 2, Reach = 6, Discipline = 3, Mobility = 1,
            WoodCost = 40, StoneCost = 20, MetalCost = 5, PopulationCost = 5, RecruitmentTimeInSeconds = 450,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Workshop, 5) },
            ModifiersThatAffectsThis = { ModifierTagEnum.SiegeCost, ModifierTagEnum.SiegeStats, ModifierTagEnum.SiegeUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.SiegeRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed },
            LootCapacity = 0
        },
        new UnitData {
            Type = UnitTypeEnum.Trebuchet, Category = UnitCategoryEnum.Siege,
            Power = 8, Armor = 3, Reach = 7, Discipline = 4, Mobility = 1,
            WoodCost = 60, StoneCost = 10, MetalCost = 0, PopulationCost = 7, RecruitmentTimeInSeconds = 600,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Workshop, 10) },
            ModifiersThatAffectsThis = { ModifierTagEnum.SiegeCost, ModifierTagEnum.SiegeStats, ModifierTagEnum.SiegeUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.SiegeRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed },
            LootCapacity = 0
        },
        new UnitData {
            Type = UnitTypeEnum.Cannon, Category = UnitCategoryEnum.Siege,
            Power = 10, Armor = 4, Reach = 7, Discipline = 5, Mobility = 1,
            WoodCost = 10, StoneCost = 10, MetalCost = 60, PopulationCost = 9, RecruitmentTimeInSeconds = 900,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Workshop, 20) },
            ModifiersThatAffectsThis = { ModifierTagEnum.SiegeCost, ModifierTagEnum.SiegeStats, ModifierTagEnum.SiegeUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.SiegeRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed },
            LootCapacity = 0
        },
        new UnitData {
            Type = UnitTypeEnum.Engineers, Category = UnitCategoryEnum.Siege,
            Power = 2, Armor = 1, Reach = 6, Discipline = 3, Mobility = 2,
            WoodCost = 12, StoneCost = 12, MetalCost = 70, PopulationCost = 10, RecruitmentTimeInSeconds = 150,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Workshop, 15) },
            ModifiersThatAffectsThis = { ModifierTagEnum.SiegeCost, ModifierTagEnum.SiegeStats, ModifierTagEnum.SiegeUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.SiegeRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed },
            LootCapacity = 10
        },

        // --- NAVAL (Harbor) ---
        new UnitData {
            Type = UnitTypeEnum.Longship, Category = UnitCategoryEnum.Naval,
            Power = 5, Armor = 3, Reach = 3, Discipline = 4, Mobility = 7,
            WoodCost = 18, StoneCost = 4, MetalCost = 8, PopulationCost = 6, UnitCapacity = 0, RecruitmentTimeInSeconds = 90,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Harbor, 1) },
            ModifiersThatAffectsThis = { ModifierTagEnum.NavalCost, ModifierTagEnum.NavalStats, ModifierTagEnum.NavalUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.NavalRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed },
            LootCapacity = 15
        },
        new UnitData {
            Type = UnitTypeEnum.WarGalley, Category = UnitCategoryEnum.Naval,
            Power = 9, Armor = 5, Reach = 4, Discipline = 6, Mobility = 6,
            WoodCost = 30, StoneCost = 10, MetalCost = 20, PopulationCost = 10, UnitCapacity = 0, RecruitmentTimeInSeconds = 180,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Harbor, 5) },
            ModifiersThatAffectsThis = { ModifierTagEnum.NavalCost, ModifierTagEnum.NavalStats, ModifierTagEnum.NavalUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.NavalRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed },
            LootCapacity = 25
        },
        new UnitData {
            Type = UnitTypeEnum.Transport, Category = UnitCategoryEnum.Naval,
            Power = 1, Armor = 4, Reach = 2, Discipline = 2, Mobility = 8,
            WoodCost = 22, StoneCost = 8, MetalCost = 12, PopulationCost = 8, UnitCapacity = 150, RecruitmentTimeInSeconds = 150,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Harbor, 3) },
            ModifiersThatAffectsThis = { ModifierTagEnum.NavalCost, ModifierTagEnum.NavalStats, ModifierTagEnum.NavalUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.NavalRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed },
            LootCapacity = 0
        },
        new UnitData {
            Type = UnitTypeEnum.GrandTransport, Category = UnitCategoryEnum.Naval,
            Power = 2, Armor = 7, Reach = 2, Discipline = 3, Mobility = 6,
            WoodCost = 40, StoneCost = 18, MetalCost = 24, PopulationCost = 14, UnitCapacity = 500, RecruitmentTimeInSeconds = 300,
            Prerequisites = new List<UnitRequirement> { new(BuildingTypeEnum.Harbor, 12) },
            ModifiersThatAffectsThis = { ModifierTagEnum.NavalCost, ModifierTagEnum.NavalStats, ModifierTagEnum.NavalUpkeep, ModifierTagEnum.Upkeep, ModifierTagEnum.NavalRecruitmentSpeed, ModifierTagEnum.RecruitmentSpeed },
            LootCapacity = 0
        }
        };

            var eliteUnits = new HashSet<UnitTypeEnum>
            {
            UnitTypeEnum.Knights,
            UnitTypeEnum.Cataphracts,
            UnitTypeEnum.Cannon
        };
            foreach (var unit in units) unit.IsElite = eliteUnits.Contains(unit.Type);

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
