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
        private readonly IPlayerAccessService _playerAccessService;
        private readonly ICityRepository _cityRepo;
        private readonly IdeologyFocusDataReader _ideologyFocusDataReader;
        private readonly IdeologyDataReader _ideologyDataReader;
        private readonly InstantUtility _instantUtility;
        private readonly ICityStatService _cityStatService;
        private readonly IJobRepository _jobRepository;
        private readonly IIdeologyFocusRepository _ideologyFocusRepository;
        private readonly IResourceService _resourceService;
        private readonly UnitDataReader _unitDataReader;
        private readonly IRandomService _random;
        private readonly TimeProvider _timeProvider;
        private readonly InstantFocusGrantService _instantGrantService;
        private readonly FocusEnactmentPolicy _enactmentPolicy;

        public IdeologyFocusService(
            IWorldPlayerRepository worldPlayerRepo,
            IWorldPlayerService worldPlayerService,
            IPlayerAccessService playerAccessService,
            ICityRepository cityRepo,
            IdeologyFocusDataReader ideologyFocusDataReader,
            IdeologyDataReader ideologyDataReader,
            InstantUtility instantUtility,
            ICityStatService cityStatService,
            IJobRepository jobRepository,
            IResourceService resourceService,
            IIdeologyFocusRepository ideologyFocusRepository,
            UnitDataReader unitDataReader,
            IRandomService random,
            TimeProvider timeProvider,
            InstantFocusGrantService instantGrantService,
            FocusEnactmentPolicy enactmentPolicy)
        {
            _worldPlayerRepo = worldPlayerRepo;
            _worldPlayerService = worldPlayerService;
            _playerAccessService = playerAccessService;
            _cityRepo = cityRepo;
            _ideologyFocusDataReader = ideologyFocusDataReader;
            _ideologyDataReader = ideologyDataReader;
            _instantUtility = instantUtility;
            _cityStatService = cityStatService;
            _jobRepository = jobRepository;
            _resourceService = resourceService;
            _ideologyFocusRepository = ideologyFocusRepository;
            _unitDataReader = unitDataReader;
            _random = random;
            _timeProvider = timeProvider;
            _instantGrantService = instantGrantService;
            _enactmentPolicy = enactmentPolicy;
        }

        public async Task<IdeologyFocusAnswerDTO?> EnactIdeologyFocus(IdeologyFocusRequestDTO ideologyFocusDTO)
        {
            await _ideologyFocusRepository.DeleteExpiredFocusesForCityAsync(ideologyFocusDTO.CityId);

            var city = await _playerAccessService.RequireOwnedCityAsync(ideologyFocusDTO.CityId);

            var ideologyFocusData = _ideologyFocusDataReader.GetIdeology(ideologyFocusDTO.IdeologyFocusName);
            var worldPlayer = city.WorldPlayer;

            if (worldPlayer == null || worldPlayer.Ideology != ideologyFocusData.RequiredIdeology)
                return new IdeologyFocusAnswerDTO(ideologyFocusData.Name, city.Id, "The focus does not belong to the player's ideology", false);

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (!_enactmentPolicy.CanEnact(ideologyFocusData, city.ActiveFocuses, now))
            {
                return new IdeologyFocusAnswerDTO(null, null, "City is already affected by the specified ideology focus", false);
            }

            if (worldPlayer!.IdeologyFocusPoints < ideologyFocusData.IdeologyFocusPointCost)
            {
                return new IdeologyFocusAnswerDTO(ideologyFocusData.Name, city.Id, "Insufficient Ideology Points", false);
            }

            IdeologyFocusEffectResultDTO? effectResult = null;
            if (ideologyFocusData.EffectKind == IdeologyFocusEffectKindEnum.Instant)
            {
                effectResult = await HandleInstantFocus(ideologyFocusData.Name, city);
                if (effectResult.GrantedQuantity <= 0)
                {
                    return new IdeologyFocusAnswerDTO(
                        ideologyFocusData.Name,
                        city.Id,
                        effectResult.Summary,
                        false,
                        effectResult);
                }
            }

            var resourceSnapshot = _resourceService.CalculateGlobalResources(worldPlayer, now);
            worldPlayer.IdeologyFocusPoints = resourceSnapshot.IdeologyFocusPoints - ideologyFocusData.IdeologyFocusPointCost;
            worldPlayer.LastResourceUpdate = now;

            _worldPlayerService.SyncGlobalResources(worldPlayer, now);
            await _worldPlayerRepo.UpdateAsync(worldPlayer);

            bool shouldPersistFocus = _enactmentPolicy.ShouldPersist(ideologyFocusData);

            if (shouldPersistFocus)
            {
                IdeologyFocus ideologyFocusEntity = new()
                {
                    Name = ideologyFocusDTO.IdeologyFocusName,
                    DateCreated = now,
                    DateLastModified = now,
                    CityId = city.Id,
                    TimeOfIdeologyStarted = now,
                    TimeOfIdeologyFinished = ideologyFocusData.TimeActive.HasValue
                        ? now.Add(ideologyFocusData.TimeActive.Value)
                        : null
                };

                await _ideologyFocusRepository.AddAsync(ideologyFocusEntity);
            }

            await _cityRepo.UpdateAsync(city);

            var successMessage = ideologyFocusData.EffectKind == IdeologyFocusEffectKindEnum.Instant
                ? effectResult!.Summary
                : $"{ideologyFocusData.Name} enacted successfully";
            return new IdeologyFocusAnswerDTO(ideologyFocusData.Name, city.Id, successMessage, true, effectResult);
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

            var city = await _playerAccessService.RequireOwnedCityAsync(cityId);

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
            var cityJobs = new List<Domain.Workers.Abstraction.BaseJob>();
            cityJobs.AddRange(await _jobRepository.GetRecruitmentJobsAsync(city.Id));
            cityJobs.AddRange(await _jobRepository.GetBuildingJobsAsync(city.Id));
            int availablePopulation = Math.Max(0, _cityStatService.GetAvailablePopulation(city, cityJobs));

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

                        AlreadyEnacted = activeRecord != null &&
                            !(staticFocus.EffectKind == IdeologyFocusEffectKindEnum.Instant && staticFocus.CanRepeat),
                        ActiveTime = staticFocus.TimeActive,
                        ExpirationTime = activeRecord?.TimeOfIdeologyFinished ?? DateTime.MinValue
                        ,EffectKind = staticFocus.EffectKind
                        ,TargetScope = staticFocus.TargetScope
                        ,CanRepeat = staticFocus.CanRepeat
                        ,ConsumesOnTrigger = staticFocus.ConsumesOnTrigger
                        ,IsAvailable = staticFocus.Name != IdeologyFocusNameEnum.LordsLevy || availablePopulation >= 100
                        ,UnavailableReason = staticFocus.Name == IdeologyFocusNameEnum.LordsLevy && availablePopulation < 100
                            ? "Requires at least 100 available population"
                            : string.Empty
                    };
                }).ToList();
            }
            else
            {
                dto.IdeologyFocuses = new List<IdeologyFocusDTO>();
            }

            return dto;
        }

        private async Task<IdeologyFocusEffectResultDTO> HandleInstantFocus(IdeologyFocusNameEnum focusName, City city)
        {
            return focusName switch
            {
                IdeologyFocusNameEnum.LordsLevy => await _instantGrantService.GrantLordsLevy(city),
                IdeologyFocusNameEnum.NewRecruits => await _instantGrantService.GrantNewRecruits(city),
                _ => throw new InvalidOperationException($"No instant handler exists for {focusName}.")
            };
        }
    }
}
