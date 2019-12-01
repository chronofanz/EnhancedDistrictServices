using ColossalFramework;
using Harmony;
using System;
using System.Reflection;
using UnityEngine;

namespace EnhancedDistrictServices
{
    public static class TransferManagerMod
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
        static TransferManagerMod()
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

        /// <summary>
        /// An alternate take of the stock code in that we check for and replace an existing offer.
        /// </summary>
        /// <param name="material"></param>
        /// <param name="offer"></param>
        public static void AddIncomingOffer(TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            AddOffer(material, offer, m_incomingAmount, m_incomingCount, m_incomingOffers);
        }

        /// <summary>
        /// An alternate take of the stock code in that we check for and replace an existing offer.
        /// </summary>
        /// <param name="material"></param>
        /// <param name="offer"></param>
        public static void AddOutgoingOffer(TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            AddOffer(material, offer, m_outgoingAmount, m_outgoingCount, m_outgoingOffers);
        }

        /// <summary>
        /// Helper method for adding the given offer to the offers array.
        /// </summary>
        /// <param name="material"></param>
        /// <param name="offer"></param>
        /// <param name="amounts"></param>
        /// <param name="count"></param>
        /// <param name="offers"></param>
        private static void AddOffer(TransferManager.TransferReason material, TransferManager.TransferOffer offer, int[] amount, ushort[] count, TransferManager.TransferOffer[] offers)
        {
            for (int priority = offer.Priority; priority >= 0; --priority)
            {
                int index1 = (int)material * 8 + priority;
                int num = count[index1];

                for (int index2 = 0; index2 < num; index2++)
                {
                    int index3 = index1 * 256 + index2;
                    if (offers[index3].m_object == offer.m_object)
                    {
                        // Found an existing offer.
                        return;
                    }
                }

                // If we reached here, we need to add a new offer.
                if (num < 256)
                {
                    offers[index1 * 256 + num] = offer;
                    count[index1] = (ushort)(num + 1);
                    amount[(int)material] += offer.Amount;
                    return;
                }
            }
        }

