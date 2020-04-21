﻿using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Tracks history of certain types of transfers, to prevent too-frequent matching between buildings.
    /// </summary>
    public static class TransferHistory
    {
        [Serializable]
        public class TransferEvent
        {
            public ushort RequestBuilding { get; set; }
            public ushort ResponseBuilding { get; set; }
            public TransferManager.TransferReason Material { get; set; }
            public DateTime TimeStamp { get; set; }
        }

        public class MaterialEvents
        {
            public Dictionary<ushort, List<TransferEvent>> Events = new Dictionary<ushort, List<TransferEvent>>();

            /// <summary>
            /// After this time period in days, TransferEvents are purged.
            /// </summary>
            public const int MAX_TTL = 30;

            /// <summary>
            /// Principles: 
            ///   1) max concurrent orders per building per type is capped per MAX_TTL day period
            ///   2) 
            /// </summary>
            /// <param name="material"></param>
            /// <param name="requestBuilding"></param>
            /// <param name="responseBuilding"></param>
            /// <returns></returns>
            public bool IsRestricted(TransferManager.TransferReason material, ushort requestBuilding, ushort responseBuilding)
            {
                if (!Events.TryGetValue(requestBuilding, out var list))
                {
                    return false;
                }

                var isRequestBuildingOutside = TransferManagerInfo.IsOutsideBuilding(requestBuilding);
                var isResponseBuildingOutside = TransferManagerInfo.IsOutsideBuilding(responseBuilding);

                if (!Settings.enableDummyCargoTraffic.value && isRequestBuildingOutside && isResponseBuildingOutside)
                {
                    return true;
                }

                var concurrentOrderCount = list.Count;

                var concurrentOrderCountToOutsideConnection = 0;
                var concurrentOrderCountToResponseBuilding = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].ResponseBuilding == responseBuilding)
                    {
                        concurrentOrderCountToResponseBuilding++;
                    }

                    if (TransferManagerInfo.IsOutsideBuilding(list[i].ResponseBuilding))
                    {
                        concurrentOrderCountToOutsideConnection++;
                    }
                }

                var vehicleCount = TransferManagerInfo.GetCargoVehicleCount(requestBuilding, material);

                var maxConcurrentOrderCount = Math.Ceiling(Constraints.GlobalOutsideConnectionIntensity() / 10.0);
                if (isRequestBuildingOutside && TransferManagerInfo.IsOutsideRoadConnection(requestBuilding))
                {
                    maxConcurrentOrderCount *= 4;
                }

                var maxConcurrentOrderCountToResponseBuilding = Math.Ceiling(maxConcurrentOrderCount / 2.0);
                var maxConcurrentOrderCountToOutsideConnection = Math.Ceiling(maxConcurrentOrderCount * Constraints.GlobalOutsideToOutsidePerc() / 100.0);

                var maxVehicleCount = Math.Ceiling(maxConcurrentOrderCount / 2.0);

                bool isRestrictedConcurrent = concurrentOrderCount >= maxConcurrentOrderCount;
                bool isRestrictedConcurrentToBuilding = concurrentOrderCountToResponseBuilding >= maxConcurrentOrderCountToResponseBuilding;
                bool isRestrictedConcurrentToOutsideConnection = isRequestBuildingOutside && isResponseBuildingOutside && concurrentOrderCountToOutsideConnection >= maxConcurrentOrderCountToOutsideConnection;
                bool isVehicleConstrained = vehicleCount >= maxVehicleCount;

                return isRestrictedConcurrent || isRestrictedConcurrentToBuilding || isRestrictedConcurrentToOutsideConnection || isVehicleConstrained;
            }

            public void PurgeOldEvents()
            {
                var timestamp = Singleton<SimulationManager>.instance.m_currentGameTime;
                var expiry = timestamp.AddDays(-MaterialEvents.MAX_TTL);

                foreach (var list in Events.Values)
                {
                    for (int i = 0; i < list.Count;)
                    {
                        if (list[i].TimeStamp < expiry)
                        {
                            list.RemoveAt(i);
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
            }
        }

        private static Dictionary<TransferManager.TransferReason, MaterialEvents> m_data = new Dictionary<TransferManager.TransferReason, MaterialEvents>();

        /// <summary>
        /// Reset all data structures.
        /// </summary>
        public static void Clear()
        {
            m_data.Clear();
        }

        /// <summary>
        /// Load data from given object.
        /// </summary>
        /// <param name="data"></param>
        public static void LoadData(Serialization.TransferHistoryv1 data)
        {
            Logger.Log($"TransferHistory::LoadData: version {data.Id}");
            Clear();

            foreach (var e in data.TransferEvents)
            {
                Logger.Log($"TransferHistory::LoadData: B{e.RequestBuilding} to B{e.ResponseBuilding}, {e.Material}, {e.TimeStamp}");
                Add(e.RequestBuilding, e.ResponseBuilding, e.Material, e.TimeStamp);
            }
        }

        /// <summary>
        /// Saves a copy of the data in this object, for serialization.
        /// </summary>
        /// <returns></returns>
        public static Serialization.TransferHistoryv1 SaveData()
        {
            var data = new Serialization.TransferHistoryv1();
            foreach (var materialEvents in m_data.Values)
            {
                foreach (var events in materialEvents.Events.Values)
                {
                    data.TransferEvents.AddRange(events);
                }
            }

            return data;
        }

        public static bool IsRestricted(TransferManager.TransferReason material, ushort requestBuilding, ushort responseBuilding)
        {
            if (!m_data.TryGetValue(material, out var materialEvents))
            {
                return false;
            }

            var isRestricted = 
                materialEvents.IsRestricted(material, requestBuilding, responseBuilding) ||
                materialEvents.IsRestricted(material, responseBuilding, requestBuilding);

            if (isRestricted)
            {
                Logger.LogMaterial($"{material} match disallowed: B{requestBuilding} to B{responseBuilding}", material);
            }

            return isRestricted;
        }

        public static void PurgeOldEvents(TransferManager.TransferReason material)
        {
            if (m_data.TryGetValue(material, out var materialEvents))
            {
                materialEvents.PurgeOldEvents();
            }
        }

        public static void RecordMatch(TransferManager.TransferReason material, ushort requestBuilding, ushort responseBuilding)
        {
            // Only record building to building matches ...
            if (requestBuilding == 0 || responseBuilding == 0)
            {
                return;
            }

            var timestamp = Singleton<SimulationManager>.instance.m_currentGameTime;

            Add(requestBuilding, responseBuilding, material, timestamp);
            Add(responseBuilding, requestBuilding, material, timestamp);
        }

        private static void Add(ushort requestBuilding, ushort responseBuilding, TransferManager.TransferReason material, DateTime timestamp)
        {
            if (!m_data.TryGetValue(material, out var materialEvents))
            {
                materialEvents = m_data[material] = new MaterialEvents();
            }

            if (!materialEvents.Events.TryGetValue(requestBuilding, out var list))
            {
                list = materialEvents.Events[requestBuilding] = new List<TransferEvent>();
            }

            list.Add(new TransferEvent
            {
                RequestBuilding = requestBuilding,
                ResponseBuilding = responseBuilding,
                Material = material,
                TimeStamp = timestamp
            });
        }
    }
}
