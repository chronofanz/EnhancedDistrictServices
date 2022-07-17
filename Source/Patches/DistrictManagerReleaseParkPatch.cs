using HarmonyLib;

namespace EnhancedDistrictServices
{
    [HarmonyPatch(typeof(DistrictManager))]
    [HarmonyPatch("ReleaseParkImplementation")]
    public class DistrictManagerReleaseParkPatch
    {
        public static bool Prefix(byte park, ref DistrictPark data)
        {
            var districtPark = DistrictPark.FromPark(park);
            if (districtPark.Name != string.Empty)
            {
                Constraints.ReleaseDistrictPark(districtPark);
            }

            return true;
        }
    }
}
