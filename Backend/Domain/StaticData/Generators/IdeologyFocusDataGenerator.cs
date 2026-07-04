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
    public class IdeologyFocusDataGenerator
    {
        public static void GenerateDefaultJson(string path)
        {
            var ideologyFocus = new List<IdeologyFocusData>();

            //Feudalism
            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.LordsLevy,
                RequiredIdeology = IdeologyTypeEnum.Feudalism,
                IdeologyFocusPointCost = 6,
                Description = "Gain +8 militia per 100 free population",
                ModifiersInternal = new(),
                SpecialFlag = true,
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.FeudalMuster,
                RequiredIdeology = IdeologyTypeEnum.Feudalism,
                TimeActive = TimeSpan.FromMinutes(120),
                IdeologyFocusPointCost = 10,
                Description = "+10% defensive unit values",
                SpecialFlag = false,
                ModifiersInternal = new List<Modifier>
                {
                    new Modifier { Tag = ModifierTagEnum.Armor, Type = ModifierTypeEnum.Increased, Value = 0.1, Source = "Feudal Muster focus: +10% defensive unit values" }
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            //ideologyFocus.Add(new IdeologyFocusData
            //{
            //    Name = IdeologyFocusNameEnum.NobleClemency,
            //    RequiredIdeology = IdeologyTypeEnum.Feudalism,
            //    IdeologyFocusPointCost = 12,
            //    Description = "+10% Resistance recovery for 3h",
            //    SpecialFlag = false,
            //    ModifiersInternal = new List<Modifier>
            //    {
            //        new Modifier { Tag = ModifierTagEnum.ResistanceRecovery, Type = ModifierTypeEnum.Increased, Value = 0.1, Source = "Noble Clemency focus: +10% Resistance recovery" }
            //    },
            //    ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            //});

            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.OathOfBlood,
                RequiredIdeology = IdeologyTypeEnum.Feudalism,
                TimeActive = TimeSpan.FromMinutes(120),
                IdeologyFocusPointCost = 18,
                SpecialFlag = false,
                Description = "-5% allied casualties in the city for 2h",
                ModifiersInternal = new List<Modifier>
                {
                    new Modifier { Tag = ModifierTagEnum.Casualties, Type = ModifierTypeEnum.Decreased, Value = 0.05, Source = "Oath of Blood focus: -5% allied casualties" }
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.NobleClemency,
                RequiredIdeology = IdeologyTypeEnum.Feudalism,
                TimeActive = TimeSpan.FromHours(3),
                IdeologyFocusPointCost = 12,
                Description = "+10% resistance recovery for 3h",
                ModifiersInternal = new()
                {
                    new Modifier { Tag = ModifierTagEnum.ResistanceRecovery, Type = ModifierTypeEnum.Increased, Value = 0.1, Source = "Noble Clemency focus: +10% resistance recovery" }
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.RoyalLogistics,
                RequiredIdeology = IdeologyTypeEnum.Monarchy,
                TimeActive = TimeSpan.FromMinutes(120),
                IdeologyFocusPointCost = 6,
                Description = "+8% movespeed from the city",
                SpecialFlag = false,
                ModifiersInternal = new List<Modifier>
                {
                    new Modifier { Tag = ModifierTagEnum.TravelSpeed, Type = ModifierTypeEnum.Increased, Value = 0.08, Source = "Royal Logistics focus: +8% movespeed" }
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.RoyalDecree,
                RequiredIdeology = IdeologyTypeEnum.Monarchy,
                TimeActive = TimeSpan.FromMinutes(120),
                IdeologyFocusPointCost = 8,
                SpecialFlag = false,
                Description = "-15% building time for 2h",
                ModifiersInternal = new List<Modifier>
                {
                    new Modifier { Tag = ModifierTagEnum.Construction, Type = ModifierTypeEnum.Increased, Value = 0.15, Source = "Royal Decree focus: -15% construction time" }
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.RoyalMedics,
                RequiredIdeology = IdeologyTypeEnum.Monarchy,
                TimeActive = TimeSpan.FromMinutes(120),
                IdeologyFocusPointCost = 10,
                Description = "Revive 10% of defensive casualties for 2h",
                SpecialFlag = false,
                ModifiersInternal = new List<Modifier>
                {
                    new Modifier { Tag = ModifierTagEnum.Revival, Type = ModifierTypeEnum.Increased, Value = 0.10, Source = "Royal Medics focus: Revive 10%" }
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.CrownTax,
                RequiredIdeology = IdeologyTypeEnum.Monarchy,
                TimeActive = TimeSpan.FromMinutes(120),
                IdeologyFocusPointCost = 14,
                SpecialFlag = false,
                Description = "Improve Coins production from all sources by 100% for 2h",
                ModifiersInternal = new List<Modifier>
                {
                    new Modifier { Tag = ModifierTagEnum.Coins, Type = ModifierTypeEnum.Increased, Value = 1.0, Source = "Crown Tax focus: +100% Coins production" }
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            //Oligarchy

            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.EnhancedWorkshop,
                RequiredIdeology = IdeologyTypeEnum.Oligarchy,
                TimeActive = TimeSpan.FromMinutes(120),
                IdeologyFocusPointCost = 6,
                SpecialFlag = false,
                Description = "10% workshop production speed for 2h",
                ModifiersInternal = new List<Modifier>
                {
                    new Modifier { Tag = ModifierTagEnum.SiegeRecruitmentSpeed, Type = ModifierTypeEnum.Increased, Value = 0.1, Source = "Enhanced Workshop focus: +10% production speed" }
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            //ideologyFocus.Add(new IdeologyFocusData
            //{
            //    Name = IdeologyFocusNameEnum.PrivateSecurity,
            //    RequiredIdeology = IdeologyTypeEnum.Oligarchy,
            //    IdeologyFocusPointCost = 8,
            //    SpecialFlag = false,
            //    Description = "All merchants leaving the city in the next 30 minutes gain +15% defensive bonus",
            //    ModifiersInternal = new List<Modifier>
            //    {
            //        new Modifier { Tag = ModifierTagEnum.Placeholder, Type = ModifierTypeEnum.Increased, Value = 0.15, Source = "Private Security focus: +15% merchant defense" }
            //    },
            //    ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            //});

            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.MarketSurge,
                RequiredIdeology = IdeologyTypeEnum.Oligarchy,
                TimeActive = TimeSpan.FromMinutes(120),
                IdeologyFocusPointCost = 10,
                SpecialFlag = false,
                Description = "Gain +20% coins production for 2h",
                ModifiersInternal = new List<Modifier>
                {
                    new Modifier { Tag = ModifierTagEnum.Coins, Type = ModifierTypeEnum.Increased, Value = 0.2, Source = "Market Surge focus: +20% coins" }
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.PrivateSecurity,
                RequiredIdeology = IdeologyTypeEnum.Oligarchy,
                TimeActive = TimeSpan.FromMinutes(30),
                IdeologyFocusPointCost = 8,
                Description = "Merchants leaving the city gain +15% armor",
                ModifiersInternal = new()
                {
                    new Modifier { Tag = ModifierTagEnum.MerchantDefense, Type = ModifierTypeEnum.Increased, Value = 0.15, Source = "Private Security focus: +15% merchant armor" }
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            //Democracy

            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.CivicInitiative,
                RequiredIdeology = IdeologyTypeEnum.Democracy,
                TimeActive = TimeSpan.FromMinutes(120),
                IdeologyFocusPointCost = 6,
                SpecialFlag = false,
                Description = "+10% research speed for 2h",
                ModifiersInternal = new List<Modifier>
                {
                    new Modifier { Tag = ModifierTagEnum.Research, Type = ModifierTypeEnum.Increased, Value = 0.1, Source = "Civic Initiative focus: +10% research speed" }
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.PublicWorks,
                RequiredIdeology = IdeologyTypeEnum.Democracy,
                TimeActive = TimeSpan.FromMinutes(120),
                IdeologyFocusPointCost = 8,
                SpecialFlag = false,
                Description = "-10% construction and repair cost for 2h",
                ModifiersInternal = new List<Modifier>
                {
                    new Modifier { Tag = ModifierTagEnum.ConstructionCost, Type = ModifierTypeEnum.Decreased, Value = 0.1, Source = "Public Works focus: -10% construction cost" }
                    ,new Modifier { Tag = ModifierTagEnum.RepairCost, Type = ModifierTypeEnum.Decreased, Value = 0.1, Source = "Public Works focus: -10% repair cost" }
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.CitizenMorale,
                RequiredIdeology = IdeologyTypeEnum.Democracy,
                TimeActive = TimeSpan.FromMinutes(120),
                IdeologyFocusPointCost = 10,
                SpecialFlag = false,
                Description = "Non-elite infantry gain +5% discipline for 2h",
                ModifiersInternal = new List<Modifier>
                {
                    new Modifier { Tag = ModifierTagEnum.Discipline, Type = ModifierTypeEnum.Increased, Value = 0.05, AppliesToCategory = UnitCategoryEnum.Infantry, ExcludeElite = true, Source = "Citizen Morale focus: +5% discipline" }
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.EconomicTransparency,
                RequiredIdeology = IdeologyTypeEnum.Democracy,
                TimeActive = TimeSpan.FromMinutes(120),
                IdeologyFocusPointCost = 12,
                SpecialFlag = false,
                Description = "-20% Building Upkeep for 2h",
                ModifiersInternal = new List<Modifier>
                {
                    new Modifier { Tag = ModifierTagEnum.BuildingUpkeep, Type = ModifierTypeEnum.Decreased, Value = 0.2, Source = "Economic Transparency focus: -20% building upkeep" }
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            //Military Junta

            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.NewRecruits,
                RequiredIdeology = IdeologyTypeEnum.MilitaryJunta,
                IdeologyFocusPointCost = 4,
                SpecialFlag = true,
                Description = "Gain 15 random non-elite units",
                ModifiersInternal = new(),
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.AcceleratedConscription,
                RequiredIdeology = IdeologyTypeEnum.MilitaryJunta,
                TimeActive = TimeSpan.FromMinutes(120),
                IdeologyFocusPointCost = 8,
                SpecialFlag = false,
                Description = "+15% recruitment speed for 2h",
                ModifiersInternal = new List<Modifier>
                {
                    new Modifier { Tag = ModifierTagEnum.RecruitmentSpeed, Type = ModifierTypeEnum.Increased, Value = 0.15, Source = "Accelerated Conscription focus: +15% recruitment speed" }
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.IronDiscipline,
                RequiredIdeology = IdeologyTypeEnum.MilitaryJunta,
                TimeActive = TimeSpan.FromMinutes(120),
                IdeologyFocusPointCost = 10,
                SpecialFlag = false,
                Description = "-10% unit upkeep 2h",
                ModifiersInternal = new List<Modifier>
                {
                    new Modifier { Tag = ModifierTagEnum.UnitUpkeep, Type = ModifierTypeEnum.Decreased, Value = 0.1, Source = "Iron Discipline focus: -10% unit upkeep" }
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            ideologyFocus.Add(new IdeologyFocusData
            {
                Name = IdeologyFocusNameEnum.FortifiedCity,
                RequiredIdeology = IdeologyTypeEnum.MilitaryJunta,
                TimeActive = TimeSpan.FromMinutes(120),
                SpecialFlag = false,
                IdeologyFocusPointCost = 14,
                Description = "Walls have +10% defense bonus for 2h",
                ModifiersInternal = new List<Modifier>
                {
                    new Modifier { Tag = ModifierTagEnum.Wall, Type = ModifierTypeEnum.Increased, Value = 0.1, Source = "Fortified City focus: +10% wall defense" }
                },
                ModifiersThatAffectsThis = { ModifierTagEnum.IdeologyFocus }
            });

            foreach (var focus in ideologyFocus)
            {
                focus.EffectKind = focus.Name is IdeologyFocusNameEnum.LordsLevy or IdeologyFocusNameEnum.NewRecruits
                    ? IdeologyFocusEffectKindEnum.Instant
                    : focus.Name == IdeologyFocusNameEnum.RoyalMedics
                        ? IdeologyFocusEffectKindEnum.Triggered
                        : focus.Name == IdeologyFocusNameEnum.PrivateSecurity
                            ? IdeologyFocusEffectKindEnum.Conditional
                            : IdeologyFocusEffectKindEnum.Modifier;
                focus.TargetScope = focus.Name == IdeologyFocusNameEnum.PrivateSecurity
                    ? IdeologyFocusTargetScopeEnum.OutgoingDeployment
                    : focus.Name is IdeologyFocusNameEnum.FeudalMuster or IdeologyFocusNameEnum.OathOfBlood or IdeologyFocusNameEnum.RoyalMedics or IdeologyFocusNameEnum.CitizenMorale or IdeologyFocusNameEnum.FortifiedCity
                        ? IdeologyFocusTargetScopeEnum.CombatAtCity
                        : IdeologyFocusTargetScopeEnum.City;
                focus.CanRepeat = focus.EffectKind != IdeologyFocusEffectKindEnum.Instant ||
                    focus.Name == IdeologyFocusNameEnum.LordsLevy;
                focus.ConsumesOnTrigger = focus.Name == IdeologyFocusNameEnum.RoyalMedics;
            }

            var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
            File.WriteAllText(path, JsonSerializer.Serialize(ideologyFocus, options));
        }
    }
}
