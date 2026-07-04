using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class UnitMovementCalculator
    {
        private readonly IModifierService _modifierService;
        public UnitMovementCalculator(IModifierService modifierService) => _modifierService = modifierService;
        public int CalculateMobilitySnapshot(City originCity, int baseMobility) => Math.Max(1,
            (int)Math.Floor(_modifierService.CalculateCityValue(originCity, Math.Max(1, baseMobility), ModifierTagEnum.TravelSpeed).FinalValue));
        public double CalculateSecondsPerHex(int mobility) => 7200.0 / Math.Max(1, mobility);
        public double CalculateTravelSeconds(int originX, int originY, int targetX, int targetY, int mobility) =>
            CalculateHexDistance(originX, originY, targetX, targetY) * CalculateSecondsPerHex(mobility);

        private static int CalculateHexDistance(int originX, int originY, int targetX, int targetY)
        {
            int deltaX = originX - targetX;
            int deltaY = originY - targetY;
            return Math.Max(Math.Abs(deltaX), Math.Max(Math.Abs(deltaY), Math.Abs(deltaX + deltaY)));
        }
    }
}
