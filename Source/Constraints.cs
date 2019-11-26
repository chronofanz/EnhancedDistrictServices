﻿using ColossalFramework;
using System.Collections.Generic;

namespace EnhancedDistrictServices
{
    public static class Constraints
    {
        public static readonly bool[] BuildingToAllLocalAreas = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public static readonly bool[] BuildingToOutsideConnections = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public static readonly List<int>[] BuildingToDistrictServiced = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Map of building id to the list of allowed destination building ids.  For supply chains only.
        /// </summary>
        public static readonly List<int>[] SupplyDestinations = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// A cached derived view of SupplyDestinations.  Maps building id to the list of allowed source building ids.
        /// For supply chains only.
        /// </summary>
        public static readonly List<int>[] SupplySources = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Static constructor.
        /// </summary>
        static Constraints()
        {
            Clear();
        }

        /// <summary>
        /// Reset all data structures.
        /// </summary>
        public static void Clear()
        {
            for (ushort buildingId = 0; buildingId < BuildingManager.MAX_BUILDING_COUNT; buildingId++)
            {
                ReleaseBuilding(buildingId);
            }
        }

        /// <summary>
        /// Called when a building is first created.  If situated in a district, then automatically restricts that
        /// building to serve its home district only.
        /// </summary>
        /// <param name="buildingId"></param>
        public static void CreateBuilding(ushort buildingId)
        {
            var position = BuildingManager.instance.m_buildings.m_buffer[buildingId].m_position;
            var homeDistrict = DistrictManager.instance.GetDistrict(position);

            if (homeDistrict != 0)
            {
                AddDistrictRestriction(buildingId, homeDistrict);
                SetAllLocalAreas(buildingId, false, true);
                SetAllOutsideConnections(buildingId, false, true);
            }
            else
            {
                SetAllLocalAreas(buildingId, true, true);
                SetAllOutsideConnections(buildingId, true, true);
            }
        }

        /// <summary>
        /// Called when a building is destroyed.
        /// </summary>
        /// <param name="buildingId"></param>
        public static void ReleaseBuilding(ushort buildingId)
        {
            BuildingToAllLocalAreas[buildingId] = true;
            BuildingToOutsideConnections[buildingId] = true;
            BuildingToDistrictServiced[buildingId] = null;

            RemoveAllSupplyChainConnectionsToDestination(buildingId);
            RemoveAllSupplyChainConnectionsFromSource(buildingId);
        }

        public static void SetAllLocalAreas(int buildingId, bool status, bool verbose)
        {
            if (!TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
            {
                return;
            }

            if (verbose || (BuildingToAllLocalAreas[buildingId] != status))
            {
                var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
                Logger.Log($"DistrictServicesTable::SetAllLocalAreas: {buildingName} ({buildingId}) = {status} ...");
            }

            BuildingToAllLocalAreas[buildingId] = status;
        }

        public static void SetAllOutsideConnections(int buildingId, bool status, bool verbose)
        {
            if (!TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
            {
                return;
            }

            if (verbose || (BuildingToOutsideConnections[buildingId] != status))
            {
                var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
                Logger.Log($"DistrictServicesTable::SetAllOutgoingConnections: {buildingName} ({buildingId}) = {status} ...");
            }

            BuildingToOutsideConnections[buildingId] = status;
        }

        public static void AddDistrictRestriction(int buildingId, int district)
        {
            if (!TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
            {
                return;
            }

            if (BuildingToDistrictServiced[buildingId] == null)
            {
                BuildingToDistrictServiced[buildingId] = new List<int>();
            }

            if (!BuildingToDistrictServiced[buildingId].Contains(district))
            {
                var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
                var districtName = DistrictManager.instance.GetDistrictName(district);
                Logger.Log($"DistrictServicesTable::AddDistrictRestriction: {buildingName} ({buildingId}) => {districtName} ...");

                BuildingToDistrictServiced[buildingId].Add(district);
            }
        }

        public static void RemoveDistrictRestriction(int buildingId, int district)
        {
            if (BuildingToDistrictServiced[buildingId] == null)
            {
                return;
            }

            if (BuildingToDistrictServiced[buildingId].Contains(district))
            {
                var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
                var districtName = DistrictManager.instance.GetDistrictName(district);
                Logger.Log($"DistrictServicesTable::RemoveDistrictRestriction: {buildingName} ({buildingId}) => {districtName} ...");

                BuildingToDistrictServiced[buildingId].Remove(district);
            }

            if (BuildingToDistrictServiced[buildingId].Count == 0)
            {
                BuildingToDistrictServiced[buildingId] = null;
            }
        }

        #region Supply Chain methods

        /// <summary>
        /// Add a supply chain link between the source and destination buildings.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
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

        /// <summary>
        /// Remove the supply chain link between the source and destination buildings, if it exists.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
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

        /// <summary>
        /// Remove all supply chain links that are sourced from the given building id.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Remove all supply chain links where the destination is the given building id.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <returns></returns>
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

        #endregion
    }
}