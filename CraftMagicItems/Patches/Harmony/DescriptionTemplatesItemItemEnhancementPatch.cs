using System;
using System.Linq;
using Kingmaker.UI.Tooltip;

namespace CraftMagicItems.Patches.Harmony
{
    [Harmony12.HarmonyPatch(typeof(DescriptionTemplatesItem), "ItemEnhancement")]
    // ReSharper disable once UnusedMember.Local
    internal static class DescriptionTemplatesItemItemEnhancementPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(TooltipData data)
        {
            if (data.Texts.ContainsKey(Enum.GetValues(typeof(TooltipElement)).Cast<TooltipElement>().Max() + 1))
            {
                data.Texts[TooltipElement.Enhancement] = data.Texts[Enum.GetValues(typeof(TooltipElement)).Cast<TooltipElement>().Max() + 1];
            }
            else if (data.Texts.ContainsKey(TooltipElement.Enhancement))
            {
                data.Texts.Remove(TooltipElement.Enhancement);
            }
        }
    }
}