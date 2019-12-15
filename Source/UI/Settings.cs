using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using UnityEngine;

namespace EnhancedDistrictServices
{
    public static class Settings
    {
        public static readonly SavedInputKey toggleMainTool = new SavedInputKey(nameof(toggleMainTool), "EnhancedDistrictServices", SavedInputKey.Encode(KeyCode.D, true, false, false), true);
        public static readonly SavedBool enableDummyCargoTraffic = new SavedBool(nameof(enableDummyCargoTraffic), "EnhancedDistrictServices", true, true);
        public static readonly SavedBool enableSelectOutsideConnection = new SavedBool(nameof(enableSelectOutsideConnection), "EnhancedDistrictServices", false, true);
        public static readonly SavedBool showParkDistricts = new SavedBool(nameof(showParkDistricts), "EnhancedDistrictServices", true, true);
    }
}
