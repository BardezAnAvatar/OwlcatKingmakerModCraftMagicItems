using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.RuleSystem.Rules.Damage;

namespace CraftMagicItems.Patches.Harmony
{
    [Harmony12.HarmonyPatch(typeof(WeaponReality), "OnEventAboutToTrigger")]
    // ReSharper disable once UnusedMember.Local
    internal static class WeaponRealityOnEventAboutToTriggerPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(WeaponReality __instance, RulePrepareDamage evt)
        {
            if (__instance is ItemEnchantmentLogic logic)
            {
                if (evt.DamageBundle.WeaponDamage == null)
                {
                    return false;
                }
                if (Validation.EquipmentEnchantmentValid(evt.DamageBundle.Weapon, logic.Owner))
                {
                    evt.DamageBundle.WeaponDamage.Reality |= __instance.Reality;
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