using Application.DTOs;
using Domain.StaticData.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IResearchService
    {
        Task<BuildingResult> QueueResearchAsync(Guid userId, Guid cityId, string researchId);

        Task<BuildingResult> CancelResearchAsync(Guid userId, Guid jobId);

        Task<List<ModifierData>> GetUserResearchModifiersAsync(Guid userId);
    }
}
