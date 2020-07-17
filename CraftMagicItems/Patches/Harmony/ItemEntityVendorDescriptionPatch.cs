using CraftMagicItems.Localization;
using Kingmaker.Items;

namespace CraftMagicItems.Patches.Harmony
{
    [Harmony12.HarmonyPatch(typeof(ItemEntity), "VendorDescription", Harmony12.MethodType.Getter)]
    // ReSharper disable once UnusedMember.Local
    internal static class ItemEntityVendorDescriptionPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(ItemEntity __instance, ref string __result)
        {
            // If the "vendor" is a party member, return that the item was crafted rather than from a merchant
            if (__instance.Vendor != null && __instance.Vendor.IsPlayerFaction)
            {
                __result = LocalizationHelper.FormatLocalizedString("craftMagicItems-crafted-source-description", __instance.Vendor.CharacterName);
                return false;
            }
            return true;
        }
    }
}