using ColossalFramework;
using HarmonyLib;
using UnityEngine;

namespace EnhancedDistrictServices
{
    [HarmonyPatch(typeof(TransferManager))]
    [HarmonyPatch("AddIncomingOffer")]
    public class TransferManagerAddIncomingOfferPatch
    {
        public static bool Prefix(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)
        {
            Logger.LogMaterial($"TransferManager::AddIncomingOffer: {Utils.ToString(ref offer, material)}!", material);

            // Inactive outside connections should not be adding offers ...
            if (OutsideConnectionInfo.IsInvalidIncomingOutsideConnection(offer.Building))
            {
                Logger.LogMaterial($"TransferManager::AddIncomingOffer: Disallowing outside connection B{offer.Building} because of missing cargo buildings!", material);
                return false;
            }

            if (!(TransferManagerInfo.IsDistrictOffer(material) || TransferManagerInfo.IsSupplyChainOffer(material)))
            {
                // Fix for certain assets that have sub buildings that should not be making offers ...
                if (offer.Building != 0 && BuildingManager.instance.m_buildings.m_buffer[offer.Building].m_parentBuilding != 0)
                {
                    if (material == TransferManager.TransferReason.ParkMaintenance)
                    {
                        Logger.LogMaterial($"TransferManager::AddIncomingOffer: Filtering out subBuilding {Utils.ToString(ref offer, material)}!", material);
                        return false;
                    }
                }

                return true;
            }

            if (material == TransferManager.TransferReason.Taxi && offer.Citizen != 0)
            {
                var instance = CitizenManager.instance.m_citizens.m_buffer[offer.Citizen].m_instance;
                var targetBuilding = CitizenManager.instance.m_instances.m_buffer[instance].m_targetBuilding;
                var targetPosition = BuildingManager.instance.m_buildings.m_buffer[targetBuilding].m_position;

                if (!TaxiMod.CanUseTaxis(offer.Position, targetPosition))
                {
                    Logger.LogMaterial($"TransferManager::AddIncomingOffer: Filtering out {Utils.ToString(ref offer, material)}!", material);
                    var instanceId = CitizenManager.instance.m_citizens.m_buffer[offer.Citizen].m_instance;
                    CitizenManager.instance.m_instances.m_buffer[instanceId].m_flags &= ~CitizenInstance.Flags.WaitingTaxi;
                    CitizenManager.instance.m_instances.m_buffer[instanceId].m_flags |= CitizenInstance.Flags.BoredOfWaiting;
                    CitizenManager.instance.m_instances.m_buffer[instanceId].m_flags |= CitizenInstance.Flags.CannotUseTaxi;
                    CitizenManager.instance.m_instances.m_buffer[instanceId].m_waitCounter = byte.MaxValue;
                    return false;
                }
            }

            TransferManagerAddOffer.ModifyOffer(material, ref offer);
            return true;
        }
    }

    [HarmonyPatch(typeof(TransferManager))]
    [HarmonyPatch("AddOutgoingOffer")]
    public class TransferManagerAddOutgoingOfferPatch
    {
        public static bool Prefix(ref TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)        
        {
            Logger.LogMaterial($"TransferManager::AddOutgoingOffer: {Utils.ToString(ref offer, material)}!", material);

            // Inactive outside connections should not be adding offers ...
            if (OutsideConnectionInfo.IsInvalidOutgoingOutsideConnection(offer.Building))
            {
                Logger.LogMaterial($"TransferManager::AddOutgoingOffer: Disallowing outside connection B{offer.Building} because of missing cargo buildings!", material);
                return false;
            }

            // Change these offers ... a bug in the base game.  Citizens should not offer health care services.
            if ((material == TransferManager.TransferReason.ElderCare || material == TransferManager.TransferReason.ChildCare) && offer.Citizen != 0)
            {
                offer.Active = true;
                TransferManager.instance.AddIncomingOffer(material, offer);
                return false;
            }

            // Too many requests for helicopters ... 
            if (material == TransferManager.TransferReason.Sick2)
            {
                if (Singleton<SimulationManager>.instance.m_randomizer.Int32(10U) != 0)
                {
                    material = TransferManager.TransferReason.Sick;
                }                
            }

            if (!(TransferManagerInfo.IsDistrictOffer(material) || TransferManagerInfo.IsSupplyChainOffer(material)))
            {
                // Fix for certain assets that have sub buildings that should not be making offers ...
                if (offer.Building != 0 && BuildingManager.instance.m_buildings.m_buffer[offer.Building].m_parentBuilding != 0)
                {
                    if (material == TransferManager.TransferReason.ParkMaintenance)
                    {
                        Logger.LogMaterial($"TransferManager::AddOutgoingOffer: Filtering out subBuilding {Utils.ToString(ref offer, material)}!", material);
                        return false;
                    }
                }

                return true;
            }

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
            var isOutsideOffer = TransferManagerInfo.IsOutsideOffer(ref offer);
            if (isOutsideOffer)
            {
                offer.Priority = 0;
            }
            else
            {
                offer.Priority = Mathf.Clamp(offer.Priority + 1, 1, 7);
            }

            if (offer.Vehicle != 0)
            {
                offer.Priority = 7;
            }
        }
    }
}
