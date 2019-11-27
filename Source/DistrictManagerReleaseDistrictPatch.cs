using ColossalFramework;
using Harmony;
using System;

namespace EnhancedDistrictServices
{
    [HarmonyPatch(typeof(DistrictManager))]
    [HarmonyPatch("ReleaseDistrict")]
    public class DistrictManagerReleaseDistrictPatch
    {
        public static bool Prefix(byte district)
        {
            Constraints.ReleaseDistrict(district);
            return true;
        }
    }
}
