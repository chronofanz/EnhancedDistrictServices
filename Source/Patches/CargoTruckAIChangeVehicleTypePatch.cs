using ColossalFramework;
using HarmonyLib;
using System;
using UnityEngine;

namespace EnhancedDistrictServices
{
    [HarmonyPatch(typeof(CargoTruckAI))]
    [HarmonyPatch("ChangeVehicleType")]
    [HarmonyPatch(new Type[] { typeof(VehicleInfo), typeof(ushort), typeof(Vehicle), typeof(PathUnit.Position), typeof(uint) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal })]
    public class CargoTruckAIChangeVehicleTypePatch
    {
        public static bool Prefix(
            VehicleInfo vehicleInfo,
            ushort vehicleID,
            ref Vehicle vehicleData,
            PathUnit.Position pathPos,
            uint laneID)
        {
            VehicleManager instance1 = Singleton<VehicleManager>.instance;
            NetManager instance2 = Singleton<NetManager>.instance;
            BuildingManager instance3 = Singleton<BuildingManager>.instance;
            NetInfo info1 = instance2.m_segments.m_buffer[(int)pathPos.m_segment].Info;
            Vector3 position1 = instance2.m_lanes.m_buffer[laneID].CalculatePosition(0.5f);
            Vector3 lastPos = position1;

            ushort cargoStation1 = FindCargoStation(position1, info1.m_class.m_service, info1.m_class.m_subService);
            ushort cargoStation2 = FindCargoStation(lastPos, info1.m_class.m_service, info1.m_class.m_subService);

            var infoFrom = instance3.m_buildings.m_buffer[(int)cargoStation1].Info;
            var infoTo = instance3.m_buildings.m_buffer[(int)cargoStation2].Info;
            var cargoStationId = (infoFrom?.m_buildingAI is OutsideConnectionAI && infoFrom?.m_class?.m_subService == infoTo?.m_class?.m_subService) ? cargoStation2 : cargoStation1;

            return true;
        }

        private static ushort FindCargoStation(
            Vector3 position,
            ItemClass.Service service,
            ItemClass.SubService subservice = ItemClass.SubService.None)
        {
            BuildingManager instance = Singleton<BuildingManager>.instance;
            if (subservice != ItemClass.SubService.PublicTransportPlane)
                subservice = ItemClass.SubService.None;
            ushort num1 = instance.FindBuilding(position, 100f, service, subservice, Building.Flags.None, Building.Flags.None);
            int num2 = 0;
            while (num1 != (ushort)0)
            {
                ushort parentBuilding = instance.m_buildings.m_buffer[(int)num1].m_parentBuilding;
                BuildingInfo info = instance.m_buildings.m_buffer[(int)num1].Info;
                if (info.m_buildingAI is CargoStationAI || info.m_buildingAI is OutsideConnectionAI || parentBuilding == (ushort)0)
                    return num1;
                num1 = parentBuilding;
                if (++num2 > 49152)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                    break;
                }
            }

            return 0;
        }
    }
}
