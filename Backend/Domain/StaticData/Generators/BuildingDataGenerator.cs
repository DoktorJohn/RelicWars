using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Domain.StaticData.Generators
{
    public static class BuildingDataGenerator
    {
        public static void GenerateDefaultJson(string path)
        {
            var data = new Dictionary<BuildingTypeEnum, List<object>>();

            // Generer data for de 3 ressource-typer
            data[BuildingTypeEnum.TimberCamp] = GenerateRessourceData<TimberCampLevelData>(BuildingTypeEnum.TimberCamp);
            data[BuildingTypeEnum.StoneQuarry] = GenerateRessourceData<StoneQuarryLevelData>(BuildingTypeEnum.StoneQuarry);
            data[BuildingTypeEnum.MetalMine] = GenerateRessourceData<MetalMineLevelData>(BuildingTypeEnum.MetalMine);

            // Generer Farm/Housing (Lvl 1-30)
            data[BuildingTypeEnum.Housing] = GenerateHousingData();

            data[BuildingTypeEnum.Barracks] = GenerateRecruitmentData<BarracksLevelData>(BuildingTypeEnum.Barracks);
            data[BuildingTypeEnum.Stable] = GenerateRecruitmentData<StableLevelData>(BuildingTypeEnum.Stable);
            data[BuildingTypeEnum.Workshop] = GenerateRecruitmentData<WorkshopLevelData>(BuildingTypeEnum.Workshop);

            data[BuildingTypeEnum.Senate] = GenerateSenateData();
            data[BuildingTypeEnum.Academy] = GenerateAcademyData();
            data[BuildingTypeEnum.Warehouse] = GenerateWarehouseData();
            data[BuildingTypeEnum.Wall] = GenerateWallData();

            

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(path, json);
        }

        private static List<object> GenerateSenateData()
        {
            var levels = new List<object>();
            double multiplier = 1.21;
            int initialBase = 200;

            for (int lvl = 1; lvl <= 30; lvl++)
            {
                int totalResCost = (int)(initialBase * Math.Pow(multiplier, lvl - 1));
                int popCost = (lvl <= 20) ? lvl * 2 : 40 + (int)(Math.Pow(lvl - 20, 2) * 2);

                var entry = new SenateLevelData
                {
                    Level = lvl,
                    BuildTime = TimeSpan.FromSeconds(Math.Pow(lvl, 1.9) + 60),
                    PopulationCost = popCost,
                    WoodCost = (int)(totalResCost * 0.4),
                    StoneCost = (int)(totalResCost * 0.4),
                    MetalCost = (int)(totalResCost * 0.2)
                };

                entry.ModifiersThatAffects.Add(ModifierTagEnum.Construction);

                // Modifer: Senate giver Increased Construction speed
                // Vi omregner (100 + lvl * 10) til en procent-stigning (f.eks. 0.10, 0.20...)
                entry.ModifiersInternal.Add(new Modifier
                {
                    Tag = ModifierTagEnum.Construction,
                    Type = ModifierTypeEnum.Increased,
                    Value = (lvl * 0.10), // 10% per level
                    Source = $"Senate Level {lvl}"
                });

                if (lvl == 20) entry.Prerequisites.Add(new(BuildingTypeEnum.Warehouse, 15));

                levels.Add(entry);
            }
            return levels;
        }

        private static List<object> GenerateRessourceData<T>(BuildingTypeEnum type) where T : BuildingLevelData, new()
        {
            var levels = new List<object>();
            // Vi bruger en lavere multiplier (1.21) for at lande på ~40k totalt ved lvl 30
            double multiplier = 1.21;
            int initialBase = 150;

            for (int lvl = 1; lvl <= 30; lvl++)
            {
                // Totalprisen for alle 3 ressourcer lander på ca. 38.000 - 42.000 ved lvl 30
                int totalResCost = (int)(initialBase * Math.Pow(multiplier, lvl - 1));

                // Population logik (beholdt fra sidst): billig til 20, 300 ved lvl 30
                int popCost = (lvl <= 20) ? lvl * 3 : 60 + (int)(Math.Pow(lvl - 20, 2) * 2.4);

                var entry = new T
                {
                    Level = lvl,
                    // Tid skal også ned, når prisen er lavere
                    BuildTime = TimeSpan.FromSeconds(Math.Pow(lvl, 1.8) + 30),
                    PopulationCost = popCost,
                    // Fordeling af de 40.000 ud på de 3 ressourcer (ca. 1/3 til hver)
                    WoodCost = (int)(totalResCost * 0.35),
                    StoneCost = (int)(totalResCost * 0.35),
                    MetalCost = (int)(totalResCost * 0.30)
                };

                entry.ModifiersThatAffects.Add(ModifierTagEnum.ResourceProduction);

                int prod = (int)(28.5 * Math.Pow(lvl, 1.1));

                if (entry is TimberCampLevelData t)
                {
                    entry.ModifiersThatAffects.Add(ModifierTagEnum.Wood);
                    t.ProductionPerHour = prod;
                }
                else if (entry is StoneQuarryLevelData s) 
                {
                    entry.ModifiersThatAffects.Add(ModifierTagEnum.Stone);
                    s.ProductionPerHour = prod;
                }

                else if (entry is MetalMineLevelData m)
                {
                    entry.ModifiersThatAffects.Add(ModifierTagEnum.Metal);
                    m.ProductionPerHour = prod;
                }

                levels.Add(entry);
            }
            return levels;
        }

        private static List<object> GenerateHousingData()
        {
            var levels = new List<object>();
            for (int lvl = 1; lvl <= 30; lvl++)
            {
                levels.Add(new HousingLevelData
                {
                    Level = lvl,
                    WoodCost = (int)(80 * Math.Pow(1.4, lvl - 1)),
                    StoneCost = (int)(60 * Math.Pow(1.4, lvl - 1)),
                    Population = (int)(150 * Math.Pow(lvl, 1.05)), // Lvl 30 giver ~5000 total
                    BuildTime = TimeSpan.FromMinutes(lvl * 2),
                    PopulationCost = 0,
                    ModifiersThatAffects = { ModifierTagEnum.Population }
                });

            }
            return levels;
        }

        private static List<object> GenerateRecruitmentData<T>(BuildingTypeEnum type) where T : BuildingLevelData, new()
        {
            var levels = new List<object>();
            double multiplier = 1.21;
            int initialBase = 180;

            for (int lvl = 1; lvl <= 30; lvl++)
            {
                int totalResCost = (int)(initialBase * Math.Pow(multiplier, lvl - 1));
                int popCost = (lvl <= 20) ? lvl * 3 : 60 + (int)(Math.Pow(lvl - 20, 2) * 2.4);

                double woodPct = 0.2, stonePct = 0.2, metalPct = 0.2;
                switch (type)
                {
                    case BuildingTypeEnum.Barracks: woodPct = 0.4; stonePct = 0.4; metalPct = 0.2; break;
                    case BuildingTypeEnum.Stable: woodPct = 0.2; stonePct = 0.4; metalPct = 0.4; break;
                    case BuildingTypeEnum.Workshop: woodPct = 0.4; stonePct = 0.2; metalPct = 0.4; break;
                }

                var entry = new T
                {
                    Level = lvl,
                    BuildTime = TimeSpan.FromSeconds(Math.Pow(lvl, 1.9) + 45),
                    PopulationCost = popCost,
                    WoodCost = (int)(totalResCost * woodPct),
                    StoneCost = (int)(totalResCost * stonePct),
                    MetalCost = (int)(totalResCost * metalPct),
                    ModifiersThatAffects = { ModifierTagEnum.Recruitment }
                };

                if (entry is BarracksLevelData)
                {
                    entry.ModifiersThatAffects.Add(ModifierTagEnum.Infantry);
                    
                }
                else if (entry is StableLevelData)
                {
                    entry.ModifiersThatAffects.Add(ModifierTagEnum.Cavalry);
                }

                else if (entry is WorkshopLevelData)
                {
                    entry.ModifiersThatAffects.Add(ModifierTagEnum.Siege);
                }

                // Modifier: Recruitment speed bonus
                double modValue = Math.Pow(lvl / 30.0, 1.7);
                entry.ModifiersInternal.Add(new Modifier
                {
                    Tag = ModifierTagEnum.Recruitment,
                    Type = ModifierTypeEnum.Increased,
                    Value = modValue,
                    Source = $"{type} Level {lvl}"
                });

                //if (lvl == 1) entry.Prerequisites.Add(new(BuildingTypeEnum.Senate, 3));
                //if (lvl == 20) entry.Prerequisites.Add(new(BuildingTypeEnum.Senate, 20));
                //if (type == BuildingTypeEnum.Workshop && lvl == 1) entry.Prerequisites.Add(new(BuildingTypeEnum.Barracks, 10));

                levels.Add(entry);
            }
            return levels;
        }

        private static List<object> GenerateAcademyData()
        {
            var levels = new List<object>();
            for (int lvl = 1; lvl <= 30; lvl++)
            {
                int totalRes = (int)(200 * Math.Pow(1.22, lvl - 1));
                var entry = new AcademyLevelData
                {
                    Level = lvl,
                    WoodCost = totalRes / 3,
                    StoneCost = totalRes / 3,
                    MetalCost = totalRes / 3,
                    BuildTime = TimeSpan.FromMinutes(lvl * 1.5),
                    PopulationCost = (lvl <= 20) ? lvl * 3 : 60 + (int)(Math.Pow(lvl - 20, 2) * 2.4),
                    ModifiersThatAffects = { ModifierTagEnum.Research }
                };

                // Modifier: Research speed
                entry.ModifiersInternal.Add(new Modifier
                {
                    Tag = ModifierTagEnum.Research,
                    Type = ModifierTypeEnum.Increased,
                    Value = (lvl / 30.0),
                    Source = $"Academy Level {lvl}"
                });
                levels.Add(entry);
            }
            return levels;
        }

        private static List<object> GenerateWarehouseData()
        {
            var levels = new List<object>();
            for (int lvl = 1; lvl <= 30; lvl++)
            {
                int totalRes = (int)(150 * Math.Pow(1.20, lvl - 1));
                levels.Add(new WarehouseLevelData
                {
                    Level = lvl,
                    WoodCost = (int)(totalRes * 0.5),
                    StoneCost = (int)(totalRes * 0.5),
                    PopulationCost = (lvl <= 20) ? lvl * 2 : 40 + (int)(Math.Pow(lvl - 20, 2) * 2.6),
                    BuildTime = TimeSpan.FromMinutes(lvl),
                    // Skalerer op til ~35.000 ved level 30
                    Capacity = 1500 + (int)(Math.Pow(lvl, 2) * 37.22),
                    ModifiersThatAffects = { ModifierTagEnum.Storage }
                });
            }
            return levels;
        }

        private static List<object> GenerateWallData()
        {
            var levels = new List<object>();
            for (int lvl = 1; lvl <= 30; lvl++)
            {
                int totalRes = (int)(250 * Math.Pow(1.23, lvl - 1));
                var entry = new WallLevelData
                {
                    Level = lvl,
                    BuildTime = TimeSpan.FromMinutes(lvl * 2),
                    PopulationCost = lvl * 2,
                    ModifiersThatAffects = { ModifierTagEnum.Wall }
                };

                // Modifier: Armor bonus til alle enheder i byen
                entry.ModifiersInternal.Add(new Modifier
                {
                    Tag = ModifierTagEnum.Wall,
                    Type = ModifierTypeEnum.Increased,
                    Value = (lvl / 30.0) * 0.55,
                    Source = $"Wall Level {lvl}"
                });

                if (lvl <= 15) { entry.WoodCost = (int)(totalRes * 0.7); entry.StoneCost = (int)(totalRes * 0.2); entry.MetalCost = (int)(totalRes * 0.1); }
                else { entry.WoodCost = (int)(totalRes * 0.2); entry.StoneCost = (int)(totalRes * 0.45); entry.MetalCost = (int)(totalRes * 0.35); }

                levels.Add(entry);
            }
            return levels;
        }
    }
}
