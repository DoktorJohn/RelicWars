using Application.DTOs;
using Domain.Entities;

namespace Application.Interfaces.IServices
{
    public interface IExoticResourceService
    {
        Task<List<CityExoticResourceDTO>> SyncCityExoticResourcesAsync(City city, DateTime currentDateTime);
        Task<List<WorldIslandExoticResourceDTO>> GetIslandResourcesAsync(Guid islandId);
        Task<List<WorldIslandExoticResourceDTO>> GetIslandResourcesForCityAsync(City city);
        Task<List<CityExoticResourceProductionDTO>> GetProductionBreakdownsForCityAsync(City city);
        Task<ExoticResourceInvestmentResponseDTO> InvestAsync(Guid cityId, ExoticResourceInvestmentRequestDTO request);
    }
}
