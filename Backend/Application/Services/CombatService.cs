using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Readers;
using Domain.User;

namespace Application.Services
{
    public record CombatContext(
        List<UnitStack> AttackerStacks,
        List<UnitStack> DefenderStacks,
        City? AttackerOriginCity,
        City? DefenderCity,
        WorldPlayer? AttackerPlayer = null,
        WorldPlayer? DefenderPlayer = null,
        UnitDeployment? AttackerDeployment = null,
        UnitDeployment? DefenderDeployment = null);

    public record CombatResult(
        List<UnitStack> RemainingAttackers,
        List<UnitStack> RemainingDefenders,
        List<UnitStack> AttackerLosses,
        List<UnitStack> DefenderLosses,
        List<UnitStack> RevivedDefenders,
        double LuckModifier,
        List<string> AppliedModifiers);

    public class CombatService
    {
        private readonly UnitDataReader _unitReader;
        private readonly IModifierService _modifierService;
        private readonly IRandomService _random;
        private const double DamageScaling = 0.5;

        public CombatService(UnitDataReader unitReader, IModifierService modifierService, IRandomService random)
        {
            _unitReader = unitReader;
            _modifierService = modifierService;
            _random = random;
        }

        public CombatResult ResolveBattle(List<UnitStack> attackers, List<UnitStack> defenders) =>
            ResolveBattle(new CombatContext(attackers, defenders, null, null));

        public CombatResult ResolveBattle(CombatContext context)
        {
            double luck = 0.8 + (_random.NextDouble() * 0.4);
            var attackerStats = GetTotalStats(context.AttackerStacks, context.AttackerOriginCity, false, context.AttackerDeployment);
            var defenderStats = GetTotalStats(context.DefenderStacks, context.DefenderCity, true, context.DefenderDeployment);

            double damageToDefender = attackerStats.Power * luck * DamageScaling;
            double damageToAttacker = defenderStats.Power * (1 / luck) * DamageScaling;

            bool defenderIsAllied = context.DefenderCity != null && (context.DefenderPlayer == null ||
                context.DefenderCity.WorldPlayerId == context.DefenderPlayer.Id ||
                (context.DefenderCity.WorldPlayer?.AllianceId.HasValue == true &&
                 context.DefenderCity.WorldPlayer.AllianceId == context.DefenderPlayer.AllianceId));
            double defenderCasualtyMultiplier = !defenderIsAllied ? 1 :
                _modifierService.CalculateCityValue(context.DefenderCity!, 1, ModifierTagEnum.Casualties).FinalValue;

            var defenderLosses = DistributeDamage(context.DefenderStacks, damageToDefender * defenderCasualtyMultiplier, context.DefenderCity, true, context.DefenderDeployment);
            var attackerLosses = DistributeDamage(context.AttackerStacks, damageToAttacker, context.AttackerOriginCity, false, context.AttackerDeployment);
            var revived = ReviveLosses(context.DefenderStacks, defenderLosses, context.DefenderCity);

            return new CombatResult(
                context.AttackerStacks.Where(s => s.Quantity > 0).ToList(),
                context.DefenderStacks.Where(s => s.Quantity > 0).ToList(),
                attackerLosses, defenderLosses, revived, luck,
                context.DefenderCity?.ActiveFocuses
                    .Where(x => x.IsActive)
                    .Select(x => x.Name.ToString())
                    .ToList() ?? new());
        }

