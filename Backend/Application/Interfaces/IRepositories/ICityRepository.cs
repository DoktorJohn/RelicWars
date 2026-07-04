using Domain.Entities;
using Domain.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IRepositories
{
    public interface ICityRepository
    {
        Task<List<City>> GetCitiesByListOfIdsAsync(List<Guid> ids);
        Task<City?> GetByIdAsync(Guid cityId);
        Task UpdateAsync(City city);
        Task<List<City>> GetAllAsync();
        Task UpdateRangeAsync(List<City> cities);
        Task AddAsync(City city);
        Task<City?> GetCityWithBuildingsByCityIdentifierAsync(Guid cityId);
        Task<City?> GetTownHallCityByCityIdentifierAsync(Guid cityId);
        Task<City?> GetByCoordinatesAsync(int x, int y);
        Task<Guid?> GetWorldPlayerIdByCityIdAsync(Guid cityId);
        Task<List<City>> GetCitiesByWorldPlayerIdAsync(Guid worldPlayerId);
    }
}
