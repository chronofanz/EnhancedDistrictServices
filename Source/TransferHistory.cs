using ColossalFramework;
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

        public static void RecordMatch(TransferManager.TransferReason material, ushort requestBuilding, ushort responseBuilding)
        {
            // Only record building to building matches ...
            if (requestBuilding == 0 || responseBuilding == 0)
            {
                return;
            }

            var timestamp = Singleton<SimulationManager>.instance.m_currentGameTime;
            Logger.Log($"{material} match: B{requestBuilding} to B{responseBuilding} @ {timestamp}");

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
                list = materialEvents.Events[requestBuilding];
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
