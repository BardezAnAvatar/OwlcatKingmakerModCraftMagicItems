using Kingmaker.UI.ActionBar;

namespace CraftMagicItems.Patches.Harmony
{
    /// <remarks>Fix issue in Owlcat's UI - ActionBarManager.Update does not refresh the Groups (spells/Actions/Belt)</remarks>
    [Harmony12.HarmonyPatch(typeof(ActionBarManager), "Update")]
    // ReSharper disable once UnusedMember.Local
    public static class ActionBarManagerUpdatePatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Prefix(ActionBarManager __instance)
        {
            var mNeedReset = Main.Accessors.GetActionBarManagerNeedReset(__instance);
            if (mNeedReset)
            {
                var mSelected = Main.Accessors.GetActionBarManagerSelected(__instance);
                __instance.Group.Set(mSelected);
            }
        }
    }
}