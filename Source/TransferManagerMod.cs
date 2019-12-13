﻿using ColossalFramework;
using ColossalFramework.Math;
using System;
using System.Reflection;
using UnityEngine;

namespace EnhancedDistrictServices
{
    public static class TransferManagerMod
    {
        private static readonly Randomizer m_randomizer = new Randomizer(0);

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

        public delegate bool MatchFilter(
            TransferManager.TransferReason material,
            ref TransferManager.TransferOffer requestOffer, int requestPriority,
            ref TransferManager.TransferOffer responseOffer, int responsePriority,
            int randomMax);

        public delegate bool PreFilterRequest(
            TransferManager.TransferReason material,
            ref TransferManager.TransferOffer requestOffer, int requestPriority);

        /// <summary>
        /// Matches all offers of the given material, if supported.  Returns true if this method did attempt to match 
        /// offers.
        /// </summary>
        /// <param name="material"></param>
        /// <returns></returns>
        public static bool MatchOffers(TransferManager.TransferReason material)
        {
            try
            {
                if (material == TransferManager.TransferReason.None)
                {
                    return true;
                }

                // Road maintenance is switched around ...
                if (material == TransferManager.TransferReason.RoadMaintenance || material == TransferManager.TransferReason.Taxi)
                {
                    MatchOffersClosest(
                        material, 
                        requestCount: m_incomingCount, requestOffers: m_incomingOffers,
                        requestPriorityMax: 7, requestPriorityMin: 1,
                        responseCount: m_outgoingCount, responseOffers: m_outgoingOffers,
                        responsePriorityMax: 7, responsePriorityMin: 1,
                        matchFilter: IsValidDistrictOffer);

                    Clear(material);
                    return true;
                }
                else if (TransferManagerInfo.IsDistrictOffer(material))
                {
                    MatchOffersClosest(
                        material, 
                        requestCount: m_outgoingCount, requestOffers: m_outgoingOffers,
                        requestPriorityMax: 7, requestPriorityMin: 1,
                        responseCount: m_incomingCount, responseOffers: m_incomingOffers,
                        responsePriorityMax: 7, responsePriorityMin: 1,
                        matchFilter: IsValidDistrictOffer);

                    Clear(material);
                    return true;
                }
                else if (TransferManagerInfo.IsSupplyChainOffer(material))
                {
                    // First try to match using supply chain rules.
                    MatchOffersClosest(
                        material,
                        requestCount: m_incomingCount, requestOffers: m_incomingOffers,
                        requestPriorityMax: 7, requestPriorityMin: 1,
                        responseCount: m_outgoingCount, responseOffers: m_outgoingOffers,
                        responsePriorityMax: 7, responsePriorityMin: 1,
                        matchFilter: IsValidSupplyChainOffer);

                    var globalOutsideConnectionIntensity = Constraints.GlobalOutsideConnectionIntensity();
                    MatchOffersClosest(
                        material,
                        requestCount: m_incomingCount, requestOffers: m_incomingOffers,
                        requestPriorityMax: 7, requestPriorityMin: 1,
                        responseCount: m_outgoingCount, responseOffers: m_outgoingOffers,
                        responsePriorityMax: 7, responsePriorityMin: 0,
                        matchFilter: IsValidLowPriorityOffer,
                        preFilterRequest: PreFilterLowPriorityOffer,
                        maxMatchesOutside: globalOutsideConnectionIntensity);

                    // Now finally try and match to outside offers, as well as match using extra supply.
                    MatchOffersClosest(
                        material,
                        requestCount: m_incomingCount, requestOffers: m_incomingOffers,
                        requestPriorityMax: 0, requestPriorityMin: 0,
                        responseCount: m_outgoingCount, responseOffers: m_outgoingOffers,
                        responsePriorityMax: 7, responsePriorityMin: 0,
                        matchFilter: IsValidLowPriorityOffer,
                        preFilterRequest: PreFilterLowPriorityOffer,
                        maxMatchesOutside: globalOutsideConnectionIntensity);

                    Clear(material);
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
        private static void MatchOffersClosest(
            TransferManager.TransferReason material, 
            ushort[] requestCount, TransferManager.TransferOffer[] requestOffers, 
            int requestPriorityMax, int requestPriorityMin,
            ushort[] responseCount, TransferManager.TransferOffer[] responseOffers,
            int responsePriorityMax, int responsePriorityMin,
            MatchFilter matchFilter, PreFilterRequest preFilterRequest = null, int maxMatchesOutside = int.MaxValue)
        {
            int matchesMissed = 0;
            int matchesOutside = 0;
            int matchesInside = 0;

            // We already previously patched the offers so that priority >= 1 correspond to local offers and priority == 0 correspond to outside offers.
            for (int requestPriority = requestPriorityMax; requestPriority >= requestPriorityMin; --requestPriority)
            {
                int requestCountIndex = (int)material * 8 + requestPriority;
                int requestSubCount = requestCount[requestCountIndex];

                // Start searching at a random index!
                int requestSubIndex = m_randomizer.Int32(0, requestSubCount - 1);

                // Search request offers in decreasing priority only.  This is appropriate for services where the citizens are the ones calling for help.
                bool matched = false;
                for (int iter = 0; iter < requestSubCount; iter++)
                {
                    requestSubIndex++;
                    if (requestSubIndex >= requestSubCount)
                    {
                        requestSubIndex = 0;
                    }

                    var requestOffer = requestOffers[requestCountIndex * 256 + requestSubIndex];
                    var requestPosition = requestOffer.Position;
                    int requestAmount = requestOffer.Amount;

                    if (preFilterRequest != null && preFilterRequest(material, ref requestOffer, requestPriority))
                    {
                        continue;
                    }

                    if (requestAmount == 0)
                    {
                        continue;
                    }

                    Logger.LogMaterial(
                        $"TransferManager::MatchOffersClosest: Searching for match for request={Utils.ToString(ref requestOffer, material)}",
                        material);

                    do
                    {
                        int bestResponsePriority = -1;
                        int bestResponseSubIndex = -1;
                        float bestDistanceSquared = float.MaxValue;

                        for (int responsePriority = responsePriorityMax; responsePriority >= responsePriorityMin; --responsePriority)
                        {
                            if (matchesOutside >= maxMatchesOutside && (requestPriority == 0 || responsePriority == 0))
                            {
                                break;
                            }

                            // Do not match to outside offer if we can match locally.
                            if (bestResponsePriority != -1 && responsePriority == 0)
                            {
                                break;
                            }

                            int responseCountIndex = (int)material * 8 + responsePriority;
                            int responseSubCount = responseCount[responseCountIndex];

                            for (int responseSubIndex = 0; responseSubIndex < responseSubCount; ++responseSubIndex)
                            {
                                var responseOffer = responseOffers[responseCountIndex * 256 + responseSubIndex];

                                if (requestOffer.m_object == responseOffer.m_object)
                                {
                                    continue;
                                }

                                if (TransferManagerInfo.GetHomeBuilding(ref requestOffer) == TransferManagerInfo.GetHomeBuilding(ref responseOffer))
                                {
                                    continue;
                                }

                                if (!matchFilter(
                                    material,
                                    ref requestOffer, requestPriority,
                                    ref responseOffer, responsePriority,
                                    maxMatchesOutside))
                                {
                                    continue;
                                }

                                var distanceSquared = Vector3.SqrMagnitude(responseOffer.Position - requestPosition);
                                var foundBetterMatch = false;

                                if (bestResponsePriority == -1)
                                {
                                    foundBetterMatch = true;
                                }

                                if (responsePriority == bestResponsePriority && distanceSquared < bestDistanceSquared)
                                {
                                    foundBetterMatch = true;
                                }

                                // Magic number 0.75.  Allow matching a lower priority offer only if it is substantially 
                                // closer.
                                if (responsePriority < bestResponsePriority && distanceSquared < 0.75 * bestDistanceSquared)
                                {
                                    foundBetterMatch = true;
                                }

                                if (foundBetterMatch)
                                {
                                    bestResponsePriority = responsePriority;
                                    bestResponseSubIndex = responseSubIndex;
                                    bestDistanceSquared = distanceSquared;
                                }
                            }
                        }

                        if (bestResponsePriority != -1)
                        {
                            int responseCountIndex = (int)material * 8 + bestResponsePriority;
                            var responseOffer = responseOffers[responseCountIndex * 256 + bestResponseSubIndex];
                            int responseAmount = responseOffer.Amount;

                            Logger.LogMaterial(
                                $"TransferManager::MatchOffersClosest: Matched {Utils.ToString(ref requestOffer, material)} to {Utils.ToString(ref responseOffer, material)}!",
                                material);

                            int delta = Mathf.Min(requestAmount, responseAmount);
                            if (delta != 0)
                            {
                                matched = true;
                                if (requestPriority == 0 || bestResponsePriority == 0)
                                {
                                    matchesOutside++;
                                }
                                else
                                {
                                    matchesInside++;
                                }

                                StartTransfer(material, requestOffer, responseOffer, delta);
                            }

                            requestAmount -= delta;
                            responseAmount -= delta;
                            if (responseAmount == 0)
                            {
                                int responseSubCount = responseCount[responseCountIndex] - 1;
                                responseCount[responseCountIndex] = (ushort)responseSubCount;
                                responseOffers[responseCountIndex * 256 + bestResponseSubIndex] = responseOffers[responseCountIndex * 256 + responseSubCount];
                            }
                            else
                            {
                                responseOffer.Amount = responseAmount;
                                responseOffers[responseCountIndex * 256 + bestResponseSubIndex].Amount = responseAmount;
                            }

                            requestOffer.Amount = requestAmount;
                            requestOffers[requestCountIndex * 256 + requestSubIndex].Amount = requestAmount;
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (requestAmount != 0);

                    if (requestPriority > 0 && !matched)
                    {
                        matchesMissed++;
                    }
                }

                if (requestSubCount > 0)
                {
                    Logger.LogMaterial(
                        $"TransferManager::MatchOffersClosest: material={material}, request_priority={requestPriority}, outside_matches={matchesOutside}, inside_matches={matchesInside}, missed_matches={matchesMissed}, max_matches_outside={maxMatchesOutside}",
                        material);
                }
            }
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
        /// <returns></returns>
        private static bool IsValidDistrictOffer(
            TransferManager.TransferReason material, 
            ref TransferManager.TransferOffer requestOffer, int requestPriority,
            ref TransferManager.TransferOffer responseOffer, int responsePriority,
            int randomMax)
        {
            var requestBuilding = TransferManagerInfo.GetHomeBuilding(ref requestOffer);
            var responseBuilding = TransferManagerInfo.GetHomeBuilding(ref responseOffer);

            if (responseBuilding == 0)
            {
                Logger.LogMaterial(
                    $"TransferManager::IsValidDistrictOffer: {Utils.ToString(ref responseOffer, material)}, not a district services building",
                    material);
                return false;
            }

            // Special logic if both buildings are warehouses.  Used to prevent goods from being shuffled back and forth between warehouses.
            if (BuildingManager.instance.m_buildings.m_buffer[requestBuilding].Info.GetAI() is WarehouseAI &&
                BuildingManager.instance.m_buildings.m_buffer[responseBuilding].Info.GetAI() is WarehouseAI)
            {
                return false;
            }

            if (Constraints.AllLocalAreas(responseBuilding))
            {
                Logger.LogMaterial(
                    $"TransferManager::IsValidDistrictOffer: {Utils.ToString(ref responseOffer, material)}, serves all local areas",
                    material);
                return true;
            }

            // The call to TransferManagerInfo.GetDistrict applies to offers that are come from buildings, service 
            // vehicles, citizens, AND segments.  The latter needs to be considered for road maintenance.
            var requestDistrictPark = TransferManagerInfo.GetDistrictPark(material, ref requestOffer);
            var responseDistrictParksServed = Constraints.DistrictParkServiced(responseBuilding);
            if (requestDistrictPark.IsServedBy(responseDistrictParksServed))
            {
                Logger.LogMaterial(
                    $"TransferManager::IsValidDistrictOffer: {Utils.ToString(ref responseOffer, material)}, serves district {requestDistrictPark.Name}",
                    material);
                return true;
            }

            Logger.LogMaterial(
                $"TransferManager::IsValidDistrictOffer: {Utils.ToString(ref responseOffer, material)}, not valid",
                material);
            return false;
        }

        /// <summary>
        /// Returns true if we can potentially match the two given offers.
        /// </summary>
        /// <returns></returns>
        private static bool IsValidSupplyChainOffer(
            TransferManager.TransferReason material,
            ref TransferManager.TransferOffer requestOffer, int requestPriority,
            ref TransferManager.TransferOffer responseOffer, int responsePriority,
            int randomMax)
        {
            var requestBuilding = TransferManagerInfo.GetHomeBuilding(ref requestOffer);
            var responseBuilding = TransferManagerInfo.GetHomeBuilding(ref responseOffer);

            if (responseBuilding == 0)
            {
                Logger.LogMaterial(
                    $"TransferManager::IsValidSupplyChainOffer: {Utils.ToString(ref responseOffer, material)}, not a district services building",
                    material);
                return false;
            }

            // First check if a supply link exists.
            var responseSupplyDestinations = Constraints.SupplyDestinations(responseBuilding);
            if (HasSupplyChainInRestriction(requestBuilding, material))
            {
                if (responseSupplyDestinations?.Count > 0)
                {
                    for (int i = 0; i < responseSupplyDestinations.Count; i++)
                    {
                        if (responseSupplyDestinations[i] == (int)requestBuilding)
                        {
                            Logger.LogMaterial(
                                $"TransferManager::IsValidSupplyChainOffer: {Utils.ToString(ref responseOffer, material)}, supply link allowed",
                                material);
                            return true;
                        }
                    }

                    Logger.LogMaterial(
                        $"TransferManager::IsValidSupplyChainOffer: {Utils.ToString(ref responseOffer, material)}, supply link disallowed",
                        material);
                    return false;
                }
                else
                {
                    // If supply source restriction exists, then do not attempt to match offer with any other source!
                    Logger.LogMaterial(
                        $"TransferManager::IsValidSupplyChainOffer: {Utils.ToString(ref responseOffer, material)}, supply link disallowed",
                        material);
                    return false;
                }
            }

            // Special logic if both buildings are warehouses.  Used to prevent goods from being shuffled back and forth between warehouses.
            if (BuildingManager.instance.m_buildings.m_buffer[requestBuilding].Info.GetAI() is WarehouseAI &&
                BuildingManager.instance.m_buildings.m_buffer[responseBuilding].Info.GetAI() is WarehouseAI)
            {
                return false;
            }

            if (Constraints.AllLocalAreas(responseBuilding))
            {
                Logger.LogMaterial(
                    $"TransferManager::IsValidSupplyChainOffer: {Utils.ToString(ref responseOffer, material)}, serves all local areas",
                    material);
                return true;
            }

            // Only if there are no supply out restrictions, and no supply in restrictions, do we try to match on 
            // district.
            if (responseSupplyDestinations == null || responseSupplyDestinations.Count == 0)
            {
                // The call to TransferManagerInfo.GetDistrict applies to offers that are come from buildings, service 
                // vehicles, citizens, AND segments.  The latter needs to be considered for road maintenance.
                var requestDistrictPark = TransferManagerInfo.GetDistrictPark(material, ref requestOffer);
                var responseDistrictParksServed = Constraints.DistrictParkServiced(responseBuilding);
                if (requestDistrictPark.IsServedBy(responseDistrictParksServed))
                {
                    Logger.LogMaterial(
                        $"TransferManager::IsValidSupplyChainOffer: {Utils.ToString(ref responseOffer, material)}, serves district {requestDistrictPark.Name}",
                        material);
                    return true;
                }
            }

            Logger.LogMaterial(
                $"TransferManager::IsValidSupplyChainOffer: {Utils.ToString(ref responseOffer, material)}, not valid",
                material);
            return false;
        }

        private static bool PreFilterLowPriorityOffer(
            TransferManager.TransferReason material,
            ref TransferManager.TransferOffer requestOffer, int requestPriority)
        {
            if (requestOffer.Amount == 0)
            {
                return true;
            }

            // First check if a supply link exists.
            var requestBuilding = TransferManagerInfo.GetHomeBuilding(ref requestOffer);
            if (HasSupplyChainInRestriction(requestBuilding, material))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if we can potentially match the two given offers.
        /// </summary>
        /// <returns></returns>
        private static bool IsValidLowPriorityOffer(
            TransferManager.TransferReason material,
            ref TransferManager.TransferOffer requestOffer, int requestPriority,
            ref TransferManager.TransferOffer responseOffer, int responsePriority,
            int randomMax)
        {
            var requestBuilding = TransferManagerInfo.GetHomeBuilding(ref requestOffer);
            var responseBuilding = TransferManagerInfo.GetHomeBuilding(ref responseOffer);

            if (responseBuilding == 0)
            {
                Logger.LogMaterial(
                    $"TransferManager::IsValidLowPriorityOffer: {Utils.ToString(ref responseOffer, material)}, not a district services building",
                    material);
                return false;
            }

            // Special logic if both buildings are warehouses.  Used to prevent goods from being shuffled back and forth between warehouses.
            if (BuildingManager.instance.m_buildings.m_buffer[requestBuilding].Info.GetAI() is WarehouseAI &&
                BuildingManager.instance.m_buildings.m_buffer[responseBuilding].Info.GetAI() is WarehouseAI)
            {
                return false;
            }

            // Special logic for recycling centers, since they can produce recycled goods but the district policies
            // should not apply to these materials.
            if (responseBuilding != 0 && BuildingManager.instance.m_buildings.m_buffer[responseBuilding].Info.GetAI() is LandfillSiteAI)
            {
                Logger.LogMaterial(
                    $"TransferManager::IsValidSupplyChainOffer: {Utils.ToString(ref responseOffer, material)}, allow recycling centers",
                    material);
                return true;
            }

            // See if the request is from an outside connection ...
            if (TransferManagerInfo.IsOutsideOffer(ref requestOffer))
            {
                if (TransferManagerInfo.IsOutsideOffer(ref responseOffer))
                {
                    if (m_randomizer.Int32(1000) <= randomMax)
                    {
                        Logger.LogMaterial(
                            $"TransferManager::IsValidLowPriorityOffer: {Utils.ToString(ref responseOffer, material)}, matching outside to outside",
                            material);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (TransferManagerInfo.GetSupplyBuildingAmount(responseBuilding) > Constraints.InternalSupplyBuffer(responseBuilding))
                {
                    Logger.LogMaterial(
                        $"TransferManager::IsValidLowPriorityOffer: {Utils.ToString(ref responseOffer, material)}, internal supply buffer, supply amount={TransferManagerInfo.GetSupplyBuildingAmount(responseBuilding)}, supply buffer={Constraints.InternalSupplyBuffer(responseBuilding)}",
                        material);
                    return true;
                }
                else if (Constraints.OutsideConnections(responseBuilding))
                {
                    Logger.LogMaterial(
                        $"TransferManager::IsValidLowPriorityOffer: {Utils.ToString(ref responseOffer, material)}, matched inside to outside offer",
                        material);
                    return true;
                }
                else
                {
                    Logger.LogMaterial(
                        $"TransferManager::IsValidLowPriorityOffer: {Utils.ToString(ref responseOffer, material)}, disallowed outside offer",
                        material);
                    return false;
                }
            }

            // Here, we are guaranteed that the request is a local offer.
            if (TransferManagerInfo.IsOutsideBuilding(responseBuilding))
            {
                // Don't be so aggressive in trying to serve low priority orders with outside connections.
                if (requestPriority > 1 || m_randomizer.Int32(100) <= randomMax)
                {
                    Logger.LogMaterial(
                        $"TransferManager::IsValidLowPriorityOffer: {Utils.ToString(ref responseOffer, material)}, matched outside to inside offer",
                        material);
                    return true;
                }
            }
            else if (TransferManagerInfo.GetSupplyBuildingAmount(responseBuilding) > Constraints.InternalSupplyBuffer(responseBuilding))
            {
                Logger.LogMaterial(
                    $"TransferManager::IsValidLowPriorityOffer: {Utils.ToString(ref responseOffer, material)}, internal supply buffer, supply amount={TransferManagerInfo.GetSupplyBuildingAmount(responseBuilding)}, supply buffer={Constraints.InternalSupplyBuffer(responseBuilding)}",
                    material);
                return true;
            }

            Logger.LogMaterial(
                $"TransferManager::IsValidLowPriorityOffer: {Utils.ToString(ref responseOffer, material)}, not valid",
                material);
            return false;
        }

        private static bool HasSupplyChainInRestriction(int requestBuilding, TransferManager.TransferReason material)
        {
            var requestSupplySources = Constraints.SupplySources((ushort)requestBuilding);
            if (requestBuilding == 0 || requestSupplySources == null || requestSupplySources.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < requestSupplySources.Count; i++)
            {
                var supplyInBuilding = (ushort)requestSupplySources[i];
                var supplyInBuildingAI = BuildingManager.instance.m_buildings.m_buffer[supplyInBuilding].Info?.GetAI();

                TransferManager.TransferReason supplyInBuildingMaterial = TransferManager.TransferReason.None;
                switch (supplyInBuildingAI)
                {
                    case ExtractingFacilityAI extractingFacilityAI:
                        supplyInBuildingMaterial = extractingFacilityAI.m_outputResource;
                        break;

                    case OutsideConnectionAI outsideConnectionAI:
                        supplyInBuildingMaterial = material;
                        break;

                    case ProcessingFacilityAI processingFacilityAI:
                        supplyInBuildingMaterial = processingFacilityAI.m_outputResource;
                        break;

                    case WarehouseAI warehouseAI:
                        supplyInBuildingMaterial = warehouseAI.GetTransferReason(supplyInBuilding, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[supplyInBuilding]);
                        break;

                    default:
                        break;
                }

                if (supplyInBuildingMaterial == TransferManager.TransferReason.None)
                {
                    Logger.LogWarning($"TransferManagerMod::HasSupplyChainRestriction: Could not determine material for building {supplyInBuilding} of type {supplyInBuildingAI?.GetType()}!");
                }

                if (supplyInBuildingMaterial == material)
                {
                    return true;
                }
            }

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