        /// <summary>
        /// Matches all offers of the given material, if supported.  Returns true if this method did attempt to match 
        /// offers.
        /// </summary>
        /// <param name="material"></param>
        /// <returns></returns>
        public static bool MatchOffer(TransferManager.TransferReason material)
        {
            try
            {
                if (material == TransferManager.TransferReason.None)
                {
                    return true;
                }

                // Road maintenance is switched around ...
                if (material == TransferManager.TransferReason.RoadMaintenance)
                {
                    MatchOffersClosest(material, requestCount: m_incomingCount, requestOffers: m_incomingOffers, responseCount: m_outgoingCount, responseOffers: m_outgoingOffers, isSupplyChainOffer: false, verbose: false);
                    return true;
                }
                else if (TransferManagerInfo.IsDistrictOffer(material))
                {
                    MatchOffersClosest(material, requestCount: m_outgoingCount, requestOffers: m_outgoingOffers, responseCount: m_incomingCount, responseOffers: m_incomingOffers, isSupplyChainOffer: false, verbose: false);
                    return true;
                }
                else if (TransferManagerInfo.IsSupplyChainOffer(material))
                {
                    MatchOffersClosest(material, requestCount: m_incomingCount, requestOffers: m_incomingOffers, responseCount: m_outgoingCount, responseOffers: m_outgoingOffers, isSupplyChainOffer: true, verbose: false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                Clear(material);
                return true;
            }

            // Did not handle the material.
            return false;
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

                                Logger.LogVerbose(
                                    $"TransferManager::MatchOffersClosest: request={TransferManagerInfo.ToString(ref requestOffer, material)}, response={TransferManagerInfo.ToString(ref responseOffer, material)}", 
                                    verbose);

                                if (requestOffer.m_object == responseOffer.m_object)
                                {
                                    continue;
                                }

                                if (TransferManagerInfo.GetHomeBuilding(ref requestOffer) == TransferManagerInfo.GetHomeBuilding(ref responseOffer))
                                {
                                    continue;
                                }

                                if (!isSupplyChainOffer && !IsValidDistrictOffer(material, ref requestOffer, ref responseOffer, verbose))
                                {
                                    continue;
                                }

                                if (isSupplyChainOffer && !IsValidSupplyChainOffer(material, ref requestOffer, ref responseOffer, verbose))
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

                            Logger.LogVerbose(
                                $"TransferManager::MatchOffersClosest: Matched {TransferManagerInfo.ToString(ref incomingOffer, material)}!",
                                verbose);

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
                                Logger.LogVerbose(
                                    $"TransferManager::MatchOffersClosest: Could not service request offer {TransferManagerInfo.ToString(ref requestOffer, material)}!",
                                    verbose);
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
        /// <param name="material"></param>
        /// <param name="requestOffer">i.e. offer by a student, residential, commerical building</param>
        /// <param name="responseOffer">i.e. offer by landfill, hospital, police</param>
        /// <returns></returns>
        private static bool IsValidDistrictOffer(TransferManager.TransferReason material, ref TransferManager.TransferOffer requestOffer, ref TransferManager.TransferOffer responseOffer, bool verbose)
        {
            var responseBuilding = TransferManagerInfo.GetHomeBuilding(ref responseOffer);
            if (responseBuilding == 0)
            {
                Logger.LogVerbose(
                    "TransferManager::IsValidDistrictOffer: Not a district services building",
                    verbose);
                return false;
            }

            if (Constraints.AllLocalAreas(responseBuilding))
            {
                Logger.LogVerbose(
                    "TransferManager::IsValidDistrictOffer: Serves all local areas",
                    verbose);
                return true;
            }

            // The call to TransferManagerInfo.GetDistrict applies to offers that are come from buildings, service 
            // vehicles, citizens, AND netSegments.  The latter needs to be considered for road maintenance.
            var requestDistrict = TransferManagerInfo.GetDistrict(material, ref requestOffer);
            var responseDistrictsServed = Constraints.DistrictServiced(responseBuilding);
            for (int i = 0; i < responseDistrictsServed?.Count; i++)
            {
                if (responseDistrictsServed[i] == (int)requestDistrict)
                {
                    Logger.LogVerbose(
                        $"TransferManager::IsValidDistrictOffer: Matched district {requestDistrict}",
                        verbose);
                    return true;
                }
            }

            Logger.LogVerbose(
                $"TransferManager::IsValidDistrictOffer: Not valid",
                verbose);
            return false;
        }

        /// <summary>
        /// Returns true if we can potentially match the two given offers.
        /// </summary>
        /// <param name="material"></param>
        /// <param name="requestOffer">consumer of goods</param>
        /// <param name="responseOffer">producer of goods</param>
        /// <returns></returns>
        private static bool IsValidSupplyChainOffer(TransferManager.TransferReason material, ref TransferManager.TransferOffer requestOffer, ref TransferManager.TransferOffer responseOffer, bool verbose)
        {
            var responseBuilding = TransferManagerInfo.GetHomeBuilding(ref responseOffer);
            if (responseBuilding == 0)
            {
                Logger.LogVerbose(
                    "TransferManager::IsValidSupplyChainOffer: Not a district services building",
                    verbose);
                return false;
            }

            // See if the request is from an outside connection ...
            var requestBuilding = TransferManagerInfo.GetHomeBuilding(ref requestOffer);
            var responseSupplyDestinations = Constraints.SupplyDestinations(responseBuilding);
            if (TransferManagerInfo.IsOutsideOffer(ref requestOffer))
            {
                if (Constraints.OutsideConnections(responseBuilding))
                {
                    Logger.LogVerbose(
                        "TransferManager::IsValidSupplyChainOffer: Matched local to outside offer",
                        verbose && !TransferManagerInfo.IsOutsideOffer(ref responseOffer));
                    Logger.LogVerbose(
                        "TransferManager::IsValidSupplyChainOffer: Matched outside to outside offer",
                        verbose && TransferManagerInfo.IsOutsideOffer(ref responseOffer));
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
                                Logger.LogVerbose(
                                    "TransferManager::IsValidSupplyChainOffer: Matched outside to outside offer",
                                    verbose);
                                return true;
                            }
                        }
                    }

                    Logger.LogVerbose(
                        "TransferManager::IsValidSupplyChainOffer: Disallowed outside offer",
                        verbose);
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
                    Logger.LogVerbose(
                        "TransferManager::IsValidSupplyChainOffer: Serves all local areas",
                        verbose);
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
                        Logger.LogVerbose(
                            "TransferManager::IsValidSupplyChainOffer: Supply link allowed",
                            verbose);
                        return true;
                    }
                }
            }
            else if (Constraints.SupplySources(requestBuilding) != null && Constraints.SupplySources(requestBuilding).Count > 0)
            {
                Logger.LogVerbose(
                    "TransferManager::IsValidSupplyChainOffer: Supply link disallowed",
                    verbose);
                return false;
            }
            else // No supply chain restrictions, so now apply district restrictions.
            {
                var requestDistrict = TransferManagerInfo.GetDistrict(requestBuilding);
                var responseDistrictsServed = Constraints.DistrictServiced(responseBuilding);
                for (int i = 0; i < responseDistrictsServed?.Count; i++)
                {
                    if (responseDistrictsServed[i] == (int)requestDistrict)
                    {
                        Logger.LogVerbose(
                            $"TransferManager::IsValidSupplyChainOffer: Matched district {requestDistrict}",
                            verbose);
                        return true;
                    }
                }
            }

            Logger.LogVerbose(
                $"TransferManager::IsValidSupplyChainOffer: Not valid",
                verbose);
            return false;
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
