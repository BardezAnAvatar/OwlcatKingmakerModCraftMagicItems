using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.FactLogic;

namespace CraftMagicItems.Patches.Harmony
{
    [Harmony12.HarmonyPatch(typeof(AttackStatReplacement), "OnEventAboutToTrigger")]
    // ReSharper disable once UnusedMember.Local
    public static class AttackStatReplacementOnEventAboutToTriggerPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(AttackStatReplacement __instance, RuleCalculateAttackBonusWithoutTarget evt)
        {
            if (evt.Weapon != null && __instance.SubCategory == WeaponSubCategory.Finessable &&
                evt.Weapon.Blueprint.Type.Category.HasSubCategory(WeaponSubCategory.Finessable) && Main.IsOversized(evt.Weapon.Blueprint))
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