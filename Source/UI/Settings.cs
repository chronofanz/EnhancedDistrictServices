using ColossalFramework;
using UnityEngine;

namespace EnhancedDistrictServices
{
    public static class Settings
    {
        public static readonly SavedInputKey toggleMainTool = new SavedInputKey(nameof(toggleMainTool), "EnhancedDistrictServices", SavedInputKey.Encode(KeyCode.D, true, false, false), true);
        public static readonly SavedInputKey keyCopy = new SavedInputKey(nameof(keyCopy), "EnhancedDistrictServices", SavedInputKey.Encode(KeyCode.C, true, false, false), true);
        public static readonly SavedInputKey keyPaste = new SavedInputKey(nameof(keyPaste), "EnhancedDistrictServices", SavedInputKey.Encode(KeyCode.V, true, false, false), true);

        public static readonly SavedBool enableDummyCargoTraffic = new SavedBool(nameof(enableDummyCargoTraffic), "EnhancedDistrictServices", true, true);
        public static readonly SavedBool enableSelectOutsideConnection = new SavedBool(nameof(enableSelectOutsideConnection), "EnhancedDistrictServices", false, true);
        public static readonly SavedBool showCampusDistricts = new SavedBool(nameof(showCampusDistricts), "EnhancedDistrictServices", true, true);
        public static readonly SavedBool showIndustryDistricts = new SavedBool(nameof(showIndustryDistricts), "EnhancedDistrictServices", true, true);
        public static readonly SavedBool showParkDistricts = new SavedBool(nameof(showParkDistricts), "EnhancedDistrictServices", true, true);
        public static readonly SavedBool showWelcomeMessage = new SavedBool(nameof(showWelcomeMessage), "EnhancedDistrictServices", true, true);
    }
}
