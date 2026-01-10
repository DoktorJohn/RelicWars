using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;

namespace Application.Interfaces.IServices
{
    public interface IRecruitmentService
    {
        Task<BuildingResult> QueueRecruitmentAsync(Guid userId, Guid cityId, UnitTypeEnum type, int quantity);
    }
}