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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class IdeologyFocusService : IIdeologyFocusService
    {
        private readonly IWorldPlayerRepository _worldPlayerRepo;
        private readonly IWorldPlayerService _worldPlayerService;
        private readonly ICityRepository _cityRepo;
        private readonly IdeologyFocusDataReader _ideologyFocusDataReader;
        private readonly IdeologyDataReader _ideologyDataReader;
        private readonly InstantUtility _instantUtility;
        private readonly ICityStatService _cityStatService;
        private readonly IJobRepository _jobRepository;
        private readonly IIdeologyFocusRepository _ideologyFocusRepository;
        private readonly IResourceService _resourceService;

        public IdeologyFocusService(
            IWorldPlayerRepository worldPlayerRepo,
            IWorldPlayerService worldPlayerService,
            ICityRepository cityRepo,
            IdeologyFocusDataReader ideologyFocusDataReader,
            IdeologyDataReader ideologyDataReader,
            InstantUtility instantUtility,
            ICityStatService cityStatService,
            IJobRepository jobRepository,
            IResourceService resourceService,
            IIdeologyFocusRepository ideologyFocusRepository)
        {
            _worldPlayerRepo = worldPlayerRepo;
            _worldPlayerService = worldPlayerService;
            _cityRepo = cityRepo;
            _ideologyFocusDataReader = ideologyFocusDataReader;
            _ideologyDataReader = ideologyDataReader;
            _instantUtility = instantUtility;
            _cityStatService = cityStatService;
            _jobRepository = jobRepository;
            _resourceService = resourceService;
            _ideologyFocusRepository = ideologyFocusRepository;
        }

        public async Task<IdeologyFocusAnswerDTO?> EnactIdeologyFocus(IdeologyFocusRequestDTO ideologyFocusDTO)
        {
            await _ideologyFocusRepository.DeleteExpiredFocusesForCityAsync(ideologyFocusDTO.CityId);

            var city = await _cityRepo.GetByIdAsync(ideologyFocusDTO.CityId);
            if (city == null) return new IdeologyFocusAnswerDTO(null, null, "City not found", false);

            var ideologyFocusData = _ideologyFocusDataReader.GetIdeology(ideologyFocusDTO.IdeologyFocusName);
            var worldPlayer = city.WorldPlayer;

            if (city.ActiveFocuses != null && city.ActiveFocuses.Any(x => x.Name == ideologyFocusDTO.IdeologyFocusName))
            {
                return new IdeologyFocusAnswerDTO(null, null, "City is already affected by the specified ideology focus", false);
            }

            if (worldPlayer!.IdeologyFocusPoints < ideologyFocusData.IdeologyFocusPointCost)
            {
                return new IdeologyFocusAnswerDTO(ideologyFocusData.Name, city.Id, "Insufficient Ideology Points", false);
            }

            var resourceSnapshot = _resourceService.CalculateGlobalResources(worldPlayer, DateTime.UtcNow);
            worldPlayer.IdeologyFocusPoints = resourceSnapshot.IdeologyFocusPoints - ideologyFocusData.IdeologyFocusPointCost;
            worldPlayer.LastResourceUpdate = DateTime.UtcNow;

            _worldPlayerService.UpdateGlobalResourceState(worldPlayer, DateTime.UtcNow);
            await _worldPlayerRepo.UpdateAsync(worldPlayer);

            if (ideologyFocusData.SpecialFlag)
            {
                await HandleSpecialFocusLogic(ideologyFocusData.Name, city);
            }

            bool isBuffWithDuration = ideologyFocusData.TimeActive.HasValue;

            if (isBuffWithDuration)
            {
                IdeologyFocus ideologyFocusEntity = new()
                {
                    Name = ideologyFocusDTO.IdeologyFocusName,
                    DateCreated = DateTime.UtcNow,
                    DateLastModified = DateTime.UtcNow,
                    CityId = city.Id,
                    TimeOfIdeologyStarted = DateTime.UtcNow,
                    TimeOfIdeologyFinished = DateTime.UtcNow.Add(ideologyFocusData.TimeActive!.Value)
                };

                await _ideologyFocusRepository.AddAsync(ideologyFocusEntity);
            }

            await _cityRepo.UpdateAsync(city);

            return new IdeologyFocusAnswerDTO(ideologyFocusData.Name, city.Id, $"{ideologyFocusData.Name} enacted successfully", true);
        }


        public async Task<IdeologyOverviewDTO?> GetIdeologyOverview(Guid cityId)
        {
            IdeologyOverviewDTO dto = new IdeologyOverviewDTO();

            if (cityId == Guid.Empty)
            {
                dto.Message = "No match for ID";
                return dto;
            }

            await _ideologyFocusRepository.DeleteExpiredFocusesForCityAsync(cityId);

            var city = await _cityRepo.GetByIdAsync(cityId);

            if (city == null)
            {
                dto.Message = $"No city with ID {cityId}";
                return dto;
            }

            if (city.WorldPlayer == null)
            {
                dto.Message = "No worldplayer owns this city";
                return dto;
            }

            var worldPlayerIdeologyConfiguration = _ideologyDataReader.GetIdeology(city.WorldPlayer.Ideology);

            if (worldPlayerIdeologyConfiguration != null)
            {
                dto.IdeologyDTO.Name = worldPlayerIdeologyConfiguration.Name;
                dto.IdeologyDTO.Description = worldPlayerIdeologyConfiguration.Description;
                dto.IdeologyDTO.IdeologyType = worldPlayerIdeologyConfiguration.IdeologyType;

                dto.IdeologyDTO.ModifiersInternal = worldPlayerIdeologyConfiguration.ModifiersInternal?
                    .Select(sourceModifier => new ModifierDTO
                    {
                        ModifierTag = sourceModifier.Tag,
                        ModifierType = sourceModifier.Type,
                        Value = sourceModifier.Value
                    }).ToList() ?? new List<ModifierDTO>();
            }
            else
            {
                dto.Message = $"Configuration data for Ideology '{city.WorldPlayer.Ideology}' is missing.";
                return dto;
            }

            // Henter den nu RENSEDE liste fra databasen
            var cityIdeologyFocusesActive = await _ideologyFocusRepository.GetAllByCityPlayer(cityId) ?? new List<IdeologyFocus>();
            var allStaticIdeologyFocuses = _ideologyFocusDataReader.GetAll();

            var allStaticIdeologyFocusesForIdeology = allStaticIdeologyFocuses.Where(x => x.RequiredIdeology == worldPlayerIdeologyConfiguration.IdeologyType);

            if (allStaticIdeologyFocusesForIdeology != null)
            {
                dto.IdeologyFocuses = allStaticIdeologyFocusesForIdeology.Select(staticFocus =>
                {
                    var activeRecord = cityIdeologyFocusesActive.FirstOrDefault(activeFocus => activeFocus.Name == staticFocus.Name);

                    return new IdeologyFocusDTO
                    {
                        Name = staticFocus.Name,
                        IdeologyFocusPointCost = staticFocus.IdeologyFocusPointCost,
                        Description = staticFocus.Description,

                        ModifiersInternal = staticFocus.ModifiersInternal?.Select(modifier => new ModifierDTO
                        {
                            ModifierTag = modifier.Tag,
                            ModifierType = modifier.Type,
                            Value = modifier.Value
                        }).ToList() ?? new List<ModifierDTO>(),

                        AlreadyEnacted = activeRecord != null,
                        ActiveTime = staticFocus.TimeActive,
                        ExpirationTime = activeRecord?.TimeOfIdeologyFinished ?? DateTime.MinValue
                    };
                }).ToList();
            }
            else
            {
                dto.IdeologyFocuses = new List<IdeologyFocusDTO>();
            }

            return dto;
        }

        private async Task HandleSpecialFocusLogic(IdeologyFocusNameEnum focusName, City city)
        {
            if (focusName == IdeologyFocusNameEnum.LordsLevy)
            {
                //Ideology focus grants the player 8 militia for each 100 free population in the given city.

                var activeJobsInCity = new List<BaseJob>();
                activeJobsInCity.AddRange(await _jobRepository.GetRecruitmentJobsAsync(city.Id));
                activeJobsInCity.AddRange(await _jobRepository.GetBuildingJobsAsync(city.Id));

                var totalFreePopulation = _cityStatService.GetAvailablePopulation(city, activeJobsInCity);

                int completedPopulationBreakpoints = totalFreePopulation / 100;

                int militiaUnitsToGrant = completedPopulationBreakpoints * 8;

                await _instantUtility.AddInstantUnitsToCityAsync(city.Id, UnitTypeEnum.Militia, militiaUnitsToGrant);
            }
            if (focusName == IdeologyFocusNameEnum.NewRecruits)
            {
                await _instantUtility.AddInstantUnitsToCityAsync(city.Id, UnitTypeEnum.Militia, 15);
            }

        }
    }
}
