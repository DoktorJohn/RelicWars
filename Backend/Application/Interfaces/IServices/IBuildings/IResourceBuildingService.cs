using Application.DTOs;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices.IBuildings
{
    public interface IResourceBuildingService
    {
        Task<List<ResourceBuildingInfoDTO>> GetResourceBuildingInfoAsync(Guid cityId, BuildingTypeEnum resourceBuildingType);
    }
}
