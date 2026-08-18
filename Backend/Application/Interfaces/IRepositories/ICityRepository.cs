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
        Task<City?> GetForJobProcessingAsync(Guid cityId, bool includeWorldPlayer) => GetByIdAsync(cityId);
        Task UpdateAsync(City city);
        Task<List<City>> GetAllAsync();
        async Task<List<City>> GetNPCsForBuildingAutomationAsync() =>
            (await GetAllAsync()).Where(city => city.IsNPC && city.WorldPlayerId == null && city.Points < 2500).ToList();
        Task<List<City>> GetCitiesForNPCBackfillAsync() => GetAllAsync();
        Task UpdateRangeAsync(List<City> cities);
        Task AddAsync(City city);
        Task AddNPCVillagesWithMapObjectsAsync(IReadOnlyCollection<City> cities);
        Task<City?> GetCityWithBuildingsByCityIdentifierAsync(Guid cityId);
        Task<City?> GetTownHallCityByCityIdentifierAsync(Guid cityId);
        Task<City?> GetByCoordinatesAsync(int x, int y);
        Task<Guid?> GetWorldPlayerIdByCityIdAsync(Guid cityId);
        Task<List<City>> GetCitiesByWorldPlayerIdAsync(Guid worldPlayerId);
        Task<City?> GetForEdictAsync(Guid cityId) => GetByIdAsync(cityId);
        Task AcquireEdictPlayerLockAsync(Guid worldPlayerId) => Task.CompletedTask;
        Task AcquireBuildingQueueLockAsync(Guid cityId) => Task.CompletedTask;
    }
}
