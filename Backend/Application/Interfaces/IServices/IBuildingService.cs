using Application.DTOs;
using Domain.Enums;
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
        Task<List<WarehouseProjectionDTO>> GetWarehouseProjectionAsync(Guid cityId);
        Task<List<HousingInfoDTO>> GetHousingInfoAsync(Guid cityId);
        Task<List<WallInfoDTO>> GetWallInfoAsync(Guid cityId);
        Task<List<ResourceBuildingInfoDTO>> GetResourceBuildingInfoAsync(Guid cityId, BuildingTypeEnum resourceBuildingType);
        Task<List<BarracksInfoDTO>> GetBarracksInfoAsync(Guid cityId);
        Task<List<StableInfoDTO>> GetStableInfoAsync(Guid cityId);
        Task<List<WorkshopInfoDTO>> GetWorkshopInfoAsync(Guid cityId);
        Task<List<AcademyInfoDTO>> GetAcademyInfoAsync(Guid cityId);

    }
}
