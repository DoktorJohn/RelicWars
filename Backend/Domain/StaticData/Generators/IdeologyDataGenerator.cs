using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace Domain.StaticData.Generators
{
    public static class IdeologyDataGenerator
    {
        public static void GenerateDefaultJson(string path)
        {
            var ideologies = new List<IdeologyData>();

            ideologies.Add(new IdeologyData
            {
                Name = "Feudalism",
                Description = "A hierarchical sociopolitical system in which vassals and nobles held the land of a ruling king, in exchange for military and economical obligations.",
                IdeologyType = IdeologyTypeEnum.Feudalism,
                ModifiersInternal = 
                { 
                    new Modifier { Tag = ModifierTagEnum.Silver, Type = ModifierTypeEnum.Increased, Value = 0.04, Source = "Feaudalism: 4% increased tax rate" },
                    new Modifier { Tag = ModifierTagEnum.ConstructionCost, Type = ModifierTypeEnum.Decreased, Value = 0.05, Source = "Feaudalism: 5% less building cost"}
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.Ideology }
            });

            ideologies.Add(new IdeologyData
            {
                Name = "Monarchy",
                Description = "An inherited form of government in which a single person, the monarch, serves as the head of state until death.",
                IdeologyType = IdeologyTypeEnum.Monarchy,
                ModifiersInternal =
                {
                    new Modifier { Tag = ModifierTagEnum.Silver, Type = ModifierTypeEnum.Increased, Value = 0.08, Source = "Monarchy: 8% increased tax rate" },
                }
            });

            ideologies.Add(new IdeologyData
            {
                Name = "Democracy",
                Description = "A form of government in which the rulers are elected by the people.",
                IdeologyType = IdeologyTypeEnum.Democracy,
                ModifiersInternal =
                {
                    new Modifier { Tag = ModifierTagEnum.Research, Type = ModifierTypeEnum.Increased, Value = 0.05, Source = "Democracy: 5% increased research rate" },
                    new Modifier { Tag = ModifierTagEnum.Population, Type = ModifierTypeEnum.Increased, Value = 0.10, Source = "Democracy: 10% increased population" },
                    new Modifier { Tag = ModifierTagEnum.Silver, Type = ModifierTypeEnum.Decreased, Value = 0.6, Source = "Democracy: 6% decreased tax rate" },
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.Ideology }
            });

            ideologies.Add(new IdeologyData
            {
                Name = "Oligarchy",
                Description = "A form of government in which the elite rules the land.",
                IdeologyType = IdeologyTypeEnum.Oligarchy,
                ModifiersInternal =
                {
                    new Modifier { Tag = ModifierTagEnum.TravelSpeed, Type = ModifierTypeEnum.Increased, Value = 0.05, Source = "Oligarchy: 5% increased travel speed" },
                    new Modifier { Tag = ModifierTagEnum.Upkeep, Type = ModifierTypeEnum.Increased, Value = 0.05, Source = "Oligarchy: 5% increased upkeep" },
                    new Modifier { Tag = ModifierTagEnum.Market, Type = ModifierTypeEnum.Increased, Value = 0.30, Source = "Oligarchy: 30% increased market silver generation" },
                    new Modifier { Tag = ModifierTagEnum.Silver, Type = ModifierTypeEnum.Increased, Value = 0.02, Source = "Oligarchy: 2% increased tax rate" },
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.Ideology }
            });

            ideologies.Add(new IdeologyData
            {
                Name = "Military Junta",
                Description = "A form of government in which the land is ruled by the army and its leaders themselves.",
                IdeologyType = IdeologyTypeEnum.MilitaryJunta,
                ModifiersInternal =
                {
                    new Modifier { Tag = ModifierTagEnum.Silver, Type = ModifierTypeEnum.Decreased, Value = 0.10, Source = "Military Junta: 10% decreased tax rate" },
                    new Modifier { Tag = ModifierTagEnum.Upkeep, Type = ModifierTypeEnum.Decreased, Value = 0.08, Source = "Military Junta: 8% decreased upkeep" },
                    new Modifier { Tag = ModifierTagEnum.RecruitmentSpeed, Type = ModifierTypeEnum.Decreased, Value = 0.5, Source = "Military Junta: 5% increased recruitment speed" },
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.Ideology }
            });

            var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
            File.WriteAllText(path, JsonSerializer.Serialize(ideologies, options));
        }
    }
}
