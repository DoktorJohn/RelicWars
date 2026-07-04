using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class ResistanceService : IResistanceService
    {
        private readonly IModifierService _modifierService;

        public ResistanceService(IModifierService modifierService) => _modifierService = modifierService;

        public double CalculateRecoveryPerHour(City city) =>
            _modifierService.CalculateCityValue(city, 1, ModifierTagEnum.ResistanceRecovery).FinalValue;

        public void UpdateResistance(City city, DateTime now)
        {
            double hours = Math.Max(0, (now - city.LastResistanceUpdate).TotalHours);
            double recovery = CalculateRecoveryPerHour(city) * hours;
            city.Resistance = Math.Min(city.ResistanceTarget, city.Resistance + recovery);
            city.LastResistanceUpdate = now;
        }
    }
}
