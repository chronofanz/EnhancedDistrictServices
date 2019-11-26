using ColossalFramework;
using System.Collections.Generic;

namespace EnhancedDistrictServices
{
    public static class DistrictServicesTable
    {
        public static readonly bool[] BuildingToAllLocalAreas = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public static readonly bool[] BuildingToOutsideConnections = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public static readonly List<int>[] BuildingToDistrictServiced = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

        static DistrictServicesTable()
        {
            Clear();
        }

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
    }
}
