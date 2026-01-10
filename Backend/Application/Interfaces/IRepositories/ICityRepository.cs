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
        Task<City?> GetByIdAsync(Guid cityId);
        Task UpdateAsync(City city);
        Task<List<City>> GetAllAsync();
        Task UpdateRangeAsync(List<City> cities);
        Task AddAsync(City city);
        Task<City> GetCityWithBuildingsByCityIdentifierAsync(Guid cityId);
    }
}
