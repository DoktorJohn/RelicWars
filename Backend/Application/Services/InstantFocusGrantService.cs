using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Readers;
using Domain.Workers.Abstraction;

namespace Application.Services
{
    public class InstantFocusGrantService
    {
        private readonly InstantUtility _instantUtility;
        private readonly ICityStatService _cityStatService;
        private readonly IJobRepository _jobRepository;
        private readonly UnitDataReader _unitDataReader;
        private readonly IRandomService _random;

        public InstantFocusGrantService(InstantUtility instantUtility, ICityStatService cityStatService,
            IJobRepository jobRepository, UnitDataReader unitDataReader, IRandomService random)
        {
            _instantUtility = instantUtility;
            _cityStatService = cityStatService;
            _jobRepository = jobRepository;
            _unitDataReader = unitDataReader;
            _random = random;
        }

        public async Task<IdeologyFocusEffectResultDTO> GrantLordsLevy(City city)
        {
            var jobs = new List<BaseJob>();
            jobs.AddRange(await _jobRepository.GetRecruitmentJobsAsync(city.Id));
            jobs.AddRange(await _jobRepository.GetBuildingJobsAsync(city.Id));
            int requested = Math.Max(0, _cityStatService.GetAvailablePopulation(city, jobs)) / 100 * 8;
            int granted = requested == 0 ? 0 : await _instantUtility.AddInstantUnitsToCityAsync(city.Id, UnitTypeEnum.Militia, requested);
            return new IdeologyFocusEffectResultDTO($"Granted {granted} militia.", requested, granted,
                granted > 0 ? new() { new UnitStackDTO(UnitTypeEnum.Militia, granted) } : new());
        }

        public async Task<IdeologyFocusEffectResultDTO> GrantNewRecruits(City city)
        {
            var candidates = _unitDataReader.GetAll().Where(unit => !unit.IsElite).ToList();
            var grantedUnits = new Dictionary<UnitTypeEnum, int>();
            for (int remaining = 15; remaining > 0 && candidates.Count > 0; remaining--)
            {
                var selected = candidates[_random.Next(candidates.Count)];
                int granted = await _instantUtility.AddInstantUnitsToCityAsync(city.Id, selected.Type, 1);
                if (granted == 0) break;
                grantedUnits[selected.Type] = grantedUnits.GetValueOrDefault(selected.Type) + granted;
            }
            int total = grantedUnits.Values.Sum();
            return new IdeologyFocusEffectResultDTO($"Granted {total} random non-elite units.", 15, total,
                grantedUnits.Select(x => new UnitStackDTO(x.Key, x.Value)).ToList());
        }
    }
}
