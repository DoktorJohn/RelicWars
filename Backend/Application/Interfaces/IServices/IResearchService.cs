using Application.DTOs;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IResearchService
    {
        Task<BuildingResult> QueueResearchAsync(Guid userId, string researchId);

        Task<BuildingResult> CancelResearchAsync(Guid userId, Guid jobId);

        Task<List<Modifier>> GetUserResearchModifiersAsync(Guid userId);
        Task<ResearchTreeDTO> GetResearchTreeAsync(Guid userId);
    }
}
