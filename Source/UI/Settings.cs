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
        public static readonly SavedBool enableParkDistricts = new SavedBool(nameof(enableParkDistricts), "EnhancedDistrictServices", true, true);
    }
}
