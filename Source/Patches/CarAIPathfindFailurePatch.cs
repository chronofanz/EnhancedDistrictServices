using ColossalFramework;
using HarmonyLib;

namespace EnhancedDistrictServices
{
    [HarmonyPatch(typeof(CarAI))]
    [HarmonyPatch("PathfindFailure")]
    public class CarAIPathfindFailurePatch
    {
        public static void Postfix(ushort vehicleID, ref Vehicle data)
        {
            GetSourceTarget(vehicleID, ref data, out ushort source, out ushort target);
            if (source != 0 && target != 0)
            {
                TransferManagerMod.AddBuildingToBuildingExclusion(source, target);
            }
        }

        private static void GetSourceTarget(ushort vehicleID, ref Vehicle vehicleData, out ushort source, out ushort target)
        {
            source = VehicleManager.instance.m_vehicles.m_buffer[vehicleID].m_sourceBuilding;
            target = VehicleManager.instance.m_vehicles.m_buffer[vehicleID].m_targetBuilding;

            if (source == 0 && target == 0)
            {
                var driverInstance = GetDriverInstance(vehicleID, ref vehicleData);
                var driver = CitizenManager.instance.m_instances.m_buffer[driverInstance].m_citizen;
                if (driver != 0)
                {
                    source = CitizenManager.instance.m_instances.m_buffer[driverInstance].m_sourceBuilding;
                    target = CitizenManager.instance.m_instances.m_buffer[driverInstance].m_targetBuilding;
                }
            }
        }

        private static ushort GetDriverInstance(ushort vehicleID, ref Vehicle data)
        {
            CitizenManager instance1 = CitizenManager.instance;
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
    }
}
