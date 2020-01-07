using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnhancedDistrictServices
{
    [HarmonyPatch(typeof(CableCarStationAI))]
    [HarmonyPatch("CreateVehicle")]
    public class CableCarStationAICreateVehiclePatch
    {
        public static bool Prefix(
            ushort buildingID,
            ref Building buildingData,
            ushort sourceNode,
            ushort targetNode)
        {
            VehicleManagerMod.CurrentSourceBuilding = buildingID;
            return true;
        }

        public static void Postfix(
            ushort buildingID,
            ref Building buildingData,
            ushort sourceNode,
            ushort targetNode)
        {
            VehicleManagerMod.CurrentSourceBuilding = 0;
        }
    }
}
