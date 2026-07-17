using Assets.Scripts.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Scripts.Domain.DTOs
{
    [Serializable]
    public class WorkshopFullViewDTO
    {
        public int BuildingLevel;
        public List<WorkshopUnitInfoDTO> AvailableUnits;
    }

    [Serializable]
    public class WorkshopUnitInfoDTO
    {
        public UnitTypeEnum UnitType;
        public string UnitName;
        public int AlreadyOwnedCount;
        public int CostWood;
        public int CostStone;
        public int CostMetal;
        public int Power;
        public int Armor;
        public int Discipline;
        public int Mobility;
        public int Reach;
        public int LootCapacity;
        public int PopulationCost;
        public int UnitCapacity;
        public int RecruitmentTimeInSeconds;
        public bool IsUnlocked;
        public List<string> UnmetRequirements;
    }
}
