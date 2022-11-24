using HarmonyLib;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Purpose: Ensure the outside connection is producing sufficient goods ...
    /// </summary>
    [HarmonyPatch(typeof(OutsideConnectionAI))]
    [HarmonyPatch("GetProductionRate")]
    public class OutsideConnectionAIGetProductionRatePatch
    {
        /// <summary>
        /// Produce the goods ...
        /// </summary>
        public static bool Prefix(int productionRate, int budget, ref int __result)
        {
            __result = 125;
            return false;
        }
    }
}
