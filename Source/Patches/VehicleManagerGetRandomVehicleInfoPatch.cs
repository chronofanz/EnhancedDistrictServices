using ColossalFramework.Math;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnhancedDistrictServices
{
    [HarmonyPatch(typeof(VehicleManager))]
    [HarmonyPatch("GetRandomVehicleInfo")]
    [HarmonyPatch(new Type[] { typeof(Randomizer), typeof(ItemClass.Service), typeof(ItemClass.SubService), typeof(ItemClass.Level) }, new ArgumentType[] { ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal })]
    public class VehicleManagerGetRandomVehicleInfoPatch1
    {
        public static bool Prefix(
            ref Randomizer r,
            ItemClass.Service service,
            ItemClass.SubService subService,
            ItemClass.Level level,
            ref VehicleInfo __result)
        {
            if (!Settings.enableCustomVehicles)
            {
                return true;
            }

            __result = VehicleManagerMod.GetRandomVehicleInfo(ref r, service, subService, level);
            if (__result == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(VehicleManager))]
    [HarmonyPatch("GetRandomVehicleInfo")]
    [HarmonyPatch(new Type[] { typeof(Randomizer), typeof(ItemClass.Service), typeof(ItemClass.SubService), typeof(ItemClass.Level), typeof(VehicleInfo.VehicleType) }, new ArgumentType[] { ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal })]
    public class VehicleManagerGetRandomVehicleInfoPatch2
    {
        public static bool Prefix(
            ref Randomizer r,
            ItemClass.Service service,
            ItemClass.SubService subService,
            ItemClass.Level level,
            VehicleInfo.VehicleType type,
            ref VehicleInfo __result)
        {
            if (!Settings.enableCustomVehicles)
            {
                return true;
            }                
                
            __result = VehicleManagerMod.GetRandomVehicleInfo(ref r, service, subService, level, type);
            if (__result == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
