using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum ModifierTagEnum
    {
        Wood, Stone, Metal, Coins,
        ResourceProduction,
        RecruitmentSpeed, Construction, ConstructionCost, Research, Population, 
        WarehouseCapacity, Market,

        InfantryCost, CavalryCost, SiegeCost, NavalCost, InfantryStats, CavalryStats, SiegeStats, NavalStats, InfantryUpkeep, CavalryUpkeep, SiegeUpkeep, NavalUpkeep, SiegeRecruitmentSpeed, InfantryRecruitmentSpeed, CavalryRecruitmentSpeed, NavalRecruitmentSpeed,
        Upkeep, BuildingUpkeep, UnitUpkeep, TravelSpeed, Power, Armor, Discipline, Casualties, Wall, LootCapacity,
        Ideology, IdeologyFocus,

        Placeholder, ResistanceRecovery, Revival, RepairCost, MerchantDefense
    }
}
