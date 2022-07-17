using HarmonyLib;

namespace EnhancedDistrictServices
{
    [HarmonyPatch(typeof(DistrictManager))]
    [HarmonyPatch("ReleaseDistrictImplementation")]
    public class DistrictManagerReleaseDistrictPatch
    {
        public static bool Prefix(byte district, ref District data)
        {
            var districtPark = DistrictPark.FromDistrict(district);
            if (districtPark.Name != string.Empty)
            {
                Constraints.ReleaseDistrictPark(districtPark);
            }

            return true;
        }
    }
}
