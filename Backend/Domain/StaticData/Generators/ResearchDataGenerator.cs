using Domain.Enums;
using Domain.StaticData.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.StaticData.Generators
{
    public static class ResearchDataGenerator
    {
        public static void GenerateDefaultJson(string path)
        {
            var nodes = new List<ResearchData>();

            // --- ECONOMY PATH (Wood, Stone, Metal Production) ---
            nodes.Add(new ResearchData
            {
                Id = "ECON_PROD_1",
                Name = "Effektiv Minedrift I",
                Description = "Øger produktionen af alle råmaterialer med 1%",
                RequiredUniversityLevel = 1,
                WoodCost = 500,
                StoneCost = 500,
                MetalCost = 500,
                ResearchTimeInSeconds = 300,
                Modifiers = {
                new Modifier { Tag = ModifierTagEnum.ResourceProduction, Type = ModifierTypeEnum.Increased, Value = 0.01, Source = "Research: Minedrift I" }
            }
            });

            nodes.Add(new ResearchData
            {
                Id = "ECON_SILVER_1",
                Name = "Handelsaftaler",
                ParentId = "ECON_PROD_1",
                Description = "+3% bonus til markedspladsens sølv-generering",
                RequiredUniversityLevel = 5,
                WoodCost = 1000,
                StoneCost = 1000,
                MetalCost = 1000,
                ResearchTimeInSeconds = 1200,
                Modifiers = {
                new Modifier { Tag = ModifierTagEnum.Silver, Type = ModifierTypeEnum.Increased, Value = 0.03, Source = "Research: Handelsaftaler" }
            }
            });

            // --- MILITARY PATH (Siege Focus) ---
            nodes.Add(new ResearchData
            {
                Id = "MIL_SIEGE_UPKEEP_1",
                Name = "Logistik for Belejring",
                Description = "Nedsætter upkeep af belejringsenheder med 5%",
                RequiredUniversityLevel = 10,
                WoodCost = 2000,
                StoneCost = 500,
                MetalCost = 2000,
                ResearchTimeInSeconds = 3600,
                Modifiers = {
                new Modifier { Tag = ModifierTagEnum.Siege, Type = ModifierTypeEnum.Decreased, Value = 0.05, Source = "Research: Belejringslogistik" },
                new Modifier { Tag = ModifierTagEnum.Upkeep, Type = ModifierTypeEnum.Decreased, Value = 0.05, Source = "Research: Belejringslogistik" }
            }
            });

            nodes.Add(new ResearchData
            {
                Id = "MIL_SIEGE_POWER_1",
                Name = "Tung Ammunition",
                ParentId = "MIL_SIEGE_UPKEEP_1",
                Description = "Belejringsvåben giver 5% mere skade",
                RequiredUniversityLevel = 15,
                WoodCost = 5000,
                StoneCost = 1000,
                MetalCost = 5000,
                ResearchTimeInSeconds = 7200,
                Modifiers = {
                new Modifier { Tag = ModifierTagEnum.Siege, Type = ModifierTypeEnum.Increased, Value = 0.05, Source = "Research: Tung Ammunition" },
                new Modifier { Tag = ModifierTagEnum.Power, Type = ModifierTypeEnum.Increased, Value = 0.05, Source = "Research: Tung Ammunition" }
            }
            });

            // --- LOGISTICS PATH ---
            nodes.Add(new ResearchData
            {
                Id = "LOG_ROADS_1",
                Name = "Vejbygning I",
                Description = "Bygning og reparation af veje er 10% hurtigere",
                RequiredUniversityLevel = 4,
                WoodCost = 300,
                StoneCost = 800,
                MetalCost = 100,
                ResearchTimeInSeconds = 600,
                Modifiers = {
                new Modifier { Tag = ModifierTagEnum.Construction, Type = ModifierTypeEnum.Increased, Value = 0.10, Source = "Research: Vejbygning I" }
            }
            });

            var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
            File.WriteAllText(path, JsonSerializer.Serialize(nodes, options));
        }
    }
}
