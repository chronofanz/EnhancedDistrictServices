using ColossalFramework;
using UnityEngine;

namespace EnhancedDistrictServices
{
    public static class Settings
    {
        public static readonly string FileName = "EnhancedDistrictServices";

        public static readonly SavedInputKey toggleMainTool = new SavedInputKey(nameof(toggleMainTool), FileName, SavedInputKey.Encode(KeyCode.D, true, false, false), true);
        public static readonly SavedInputKey keyCopy = new SavedInputKey(nameof(keyCopy), FileName, SavedInputKey.Encode(KeyCode.C, true, false, false), true);
        public static readonly SavedInputKey keyPaste = new SavedInputKey(nameof(keyPaste), FileName, SavedInputKey.Encode(KeyCode.V, true, false, false), true);

        public static readonly SavedBool enableCustomVehicles = new SavedBool(nameof(enableCustomVehicles), FileName, false, true);
        public static readonly SavedBool enableDummyCargoTraffic = new SavedBool(nameof(enableDummyCargoTraffic), FileName, true, true);
        public static readonly SavedBool enableSelectOutsideConnection = new SavedBool(nameof(enableSelectOutsideConnection), FileName, false, true);
        public static readonly SavedBool showCampusDistricts = new SavedBool(nameof(showCampusDistricts), FileName, true, true);
        public static readonly SavedBool showIndustryDistricts = new SavedBool(nameof(showIndustryDistricts), FileName, true, true);
        public static readonly SavedBool showParkDistricts = new SavedBool(nameof(showParkDistricts), FileName, true, true);
        public static readonly SavedBool showWelcomeMessage = new SavedBool(nameof(showWelcomeMessage), FileName, true, true);

        public static readonly SavedInt savedEDSButtonX = new SavedInt(nameof(savedEDSButtonX), FileName, -1000, true);
        public static readonly SavedInt savedEDSButtonY = new SavedInt(nameof(savedEDSButtonY), FileName, -1000, true);

        public static readonly SavedString modVersion = new SavedString(nameof(modVersion), FileName, "1.0.0.0", true);
    }
}
