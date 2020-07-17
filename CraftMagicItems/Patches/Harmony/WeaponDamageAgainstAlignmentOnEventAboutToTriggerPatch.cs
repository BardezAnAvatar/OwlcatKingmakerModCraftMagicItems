using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;

namespace CraftMagicItems.Patches.Harmony
{
    [Harmony12.HarmonyPatch(typeof(WeaponDamageAgainstAlignment), "OnEventAboutToTrigger")]
    // ReSharper disable once UnusedMember.Local
    internal static class WeaponDamageAgainstAlignmentOnEventAboutToTriggerPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(WeaponDamageAgainstAlignment __instance, RulePrepareDamage evt)
        {
            if (__instance is ItemEnchantmentLogic logic)
            {
                if (evt.DamageBundle.WeaponDamage == null)
                {
                    return false;
                }
                evt.DamageBundle.WeaponDamage.AddAlignment(__instance.WeaponAlignment);

                if (evt.Target.Descriptor.Alignment.Value.HasComponent(__instance.EnemyAlignment) && Validation.EquipmentEnchantmentValid(evt.DamageBundle.Weapon, logic.Owner))
                {
                    int rollsCount = __instance.Value.DiceCountValue.Calculate(logic.Context);
                    int bonusDamage = __instance.Value.BonusValue.Calculate(logic.Context);
                    EnergyDamage energyDamage = new EnergyDamage(new DiceFormula(rollsCount, __instance.Value.DiceType), __instance.DamageType);
                    energyDamage.AddBonusTargetRelated(bonusDamage);
                    evt.DamageBundle.Add(energyDamage);
                }
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}