        private (double Power, double Armor) GetTotalStats(List<UnitStack> army, City? city, bool isDefender, UnitDeployment? deployment)
        {
            double totalPower = 0;
            double totalArmor = 0;
            foreach (var stack in army)
            {
                var data = _unitReader.GetUnit(stack.Type);
                double power = data.Power;
                double armor = data.Armor;
                double discipline = data.Discipline;
                if (city != null)
                {
                    var categoryTag = data.Category switch
                    {
                        UnitCategoryEnum.Infantry => ModifierTagEnum.InfantryStats,
                        UnitCategoryEnum.Cavalry => ModifierTagEnum.CavalryStats,
                        UnitCategoryEnum.Siege => ModifierTagEnum.SiegeStats,
                        UnitCategoryEnum.Naval => ModifierTagEnum.NavalStats,
                        _ => ModifierTagEnum.Placeholder
                    };
                    power = _modifierService.CalculateCityUnitValue(city, data, power, ModifierTagEnum.Power, categoryTag).FinalValue;
                    discipline = _modifierService.CalculateCityUnitValue(city, data, discipline, ModifierTagEnum.Discipline, categoryTag).FinalValue;
                    if (isDefender)
                        armor = _modifierService.CalculateCityUnitValue(city, data, armor, ModifierTagEnum.Armor, ModifierTagEnum.Wall, categoryTag).FinalValue;
                }
                else if (deployment != null && isDefender)
                {
                    armor = _modifierService.CalculateEntityValueWithModifiers(
                        armor, new[] { ModifierTagEnum.Armor }, new[] { deployment }).FinalValue;
                }
                double disciplineMultiplier = 1 + discipline / 100.0;
                totalPower += stack.Quantity * power * disciplineMultiplier;
                totalArmor += stack.Quantity * armor * disciplineMultiplier;
            }
            return (totalPower, totalArmor);
        }

        private List<UnitStack> DistributeDamage(List<UnitStack> army, double totalDamage, City? city, bool isDefender, UnitDeployment? deployment)
        {
            var losses = new List<UnitStack>();
            foreach (var item in army.Select(s => new { Stack = s, Data = _unitReader.GetUnit(s.Type) }).OrderBy(x => x.Data.Reach))
            {
                if (totalDamage <= 0) break;
                double armor = item.Data.Armor;
                double discipline = item.Data.Discipline;
                if (city != null && isDefender)
                {
                    armor = _modifierService.CalculateCityUnitValue(city, item.Data, armor, ModifierTagEnum.Armor, ModifierTagEnum.Wall).FinalValue;
                    discipline = _modifierService.CalculateCityUnitValue(city, item.Data, discipline, ModifierTagEnum.Discipline).FinalValue;
                }
                else if (deployment != null && isDefender)
                {
                    armor = _modifierService.CalculateEntityValueWithModifiers(
                        armor, new[] { ModifierTagEnum.Armor }, new[] { deployment }).FinalValue;
                }
                double armorPerUnit = Math.Max(0.1, armor * (1 + discipline / 100.0));
                int killed = (int)Math.Min(item.Stack.Quantity, Math.Floor(totalDamage / armorPerUnit));
                if (killed == 0 && totalDamage > 0 && item.Stack.Quantity > 0 && _random.NextDouble() < totalDamage / armorPerUnit) killed = 1;
                if (killed > 0)
                {
                    item.Stack.Quantity -= killed;
                    totalDamage -= killed * armorPerUnit;
                    losses.Add(new UnitStack { Type = item.Stack.Type, Quantity = killed });
                }
            }
            return losses;
        }

        private List<UnitStack> ReviveLosses(List<UnitStack> survivors, List<UnitStack> losses, City? city)
        {
            var revived = new List<UnitStack>();
            if (city == null) return revived;
            double revivalRate = Math.Max(0, _modifierService.CalculateCityValue(city, 1, ModifierTagEnum.Revival).FinalValue - 1);
            foreach (var loss in losses)
            {
                int quantity = (int)Math.Floor(loss.Quantity * revivalRate);
                if (quantity <= 0) continue;
                var survivor = survivors.FirstOrDefault(s => s.Type == loss.Type);
                if (survivor == null) survivors.Add(new UnitStack { Type = loss.Type, Quantity = quantity });
                else survivor.Quantity += quantity;
                loss.Quantity -= quantity;
                revived.Add(new UnitStack { Type = loss.Type, Quantity = quantity });
            }
            return revived;
        }
    }
}
