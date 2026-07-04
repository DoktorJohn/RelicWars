using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Domain.Enums
{
    public enum ModifierTypeEnum
    {
        Flat,
        Increased,
        Decreased
    }

    public enum ModifierTagEnum
    {
        Wood, Stone, Metal, Coins,
        ResourceProduction,
        RecruitmentSpeed, Construction, ConstructionCost, Research, Population,
        WarehouseCapacity, Market,

        InfantryCost, CavalryCost, SiegeCost, InfantryStats, CavalryStats, SiegeStats, InfantryUpkeep, CavalryUpkeep, SiegeUpkeep, SiegeRecruitmentSpeed, InfantryRecruitmentSpeed, CavalryRecruitmentSpeed,
        Upkeep, BuildingUpkeep, UnitUpkeep, TravelSpeed, Power, Armor, Discipline, Casualties, Wall, LootCapacity,
        Ideology, IdeologyFocus,

        Placeholder, ResistanceRecovery, Revival, RepairCost, MerchantDefense
    }
}
