using Application.DTOs;
using Domain.Enums;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IBuildingService
    {
        Task<BuildingResult> QueueUpgradeAsync(Guid cityId, BuildingTypeEnum type);
        Task<BuildingResult> QueueNPCUpgradeAsync(Guid cityId, BuildingTypeEnum type);
        Task<BuildingResult> QueueNPCUpgradeAsync(City city, BuildingTypeEnum type) =>
            QueueNPCUpgradeAsync(city.Id, type);
        Task<List<BuildingDTO>> GetBuildingQueueAsync(Guid cityId);
        Task<BuildingResult> RepairAsync(Guid cityId, BuildingTypeEnum type);

    }
}
