using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;

namespace Application.Interfaces.IServices
{
    public interface IRecruitmentService
    {
        Task<RecruitmentResult> QueueRecruitmentAsync(Guid userId, Guid cityId, UnitTypeEnum type, int quantity);
        Task<RecruitmentResult> CancelRecruitmentAsync(Guid cityId, Guid queueId);
        Task<List<RecruitmentQueueItemDTO>> GetRecruitmentQueueAsync(GetRecruitmentQueueItemsDTO dto);
    }
}
