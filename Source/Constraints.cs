using System;
using System.Collections.Generic;
using System.Linq;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Contains all the district and supply chain constraint data that used by the TransferManager (patch) to determine
    /// how best to match incoming and outgoing offers.
    /// </summary>
    public static class Constraints
    {
        /// <summary>
        /// Map of building id to bool indicating whether all local areas are serviced by the building.
        /// If true, this overrides the BuildingToDistrictServiced constraint.
        /// </summary>
        private static readonly bool[] m_buildingToAllLocalAreas = new bool[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Map of building id to bool indicating whether outside connections are serviced by the building.
        /// </summary>
        private static readonly bool[] m_buildingToOutsideConnections = new bool[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Map of building id to list of districts served by the building.
        /// </summary>
        private static readonly List<int>[] m_buildingToDistrictServiced = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Map of building id to the list of allowed destination building ids.  For supply chains only.
        /// If specified, this overrides all other constraints.
        /// </summary>
        private static readonly List<int>[] m_supplyDestinations = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// A cached derived view of SupplyDestinations.  Maps building id to the list of allowed source building ids.
        /// For supply chains only.  If specified, this overrides all other constraints.
        /// </summary>
        private static readonly List<int>[] m_supplySources = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

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
        /// Helper struct for sorting buildings by their names, to make debugging nicer.
        /// </summary>
        private struct Building : IComparable<Building>
        {
            public string Name { get; set; }
            public int Id { get; set; }

            public int CompareTo(Building other)
            {
                if (Name == null)
                {
                    return -1;
                }
                else if (other.Name == null)
                {
                    return +1;
                }
                else if (!string.Equals(Name, other.Name))
                {
                    return Name.CompareTo(other.Name);
                }
                else
                {
                    return Id.CompareTo(other.Id);
                }
            }
        }

        /// <summary>
        /// Load data from given object.
        /// </summary>
        /// <param name="data"></param>
        public static void LoadData(CitiesMod.EnhancedDistrictServicesSerializableData.Data data)
        {
            Clear();

            var bs = new List<Building>();
            for (int buildingId = 0; buildingId < BuildingManager.MAX_BUILDING_COUNT; buildingId++)
            {
                if (TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
                {
                    var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
                    bs.Add(new Building
                    {
                        Name = TransferManagerInfo.GetBuildingName(buildingId),
                        Id = buildingId
                    });
                }
            }

            bs.Sort();

            foreach (var b in bs)
            {
                var buildingInfo = BuildingManager.instance.m_buildings.m_buffer[b.Id].Info;
                var service = buildingInfo.GetService();
                var subService = buildingInfo.GetSubService();
                var ai = buildingInfo.GetAI();

                Logger.Log($"Constraints::LoadData: buildingName={b.Name}, buildingId={b.Id}, service={service}, subService={subService}, ai={ai}");

                var restrictions1 = data.BuildingToAllLocalAreas[b.Id];
                SetAllLocalAreas(b.Id, restrictions1);

                var restrictions2 = data.BuildingToOutsideConnections[b.Id];
                SetAllOutsideConnections(b.Id, restrictions2);

                var restrictions3 = data.BuildingToBuildingServiced[b.Id];
                if (restrictions3 != null)
                {
                    foreach (var destination in restrictions3)
                    {
                        AddSupplyChainConnection(b.Id, destination);
                    }
                }

                var restrictions4 = data.BuildingToDistrictServiced[b.Id];
                if (restrictions4 != null)
                {
                    foreach (var district in restrictions4)
                    {
                        AddDistrictServiced(b.Id, district);
                    }
                }

                Logger.Log("");
            }
        }

        /// <summary>
        /// Saves a copy of the data in this object, for serialization.
        /// </summary>
        /// <returns></returns>
        public static CitiesMod.EnhancedDistrictServicesSerializableData.Data SaveData()        
        {
            return new CitiesMod.EnhancedDistrictServicesSerializableData.Data
            {
                BuildingToAllLocalAreas = Constraints.m_buildingToAllLocalAreas.ToArray(),
                BuildingToOutsideConnections = Constraints.m_buildingToOutsideConnections.ToArray(),
                BuildingToBuildingServiced = Constraints.m_supplyDestinations.ToArray(),
                BuildingToDistrictServiced = Constraints.m_buildingToDistrictServiced.ToArray()
            };
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
                AddDistrictServiced(buildingId, homeDistrict);
                SetAllLocalAreas(buildingId, false);
                SetAllOutsideConnections(buildingId, false);
            }
            else
            {
                SetAllLocalAreas(buildingId, true);
                SetAllOutsideConnections(buildingId, true);
            }
        }

        /// <summary>
        /// Called when a building is destroyed.
        /// </summary>
        /// <param name="buildingId"></param>
        public static void ReleaseBuilding(ushort buildingId)
        {
            m_buildingToAllLocalAreas[buildingId] = true;
            m_buildingToOutsideConnections[buildingId] = true;
            m_buildingToDistrictServiced[buildingId] = null;

            RemoveAllSupplyChainConnectionsToDestination(buildingId);
            RemoveAllSupplyChainConnectionsFromSource(buildingId);
        }

        /// <summary>
        /// Called when a district is removed.
        /// </summary>
        /// <param name="district"></param>
        public static void ReleaseDistrict(byte district)
        {
            for (int buildingId = 0; buildingId < BuildingManager.MAX_BUILDING_COUNT; buildingId++)
            {
                var restrictions = m_buildingToDistrictServiced[buildingId];
                if (restrictions != null)
                {
                    RemoveDistrictServiced(buildingId, district);
                }
            }
        }

        #region Accessors

        /// <summary>
        /// Returns true if all local areas are serviced by the building.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static bool AllLocalAreas(ushort buildingId)
        {
            return m_buildingToAllLocalAreas[buildingId];
        }

        /// <summary>
        /// Returns true if outside connections are allowed by the building.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static bool OutsideConnections(ushort buildingId)
        {
            return m_buildingToOutsideConnections[buildingId];
        }

        /// <summary>
        /// Returns the list of districts served by the building.
        /// TODO: Replace with IReadOnlyList.  Can't do it with older version of .NET.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static List<int> DistrictServiced(ushort buildingId)
        {
            return m_buildingToDistrictServiced[buildingId];
        }

        /// <summary>
        /// Returns the list of allowed destination building ids.  For supply chains only.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static List<int> SupplyDestinations(ushort buildingId)
        {
            return m_supplyDestinations[buildingId];
        }

        /// <summary>
        /// Returns the list of allowed source building ids.  For supply chains only.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static List<int> SupplySources(ushort buildingId)
        {
            return m_supplySources[buildingId];
        }

        #endregion

        #region Local Areas and Outside Connections methods

        /// <summary>
        /// If <paramref name="status"/> is true, then all local connections are allowed to the specified building.
        /// If <paramref name="status"/> is false, then all local connections are disallowed to the specified building.
        /// This setting overrides district constraints, but is itself overriden by supply chain link specifications.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <param name="status"></param>
        /// <param name="verbose">If true, log the change to the log file</param>
        public static void SetAllLocalAreas(int buildingId, bool status)
        {
            if (!TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
            {
                return;
            }

            if (m_buildingToAllLocalAreas[buildingId] != status)
            {
                var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
                Logger.Log($"Constraints::SetAllLocalAreas: {buildingName} ({buildingId}) = {status} ...");
            }

            m_buildingToAllLocalAreas[buildingId] = status;
        }

        /// <summary>
        /// If <paramref name="status"/> is true, then all outside connections are allowed to the specified building.
        /// If <paramref name="status"/> is false, then all outside connections are disallowed to the specified building.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <param name="status"></param>
        /// <param name="verbose">If true, log the change to the log file</param>
        public static void SetAllOutsideConnections(int buildingId, bool status)
        {
            if (!TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
            {
                return;
            }

            if (m_buildingToOutsideConnections[buildingId] != status)
            {
                var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
                Logger.Log($"Constraints::SetAllOutsideConnections: {buildingName} ({buildingId}) = {status} ...");
            }

            m_buildingToOutsideConnections[buildingId] = status;
        }

        #endregion

        #region District Services methods

        /// <summary>
        /// Allow the specified district to be serviced by the specified building
        /// </summary>
        /// <param name="buildingId"></param>
        /// <param name="district"></param>
        public static void AddDistrictServiced(int buildingId, int district)
        {
            if (!TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
            {
                return;
            }

            if (m_buildingToDistrictServiced[buildingId] == null)
            {
                m_buildingToDistrictServiced[buildingId] = new List<int>();
            }

            if (!m_buildingToDistrictServiced[buildingId].Contains(district))
            {
                var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
                var districtName = DistrictManager.instance.GetDistrictName(district);
                Logger.Log($"Constraints::AddDistrictRestriction: {buildingName} ({buildingId}) => {districtName} ...");

                m_buildingToDistrictServiced[buildingId].Add(district);
            }
        }

        /// <summary>
        /// Disallow the specified district from being serviced by the specified building
        /// </summary>
        /// <param name="buildingId"></param>
        /// <param name="district"></param>
        public static void RemoveDistrictServiced(int buildingId, int district)
        {
            if (m_buildingToDistrictServiced[buildingId] == null)
            {
                return;
            }

            if (m_buildingToDistrictServiced[buildingId].Contains(district))
            {
                var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
                var districtName = DistrictManager.instance.GetDistrictName(district);
                Logger.Log($"Constraints::RemoveDistrictRestriction: {buildingName} ({buildingId}) => {districtName} ...");

                m_buildingToDistrictServiced[buildingId].Remove(district);
            }

            if (m_buildingToDistrictServiced[buildingId].Count == 0)
            {
                m_buildingToDistrictServiced[buildingId] = null;
            }
        }

        #endregion

        #region Supply Chain methods

        /// <summary>
        /// Add a supply chain link between the source and destination buildings.
        /// The supply chain link overrides all local area, all outside connections, and all district constraints.
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

            if (m_supplyDestinations[source] == null)
            {
                m_supplyDestinations[source] = new List<int>();
            }

            if (!m_supplyDestinations[source].Contains(destination))
            {
                added = true;
                m_supplyDestinations[source].Add(destination);
            }

            if (m_supplySources[destination] == null)
            {
                m_supplySources[destination] = new List<int>();
            }

            if (!m_supplySources[destination].Contains(source))
            {
                added = true;
                m_supplySources[destination].Add(source);
            }

            if (added)
            {
                var sourceBuildingName = TransferManagerInfo.GetBuildingName(source);
                var destinationBuildingName = TransferManagerInfo.GetBuildingName(destination);
                Logger.Log($"Constraints::AddSupplyChainConnection: {sourceBuildingName} ({source}) => {destinationBuildingName} ({destination}) ...");
            }
        }

        /// <summary>
        /// Remove the supply chain link between the source and destination buildings, if it exists.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        private static void RemoveSupplyChainConnection(int source, int destination)
        {
            m_supplyDestinations[source]?.Remove(destination);
            if (m_supplyDestinations[source]?.Count == 0)
            {
                m_supplyDestinations[source] = null;
            }

            m_supplySources[destination]?.Remove(source);
            if (m_supplySources[destination]?.Count == 0)
            {
                m_supplySources[destination] = null;
            }
        }

        /// <summary>
        /// Remove all supply chain links that are sourced from the given building id.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static bool RemoveAllSupplyChainConnectionsFromSource(int buildingId)
        {
            if (m_supplyDestinations[buildingId] != null)
            {
                while (m_supplyDestinations[buildingId]?.Count > 0)
                {
                    RemoveSupplyChainConnection(buildingId, m_supplyDestinations[buildingId][0]);
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
            for (int b = 0; b < m_supplyDestinations.Length; b++)
            {
                if (m_supplyDestinations[b] != null && m_supplyDestinations[b].Contains(buildingId))
                {
                    RemoveSupplyChainConnection(b, buildingId);
                    removed = true;
                }
            }

            return removed;
        }

        #endregion
    }
}
