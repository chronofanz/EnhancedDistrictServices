using ColossalFramework;
using System.Collections.Generic;
using System.Linq;

namespace EnhancedDistrictServices
{
    public static class SupplyChainTable
    {
        // Outgoing to list of allowed incoming.
        public static List<int>[] BuildingToBuildingServiced = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

        // Maps building id to true/false depending on whether it restricted to one or more particular outgoing offers or not.
        // CACHE to boost perf.
        public static List<int>[] IncomingOfferRestricted = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

        // 
        public static void Clear()
        {
            for (uint buildingId = 0; buildingId < BuildingToBuildingServiced.Length; buildingId++)
            {
                BuildingToBuildingServiced[buildingId] = null;
                IncomingOfferRestricted[buildingId] = null;
            }
        }

        public static void AddSupplyChainConnection(int source, int destination)
        {
            if (!TransferManagerInfo.IsSupplyChainBuilding(source) || !TransferManagerInfo.IsSupplyChainBuilding(destination))
            {
                return;
            }

            bool added = false;

            if (BuildingToBuildingServiced[source] == null)
            {
                BuildingToBuildingServiced[source] = new List<int>();
            }

            if (!BuildingToBuildingServiced[source].Contains(destination))
            {
                added = true;
                BuildingToBuildingServiced[source].Add(destination);
            }

            if (IncomingOfferRestricted[destination] == null)
            {
                IncomingOfferRestricted[destination] = new List<int>();
            }

            if (!IncomingOfferRestricted[destination].Contains(source))
            {
                added = true;
                IncomingOfferRestricted[destination].Add(source);
            }

            if (added)
            {
                var sourceBuildingName = TransferManagerInfo.GetBuildingName(source);
                var destinationBuildingName = TransferManagerInfo.GetBuildingName(destination);
                Logger.Log($"SupplyChainTable::AddSupplyChainConnection: {sourceBuildingName} ({source}) => {destinationBuildingName} ({destination}) ...");
            }
        }

        public static void RemoveSupplyChainConnection(int source, int destination)
        {
            BuildingToBuildingServiced[source]?.Remove(destination);
            if (BuildingToBuildingServiced[source]?.Count > 0)
            {
                BuildingToBuildingServiced[source] = null;
            }

            IncomingOfferRestricted[destination]?.Remove(source);
            if (IncomingOfferRestricted[destination]?.Count > 0)
            {
                IncomingOfferRestricted[destination] = null;
            }
        }

        public static void RemoveBuilding(uint buildingId)
        {
            bool removed = false;

            removed |= RemoveBuildingFromOtherTargets((int)buildingId);
            removed |= RemoveBuildingTargets((int)buildingId);

            if (removed)
            {
                Logger.Log($"SupplyChainTable::RemoveBuilding: building={buildingId}");
            }
        }

        public static bool RemoveBuildingTargets(int buildingId)
        {
            if (BuildingToBuildingServiced[buildingId] != null)
            {
                while (BuildingToBuildingServiced[buildingId]?.Count > 0)
                {
                    RemoveSupplyChainConnection(buildingId, BuildingToBuildingServiced[buildingId][0]);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool RemoveBuildingFromOtherTargets(int buildingId)
        {
            bool removed = false;

            // First remove this building from any lists that might refer to this building ...
            for (uint b = 0; b < BuildingToBuildingServiced.Length; b++)
            {
                if (BuildingToBuildingServiced[b] != null && BuildingToBuildingServiced[b].Contains(buildingId))
                {
                    RemoveSupplyChainConnection((int)b, buildingId);
                    removed = true;
                }
            }

            return removed;
        }
    }
}
