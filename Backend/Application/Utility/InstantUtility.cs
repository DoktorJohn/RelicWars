using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Readers;
using Domain.Workers.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Utility
{
    public class InstantUtility
    {
        private readonly ICityRepository _cityRepository;
        private readonly IJobRepository _jobRepository;
        private readonly ICityStatService _cityStatService;
        private readonly UnitDataReader _unitDataReader;

        public InstantUtility(
            ICityRepository cityRepository,
            IJobRepository jobRepository,
            ICityStatService cityStatService,
            UnitDataReader unitDataReader)
        {
            _cityRepository = cityRepository;
            _jobRepository = jobRepository;
            _cityStatService = cityStatService;
            _unitDataReader = unitDataReader;
        }

        public async Task AddInstantUnitsToCityAsync(Guid cityId, UnitTypeEnum unitType, int requestedQuantity)
        {
            var cityEntity = await _cityRepository.GetByIdAsync(cityId);
            if (cityEntity == null) return;

            var unitStaticData = _unitDataReader.GetUnit(unitType);

            var activeJobsInCity = new List<BaseJob>();
            activeJobsInCity.AddRange(await _jobRepository.GetRecruitmentJobsAsync(cityId));
            activeJobsInCity.AddRange(await _jobRepository.GetBuildingJobsAsync(cityId));

            int availablePopulation = _cityStatService.GetAvailablePopulation(cityEntity, activeJobsInCity);

            int maxUnitsPossible = availablePopulation / unitStaticData.PopulationCost;
            int finalQuantityToAdd = Math.Min(requestedQuantity, maxUnitsPossible);

            if (finalQuantityToAdd <= 0) return;

            var existingStack = cityEntity.UnitStacks.FirstOrDefault(u => u.Type == unitType);

            if (existingStack != null)
            {
                existingStack.Quantity += finalQuantityToAdd;
            }
            else
            {
                cityEntity.UnitStacks.Add(new UnitStack
                {
                    Type = unitType,
                    Quantity = finalQuantityToAdd,
                    CityId = cityId
                });
            }

            await _cityRepository.UpdateAsync(cityEntity);
        }
    }
}
