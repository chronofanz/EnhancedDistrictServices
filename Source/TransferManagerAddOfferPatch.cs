using ColossalFramework;
using Harmony;
using System;

namespace EnhancedDistrictServices
{
    [HarmonyPatch(typeof(TransferManager))]
    [HarmonyPatch("AddIncomingOffer")]
    public class TransferManagerPatchAddIncomingOffer
    {
        public static bool Prefix(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)
        {
            // Logger.Log($"TransferManager::AddIncomingOffer: {TransferManagerInfo.ToString(ref offer, material)}!");

            // Increase the rate at which we can dispatch vehicles ...
            if (offer.Building != 0 && offer.Vehicle == 0)
            {
                if (material == TransferManager.TransferReason.Crime || material == TransferManager.TransferReason.Dead || material == TransferManager.TransferReason.Garbage || material == TransferManager.TransferReason.Sick)
                {
                    int capacity = GetVehicleCapacity(
                        offer.Building,
                        ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[offer.Building],
                        material);

                    int count = GetVehicleCount(
                        offer.Building,
                        ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[offer.Building],
                        material);

                    if (count <= capacity - 4)
                    {
                        offer.Amount = Math.Min(capacity - count, 2);
                        offer.Priority = 3;
                    }
                }
            }

            TransferManagerAddOffer.ModifyOffer(material, ref offer);
            return true;
        }

        private static int GetVehicleCapacity(ushort buildingID, ref Building _, TransferManager.TransferReason material)
        {
            if (material == TransferManager.TransferReason.Crime)
            {
                var buildingAI = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info.GetAI();
                if (buildingAI is PoliceStationAI policeStationAI)
                {
                    return policeStationAI.PoliceCarCount;
                }
            }

            if (material == TransferManager.TransferReason.Dead)
            {
                var buildingAI = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info.GetAI();
                if (buildingAI is CemeteryAI cemetaryAI)
                {
                    return cemetaryAI.m_hearseCount;
                }
            }

            if (material == TransferManager.TransferReason.Garbage)
            {
                var buildingAI = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info.GetAI();
                if (buildingAI is LandfillSiteAI landfillSiteAI)
                {
                    return landfillSiteAI.m_garbageTruckCount;
                }
            }

            if (material == TransferManager.TransferReason.Sick)
            {
                var buildingAI = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info.GetAI();
                if (buildingAI is HospitalAI hospitalAI)
                {
                    return hospitalAI.AmbulanceCount;
                }
            }

            return 0;
        }

        private static int GetVehicleCount(ushort _, ref Building data, TransferManager.TransferReason material)
        {
            int count = 0;

            VehicleManager instance = Singleton<VehicleManager>.instance;
            ushort vehicleID = data.m_ownVehicles;
            int num = 0;
            while ((int)vehicleID != 0)
            {
                if ((TransferManager.TransferReason)instance.m_vehicles.m_buffer[(int)vehicleID].m_transferType == material)
                {
                    count += 1;
                }

                vehicleID = instance.m_vehicles.m_buffer[(int)vehicleID].m_nextOwnVehicle;
                if (++num > 16384)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                    break;
                }
            }

            return count;
        }
    }

    [HarmonyPatch(typeof(TransferManager))]
    [HarmonyPatch("AddOutgoingOffer")]
    public class TransferManagerPatchAddOutgoingOffer
    {
        public static bool Prefix(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)        
        {
            // Logger.Log($"TransferManager::AddOutgoingOffer: {TransferManagerInfo.ToString(ref offer, material)}!");
            TransferManagerAddOffer.ModifyOffer(material, ref offer);
            return true;
        }
    }

    /// <summary>
    /// Helper class for altering the TransferManager Add*Offer methods.
    /// </summary>
    internal static class TransferManagerAddOffer
    {
        /// <summary>
        /// Sets the priority of outside connection offers to 0, while ensuring that local offers have priority 1 
        /// or greater.
        /// </summary>
        /// <remarks>
        /// We are a modifying the priority of offers as a way of prioritizing the local supply chain, and only 
        /// resorting to outside connections if materials cannot be found locally.
        /// </remarks>
        /// <param name="material"></param>
        /// <param name="offer"></param>
        public static void ModifyOffer(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)
        {
            if (TransferManagerInfo.IsDistrictOffer(material) || TransferManagerInfo.IsSupplyChainOffer(material))
            {
                var isOutsideOffer = TransferManagerInfo.IsOutsideOffer(ref offer);
                if (isOutsideOffer)
                {
                    offer.Priority = 0;
                }
                else
                {
                    offer.Priority = Math.Max(offer.Priority, 1);
                }

                if (offer.Vehicle != 0)
                {
                    offer.Priority = 7;
                }
            }
        }
    }
}
