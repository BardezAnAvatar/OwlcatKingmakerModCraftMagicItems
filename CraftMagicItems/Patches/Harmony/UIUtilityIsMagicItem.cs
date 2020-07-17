using Kingmaker.Items;
using Kingmaker.UI.Common;

namespace CraftMagicItems.Patches.Harmony
{
    [Harmony12.HarmonyPatch(typeof(UIUtility), "IsMagicItem")]
    // ReSharper disable once UnusedMember.Local
    internal static class UIUtilityIsMagicItem
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(ItemEntity item, ref bool __result)
        {
            if (__result == false && item != null && item.IsIdentified && item is ItemEntityShield shield && shield.WeaponComponent != null)
            {
                __result = Main.ItemPlusEquivalent(shield.WeaponComponent.Blueprint) > 0;
            }
        }
    }
}