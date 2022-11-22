using HarmonyLib;

namespace EnhancedDistrictServices
{
    [HarmonyPatch(typeof(DistrictManager))]
    [HarmonyPatch("ReleaseParkImplementation")]
    public class DistrictManagerReleaseParkPatch
    {
        public static bool Prefix(byte park, ref EDSDistrictPark data)
        {
            var districtPark = EDSDistrictPark.FromPark(park);
            if (districtPark.Name != string.Empty)
            {
                Constraints.ReleaseDistrictPark(districtPark);
            }

            return true;
        }
    }
}
