using ColossalFramework;
using Harmony;
using System;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Purpose: Prevent overloading network with vehicles ...
    /// </summary>
    [HarmonyPatch(typeof(CarAI))]
    [HarmonyPatch(nameof(CarAI.CreateVehicle))]
    public class CarAICreateVehiclePatch
    {
        /// <summary>
        /// Purpose: Prevent overloading network with vehicles ...
        /// </summary>
        public static bool Prefix(ushort vehicleID, ref Vehicle data)
        {
            GetSourceTarget(vehicleID, ref data, out var source, out var target);

            // Do not spawn if the building has too many vehicles!
            if (!(Singleton<BuildingManager>.instance.m_buildings.m_buffer[source].Info.m_buildingAI is DepotAI))
            {
                if (GetVehicleCount(source) > 300)
                {
                    return false;
                }
            }

            return true;
        }

        public static ushort GetDriverInstance(ushort vehicleID, ref Vehicle data)
        {
            CitizenManager instance1 = Singleton<CitizenManager>.instance;
            uint num1 = data.m_citizenUnits;
            int num2 = 0;
            while ((int)num1 != 0)
            {
                uint nextUnit = instance1.m_units.m_buffer[num1].m_nextUnit;
                for (int index = 0; index < 5; ++index)
                {
                    uint citizen = instance1.m_units.m_buffer[num1].GetCitizen(index);
                    if ((int)citizen != 0)
                    {
                        ushort instance2 = instance1.m_citizens.m_buffer[citizen].m_instance;
                        if ((int)instance2 != 0)
                            return instance2;
                    }
                }
                num1 = nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                    break;
                }
            }
            return 0;
        }

        public static void GetSourceTarget(ushort vehicleID, ref Vehicle vehicleData, out ushort source, out ushort target)
        {
            var driverInstance = GetDriverInstance(vehicleID, ref vehicleData);
            var driver = CitizenManager.instance.m_instances.m_buffer[driverInstance].m_citizen;

            source = VehicleManager.instance.m_vehicles.m_buffer[vehicleID].m_sourceBuilding;
            target = VehicleManager.instance.m_vehicles.m_buffer[vehicleID].m_targetBuilding;
            if (source == 0 && target == 0 && driver != 0)
            {
                source = CitizenManager.instance.m_instances.m_buffer[driverInstance].m_sourceBuilding;
                target = CitizenManager.instance.m_instances.m_buffer[driverInstance].m_targetBuilding;
            }
        }

        public static ushort GetVehicleCount(ushort buildingID)
        {
            var instance = Singleton<VehicleManager>.instance;

            ushort count = 0;
            ushort vehicleID = BuildingManager.instance.m_buildings.m_buffer[buildingID].m_ownVehicles;
            while (vehicleID != (ushort)0)
            {
                var prefabAI = instance.m_vehicles.m_buffer[(int)vehicleID].Info.GetAI();
                var material = (TransferManager.TransferReason)instance.m_vehicles.m_buffer[(int)vehicleID].m_transferType;
                count++;

                vehicleID = instance.m_vehicles.m_buffer[(int)vehicleID].m_nextOwnVehicle;
            }

            return count;
        }

    }
}
