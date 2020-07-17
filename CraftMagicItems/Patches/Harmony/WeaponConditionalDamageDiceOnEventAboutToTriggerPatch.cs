using System.Linq;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.Mechanics.WeaponEnchants;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Utility;

namespace CraftMagicItems.Patches.Harmony
{

    [Harmony12.HarmonyPatch(typeof(WeaponConditionalDamageDice), "OnEventAboutToTrigger")]
    // ReSharper disable once UnusedMember.Local
    internal static class WeaponConditionalDamageDiceOnEventAboutToTriggerPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(WeaponConditionalDamageDice __instance, RulePrepareDamage evt)
        {
            if (__instance is ItemEnchantmentLogic logic)
            {
                if (evt.DamageBundle.WeaponDamage == null)
                {
                    return false;
                }
                if (__instance.IsBane)
                {
                    if (logic.Owner.Enchantments.Any((ItemEnchantment e) => e.Get<SuppressBane>()))
                    {
                        return false;
                    }
                }
                if (__instance.CheckWielder)
                {
                    using (logic.Enchantment.Context.GetDataScope(logic.Owner.Wielder.Unit))
                    {
                        if (Validation.EquipmentEnchantmentValid(evt.DamageBundle.Weapon, logic.Owner) && __instance.Conditions.Check(null))
                        {
                            BaseDamage damage = __instance.Damage.CreateDamage();
                            evt.DamageBundle.Add(damage);
                        }
                    }
                }
                else
                {
                    using (logic.Enchantment.Context.GetDataScope(evt.Target))
                    {
                        if (Validation.EquipmentEnchantmentValid(evt.DamageBundle.Weapon, logic.Owner) && __instance.Conditions.Check(null))
                        {
                            BaseDamage damage2 = __instance.Damage.CreateDamage();
                            evt.DamageBundle.Add(damage2);
                        }
                    }
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