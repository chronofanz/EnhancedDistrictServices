using ColossalFramework;
using HarmonyLib;
using UnityEngine;

namespace EnhancedDistrictServices
{
    [HarmonyPatch(typeof(TransferManager))]
    [HarmonyPatch(nameof(TransferManager.AddIncomingOffer))]
    public class TransferManagerAddIncomingOfferPatch
    {
        public static bool Prefix(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)
        {
            // Inactive outside connections should not be adding offers ...
            if (OutsideConnectionInfo.IsInvalidIncomingOutsideConnection(offer.Building))
            {
                OfferTracker.LogEvent("AddIncomingDisallowInactiveOutside", ref offer, material);
                return false;
            }

            if (!(TransferManagerInfo.IsDistrictOffer(material) || TransferManagerInfo.IsSupplyChainOffer(material)))
            {
                // Fix for certain assets that have sub buildings that should not be making offers ...
                if (offer.Building != 0 && BuildingManager.instance.m_buildings.m_buffer[offer.Building].m_parentBuilding != 0)
                {
                    if (material == TransferManager.TransferReason.ParkMaintenance)
                    {
                        OfferTracker.LogEvent("AddIncomingDisallowSubBuilding", ref offer, material);
                        return false;
                    }
                }
            }

            TransferManagerAddOffer.ModifyOffer(material, ref offer);

            // Stock Code, 1.17.1-f4
            if (offer.Building != 0)
            {
                byte park = Singleton<DistrictManager>.instance.GetPark(offer.Position);
                Building[] buffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                if (park != 0 && Singleton<DistrictManager>.instance.m_parks.m_buffer[park].IsPedestrianZone && buffer[offer.Building].Info.m_buildingAI.GetUseServicePoint(offer.Building, ref buffer[offer.Building]) && DistrictPark.TryGetPedestrianReason(material, out var reason))
                {
                    bool flag = false;
                    if ((Singleton<DistrictManager>.instance.m_parks.m_buffer[park].m_parkPolicies & DistrictPolicies.Park.ForceServicePoint) != 0)
                    {
                        flag = true;
                    }

                    if (!flag)
                    {
                        ushort accessSegment = buffer[offer.Building].m_accessSegment;
                        if (accessSegment == 0 && (buffer[offer.Building].m_problems & new Notification.ProblemStruct(Notification.Problem1.RoadNotConnected, Notification.Problem2.NotInPedestrianZone)).IsNone)
                        {
                            buffer[offer.Building].Info.m_buildingAI.CheckRoadAccess(offer.Building, ref buffer[offer.Building]);
                            accessSegment = buffer[offer.Building].m_accessSegment;
                        }

                        if (accessSegment != 0 && (Singleton<NetManager>.instance.m_segments.m_buffer[accessSegment].Info.m_vehicleCategories & reason.m_vehicleCategory) == 0)
                        {
                            flag = true;
                        }
                    }

                    if (flag)
                    {
                        offer.m_isLocalPark = park;
                        Singleton<DistrictManager>.instance.m_parks.m_buffer[park].AddMaterialRequest(offer.Building, material);
                    }
                }
            }

            TransferManagerMod.AddIncomingOffer(material, offer);
            return false;
        }
    }

    [HarmonyPatch(typeof(TransferManager))]
    [HarmonyPatch(nameof(TransferManager.AddOutgoingOffer))]
    public class TransferManagerAddOutgoingOfferPatch
    {
        private static readonly MyRandomizer m_randomizer = new MyRandomizer(1);

        public static bool Prefix(ref TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)        
        {
            // Inactive outside connections should not be adding offers ...
            if (OutsideConnectionInfo.IsInvalidOutgoingOutsideConnection(offer.Building))
            {
                OfferTracker.LogEvent("AddOutgoingDisallowInactiveOutside", ref offer, material);
                return false;
            }

            // Too many requests for helicopters ... 
            if (material == TransferManager.TransferReason.Sick2)
            {
                if (m_randomizer.Int32(10U) != 0)
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
                        OfferTracker.LogEvent("AddOutgoingDisallowSubBuilding", ref offer, material);
                        return false;
                    }
                }
            }

            TransferManagerAddOffer.ModifyOffer(material, ref offer);

            // Stock Code, 1.17.1-f4
            if (offer.Building != 0)
            {
                byte park = Singleton<DistrictManager>.instance.GetPark(offer.Position);
                Building[] buffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                if (park != 0 && Singleton<DistrictManager>.instance.m_parks.m_buffer[park].IsPedestrianZone && buffer[offer.Building].Info.m_buildingAI.GetUseServicePoint(offer.Building, ref buffer[offer.Building]) && DistrictPark.TryGetPedestrianReason(material, out var reason))
                {
                    bool flag = false;
                    if ((Singleton<DistrictManager>.instance.m_parks.m_buffer[park].m_parkPolicies & DistrictPolicies.Park.ForceServicePoint) != 0)
                    {
                        flag = true;
                    }

                    if (!flag)
                    {
                        ushort accessSegment = buffer[offer.Building].m_accessSegment;
                        if (accessSegment == 0 && (buffer[offer.Building].m_problems & new Notification.ProblemStruct(Notification.Problem1.RoadNotConnected, Notification.Problem2.NotInPedestrianZone)).IsNone)
                        {
                            buffer[offer.Building].Info.m_buildingAI.CheckRoadAccess(offer.Building, ref buffer[offer.Building]);
                            accessSegment = buffer[offer.Building].m_accessSegment;
                        }

                        if (accessSegment != 0 && (Singleton<NetManager>.instance.m_segments.m_buffer[accessSegment].Info.m_vehicleCategories & reason.m_vehicleCategory) == 0)
                        {
                            flag = true;
                        }
                    }

                    if (flag)
                    {
                        offer.m_isLocalPark = park;
                        Singleton<DistrictManager>.instance.m_parks.m_buffer[park].AddMaterialSuggestion(offer.Building, material);
                    }
                }
            }

            TransferManagerMod.AddOutgoingOffer(material, offer);
            return false;
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
            var isOutsideOffer = TransferManagerInfo.IsOutsideOffer(ref offer, material);
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
                offer.Priority = 6;
            }
        }
    }
}
