using Kingmaker;
using Kingmaker.UI;
using Kingmaker.UI.Common;

namespace CraftMagicItems.Patches.Harmony
{
    [Harmony12.HarmonyPatch(typeof(Game), "OnAreaLoaded")]
    // ReSharper disable once UnusedMember.Local
    public static class GameOnAreaLoadedPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix()
        {
            if (CustomBlueprintBuilder.DidDowngrade)
            {
                UIUtility.ShowMessageBox("Craft Magic Items is disabled.  All your custom enchanted items and crafting feats have been replaced with vanilla versions.", DialogMessageBox.BoxType.Message, null);
                CustomBlueprintBuilder.Reset();
            }
        }
    }
}