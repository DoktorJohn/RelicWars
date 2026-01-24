using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Readers;
using Domain.Workers.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class IdeologyFocusService : IIdeologyFocusService
    {
        private readonly IWorldPlayerRepository _worldPlayerRepo;
        private readonly ICityRepository _cityRepo;
        private readonly IdeologyFocusDataReader _ideologyFocusDataReader;
        private readonly InstantUtility _instantUtility;
        private readonly ICityStatService _cityStatService;
        private readonly IJobRepository _jobRepository;

        public IdeologyFocusService(
            IWorldPlayerRepository worldPlayerRepo,
            ICityRepository cityRepo,
            IdeologyFocusDataReader ideologyFocusDataReader,
            InstantUtility instantUtility,
            ICityStatService cityStatService,
            IJobRepository jobRepository)
        {
            _worldPlayerRepo = worldPlayerRepo;
            _cityRepo = cityRepo;
            _ideologyFocusDataReader = ideologyFocusDataReader;
            _instantUtility = instantUtility;
            _cityStatService = cityStatService;
            _jobRepository = jobRepository;
        }

        public async Task<IdeologyFocusAnswerDTO?> EnactIdeologyFocus(IdeologyFocusRequestDTO ideologyFocusDTO)
        {
            var city = await _cityRepo.GetByIdAsync(ideologyFocusDTO.CityId);
            if (city == null) return new IdeologyFocusAnswerDTO(null, null, "City not found", false);

            var ideologyFocusData = _ideologyFocusDataReader.GetIdeology(ideologyFocusDTO.IdeologyFocusName.ToString());
            var worldPlayer = city.WorldPlayer;

            if (worldPlayer!.IdeologyFocusPoints < ideologyFocusData.IdeologyFocusPointCost)
            {
                return new IdeologyFocusAnswerDTO(ideologyFocusData.Name, city.Id, "Insufficient Ideology Points", false);
            }

            worldPlayer.IdeologyFocusPoints -= ideologyFocusData.IdeologyFocusPointCost;

            if (ideologyFocusData.SpecialFlag)
            {
                await HandleSpecialFocusLogic(ideologyFocusData.Name, city);
            }

            bool isBuffWithDuration = ideologyFocusData.TimeActive.HasValue;

            IdeologyFocus ideologyFocusEntity = new()
            {
                Id = Guid.NewGuid(),
                DateCreated = DateTime.UtcNow,
                DateLastModified = DateTime.UtcNow,
                CityId = city.Id,

                TimeOfIdeologyStarted = isBuffWithDuration ? DateTime.UtcNow : null,
                TimeOfIdeologyFinished = isBuffWithDuration
                    ? DateTime.UtcNow.Add(ideologyFocusData.TimeActive!.Value)
                    : null
            };

            city.ActiveFocuses.Add(ideologyFocusEntity);

            await _worldPlayerRepo.UpdateAsync(worldPlayer);
            await _cityRepo.UpdateAsync(city);

            return new IdeologyFocusAnswerDTO(ideologyFocusData.Name, city.Id, $"{ideologyFocusData.Name} enacted successfully", true);
        }

        private async Task HandleSpecialFocusLogic(IdeologyFocusNameEnum focusName, City city)
        {
            switch (focusName)
            {
                case IdeologyFocusNameEnum.NewRecruits:
                    await _instantUtility.AddInstantUnitsToCityAsync(city.Id, UnitTypeEnum.Militia, 15);
                    break;

            }
        }
    }
}
