using Kingmaker;
using Kingmaker.UI.Log;

namespace CraftMagicItems.Patches.Harmony
{
    [Harmony12.HarmonyPatch(typeof(LogDataManager.LogItemData), "UpdateSize")]
    // ReSharper disable once UnusedMember.Local
    public static class LogItemDataUpdateSizePatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix()
        {
            // Avoid null pointer exception when BattleLogManager not set.
            return Game.Instance.UI.BattleLogManager != null;
        }
    }
}