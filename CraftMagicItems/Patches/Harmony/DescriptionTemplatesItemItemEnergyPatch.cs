using Kingmaker.UI.Tooltip;

namespace CraftMagicItems.Patches.Harmony
{
    [Harmony12.HarmonyPatch(typeof(DescriptionTemplatesItem), "ItemEnergy")]
    // ReSharper disable once UnusedMember.Local
    internal static class DescriptionTemplatesItemItemEnergyPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(TooltipData data, bool __result)
        {
            if (__result)
            {
                if (data.Energy.Count > 0)
                {
                    data.Energy.Clear();
                }
            }
        }
    }
}