using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class DeploymentModifierSnapshotService
    {
        private readonly IModifierService _modifierService;
        public DeploymentModifierSnapshotService(IModifierService modifierService) => _modifierService = modifierService;

        public void ApplyOutgoingModifiers(City city, UnitDeployment deployment)
        {
            if (deployment.Type != UnitDeploymentTypeEnum.Trade) return;
            double multiplier = _modifierService.CalculateCityValue(city, 1, ModifierTagEnum.MerchantDefense).FinalValue;
            if (multiplier <= 1) return;
            deployment.ModifiersInternal.Add(new Modifier
            {
                Tag = ModifierTagEnum.Armor,
                Type = ModifierTypeEnum.Increased,
                Value = multiplier - 1,
                Source = "Outgoing merchant defense snapshot"
            });
        }
    }
}
