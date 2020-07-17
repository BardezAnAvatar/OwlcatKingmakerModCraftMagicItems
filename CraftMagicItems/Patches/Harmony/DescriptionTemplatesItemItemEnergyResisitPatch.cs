using Kingmaker.UI.Tooltip;

namespace CraftMagicItems.Patches.Harmony
{
    [Harmony12.HarmonyPatch(typeof(DescriptionTemplatesItem), "ItemEnergyResisit")]
    // ReSharper disable once UnusedMember.Local
    internal static class DescriptionTemplatesItemItemEnergyResisitPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }
}