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
            if (!(TransferManagerInfo.IsDistrictOffer(material) || TransferManagerInfo.IsSupplyChainOffer(material)))
            {
                return true;
            }

            Logger.LogVerbose($"TransferManager::AddIncomingOffer: {Utils.ToString(ref offer, material)}!");
            TransferManagerAddOffer.ModifyOffer(material, ref offer);
            return true;
        }
    }

    [HarmonyPatch(typeof(TransferManager))]
    [HarmonyPatch("AddOutgoingOffer")]
    public class TransferManagerPatchAddOutgoingOffer
    {
        public static bool Prefix(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)        
        {
            if (!(TransferManagerInfo.IsDistrictOffer(material) || TransferManagerInfo.IsSupplyChainOffer(material)))
            {
                return true;
            }

            Logger.LogVerbose($"TransferManager::AddOutgoingOffer: {Utils.ToString(ref offer, material)}!");
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
                offer.Priority = Math.Max(offer.Priority, 1);
            }

            if (offer.Vehicle != 0)
            {
                offer.Priority = 7;
            }
        }
    }
}
