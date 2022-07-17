using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Purpose: Better matching has a consequence - a tsunami of vehicles waiting to spawn at outside connections.
    /// </summary>
    public class CarAITrySpawnPatch
    {
        public static void Enable(Harmony harmony)
        {
            var original = typeof(CarAI).GetMethod("TrySpawn");
            if (original == null)
            {
                throw new InvalidOperationException("Could not find CarAI::TrySpawn!");
            }

            var prefix = typeof(CarAITrySpawnPatch).GetMethod("Prefix");
            if (prefix == null)
            {
                throw new InvalidOperationException("Could not find CarAITrySpawnPatch::Prefix!");
            }

            harmony.Patch(original, prefix: new HarmonyMethod(prefix));
        }

        private static bool[] m_congestionStatus = new bool[BuildingManager.MAX_BUILDING_COUNT];
        private static int[] m_vehicleSpawnCounter = new int[BuildingManager.MAX_BUILDING_COUNT];

        public static bool Prefix(ushort vehicleID, ref Vehicle vehicleData, ref bool __result)
        {
            ushort outsideRoadConnection = OutsideConnectionInfo.FindNearestOutsideRoadConnection(ref vehicleData.m_segment);
            if (outsideRoadConnection == 0)
            {
                return true;
            }

            if ((vehicleData.m_flags & Vehicle.Flags.Spawned) != 0)
            {
                __result = true;
                return false;
            }

            bool spawnVehicle = false;

            // If in the process of spawning next batch of vehicles ...
            if (m_vehicleSpawnCounter[outsideRoadConnection] >= 0)
            {
                if (vehicleID >= m_vehicleSpawnCounter[outsideRoadConnection])
                {
                    m_vehicleSpawnCounter[outsideRoadConnection] = vehicleID;
                }
                else
                {
                    var counter = vehicleID;
                    while (counter < m_vehicleSpawnCounter[outsideRoadConnection])
                    {
                        counter += VehicleManager.MAX_VEHICLE_COUNT;
                    }

                    m_vehicleSpawnCounter[outsideRoadConnection] = counter;
                }

                spawnVehicle = true;

                if (m_vehicleSpawnCounter[outsideRoadConnection] >= 2 * VehicleManager.MAX_VEHICLE_COUNT)
                {
                    // Logger.Log($"CarAI::TrySpawn: (batch spawn end) source={outsideRoadConnection}");
                    m_vehicleSpawnCounter[outsideRoadConnection] = -1;
                }
            }
            else if (CheckOverlap(vehicleData.m_segment, (ushort)0, 1000f))
            {
                m_congestionStatus[outsideRoadConnection] = true;
                vehicleData.m_flags |= Vehicle.Flags.WaitingSpace;
            }
            else
            {
                spawnVehicle = true;

                // It was previously congested ... setup the vehicleSpawnCounter
                if (m_congestionStatus[outsideRoadConnection])
                {
                    // Logger.Log($"CarAI::TrySpawn: (batch spawn start) source={outsideRoadConnection}");
                    m_congestionStatus[outsideRoadConnection] = false;
                    m_vehicleSpawnCounter[outsideRoadConnection] = vehicleID;
                }
            }

            if (spawnVehicle)
            {
                vehicleData.Spawn(vehicleID);
                vehicleData.m_flags &= Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive;

                __result = true;
                return false;
            }

            __result = false;
            return false;
        }

        private static bool CheckOverlap(Segment3 segment, ushort ignoreVehicle, float maxVelocity)
        {
            VehicleManager instance = Singleton<VehicleManager>.instance;
            Vector3 vector3_1 = segment.Min();
            Vector3 vector3_2 = segment.Max();
            int num1 = Mathf.Max((int)(((double)vector3_1.x - 10.0) / 32.0 + 270.0), 0);
            int num2 = Mathf.Max((int)(((double)vector3_1.z - 10.0) / 32.0 + 270.0), 0);
            int num3 = Mathf.Min((int)(((double)vector3_2.x + 10.0) / 32.0 + 270.0), 539);
            int num4 = Mathf.Min((int)(((double)vector3_2.z + 10.0) / 32.0 + 270.0), 539);
            bool overlap = false;
            for (int index1 = num2; index1 <= num4; ++index1)
            {
                for (int index2 = num1; index2 <= num3; ++index2)
                {
                    ushort otherID = instance.m_vehicleGrid[index1 * 540 + index2];
                    int num5 = 0;
                    while (otherID != (ushort)0)
                    {
                        otherID = CheckOverlap(segment, ignoreVehicle, maxVelocity, otherID, ref instance.m_vehicles.m_buffer[(int)otherID], ref overlap);
                        if (++num5 > 16384)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            return overlap;
        }

        private static ushort CheckOverlap(
            Segment3 segment,
            ushort ignoreVehicle,
            float maxVelocity,
            ushort otherID,
            ref Vehicle otherData,
            ref bool overlap)
        {
            float u;
            float v;
            if ((ignoreVehicle == (ushort)0 || (int)otherID != (int)ignoreVehicle && (int)otherData.m_leadingVehicle != (int)ignoreVehicle && (int)otherData.m_trailingVehicle != (int)ignoreVehicle) && ((double)segment.DistanceSqr(otherData.m_segment, out u, out v) < 4.0 && otherData.Info.m_vehicleType != VehicleInfo.VehicleType.Bicycle) && (double)otherData.GetLastFrameData().m_velocity.sqrMagnitude < (double)maxVelocity * (double)maxVelocity)
                overlap = true;
            return otherData.m_nextGridVehicle;
        }

        private static ushort GetDriverInstance(ushort vehicleID, ref Vehicle data)
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

        private static void GetSourceTarget(ushort vehicleID, ref Vehicle vehicleData, out ushort source, out ushort target)
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

        private static bool IsOutsideRoadConnection(ushort buildingId)
        {
            var info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId].Info;
            return info.m_buildingAI is OutsideConnectionAI outsideConnectionAI && outsideConnectionAI.m_transportInfo?.m_vehicleType == VehicleInfo.VehicleType.Car;
        }
    }
}
