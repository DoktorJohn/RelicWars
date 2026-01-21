using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface ICityService
    {
        Task UpdateCityPointsAsync(Guid cityId);
        Task<CityControllerGetDetailedCityInformationDTO?> GetDetailedCityInformationByCityIdentifierAsync(Guid cityId);
        Task<List<AvailableBuildingDTO>> GetAvailableBuildingsForTownHallAsync(Guid cityId);
        Task<CityOverviewHUD> GetCityOverviewHUD(Guid cityId);
    }
}
