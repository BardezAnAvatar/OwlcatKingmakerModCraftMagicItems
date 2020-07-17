using Kingmaker.Items;
using Kingmaker.Items.Slots;

namespace CraftMagicItems.Patches.Harmony
{
    [Harmony12.HarmonyPatch(typeof(ItemEntityWeapon), "HoldInTwoHands", Harmony12.MethodType.Getter)]
    // ReSharper disable once UnusedMember.Local
    internal static class ItemEntityWeaponHoldInTwoHandsPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(ItemEntityWeapon __instance, ref bool __result)
        {
            if (!__result)
            {
                if (__instance.IsShield && __instance.Blueprint.IsOneHandedWhichCanBeUsedWithTwoHands && __instance.Wielder != null)
                {
                    HandSlot handSlot = __instance.Wielder.Body.PrimaryHand;
                    __result = handSlot != null && !handSlot.HasItem;
                }
            }
        }
    }
}