// #define VERBOSE

using ColossalFramework;
using Harmony;
using System;
using System.Reflection;
using UnityEngine;

namespace EnhancedDistrictServices
{
    [HarmonyPatch(typeof(TransferManager))]
    [HarmonyPatch("AddIncomingOffer")]
    public class TransferManagerPatchAddIncomingOffer
    {
        public static bool Prefix(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)
        {
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

                    if (count <= capacity - 5)
                    {
                        offer.Amount = Math.Min(capacity - count, 5);
                        offer.Priority = 3;
                    }
                }
            }

            if (TransferManagerInfo.IsCustomOffer(material))
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

            // Logger.LogWarning($"TransferManager::AddIncomingOffer: Adding incoming offer {TransferManagerPatchHelper.ToString(ref offer, material)}!");
            return true;
        }

        private static int GetVehicleCapacity(ushort buildingID, ref Building data, TransferManager.TransferReason material)
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

            return 0;
        }

        private static int GetVehicleCount(ushort buildingID, ref Building data, TransferManager.TransferReason material)
        {
            int count = 0;

            VehicleManager instance = Singleton<VehicleManager>.instance;
            ushort vehicleID = data.m_ownVehicles;
            int num = 0;
            while ((int)vehicleID != 0)
            {
                if ((TransferManager.TransferReason)instance.m_vehicles.m_buffer[(int)vehicleID].m_transferType == material)
                {
                    count = count + 1;
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
            if (TransferManagerInfo.IsCustomOffer(material))
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

            // Logger.LogWarning($"TransferManager::AddOutgoingOffer: Adding outgoing offer {TransferManagerPatchHelper.ToString(ref offer, material)}!");
            return true;
        }
    }

    [HarmonyPatch(typeof(TransferManager))]
    [HarmonyPatch("MatchOffers")]
    public class TransferManagerPatchMatchOffers
    {
        private static readonly TransferManager.TransferOffer[] m_outgoingOffers;
        private static readonly TransferManager.TransferOffer[] m_incomingOffers;
        private static readonly ushort[] m_outgoingCount;
        private static readonly ushort[] m_incomingCount;
        private static readonly int[] m_outgoingAmount;
        private static readonly int[] m_incomingAmount;

        static TransferManagerPatchMatchOffers()
        {
            var instance = TransferManager.instance;

            var fi1 = typeof(TransferManager).GetField("m_outgoingOffers", BindingFlags.NonPublic | BindingFlags.Instance);
            m_outgoingOffers = (TransferManager.TransferOffer[])fi1.GetValue(instance);

            var fi2 = typeof(TransferManager).GetField("m_incomingOffers", BindingFlags.NonPublic | BindingFlags.Instance);
            m_incomingOffers = (TransferManager.TransferOffer[])fi2.GetValue(instance);

            var fi3 = typeof(TransferManager).GetField("m_outgoingCount", BindingFlags.NonPublic | BindingFlags.Instance);
            m_outgoingCount = (ushort[])fi3.GetValue(instance);

            var fi4 = typeof(TransferManager).GetField("m_incomingCount", BindingFlags.NonPublic | BindingFlags.Instance);
            m_incomingCount = (ushort[])fi4.GetValue(instance);

            var fi5 = typeof(TransferManager).GetField("m_outgoingAmount", BindingFlags.NonPublic | BindingFlags.Instance);
            m_outgoingAmount = (int[])fi5.GetValue(instance);

            var fi6 = typeof(TransferManager).GetField("m_incomingAmount", BindingFlags.NonPublic | BindingFlags.Instance);
            m_incomingAmount = (int[])fi6.GetValue(instance);
        }

        public static bool Prefix(TransferManager.TransferReason material)
        {
            try
            {
                if (material == TransferManager.TransferReason.None)
                {
                    return false;
                }

                if (TransferManagerInfo.IsDistrictOffer(material))
                {
                    MatchOffersClosest(material, requestCount: m_outgoingCount, requestOffers: m_outgoingOffers, responseCount: m_incomingCount, responseOffers: m_incomingOffers, isDistrictOffer: true, isSupplyChainOffer: false, verbose: false);
                    return false;
                }

                if (TransferManagerInfo.IsSupplyChainOffer(material))
                {
                    MatchOffersClosest(material, requestCount: m_incomingCount, requestOffers: m_incomingOffers, responseCount: m_outgoingCount, responseOffers: m_outgoingOffers, isDistrictOffer: true, isSupplyChainOffer: true, verbose: false);
                    return false;
                }

                if (TransferManagerInfo.IsExhaustiveOffer(material))
                {
                    MatchOffersClosest(material, requestCount: m_outgoingCount, requestOffers: m_outgoingOffers, responseCount: m_incomingCount, responseOffers: m_incomingOffers, isDistrictOffer: false, isSupplyChainOffer: false, verbose: false);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                Clear(material);
                return false;
            }

            // Run stock code
            return true;
        }

        /// <summary>
        /// Stock code copied from 1.20.0-f5 Campus update.
        /// </summary>
        private static void StartTransfer(TransferManager.TransferReason material, TransferManager.TransferOffer offerOut, TransferManager.TransferOffer offerIn, int delta)
        {
#if (VERBOSE)
            Logger.LogFormat($"TransferManager::StartTransfer: In:{ToString(ref offerIn, material)}, Out:{ToString(ref offerOut, material)}, Dist:{Vector3.Distance(offerIn.Position, offerOut.Position)}");
#endif

            bool active1 = offerIn.Active;
            bool active2 = offerOut.Active;
            if (active1 && offerIn.Vehicle != 0)
            {
                Array16<Vehicle> vehicles = Singleton<VehicleManager>.instance.m_vehicles;
                ushort vehicle = offerIn.Vehicle;
                VehicleInfo info = vehicles.m_buffer[vehicle].Info;
                offerOut.Amount = delta;
                info.m_vehicleAI.StartTransfer(vehicle, ref vehicles.m_buffer[vehicle], material, offerOut);
            }
            else if (active2 && offerOut.Vehicle != 0)
            {
                Array16<Vehicle> vehicles = Singleton<VehicleManager>.instance.m_vehicles;
                ushort vehicle = offerOut.Vehicle;
                VehicleInfo info = vehicles.m_buffer[vehicle].Info;
                offerIn.Amount = delta;
                info.m_vehicleAI.StartTransfer(vehicle, ref vehicles.m_buffer[vehicle], material, offerIn);
            }
            else if (active1 && (int)offerIn.Citizen != 0)
            {
                Array32<Citizen> citizens = Singleton<CitizenManager>.instance.m_citizens;
                uint citizen = offerIn.Citizen;
                CitizenInfo citizenInfo = citizens.m_buffer[citizen].GetCitizenInfo(citizen);
                if (citizenInfo == null)
                    return;
                offerOut.Amount = delta;
                citizenInfo.m_citizenAI.StartTransfer(citizen, ref citizens.m_buffer[citizen], material, offerOut);
            }
            else if (active2 && (int)offerOut.Citizen != 0)
            {
                Array32<Citizen> citizens = Singleton<CitizenManager>.instance.m_citizens;
                uint citizen = offerOut.Citizen;
                CitizenInfo citizenInfo = citizens.m_buffer[citizen].GetCitizenInfo(citizen);
                if (citizenInfo == null)
                    return;
                offerIn.Amount = delta;
                citizenInfo.m_citizenAI.StartTransfer(citizen, ref citizens.m_buffer[citizen], material, offerIn);
            }
            else if (active2 && offerOut.Building != 0)
            {
                Array16<Building> buildings = Singleton<BuildingManager>.instance.m_buildings;
                ushort building = offerOut.Building;
                BuildingInfo info = buildings.m_buffer[building].Info;
                offerIn.Amount = delta;
                info.m_buildingAI.StartTransfer(building, ref buildings.m_buffer[building], material, offerIn);
            }
            else
            {
                if (!active1 || offerIn.Building == 0)
                    return;
                Array16<Building> buildings = Singleton<BuildingManager>.instance.m_buildings;
                ushort building = offerIn.Building;
                BuildingInfo info = buildings.m_buffer[building].Info;
                offerOut.Amount = delta;
                info.m_buildingAI.StartTransfer(building, ref buildings.m_buffer[building], material, offerOut);
            }
        }

        #region Custom Code

        private static void MatchOffersClosest(TransferManager.TransferReason material, ushort[] requestCount, TransferManager.TransferOffer[] requestOffers, ushort[] responseCount, TransferManager.TransferOffer[] responseOffers, bool isDistrictOffer, bool isSupplyChainOffer, bool verbose)
        {
            if (material == TransferManager.TransferReason.None)
                return;

            // We already previously patched the offers so that priority >= 1 correspond to local offers and priority == 0 correspond to outside offers.
            for (int priorityOut = 7; priorityOut >= 0; --priorityOut)
            {
                int requestCountIndex = (int)material * 8 + priorityOut;
                int requestSubCount = requestCount[requestCountIndex];
                int requestSubIndex = 0;

                // pachang: Search request offers in decreasing priority only.  This is appropriate for services where
                //          the citizens are the ones calling for help.
                while (requestSubIndex < requestSubCount)
                {
                    TransferManager.TransferOffer requestOffer = requestOffers[requestCountIndex * 256 + requestSubIndex];
                    var requestDistrict = GetDistrict(ref requestOffer);

                    Vector3 requestPosition = requestOffer.Position;
                    int requestAmount = requestOffer.Amount;

                    if (verbose)
                    {
                        Logger.Log($"TransferManager::MatchOffersClosest: Matching offer for {TransferManagerPatchHelper.ToString(ref requestOffer, material)}!");
                    }

                    do
                    {
                        int bestPriorityIn = -1;
                        int bestResponseSubIndex = -1;
                        float bestDistanceSquared = float.MaxValue;

                        for (int priorityIn = 7; priorityIn >= 0; --priorityIn)
                        {
                            // Do not match to outside offer if we can match locally.
                            if (bestPriorityIn != -1 && priorityIn == 0)
                            {
                                break;
                            }

                            /*
                            // Do not match outside to outside offers, because it clogs up the cargo harbors.
                            if (priorityOut == 0 && priorityIn == 0)
                            {
                                break;
                            }
                            */

                            int responseCountIndex = (int)material * 8 + priorityIn;
                            int responseSubCount = responseCount[responseCountIndex];

                            // pachang: custom code below
                            for (int responseSubIndex = 0; responseSubIndex < responseSubCount; ++responseSubIndex)
                            {
                                TransferManager.TransferOffer responseOffer = responseOffers[responseCountIndex * 256 + responseSubIndex];

                                if (requestOffer.m_object == responseOffer.m_object)
                                {
                                    continue;
                                }

                                if (TransferManagerInfo.GetHomeBuilding(ref requestOffer) == TransferManagerInfo.GetHomeBuilding(ref responseOffer))
                                {
                                    continue;
                                }

                                if (verbose)
                                {
                                    Logger.Log($"TransferManager::MatchOffersClosest: Considering {TransferManagerPatchHelper.ToString(ref responseOffer, material)}!");
                                }

                                if (isDistrictOffer)
                                {
                                    if (IsValidDistrictOffer(requestDistrict, ref responseOffer))
                                    {
                                        // Awesome, from a district restrictions perspective, we can service the offer.
                                        // But also check for supply chain restrictions.
                                        if (isSupplyChainOffer && !IsValidBuildingOffer(ref responseOffer, ref requestOffer, allowForSpecifiedConnectionOnly: false))
                                        {
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        // Cannot service from a district restrictions perspective.
                                        // But can we still service it from a supply chain perspective?
                                        if (isSupplyChainOffer && IsValidBuildingOffer(ref responseOffer, ref requestOffer, allowForSpecifiedConnectionOnly: true))
                                        {
                                            // Great, allow the match.
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                }

                                var distanceSquared = Vector3.SqrMagnitude(responseOffer.Position - requestPosition);
                                var foundBetterMatch = false;

                                if (bestPriorityIn == -1)
                                {
                                    foundBetterMatch = true;
                                }

                                if (priorityIn == bestPriorityIn && distanceSquared < bestDistanceSquared)
                                {
                                    foundBetterMatch = true;
                                }

                                if (priorityIn < bestPriorityIn && distanceSquared < 0.75 * bestDistanceSquared)
                                {
                                    foundBetterMatch = true;
                                }

                                if (foundBetterMatch)
                                {
                                    bestPriorityIn = priorityIn;
                                    bestResponseSubIndex = responseSubIndex;
                                    bestDistanceSquared = distanceSquared;
                                }
                            }
                        }

                        if (bestPriorityIn != -1)
                        {
                            int incomingCountIndex = (int)material * 8 + bestPriorityIn;
                            var incomingOffer = responseOffers[incomingCountIndex * 256 + bestResponseSubIndex];
                            int incomingAmount = incomingOffer.Amount;

                            if (verbose)
                            {
                                Logger.Log($"TransferManager::MatchOffersClosest: Matched {TransferManagerPatchHelper.ToString(ref incomingOffer, material)}!");
                            }

                            int delta = Mathf.Min(requestAmount, incomingAmount);
                            if (delta != 0)
                            {
                                StartTransfer(material, requestOffer, incomingOffer, delta);
                            }                                

                            requestAmount -= delta;
                            incomingAmount -= delta;
                            if (incomingAmount == 0)
                            {
                                int incomingSubCount = responseCount[incomingCountIndex] - 1;
                                responseCount[incomingCountIndex] = (ushort)incomingSubCount;
                                responseOffers[incomingCountIndex * 256 + bestResponseSubIndex] = responseOffers[incomingCountIndex * 256 + incomingSubCount];
                            }
                            else
                            {
                                incomingOffer.Amount = incomingAmount;
                                responseOffers[incomingCountIndex * 256 + bestResponseSubIndex] = incomingOffer;
                            }

                            requestOffer.Amount = requestAmount;
                        }
                        else
                        {
                            // Don't warn if we could not satisfy an outside connection's offer
                            if (!TransferManagerInfo.IsOutsideOffer(ref requestOffer))
                            {
                                if (verbose || requestOffer.Priority > 2)
                                {
                                    Logger.LogWarning($"TransferManager::MatchOffersClosest: Could not service request offer {TransferManagerPatchHelper.ToString(ref requestOffer, material)}!");
                                }
                            }

                            break;
                        }
                    }
                    while (requestAmount != 0);

                    if (requestAmount == 0)
                    {
                        --requestSubCount;
                        requestCount[requestCountIndex] = (ushort)requestSubCount;
                        requestOffers[requestCountIndex * 256 + requestSubIndex] = requestOffers[requestCountIndex * 256 + requestSubCount];
                    }
                    else
                    {
                        requestOffer.Amount = requestAmount;
                        m_outgoingOffers[requestCountIndex * 256 + requestSubIndex] = requestOffer;
                        ++requestSubIndex;
                    }
                }
            }

            Clear(material);
        }

        private static void Clear(TransferManager.TransferReason material)
        {
            for (int index1 = 0; index1 < 8; ++index1)
            {
                int index2 = (int)material * 8 + index1;
                m_incomingCount[index2] = 0;
                m_outgoingCount[index2] = 0;
            }
            m_incomingAmount[(int)material] = 0;
            m_outgoingAmount[(int)material] = 0;
        }

        private static byte GetDistrict(ref TransferManager.TransferOffer offer)
        {
            if (offer.Building != 0)
            {
                var position = BuildingManager.instance.m_buildings.m_buffer[offer.Building].m_position;
                return DistrictManager.instance.GetDistrict(position);
            }
            else if (offer.Citizen != 0)
            {
                var homeBuilding = Singleton<CitizenManager>.instance.m_citizens.m_buffer[offer.Citizen].m_homeBuilding;
                if (homeBuilding != 0)
                {
                    var homeBuildingPosition = BuildingManager.instance.m_buildings.m_buffer[homeBuilding].m_position;
                    return DistrictManager.instance.GetDistrict(homeBuildingPosition);
                }
                else
                {
                    return 0;
                }
            }
            else if (offer.Vehicle != 0)
            {
                var homeBuilding = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[offer.Vehicle].m_sourceBuilding;
                if (homeBuilding != 0)
                {
                    var homeBuildingPosition = BuildingManager.instance.m_buildings.m_buffer[homeBuilding].m_position;
                    return DistrictManager.instance.GetDistrict(homeBuildingPosition);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        private static bool IsValidBuildingOffer(ref TransferManager.TransferOffer outgoingOffer, ref TransferManager.TransferOffer incomingOffer, bool allowForSpecifiedConnectionOnly)
        {
            var outgoingHomeBuilding = TransferManagerInfo.GetHomeBuilding(ref outgoingOffer);
            var incomingHomeBuilding = TransferManagerInfo.GetHomeBuilding(ref incomingOffer);

            if (outgoingHomeBuilding == 0 || incomingHomeBuilding == 0)
            {
                // Default to false ...
                return false;
            }

            var outgoingBuildingsServed = SupplyChainTable.BuildingToBuildingServiced[outgoingHomeBuilding];
            var incomingOfferRestricted = false;
            if (SupplyChainTable.IncomingOfferRestricted[incomingHomeBuilding]?.Count > 0)
            {
                incomingOfferRestricted = true;
            }

            bool AllowOutsideConnections(ushort homeBuilding)
            {
                if (!TransferManagerInfo.IsDistrictServicesBuilding(homeBuilding))
                {
                    return true;
                }

                if (DistrictServicesTable.BuildingToOutsideConnections[homeBuilding])
                {
                    return true;
                }

                return false;
            }

            if (!allowForSpecifiedConnectionOnly)
            {
                if (TransferManagerInfo.IsOutsideOffer(ref outgoingOffer))
                {
                    return AllowOutsideConnections(incomingHomeBuilding);
                }

                if (TransferManagerInfo.IsOutsideOffer(ref incomingOffer))
                {
                    return AllowOutsideConnections(outgoingHomeBuilding);
                }

                if (outgoingBuildingsServed == null && !incomingOfferRestricted)
                {
                    return true;
                }
            }

            if (outgoingBuildingsServed == null || !incomingOfferRestricted)
            {
                return false;
            }

            for (int i = 0; i < outgoingBuildingsServed.Count; i++)
            {
                if (outgoingBuildingsServed[i] == (int)incomingHomeBuilding)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidDistrictOffer(byte district, ref TransferManager.TransferOffer offer)
        {
            var homeBuilding = TransferManagerInfo.GetHomeBuilding(ref offer);

            // Logger.LogWarning($"TransferManager::IsValidDistrictOffer: District={district}, offer={TransferManagerPatchHelper.ToString(ref offer, TransferManager.TransferReason.None)}, homeBuilding={homeBuilding}!");

            if (homeBuilding == 0)
            {
                return true;
            }

            if (DistrictServicesTable.BuildingToAllLocalAreas[homeBuilding])
            {
                return true;
            }

            var districtsServed = DistrictServicesTable.BuildingToDistrictServiced[homeBuilding];
            if (districtsServed == null)
            {
                return false;
            }

            for (int i = 0; i < districtsServed.Count; i++)
            {
                if (districtsServed[i] == (int)district)
                {
                    return true;
                }
            }

            return false;
        }
#endregion
    }

    public static class TransferManagerPatchHelper
    {
        public static string ToString(ref TransferManager.TransferOffer offer, TransferManager.TransferReason material)
        {
            if (offer.Building != 0)
            {
                return $"Id=B{offer.Building}, (Amt,Mat,Pri,Exc)=({offer.Amount},{material},{offer.Priority},{offer.Exclude})";
            }

            if (offer.Citizen != 0)
            {
                var homeBuilding = Singleton<CitizenManager>.instance.m_citizens.m_buffer[offer.Citizen].m_homeBuilding;
                return $"Id=C{offer.Citizen}, Home=B{homeBuilding}, (Amt,Mat,Pri,Exc)=({offer.Amount},{material},{offer.Priority},{offer.Exclude})";
            }

            if (offer.Vehicle != 0)
            {
                return $"Id=V{offer.Vehicle}, (Amt,Mat,Pri,Exc)=({offer.Amount},{material},{offer.Priority},{offer.Exclude})";
            }

            return $"Id=0, (Amt,Mat,Pri,Exc)=({offer.Amount},{material},{offer.Priority},{offer.Exclude})";
        }
    }
}
