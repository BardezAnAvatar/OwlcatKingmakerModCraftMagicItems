using System;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.ActivatableAbilities;

namespace CraftMagicItems.Patches.Harmony
{
    [Harmony12.HarmonyPatch(typeof(ActivatableAbility), "OnEventDidTrigger", new Type[] { typeof(RuleAttackWithWeaponResolve) })]
    // ReSharper disable once UnusedMember.Local
    internal static class ActivatableAbilityOnEventDidTriggerRuleAttackWithWeaponResolvePatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(ActivatableAbility __instance, RuleAttackWithWeaponResolve evt)
        {
            if (evt.Damage != null && evt.AttackRoll.IsHit)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}