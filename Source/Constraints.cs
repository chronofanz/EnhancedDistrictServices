using ColossalFramework;
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
        /// Map of building id to the specified supply goods buffer, below which all good sent from this building will
        /// be to specified districts or supply out buildings only.  Default value is 100, meaning all goods are sent
        /// to destinations that satisfy district and supply out restrictions.
        /// </summary>
        private static readonly int[] m_buildingToInternalSupplyBuffer = new int[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Map of building id to list of districts or parks served by the building.
        /// </summary>
        private static readonly List<DistrictPark>[] m_buildingToDistrictParkServiced = new List<DistrictPark>[BuildingManager.MAX_BUILDING_COUNT];

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
        /// Betweeen 0 and 100 inclusive, this controls how much incoming/outgoing traffic comes into the game.
        /// </summary>
        private static int m_globalOutsideConnectionIntensity = 5;

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
        /// Load data from given object.
        /// </summary>
        /// <param name="data"></param>
        public static void LoadData(Serialization.Datav2 data)
        {
            Logger.Log($"Constraints::LoadData: version {data.Id}");
            Clear();

            var buildings = Utils.GetSupportedServiceBuildings();
            foreach (var building in buildings)
            {
                var name = TransferManagerInfo.GetBuildingName(building);
                var buildingInfo = BuildingManager.instance.m_buildings.m_buffer[building].Info;
                var service = buildingInfo.GetService();
                var subService = buildingInfo.GetSubService();
                var ai = buildingInfo.GetAI();

                Logger.Log($"Constraints::LoadData: buildingName={name}, buildingId={building}, service={service}, subService={subService}, ai={ai}");

                var restrictions1 = data.BuildingToAllLocalAreas[building];
                SetAllLocalAreas(building, restrictions1);

                var restrictions2 = data.BuildingToOutsideConnections[building];
                SetAllOutsideConnections(building, restrictions2);

                var restrictions3 = data.BuildingToInternalSupplyBuffer[building];
                SetInternalSupplyReserve(building, restrictions3);

                var restrictions4 = data.BuildingToBuildingServiced[building];
                if (restrictions4 != null)
                {
                    foreach (var destination in restrictions4)
                    {
                        AddSupplyChainConnection(building, destination);
                    }
                }

                var restrictions5 = data.BuildingToDistrictServiced[building];
                if (restrictions5 != null)
                {
                    foreach (var districtPark in restrictions5)
                    {
                        AddDistrictParkServiced(building, DistrictPark.FromSerializedInt(districtPark));
                    }
                }

                m_globalOutsideConnectionIntensity = data.GlobalOutsideConnectionIntensity;

                Logger.Log("");
            }
        }

        /// <summary>
        /// Saves a copy of the data in this object, for serialization.
        /// </summary>
        /// <returns></returns>
        public static Serialization.Datav2 SaveData()        
        {
            return new Serialization.Datav2
            {
                BuildingToAllLocalAreas = m_buildingToAllLocalAreas.ToArray(),
                BuildingToOutsideConnections = m_buildingToOutsideConnections.ToArray(),
                BuildingToInternalSupplyBuffer = m_buildingToInternalSupplyBuffer.ToArray(),
                BuildingToBuildingServiced = m_supplyDestinations.ToArray(),
                BuildingToDistrictServiced = m_buildingToDistrictParkServiced
                    .Select(list => list?.Select(districtPark => districtPark.ToSerializedInt()).ToList())
                    .ToArray(),
                GlobalOutsideConnectionIntensity = m_globalOutsideConnectionIntensity
            };
        }

        /// <summary>
        /// Called when a building is first created.  If situated in a district or park, then automatically restricts that
        /// building to serve its home district only.
        /// </summary>
        /// <param name="buildingId"></param>
        public static void CreateBuilding(ushort buildingId)
        {
            if (!TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
            {
                return;
            }

            var buildingInfo = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info;
            var service = buildingInfo.GetService();
            var subService = buildingInfo.GetSubService();
            var ai = buildingInfo.GetAI();

            // Do not pack the homeDistrict and homePark into a single DistrictPark struct.  Otherwise, it will make 
            // removing districts/parks a lot harder!!
            var position = BuildingManager.instance.m_buildings.m_buffer[buildingId].m_position;
            var homeDistrict = DistrictManager.instance.GetDistrict(position);
            var homePark = DistrictManager.instance.GetPark(position);

            Logger.Log($"Constraints::CreateBuilding: buildingId={buildingId}, homeDistrict={homeDistrict}, homePark={homePark}, service={service}, subService={subService}, ai={ai}");

            // Serve all areas if the building doesn't belong to any district or park.
            SetAllLocalAreas(buildingId, homeDistrict == 0 && homePark == 0);
            SetAllOutsideConnections(buildingId, homeDistrict == 0 && homePark == 0);

            if (homeDistrict != 0)
            {
                AddDistrictParkServiced(buildingId, DistrictPark.FromDistrict(homeDistrict));
            }

            if (homePark != 0)
            {
                AddDistrictParkServiced(buildingId, DistrictPark.FromPark(homePark));
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
            m_buildingToInternalSupplyBuffer[buildingId] = 100;
            m_buildingToDistrictParkServiced[buildingId] = null;

            RemoveAllSupplyChainConnectionsToDestination(buildingId);
            RemoveAllSupplyChainConnectionsFromSource(buildingId);
        }

        /// <summary>
        /// Called when a district or park is removed.
        /// </summary>
        /// <param name="districtPark"></param>
        public static void ReleaseDistrictPark(DistrictPark districtPark)
        {
            for (int buildingId = 0; buildingId < BuildingManager.MAX_BUILDING_COUNT; buildingId++)
            {
                var restrictions = m_buildingToDistrictParkServiced[buildingId];
                if (restrictions != null)
                {
                    RemoveDistrictParkServiced(buildingId, districtPark);
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
        /// Returns the internal supply buffer on the building.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static int InternalSupplyBuffer(ushort buildingId)
        {
            return m_buildingToInternalSupplyBuffer[buildingId];
        }

        /// <summary>
        /// Returns the list of districts or parks served by the building.
        /// TODO: Replace with IReadOnlyList.  Can't do it with older version of .NET.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static List<DistrictPark> DistrictParkServiced(ushort buildingId)
        {
            return m_buildingToDistrictParkServiced[buildingId];
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

        /// <summary>
        /// Between 0 and 100 inclusive, controls the intensity of traffic gonig to outside connections.
        /// </summary>
        /// <returns></returns>
        public static int GlobalOutsideConnectionIntensity()
        {
            return m_globalOutsideConnectionIntensity;
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

        /// <summary>
        /// Sets the internal supply reserve.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <param name="amount">Must bet between 0 and 100.</param>
        public static void SetInternalSupplyReserve(int buildingId, int amount)
        {
            m_buildingToInternalSupplyBuffer[buildingId] = COMath.Clamp(amount, 0, 100);
        }

        /// <summary>
        /// Set the global outside connection intensity.
        /// </summary>
        /// <param name="amount"></param>
        public static void SetGlobalOutsideConnectionIntensity(int amount)
        {
            Logger.Log($"Constraints::SetGlobalOutsideConnectionIntensity: {amount}");
            m_globalOutsideConnectionIntensity = COMath.Clamp(amount, 0, 100);
        }

        #endregion

        #region District Services methods

        /// <summary>
        /// Allow the specified district or park to be serviced by the specified building
        /// </summary>
        /// <param name="buildingId"></param>
        /// <param name="districtPark"></param>
        public static void AddDistrictParkServiced(int buildingId, DistrictPark districtPark)
        {
            if (!TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
            {
                return;
            }

            if (m_buildingToDistrictParkServiced[buildingId] == null)
            {
                m_buildingToDistrictParkServiced[buildingId] = new List<DistrictPark>();
            }

            if (!m_buildingToDistrictParkServiced[buildingId].Contains(districtPark))
            {
                var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
                Logger.Log($"Constraints::AddDistrictRestriction: {buildingName} ({buildingId}) => {districtPark.Name} ...");

                m_buildingToDistrictParkServiced[buildingId].Add(districtPark);
            }
        }

        /// <summary>
        /// Disallow the specified district or park from being serviced by the specified building
        /// </summary>
        /// <param name="buildingId"></param>
        /// <param name="districtPark"></param>
        public static void RemoveDistrictParkServiced(int buildingId, DistrictPark districtPark)
        {
            if (m_buildingToDistrictParkServiced[buildingId] == null)
            {
                return;
            }

            if (m_buildingToDistrictParkServiced[buildingId].Contains(districtPark))
            {
                var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
                Logger.Log($"Constraints::RemoveDistrictRestriction: {buildingName} ({buildingId}) => {districtPark.Name} ...");

                m_buildingToDistrictParkServiced[buildingId].Remove(districtPark);
            }

            if (m_buildingToDistrictParkServiced[buildingId].Count == 0)
            {
                m_buildingToDistrictParkServiced[buildingId] = null;
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
