using System;
using System.Linq;
using System.Reflection;
using Kingmaker.Enums.Damage;
using Kingmaker.Utility;

namespace CraftMagicItems.Patches
{
    /// <summary>Class that performs the Harmony patching</summary>
    public class HarmonyPatcher
    {
        /// <summary><see cref="Action{string}" /> that logs error messages</summary>
        protected Action<string> LogError;

        /// <summary>Harmony instance used to patch code</summary>
        protected Harmony12.HarmonyInstance HarmonyInstance;

        /// <summary>Definition constructor</summary>
        /// <param name="logger"><see cref="Action{string}" /> that logs error messages</param>
        public HarmonyPatcher(Action<string> logger)
        {
            HarmonyInstance = Harmony12.HarmonyInstance.Create("kingmaker.craftMagicItems");
            LogError = logger;
        }

        /// <summary>
        ///     Patches all classes in the assembly decorated with <see cref="Harmony12.HarmonyPatch" />,
        ///     starting in the order of the methods named in <paramref name="methodNameOrder" />.
        /// </summary>
        /// <param name="methodNameOrder">
        ///     Ordered array of method names that should be patched in this order before any
        ///     other methods are patched.
        /// </param>
        public void PatchAllOrdered(params string[] methodNameOrder)
        {
            var allAttributes = Assembly.GetExecutingAssembly().GetTypes()
                .Select(type => new { type, methods = Harmony12.HarmonyMethodExtensions.GetHarmonyMethods(type) })
                .Where(pair => pair.methods != null && pair.methods.Count > 0)
                .Select(pair => new { pair.type, attributes = Harmony12.HarmonyMethod.Merge(pair.methods) })
                .OrderBy(pair =>
                    methodNameOrder
                        .Select((name, index) => new { name, index })
                        .FirstOrDefault(nameIndex => nameIndex.name.Equals(pair.attributes.methodName))
                        ?.index
                    ?? methodNameOrder.Length)
                ;

            foreach (var pair in allAttributes)
            {
                new Harmony12.PatchProcessor(HarmonyInstance, pair.type, pair.attributes).Patch();
            }
        }

        /// <summary>
        ///     Unpatches all classes in the assembly decorated with <see cref="Harmony12.HarmonyPatch" />,
        ///     except the ones whose method names match any in <paramref name="exceptMethodName" />.
        /// </summary>
        /// <param name="exceptMethodName">Array of method names that should be not be unpatched</param>
        public void UnpatchAllExcept(params string[] exceptMethodName)
        {
            if (HarmonyInstance != null)
            {
                try
                {
                    foreach (var method in HarmonyInstance.GetPatchedMethods().ToArray())
                    {
                        if (!exceptMethodName.Contains(method.Name) && HarmonyInstance.GetPatchInfo(method).Owners.Contains(HarmonyInstance.Id))
                        {
                            HarmonyInstance.Unpatch(method, Harmony12.HarmonyPatchType.All, HarmonyInstance.Id);
                        }
                    }
                }
                catch (Exception e)
                {
                    LogError($"Exception during Un-patching: {e}");
                }
            }
        }
    }
}