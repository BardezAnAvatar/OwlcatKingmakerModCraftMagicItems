using System.Linq;
using Kingmaker;
using Kingmaker.UI.Log;

namespace CraftMagicItems.Patches.Harmony
{
    /// <remarks>
    ///     Add "pending" log items when the battle log becomes available again, so crafting messages sent
    ///     when e.g. camping in the overland map are still shown eventually.
    /// </remarks>
    [Harmony12.HarmonyPatch(typeof(BattleLogManager), "Initialize")]
    // ReSharper disable once UnusedMember.Local
    public static class BattleLogManagerInitializePatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix()
        {
            if (Enumerable.Any(Main.PendingLogItems))
            {
                foreach (var item in Main.PendingLogItems)
                {
                    item.UpdateSize();
                    Game.Instance.UI.BattleLogManager.LogView.AddLogEntry(item);
                }

                Main.PendingLogItems.Clear();
            }
        }
    }
}