﻿using ColossalFramework;
using Harmony;
using System;
using UnityEngine;

namespace EnhancedDistrictServices
{
    [HarmonyPatch(typeof(CargoTruckAI))]
    [HarmonyPatch("SimulationStep")]
    [HarmonyPatch(new Type[] { typeof(ushort), typeof(Vehicle), typeof(Vector3) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal })]
    public class CargoTruckAIPatchSimulationStep
    {
        public static bool Prefix(ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
        {
            if ((data.m_flags & Vehicle.Flags.WaitingTarget) != ~(Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive))
            {
                if ((data.m_flags & Vehicle.Flags.TransferToSource) != ~(Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive))
                {
                    // Artifically lengthen the wait counter in a way that interferes with the base game code as little as possible ...
                    var index = SimulationManager.instance.m_currentFrameIndex >> 4;
                    if (data.m_waitCounter > 2 && index % 2 == 0)
                    {
                        --data.m_waitCounter;
                    }

                    if (data.m_waitCounter < 20)
                    {
                        AddIncomingOffer(vehicleID, ref data);
                    }
                }
            }

            return true;
        }

        private static void AddIncomingOffer(ushort vehicleID, ref Vehicle data)
        {
            if ((data.m_flags & Vehicle.Flags.TransferToSource) != ~(Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive))
            {
                if ((int)data.m_transferSize < ((CargoTruckAI)data.Info.GetAI()).m_cargoCapacity)
                {
                    Singleton<TransferManager>.instance.AddIncomingOffer((TransferManager.TransferReason)data.m_transferType, new TransferManager.TransferOffer()
                    {
                        Priority = 7,
                        Vehicle = vehicleID,
                        Position = data.m_sourceBuilding == (ushort)0 ? data.GetLastFramePosition() : (data.GetLastFramePosition() + Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)data.m_sourceBuilding].m_position) * 0.5f,
                        Amount = 1,
                        Active = true
                    });
                    data.m_flags |= Vehicle.Flags.WaitingTarget;
                }
            }
        }
    }

    [HarmonyPatch(typeof(GarbageTruckAI))]
    [HarmonyPatch("SimulationStep")]
    [HarmonyPatch(new Type[] { typeof(ushort), typeof(Vehicle), typeof(Vector3) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal })]
    public class GarbageTruckAIPatchSimulationStep
    {
        public static bool Prefix(ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
        {
            if ((data.m_flags & Vehicle.Flags.WaitingTarget) != ~(Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive))
            {
                if ((data.m_flags & Vehicle.Flags.TransferToSource) != ~(Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive))
                {
                    // Artifically lengthen the wait counter in a way that interferes with the base game code as little as possible ...
                    var index = SimulationManager.instance.m_currentFrameIndex >> 4;
                    if (data.m_waitCounter > 2 && index % 2 == 0)
                    {
                        --data.m_waitCounter;
                    }

                    if (data.m_waitCounter < 20)
                    {
                        AddIncomingOffer(vehicleID, ref data);
                    }
                }
            }

            return true;
        }

        private static void AddIncomingOffer(ushort vehicleID, ref Vehicle data)
        {
            if ((data.m_flags & Vehicle.Flags.TransferToSource) != ~(Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive))
            {
                if ((int)data.m_transferSize < ((GarbageTruckAI)data.Info.GetAI()).m_cargoCapacity)
                {
                    Singleton<TransferManager>.instance.AddIncomingOffer((TransferManager.TransferReason)data.m_transferType, new TransferManager.TransferOffer()
                    {
                        Priority = 7,
                        Vehicle = vehicleID,
                        Position = data.m_sourceBuilding == (ushort)0 ? data.GetLastFramePosition() : (data.GetLastFramePosition() + Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)data.m_sourceBuilding].m_position) * 0.5f,
                        Amount = 1,
                        Active = true
                    });
                    data.m_flags |= Vehicle.Flags.WaitingTarget;
                }
            }
        }
    }

    [HarmonyPatch(typeof(HearseAI))]
    [HarmonyPatch("SimulationStep")]
    [HarmonyPatch(new Type[] { typeof(ushort), typeof(Vehicle), typeof(Vector3) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal })]
    public class HearseAIPatchSimulationStep
    {
        public static bool Prefix(ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
        {
            if ((data.m_flags & Vehicle.Flags.WaitingTarget) != ~(Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive))
            {
                if ((data.m_flags & Vehicle.Flags.TransferToSource) != ~(Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive))
                {
                    // Artifically lengthen the wait counter in a way that interferes with the base game code as little as possible ...
                    var index = SimulationManager.instance.m_currentFrameIndex >> 4;
                    if (data.m_waitCounter > 2 && index % 2 == 0)
                    {
                        --data.m_waitCounter;
                    }

                    if (data.m_waitCounter < 20)
                    {
                        AddIncomingOffer(vehicleID, ref data);
                    }
                }
            }

            return true;
        }

        private static void AddIncomingOffer(ushort vehicleID, ref Vehicle data)
        {
            BuildingManager instance = Singleton<BuildingManager>.instance;
            if ((data.m_flags & Vehicle.Flags.TransferToSource) != ~(Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive))
            {
                int a = ((HearseAI)data.Info.GetAI()).m_corpseCapacity;
                if (data.m_sourceBuilding != (ushort)0)
                {
                    BuildingInfo info = instance.m_buildings.m_buffer[(int)data.m_sourceBuilding].Info;
                    if (info == null)
                        return;
                    int amount;
                    int max;
                    info.m_buildingAI.GetMaterialAmount(data.m_sourceBuilding, ref instance.m_buildings.m_buffer[(int)data.m_sourceBuilding], TransferManager.TransferReason.Dead, out amount, out max);
                    a = Mathf.Min(a, max - amount);
                }

                if ((int)data.m_transferSize < a)
                {
                    Singleton<TransferManager>.instance.AddIncomingOffer((TransferManager.TransferReason)data.m_transferType, new TransferManager.TransferOffer()
                    {
                        Priority = 7,
                        Vehicle = vehicleID,
                        Position = data.m_sourceBuilding == (ushort)0 ? data.GetLastFramePosition() : (data.GetLastFramePosition() + Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)data.m_sourceBuilding].m_position) * 0.5f,
                        Amount = 1,
                        Active = true
                    });
                    data.m_flags |= Vehicle.Flags.WaitingTarget;
                }
            }
        }

        [HarmonyPatch(typeof(PoliceCarAI))]
        [HarmonyPatch("SimulationStep")]
        [HarmonyPatch(new Type[] { typeof(ushort), typeof(Vehicle), typeof(Vector3) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal })]
        public class PoliceCarAIPatchSimulationStep
        {
            public static bool Prefix(ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
            {
                if ((data.m_flags & Vehicle.Flags.WaitingTarget) != ~(Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive))
                {
                    if (data.Info.GetAI() is PoliceCarAI)
                    {
                        // Artifically lengthen the wait counter in a way that interferes with the base game code as little as possible ...
                        var index = SimulationManager.instance.m_currentFrameIndex >> 4;
                        if (data.m_waitCounter > 2 && index % 2 == 0)
                        {
                            --data.m_waitCounter;
                        }

                        if (data.m_waitCounter < 20)
                        {
                            AddIncomingOffer(vehicleID, ref data);
                        }
                    }
                }

                return true;
            }

            private static void AddIncomingOffer(ushort vehicleID, ref Vehicle data)
            {
                if ((int)data.m_transferSize < ((PoliceCarAI)data.Info.GetAI()).m_criminalCapacity)
                {
                    Singleton<TransferManager>.instance.AddIncomingOffer((TransferManager.TransferReason)data.m_transferType, new TransferManager.TransferOffer()
                    {
                        Priority = 7,
                        Vehicle = vehicleID,
                        Position = data.m_sourceBuilding == (ushort)0 ? data.GetLastFramePosition() : data.GetLastFramePosition() * 0.25f + Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)data.m_sourceBuilding].m_position * 0.75f,
                        Amount = 1,
                        Active = true
                    });
                    data.m_flags |= Vehicle.Flags.WaitingTarget;
                }
            }
        }
    }
}
