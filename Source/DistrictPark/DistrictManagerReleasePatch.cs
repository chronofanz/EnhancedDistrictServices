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
            Constraints.ReleaseDistrictPark(DistrictPark.FromDistrict(district));
            return true;
        }
    }

    [HarmonyPatch(typeof(DistrictManager))]
    [HarmonyPatch("ReleasePark")]
    public class DistrictManagerReleaseParkPatch
    {
        public static bool Prefix(byte park)
        {
            Constraints.ReleaseDistrictPark(DistrictPark.FromPark(park));
            return true;
        }
    }
}
