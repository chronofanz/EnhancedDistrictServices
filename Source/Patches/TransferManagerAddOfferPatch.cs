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

            if (material == TransferManager.TransferReason.Taxi && offer.Citizen != 0)
            {
                var instance = CitizenManager.instance.m_citizens.m_buffer[offer.Citizen].m_instance;
                var targetBuilding = CitizenManager.instance.m_instances.m_buffer[instance].m_targetBuilding;
                var targetPosition = BuildingManager.instance.m_buildings.m_buffer[targetBuilding].m_position;

                if (!TaxiMod.CanUseTaxis(offer.Position, targetPosition))
                {
                    OfferTracker.LogEvent("AddIncomingDisallowTaxis", ref offer, material);
                    var instanceId = CitizenManager.instance.m_citizens.m_buffer[offer.Citizen].m_instance;
                    CitizenManager.instance.m_instances.m_buffer[instanceId].m_flags &= ~CitizenInstance.Flags.WaitingTaxi;
                    CitizenManager.instance.m_instances.m_buffer[instanceId].m_flags |= CitizenInstance.Flags.BoredOfWaiting;
                    CitizenManager.instance.m_instances.m_buffer[instanceId].m_flags |= CitizenInstance.Flags.CannotUseTaxi;
                    CitizenManager.instance.m_instances.m_buffer[instanceId].m_waitCounter = byte.MaxValue;
                    return false;
                }
            }

            TransferManagerAddOffer.ModifyOffer(material, ref offer);

            if (offer.Building != (ushort)0)
            {
                byte park = Singleton<DistrictManager>.instance.GetPark(offer.Position);
                Building[] buffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                DistrictPark.PedestrianZoneTransferReason reason;
                if (park != (byte)0 && Singleton<DistrictManager>.instance.m_parks.m_buffer[(int)park].IsPedestrianZone && buffer[(int)offer.Building].Info.m_buildingAI.GetUseServicePoint(offer.Building, ref buffer[(int)offer.Building]) && DistrictPark.TryGetPedestrianReason(material, out reason))
                {
                    bool flag = false;
                    if ((Singleton<DistrictManager>.instance.m_parks.m_buffer[(int)park].m_parkPolicies & DistrictPolicies.Park.ForceServicePoint) != DistrictPolicies.Park.None)
                        flag = true;
                    if (!flag)
                    {
                        ushort accessSegment = buffer[(int)offer.Building].m_accessSegment;
                        if (accessSegment == (ushort)0 && (buffer[(int)offer.Building].m_problems & new Notification.ProblemStruct(Notification.Problem1.RoadNotConnected, Notification.Problem2.NotInPedestrianZone)).IsNone)
                        {
                            buffer[(int)offer.Building].Info.m_buildingAI.CheckRoadAccess(offer.Building, ref buffer[(int)offer.Building]);
                            accessSegment = buffer[(int)offer.Building].m_accessSegment;
                        }
                        if (accessSegment != (ushort)0 && (Singleton<NetManager>.instance.m_segments.m_buffer[(int)accessSegment].Info.m_vehicleCategories & reason.m_vehicleCategory) == VehicleInfo.VehicleCategory.None)
                            flag = true;
                    }
                    if (flag)
                    {
                        offer.m_isLocalPark = park;
                        Singleton<DistrictManager>.instance.m_parks.m_buffer[(int)park].AddMaterialRequest(offer.Building, material);
                    }
                }
            }

            TransferManagerMod.AddIncomingOffer(material, offer);
            return false;
        }
    }

    [HarmonyPatch(typeof(TransferManager))]
    [HarmonyPatch("AddOutgoingOffer")]
    public class TransferManagerAddOutgoingOfferPatch
    {
        private readonly static MyRandomizer m_randomizer = new MyRandomizer(1);

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

            if (offer.Building != (ushort)0)
            {
                byte park = Singleton<DistrictManager>.instance.GetPark(offer.Position);
                Building[] buffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                DistrictPark.PedestrianZoneTransferReason reason;
                if (park != (byte)0 && Singleton<DistrictManager>.instance.m_parks.m_buffer[(int)park].IsPedestrianZone && buffer[(int)offer.Building].Info.m_buildingAI.GetUseServicePoint(offer.Building, ref buffer[(int)offer.Building]) && DistrictPark.TryGetPedestrianReason(material, out reason))
                {
                    bool flag = false;
                    if ((Singleton<DistrictManager>.instance.m_parks.m_buffer[(int)park].m_parkPolicies & DistrictPolicies.Park.ForceServicePoint) != DistrictPolicies.Park.None)
                        flag = true;
                    if (!flag)
                    {
                        ushort accessSegment = buffer[(int)offer.Building].m_accessSegment;
                        if (accessSegment == (ushort)0 && (buffer[(int)offer.Building].m_problems & new Notification.ProblemStruct(Notification.Problem1.RoadNotConnected, Notification.Problem2.NotInPedestrianZone)).IsNone)
                        {
                            buffer[(int)offer.Building].Info.m_buildingAI.CheckRoadAccess(offer.Building, ref buffer[(int)offer.Building]);
                            accessSegment = buffer[(int)offer.Building].m_accessSegment;
                        }
                        if (accessSegment != (ushort)0 && (Singleton<NetManager>.instance.m_segments.m_buffer[(int)accessSegment].Info.m_vehicleCategories & reason.m_vehicleCategory) == VehicleInfo.VehicleCategory.None)
                            flag = true;
                    }
                    if (flag)
                    {
                        offer.m_isLocalPark = park;
                        Singleton<DistrictManager>.instance.m_parks.m_buffer[(int)park].AddMaterialSuggestion(offer.Building, material);
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
