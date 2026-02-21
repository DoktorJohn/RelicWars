using Application.DTOs;
using Domain.Abstraction;
using Domain.Entities;
using Domain.Enums;
using Domain.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IModifierService
    {
        ModifierCalculationResult CalculateEntityValueWithModifiers(
            double baseValue,
            IEnumerable<ModifierTagEnum> targetTags,
            IEnumerable<IModifierProvider> providers);

        ModifierCalculationResult CalculateCityValue(City city, double baseValue, params ModifierTagEnum[] targetTags);
        ModifierCalculationResult CalculatePlayerValue(WorldPlayer player, double baseValue, params ModifierTagEnum[] targetTags);
    }
}
