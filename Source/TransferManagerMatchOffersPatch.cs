﻿using ColossalFramework;
using Harmony;
using System;
using System.Reflection;
using UnityEngine;

namespace EnhancedDistrictServices
{
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

        /// <summary>
        /// Constructor.  Gets references to the array of incoming and outgoing offers from the TransferManager, so 
        /// that we can process these offers instead of having the game's TransferManager do so.
        /// </summary>
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
                    MatchOffersClosest(material, requestCount: m_outgoingCount, requestOffers: m_outgoingOffers, responseCount: m_incomingCount, responseOffers: m_incomingOffers, isSupplyChainOffer: false, verbose: false);
                    return false;
                }

                if (TransferManagerInfo.IsSupplyChainOffer(material))
                {
                    MatchOffersClosest(material, requestCount: m_incomingCount, requestOffers: m_incomingOffers, responseCount: m_outgoingCount, responseOffers: m_outgoingOffers, isSupplyChainOffer: true, verbose: false);
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
        /// Tries to match the request and response offers, where we do the search in decreasing priority.
        /// </summary>
        /// <param name="material"></param>
        /// <param name="requestCount"></param>
        /// <param name="requestOffers"></param>
        /// <param name="responseCount"></param>
        /// <param name="responseOffers"></param>
        /// <param name="isSupplyChainOffer"></param>
        /// <param name="verbose"></param>
        private static void MatchOffersClosest(TransferManager.TransferReason material, ushort[] requestCount, TransferManager.TransferOffer[] requestOffers, ushort[] responseCount, TransferManager.TransferOffer[] responseOffers, bool isSupplyChainOffer, bool verbose)
        {
            // We already previously patched the offers so that priority >= 1 correspond to local offers and priority == 0 correspond to outside offers.
            for (int priorityOut = 7; priorityOut >= 0; --priorityOut)
            {
                int requestCountIndex = (int)material * 8 + priorityOut;
                int requestSubCount = requestCount[requestCountIndex];
                int requestSubIndex = 0;

                // Search request offers in decreasing priority only.  This is appropriate for services where the citizens are the ones calling for help.
                while (requestSubIndex < requestSubCount)
                {
                    var requestOffer = requestOffers[requestCountIndex * 256 + requestSubIndex];
                    var requestPosition = requestOffer.Position;
                    int requestAmount = requestOffer.Amount;

                    if (verbose)
                    {
                        Logger.Log($"TransferManager::MatchOffersClosest: Matching offer for {ToString(ref requestOffer, material)}!");
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

                            for (int responseSubIndex = 0; responseSubIndex < responseSubCount; ++responseSubIndex)
                            {
                                TransferManager.TransferOffer responseOffer = responseOffers[responseCountIndex * 256 + responseSubIndex];
                                // Logger.Log($"TransferManager: request={ToString(ref requestOffer, material)}, response={ToString(ref responseOffer, material)}");

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
                                    Logger.Log($"TransferManager::MatchOffersClosest: Considering {ToString(ref responseOffer, material)}!");
                                }

                                if (!isSupplyChainOffer && !IsValidDistrictOffer(ref requestOffer, ref responseOffer))
                                {
                                    continue;
                                }

                                if (isSupplyChainOffer && !IsValidSupplyChainOffer(ref requestOffer, ref responseOffer))
                                {
                                    continue;
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

                                // Magic number 0.75.  Allow matching a lower priority offer only if it is substantially 
                                // closer.
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
                                Logger.Log($"TransferManager::MatchOffersClosest: Matched {ToString(ref incomingOffer, material)}!");
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
                                    Logger.LogWarning($"TransferManager::MatchOffersClosest: Could not service request offer {ToString(ref requestOffer, material)}!");
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

        /// <summary>
        /// Clears all offers, especially those offers that we could not match, for the given material.
        /// Make sure the offer arrays are in a good state when we are done processing these offers.
        /// </summary>
        /// <param name="material"></param>
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

        /// <summary>
        /// Returns true if we can potentially match the two given offers.
        /// </summary>
        /// <param name="requestOffer">i.e. offer by a student, residential, commerical building</param>
        /// <param name="responseOffer">i.e. offer by landfill, hospital, police</param>
        /// <returns></returns>
        private static bool IsValidDistrictOffer(ref TransferManager.TransferOffer requestOffer, ref TransferManager.TransferOffer responseOffer)
        {
            var responseBuilding = TransferManagerInfo.GetHomeBuilding(ref responseOffer);
            if (responseBuilding == 0 || !TransferManagerInfo.IsDistrictServicesBuilding(responseBuilding))
            {
                return true;
            }

            if (Constraints.AllLocalAreas(responseBuilding))
            {
                return true;
            }

            var requestBuilding = TransferManagerInfo.GetHomeBuilding(ref requestOffer);
            var requestDistrict = TransferManagerInfo.GetDistrict(requestBuilding);

            var responseDistrictsServed = Constraints.DistrictServiced(responseBuilding);
            for (int i = 0; i < responseDistrictsServed?.Count; i++)
            {
                if (responseDistrictsServed[i] == (int)requestDistrict)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if we can potentially match the two given offers.
        /// </summary>
        /// <param name="requestOffer">consumer of goods</param>
        /// <param name="responseOffer">producer of goods</param>
        /// <returns></returns>
        private static bool IsValidSupplyChainOffer(ref TransferManager.TransferOffer requestOffer, ref TransferManager.TransferOffer responseOffer)
        {
            var responseBuilding = TransferManagerInfo.GetHomeBuilding(ref responseOffer);
            if (responseBuilding == 0 || !TransferManagerInfo.IsDistrictServicesBuilding(responseBuilding))
            {
                return true;
            }

            // See if the request is from an outside connection ...
            var requestBuilding = TransferManagerInfo.GetHomeBuilding(ref requestOffer);
            var responseSupplyDestinations = Constraints.SupplyDestinations(responseBuilding);
            if (TransferManagerInfo.IsOutsideOffer(ref requestOffer))
            {
                if (Constraints.OutsideConnections(responseBuilding))
                {
                    return true;
                }
                else 
                {
                    // This method not advertised yet ... we can also allow the outside connection's building id if we 
                    // specify it in the supply chain out field.
                    if (responseSupplyDestinations?.Count > 0)
                    {
                        for (int i = 0; i < responseSupplyDestinations.Count; i++)
                        {
                            if (responseSupplyDestinations[i] == (int)requestBuilding)
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                }
            }

            // All requests are now guaranteed to be local.
            // Next see if all local areas are served.
            if (Constraints.AllLocalAreas(responseBuilding))
            {
                // Serve only if the request is not supply chain restricted.
                // Otherwise, we'll need to check below if any existing supply chain restriction is tied to the response building.
                if (Constraints.SupplySources(requestBuilding) == null || Constraints.SupplySources(requestBuilding).Count == 0)
                {
                    return true;
                }
            }

            // Now check supply chain restrictions.
            if (responseSupplyDestinations?.Count > 0)
            {
                for (int i = 0; i < responseSupplyDestinations.Count; i++)
                {
                    if (responseSupplyDestinations[i] == (int)requestBuilding)
                    {
                        return true;
                    }
                }
            }
            else // No supply chain restrictions, so now apply district restrictions.
            {
                var requestDistrict = TransferManagerInfo.GetDistrict(requestBuilding);

                var responseDistrictsServed = Constraints.DistrictServiced(responseBuilding);
                for (int i = 0; i < responseDistrictsServed?.Count; i++)
                {
                    if (responseDistrictsServed[i] == (int)requestDistrict)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Helper method for dumping the contents of an offer, for debugging purposes.
        /// </summary>
        /// <param name="offer"></param>
        /// <param name="material"></param>
        /// <returns></returns>
        private static string ToString(ref TransferManager.TransferOffer offer, TransferManager.TransferReason material)
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

        #region Stock Code

        /// <summary>
        /// Stock code that transfers people/materials between the buildings/vehicles referenced in the given offers.
        /// </summary>
        private static void StartTransfer(TransferManager.TransferReason material, TransferManager.TransferOffer offerOut, TransferManager.TransferOffer offerIn, int delta)
        {
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

        #endregion
    }
}
