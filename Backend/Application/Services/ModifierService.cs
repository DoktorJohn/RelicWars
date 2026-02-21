using Application.DTOs;
using Application.Interfaces.IServices;
using Domain.Abstraction;
using Domain.Entities;
using Domain.Enums;
using Domain.User;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Application.Services
{
    public class ModifierService : IModifierService
    {
        private readonly ILogger<ModifierService> _logger;
        private readonly IModifierCollectorService _collector;

        public ModifierService(ILogger<ModifierService> logger, IModifierCollectorService collector)
        {
            _logger = logger;
            _collector = collector;
        }

        public ModifierCalculationResult CalculateCityValue(City city, double baseValue, params ModifierTagEnum[] targetTags)
        {
            var providers = _collector.CollectAllProvidersForCity(city);
            return CalculateEntityValueWithModifiers(baseValue, targetTags, providers);
        }

        public ModifierCalculationResult CalculatePlayerValue(WorldPlayer player, double baseValue, params ModifierTagEnum[] targetTags)
        {
            var providers = _collector.CollectAllProvidersForPlayer(player);
            return CalculateEntityValueWithModifiers(baseValue, targetTags, providers);
        }

        public ModifierCalculationResult CalculateEntityValueWithModifiers(
        double baseValue,
        IEnumerable<ModifierTagEnum> targetTags,
        IEnumerable<IModifierProvider> providers)
        {
            double flatSum = 0;
            double increasedSum = 0;
            double decreasedSum = 0;
            var appliedModifiers = new List<Modifier>();
            var tagLookup = targetTags.ToHashSet();

            // Verbøs logging af providers for at fange manglende referencer
            var providerNames = providers.Select(p => p?.GetType().Name ?? "NULL");
            _logger.LogDebug("[ModifierService] Calculating with providers: {Providers}", string.Join(", ", providerNames));

            foreach (var provider in providers)
            {
                if (provider == null) continue;

                var modifiersFromProvider = provider.GetModifiers();

                foreach (var modifier in modifiersFromProvider)
                {
                    if (!tagLookup.Contains(modifier.Tag)) continue;

                    appliedModifiers.Add(modifier);

                    switch (modifier.Type)
                    {
                        case ModifierTypeEnum.Flat:
                            flatSum += modifier.Value;
                            break;
                        case ModifierTypeEnum.Increased:
                            increasedSum += modifier.Value;
                            break;
                        case ModifierTypeEnum.Decreased:
                            decreasedSum += modifier.Value;
                            break;
                    }

                    _logger.LogInformation("[ModifierService] APPLYING: {Type} {Value} from {Tag} (Source: {Source})",
                        modifier.Type, modifier.Value, modifier.Tag, provider.GetType().Name);
                }
            }

            double totalMultiplier = 1.0 + increasedSum - decreasedSum;
            if (totalMultiplier < 0) totalMultiplier = 0;

            double finalValue = (baseValue + flatSum) * totalMultiplier;

            return new ModifierCalculationResult
            {
                BaseValue = baseValue,
                FlatBonus = flatSum,
                PercentageBonus = (increasedSum - decreasedSum),
                FinalValue = finalValue,
                AppliedModifiers = appliedModifiers
            };
        }
    }
}