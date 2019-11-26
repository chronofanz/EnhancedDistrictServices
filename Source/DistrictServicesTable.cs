using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnhancedDistrictServices
{
    public static class DistrictServicesTable
    {
        public static bool[] BuildingToAllLocalAreas = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public static bool[] BuildingToOutsideConnections = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public static List<int>[] BuildingToDistrictServiced = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

        public static void Clear()
        {
            for (int buildingId = 0; buildingId < BuildingToDistrictServiced.Length; buildingId++)
            {
                RemoveBuilding(buildingId);
            }
        }

        public static void SetAllLocalAreas(int buildingId, bool status, bool verbose)
        {
            if (!TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
            {
                return;
            }

            if (verbose || (BuildingToAllLocalAreas[buildingId] != status))
            {
                var buildingName = Singleton<BuildingManager>.instance.GetBuildingName((ushort)buildingId, InstanceID.Empty);
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
                var buildingName = Singleton<BuildingManager>.instance.GetBuildingName((ushort)buildingId, InstanceID.Empty);
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

            var buildingName = Singleton<BuildingManager>.instance.GetBuildingName((ushort)buildingId, InstanceID.Empty);
            var districtName = DistrictManager.instance.GetDistrictName(district);

            if (BuildingToDistrictServiced[buildingId] == null)
            {
                BuildingToDistrictServiced[buildingId] = new List<int>();
            }

            if (!BuildingToDistrictServiced[buildingId].Contains((int)district))
            {
                Logger.Log($"DistrictServicesTable::AddDistrictRestriction: {buildingName} ({buildingId}) => {districtName} ...");
                BuildingToDistrictServiced[buildingId].Add((int)district);
            }
        }

        public static void RemoveDistrictRestriction(int buildingId, int district)
        {
            var buildingName = Singleton<BuildingManager>.instance.GetBuildingName((ushort)buildingId, InstanceID.Empty);
            var districtName = DistrictManager.instance.GetDistrictName(district);

            if (BuildingToDistrictServiced[buildingId] == null)
            {
                return;
            }

            if (BuildingToDistrictServiced[buildingId].Contains((int)district))
            {
                Logger.Log($"DistrictServicesTable::RemoveDistrictRestriction: {buildingName} ({buildingId}) => {districtName} ...");
                BuildingToDistrictServiced[buildingId].Remove((int)district);
            }

            if (BuildingToDistrictServiced[buildingId].Count == 0)
            {
                BuildingToDistrictServiced[buildingId] = null;
            }
        }

        public static void RemoveBuilding(int buildingId)
        {
            BuildingToAllLocalAreas[buildingId] = false;
            BuildingToOutsideConnections[buildingId] = false;
            BuildingToDistrictServiced[buildingId] = null;
        }

        public static string ToString(uint buildingId, List<uint> buildingToDistrictServiced)
        {
            if (buildingToDistrictServiced == null)
            {
                return buildingId.ToString();
            }
            else
            {
                return $"{buildingId}-{string.Join(",", buildingToDistrictServiced.Select(district => district.ToString()).ToArray())}";
            }
        }
    }
}
