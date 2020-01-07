using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnhancedDistrictServices
{
    [HarmonyPatch(typeof(TransportStationAI))]
    [HarmonyPatch("CreateIncomingVehicle")]
    public class TransportStationAICreateIncomingVehiclePatch
    {
        public static bool Prefix(
            ushort buildingID,
            ref Building buildingData,
            ushort startStop,
            int gateIndex)
        {
            VehicleManagerMod.CurrentSourceBuilding = buildingID;
            return true;
        }

        public static void Postfix(
            ushort buildingID,
            ref Building buildingData,
            ushort startStop,
            int gateIndex)
        {
            VehicleManagerMod.CurrentSourceBuilding = 0;
        }
    }

    [HarmonyPatch(typeof(TransportStationAI))]
    [HarmonyPatch("CreateOutgoingVehicle")]
    public class TransportStationAICreateOutgoingVehiclePatch
    {
        public static bool Prefix(
            ushort buildingID,
            ref Building buildingData,
            ushort startStop,
            int gateIndex)
        {
            VehicleManagerMod.CurrentSourceBuilding = buildingID;
            return true;
        }

        public static void Postfix(
            ushort buildingID,
            ref Building buildingData,
            ushort startStop,
            int gateIndex)
        {
            VehicleManagerMod.CurrentSourceBuilding = 0;
        }
    }
}
