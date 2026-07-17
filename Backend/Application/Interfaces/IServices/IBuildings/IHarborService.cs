using Application.DTOs;

namespace Application.Interfaces.IServices.IBuildings
{
    public interface IHarborService
    {
        Task<HarborFullViewDTO> GetHarborOverviewAsync(Guid userId, Guid cityId);
    }
}
