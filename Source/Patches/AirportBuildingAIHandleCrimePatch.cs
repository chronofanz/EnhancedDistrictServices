using ColossalFramework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnhancedDistrictServices
{
    [HarmonyPatch(typeof(AirportBuildingAI))]
    [HarmonyPatch("HandleCrime")]
    public class AirportBuildingAIHandleCrimePatch
    {
        public static bool Prefix(
            AirportBuildingAI __instance,
            ushort buildingID,
            ref Building data,
            int crimeAccumulation,
            int citizenCount)
        {
            DistrictManager instance1 = Singleton<DistrictManager>.instance;
            BuildingManager instance2 = Singleton<BuildingManager>.instance;
            byte num1 = instance1.GetPark(data.m_position);
            if (num1 != (byte)0 && !instance1.m_parks.m_buffer[(int)num1].IsAirport)
                num1 = (byte)0;
            ushort num2 = 0;
            if (num1 != (byte)0)
            {
                num2 = instance1.m_parks.m_buffer[(int)num1].m_randomGate;
                if (num2 == (ushort)0)
                    num2 = instance1.m_parks.m_buffer[(int)num1].m_mainGate;
            }
            if (num2 == buildingID) // pachang: bug fix in main code ...
            {
                HandleCrime(buildingID, ref data, crimeAccumulation, citizenCount);
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// CommonBuildingAI::HandleCrime
        /// </summary>
        /// <param name="buildingID"></param>
        /// <param name="data"></param>
        /// <param name="crimeAccumulation"></param>
        /// <param name="citizenCount"></param>
        private static void HandleCrime(
          ushort buildingID,
          ref Building data,
          int crimeAccumulation,
          int citizenCount)
        {
            if (crimeAccumulation != 0)
            {
                byte park = Singleton<DistrictManager>.instance.GetPark(data.m_position);
                if (park != (byte)0 && (Singleton<DistrictManager>.instance.m_parks.m_buffer[(int)park].m_parkPolicies & DistrictPolicies.Park.SugarBan) != DistrictPolicies.Park.None)
                    crimeAccumulation = (int)((double)crimeAccumulation * 1.20000004768372);
                if (Singleton<SimulationManager>.instance.m_isNightTime)
                    crimeAccumulation = crimeAccumulation * 5 >> 2;
                if (data.m_eventIndex != (ushort)0)
                {
                    EventManager instance = Singleton<EventManager>.instance;
                    crimeAccumulation = instance.m_events.m_buffer[(int)data.m_eventIndex].Info.m_eventAI.GetCrimeAccumulation(data.m_eventIndex, ref instance.m_events.m_buffer[(int)data.m_eventIndex], crimeAccumulation);
                }
                crimeAccumulation = Singleton<SimulationManager>.instance.m_randomizer.Int32((uint)crimeAccumulation);
                crimeAccumulation = UniqueFacultyAI.DecreaseByBonus(UniqueFacultyAI.FacultyBonus.Law, crimeAccumulation);
                if (!Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.PoliceDepartment))
                    crimeAccumulation = 0;
            }
            data.m_crimeBuffer = (ushort)Mathf.Min(citizenCount * 100, (int)data.m_crimeBuffer + crimeAccumulation);
            int crimeBuffer = (int)data.m_crimeBuffer;
            if (citizenCount != 0 && crimeBuffer > citizenCount * 25 && Singleton<SimulationManager>.instance.m_randomizer.Int32(5U) == 0)
            {
                int count = 0;
                int cargo = 0;
                int capacity = 0;
                int outside = 0;
                CalculateGuestVehicles(buildingID, ref data, TransferManager.TransferReason.Crime, ref count, ref cargo, ref capacity, ref outside);
                if (count == 0)
                    Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Crime, new TransferManager.TransferOffer()
                    {
                        Priority = crimeBuffer / Mathf.Max(1, citizenCount * 10),
                        Building = buildingID,
                        Position = data.m_position,
                        Amount = 1
                    });
            }
            SetCrimeNotification(ref data, citizenCount);
        }

        private static void CalculateGuestVehicles(
            ushort buildingID,
            ref Building data,
            TransferManager.TransferReason material,
            ref int count,
            ref int cargo,
            ref int capacity,
            ref int outside)
        {
            VehicleManager instance = Singleton<VehicleManager>.instance;
            ushort vehicleID = data.m_guestVehicles;
            int num = 0;
            while (vehicleID != (ushort)0)
            {
                if ((TransferManager.TransferReason)instance.m_vehicles.m_buffer[(int)vehicleID].m_transferType == material)
                {
                    int size;
                    int max;
                    instance.m_vehicles.m_buffer[(int)vehicleID].Info.m_vehicleAI.GetSize(vehicleID, ref instance.m_vehicles.m_buffer[(int)vehicleID], out size, out max);
                    cargo += Mathf.Min(size, max);
                    capacity += max;
                    ++count;
                    if ((instance.m_vehicles.m_buffer[(int)vehicleID].m_flags & (Vehicle.Flags.Importing | Vehicle.Flags.Exporting)) != ~(Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive))
                        ++outside;
                }
                vehicleID = instance.m_vehicles.m_buffer[(int)vehicleID].m_nextGuestVehicle;
                if (++num > 16384)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                    break;
                }
            }
        }

        private static void SetCrimeNotification(ref Building data, int citizenCount)
        {
            Notification.ProblemStruct problems1 = Notification.RemoveProblems(data.m_problems, (Notification.ProblemStruct)Notification.Problem1.Crime);
            if ((int)data.m_crimeBuffer > citizenCount * 90)
                problems1 = Notification.AddProblems(problems1, (Notification.ProblemStruct)(Notification.Problem1.Crime | Notification.Problem1.MajorProblem));
            else if ((int)data.m_crimeBuffer > citizenCount * 60)
                problems1 = Notification.AddProblems(problems1, (Notification.ProblemStruct)Notification.Problem1.Crime);
            data.m_problems = problems1;
        }
    }
}
