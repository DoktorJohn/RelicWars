using Assets._Project.Scripts.Domain.Enums;
using Assets.Scripts.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Scripts.Domain.DTOs
{
    [Serializable]
    public class RecruitmentQueueItemDTO
    {
        public Guid QueueId;
        public UnitTypeEnum UnitType;
        public int Amount;
        public double TimeRemainingSeconds;
        public int TotalDurationSeconds;
    }

    [Serializable]
    public class RecruitUnitRequestDTO
    {
        public UnitTypeEnum UnitType;
        public int Amount;
    }

    [Serializable]
    public class RecruitmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class GetRecruitmentQueueItemsDTO
    {
        public Guid CityId { get; set; }
        public List<UnitCategoryEnum> UnitCategories { get; set; } = new();
    }


}
