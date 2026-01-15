using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;

namespace Application.Interfaces.IServices
{
    public interface IRecruitmentService
    {
        Task<BuildingResult> QueueRecruitmentAsync(Guid userId, Guid cityId, UnitTypeEnum type, int quantity);
        Task<BarracksFullViewDTO> GetBarracksOverviewAsync(Guid userId, Guid cityId);
        Task<StableFullViewDTO> GetStableOverviewAsync(Guid userId, Guid cityId);
        Task<WorkshopFullViewDTO> GetWorkshopOverviewAsync(Guid userId, Guid cityId);
    }
}