using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Readers;
using Domain.StaticData; // Her ligger din UnitData class

namespace Application.Services
{
    public record CombatResult(
        List<UnitStack> RemainingAttackers,
        List<UnitStack> RemainingDefenders,
        List<UnitStack> AttackerLosses,
        List<UnitStack> DefenderLosses,
        double LuckModifier);

    public class CombatService
    {
        private readonly UnitDataReader _unitReader;
        private const double DamageScaling = 0.5; // Controls how many units die per battle

        public CombatService(UnitDataReader unitReader)
        {
            _unitReader = unitReader;
        }

        public CombatResult ResolveBattle(List<UnitStack> attackers, List<UnitStack> defenders)
        {
            // 1. Calculate Luck (-20% to +20%)
            double luck = 0.8 + (Random.Shared.NextDouble() * 0.4);

            // 2. Calculate Totals based on your GetUnit method
            var attackerStats = GetTotalStats(attackers);
            var defenderStats = GetTotalStats(defenders);

            // 3. Calculate Damage
            double damageToDefender = (attackerStats.Power * luck) * DamageScaling;
            double damageToAttacker = (defenderStats.Power * (1 / luck)) * DamageScaling;

            // 4. Distribute Casualties
            var defenderLosses = DistributeDamage(defenders, damageToDefender);
            var attackerLosses = DistributeDamage(attackers, damageToAttacker);

            return new CombatResult(
                attackers.Where(s => s.Quantity > 0).ToList(),
                defenders.Where(s => s.Quantity > 0).ToList(),
                attackerLosses,
                defenderLosses,
                luck
            );
        }

        private (double Power, double Armor) GetTotalStats(List<UnitStack> army)
        {
            double totalPower = 0;
            double totalArmor = 0;

            foreach (var stack in army)
            {
                // HER BRUGER VI DIN RIGTIGE METODE: GetUnit
                var data = _unitReader.GetUnit(stack.Type);

                double discMod = 1 + (data.Discipline / 100.0);
                totalPower += stack.Quantity * data.Power * discMod;
                totalArmor += stack.Quantity * data.Armor * discMod;
            }

            return (totalPower, totalArmor);
        }

        private List<UnitStack> DistributeDamage(List<UnitStack> army, double totalDamage)
        {
            var losses = new List<UnitStack>();

            // Sorter efter Reach: Units with Reach 1 (Frontline) die first
            var sortedArmy = army
                .Select(s => new { Stack = s, Data = _unitReader.GetUnit(s.Type) })
                .OrderBy(x => x.Data.Reach)
                .ToList();

            double remainingDamage = totalDamage;

            foreach (var item in sortedArmy)
            {
                if (remainingDamage <= 0) break;

                double armorPerUnit = item.Data.Armor * (1 + (item.Data.Discipline / 100.0));

                // Calculate how many units are destroyed by the damage
                int killed = (int)Math.Min(item.Stack.Quantity, Math.Floor(remainingDamage / armorPerUnit));

                if (killed > 0)
                {
                    item.Stack.Quantity -= killed;
                    remainingDamage -= (killed * armorPerUnit);
                    losses.Add(new UnitStack { Type = item.Stack.Type, Quantity = killed });
                }
                else if (remainingDamage > 0 && item.Stack.Quantity > 0)
                {
                    // "Chip damage": If damage is less than 1 unit's armor, there's a % chance it dies
                    if (Random.Shared.NextDouble() < (remainingDamage / armorPerUnit))
                    {
                        item.Stack.Quantity -= 1;
                        losses.Add(new UnitStack { Type = item.Stack.Type, Quantity = 1 });
                    }
                    remainingDamage = 0;
                }
            }
            return losses;
        }
    }
}