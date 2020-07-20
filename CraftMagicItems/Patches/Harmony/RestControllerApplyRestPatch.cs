using Kingmaker.Controllers.Rest;
using Kingmaker.UnitLogic;

namespace CraftMagicItems.Patches.Harmony
{
    /// <remarks>Make characters in the party work on their crafting projects when they rest.</remarks>
    [Harmony12.HarmonyPatch(typeof(RestController), "ApplyRest")]
    // ReSharper disable once UnusedMember.Local
    public static class RestControllerApplyRestPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Prefix(UnitDescriptor unit)
        {
            CraftingLogic.WorkOnProjects(unit, false);
        }
    }
}