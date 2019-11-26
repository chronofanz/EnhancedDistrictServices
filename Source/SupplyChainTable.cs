using System.Collections.Generic;

namespace EnhancedDistrictServices
{
    public static class SupplyChainTable
    {
        // Outgoing to list of allowed incoming.
        public static readonly List<int>[] SupplyDestinations = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

        // Maps building id to true/false depending on whether it restricted to one or more particular outgoing offers or not.
        // CACHE to boost perf.
        public static readonly List<int>[] SupplySources = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

        static SupplyChainTable()
        {
            Clear();
        }

        // 
        public static void Clear()
        {
            for (ushort buildingId = 0; buildingId < SupplyDestinations.Length; buildingId++)
            {
                ReleaseBuilding(buildingId);
            }
        }

        /// <summary>
        /// Called when a building is first created.
        /// </summary>
        /// <param name="buildingId"></param>
        public static void CreateBuilding(ushort buildingId)
        {
        }

        /// <summary>
        /// Called when a building is destroyed.
        /// </summary>
        /// <param name="buildingId"></param>
        public static void ReleaseBuilding(ushort buildingId)
        {
            RemoveAllSupplyChainConnectionsToDestination(buildingId);
            RemoveAllSupplyChainConnectionsFromSource(buildingId);
        }

        public static void AddSupplyChainConnection(int source, int destination)
        {
            if (!TransferManagerInfo.IsSupplyChainBuilding(source) || !TransferManagerInfo.IsSupplyChainBuilding(destination))
            {
                return;
            }

            bool added = false;

            if (SupplyDestinations[source] == null)
            {
                SupplyDestinations[source] = new List<int>();
            }

            if (!SupplyDestinations[source].Contains(destination))
            {
                added = true;
                SupplyDestinations[source].Add(destination);
            }

            if (SupplySources[destination] == null)
            {
                SupplySources[destination] = new List<int>();
            }

            if (!SupplySources[destination].Contains(source))
            {
                added = true;
                SupplySources[destination].Add(source);
            }

            if (added)
            {
                var sourceBuildingName = TransferManagerInfo.GetBuildingName(source);
                var destinationBuildingName = TransferManagerInfo.GetBuildingName(destination);
                Logger.Log($"SupplyChainTable::AddSupplyChainConnection: {sourceBuildingName} ({source}) => {destinationBuildingName} ({destination}) ...");
            }
        }

        private static void RemoveSupplyChainConnection(int source, int destination)
        {
            SupplyDestinations[source]?.Remove(destination);
            if (SupplyDestinations[source]?.Count > 0)
            {
                SupplyDestinations[source] = null;
            }

            SupplySources[destination]?.Remove(source);
            if (SupplySources[destination]?.Count > 0)
            {
                SupplySources[destination] = null;
            }
        }

        public static bool RemoveAllSupplyChainConnectionsFromSource(int buildingId)
        {
            if (SupplyDestinations[buildingId] != null)
            {
                while (SupplyDestinations[buildingId]?.Count > 0)
                {
                    RemoveSupplyChainConnection(buildingId, SupplyDestinations[buildingId][0]);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool RemoveAllSupplyChainConnectionsToDestination(int buildingId)
        {
            bool removed = false;

            // First remove this building from any lists that might refer to this building ...
            for (uint b = 0; b < SupplyDestinations.Length; b++)
            {
                if (SupplyDestinations[b] != null && SupplyDestinations[b].Contains(buildingId))
                {
                    RemoveSupplyChainConnection((int)b, buildingId);
                    removed = true;
                }
            }

            return removed;
        }
    }
}
