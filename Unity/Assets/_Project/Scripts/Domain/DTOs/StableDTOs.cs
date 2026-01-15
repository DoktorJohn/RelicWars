using Assets.Scripts.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Scripts.Domain.DTOs
{
    [Serializable]
    public class StableFullViewDTO
    {
        public int BuildingLevel;
        public List<StableUnitInfoDTO> AvailableUnits;
        public List<RecruitmentQueueItemDTO> RecruitmentQueue;
    }

    [Serializable]
    public class StableUnitInfoDTO
    {
        public UnitTypeEnum UnitType;
        public string UnitName;
        public int CurrentInventoryCount;
        public int CostWood;
        public int CostStone;
        public int CostMetal;
        public int RecruitmentTimeInSeconds;
        public bool IsUnlocked;
    }
}
