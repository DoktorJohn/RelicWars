using Domain.Entities;
using Domain.Workers.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface ICityStatService
    {
        double GetWarehouseCapacity(City city);
        int GetMaxPopulation(City city);
        int GetCurrentPopulationUsage(City city);
        int GetAvailablePopulation(City city, IEnumerable<BaseJob> activeJobs);
        // Senere: GetDefenseBonus, GetResearchSpeed, osv.
    }
}
