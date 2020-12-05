using ColossalFramework;
using System.Collections.Generic;
using System.Linq;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Misc helper methods for classifying outside connections and determining whether it's possible to match offers
    /// to these outside connections.
    /// </summary>
    public static class OutsideConnectionInfo
    {
        private static HashSet<ushort> m_planeCargoBuildings = new HashSet<ushort>();
        private static HashSet<ushort> m_shipCargoBuildings = new HashSet<ushort>();
        private static HashSet<ushort> m_trainCargoBuildings = new HashSet<ushort>();

        /// <summary>
        /// Static constructor.
        /// </summary>
        static OutsideConnectionInfo()
        {
            Reload();
        }

        public static bool IsInvalidIncomingOutsideConnection(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return false;
            }

            var info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId].Info;
            if (info.m_buildingAI is OutsideConnectionAI outsideConnectionAI)
            {
                switch (outsideConnectionAI.m_transportInfo?.m_vehicleType)
                {
                    case VehicleInfo.VehicleType.Car:
                        return false;

                    case VehicleInfo.VehicleType.Plane:
                        return m_planeCargoBuildings.Count == 0;

                    case VehicleInfo.VehicleType.Ship:
                        return m_shipCargoBuildings.Count == 0;

                    case VehicleInfo.VehicleType.Train:
                        return m_trainCargoBuildings.Count == 0;

                    default:
                        return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static bool IsInvalidOutgoingOutsideConnection(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return false;
            }

            var info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId].Info;
            if (info.m_buildingAI is OutsideConnectionAI outsideConnectionAI)
            {
                switch (outsideConnectionAI.m_transportInfo?.m_vehicleType)
                {
                    case VehicleInfo.VehicleType.Car:
                        return false;

                    case VehicleInfo.VehicleType.Plane:
                        return m_planeCargoBuildings.Count == 0;

                    case VehicleInfo.VehicleType.Ship:
                        return m_shipCargoBuildings.Count == 0;

                    case VehicleInfo.VehicleType.Train:
                        return m_trainCargoBuildings.Count == 0;

                    default:
                        return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Reset all data structures.
        /// </summary>
        public static void Reload()
        {
            m_planeCargoBuildings.Clear();
            m_shipCargoBuildings.Clear();
            m_trainCargoBuildings.Clear();

            for (ushort buildingId = 0; buildingId < BuildingManager.MAX_BUILDING_COUNT; buildingId++)
            {
                RegisterCargoBuilding(buildingId);
            }
        }

        public static void RegisterCargoBuilding(ushort buildingId)
        {
            if ((BuildingManager.instance.m_buildings.m_buffer[buildingId].m_flags & Building.Flags.Created) == Building.Flags.None)
            {
                return;
            }

            var info = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info;
            if (info.GetAI() is CargoStationAI cargoStationAI)
            {
                RegisterCargoBuilding(buildingId, info.GetSubService());
                RegisterCargoBuilding(buildingId, cargoStationAI.m_transportInfo.m_netSubService);
            }
        }

        public static void DeregisterCargoBuilding(ushort buildingId)
        {
            if (m_planeCargoBuildings.Contains(buildingId))
            {
                m_planeCargoBuildings.Remove(buildingId);
            }

            if (m_shipCargoBuildings.Contains(buildingId))
            {
                m_shipCargoBuildings.Remove(buildingId);
            }

            if (m_trainCargoBuildings.Contains(buildingId))
            {
                m_trainCargoBuildings.Remove(buildingId);
            }

            Logger.Log($"OutsideConnectionInfo: Deregistering B{buildingId} as cargo building, planeCargoBuildingCount={m_planeCargoBuildings.Count}, shipCargoBuildingCount={m_shipCargoBuildings.Count}, trainCargoBuildingCount={m_trainCargoBuildings.Count}");
        }

        private static void RegisterCargoBuilding(ushort buildingId, ItemClass.SubService subService)
        {
            switch (subService)
            {
                case ItemClass.SubService.PublicTransportPlane:
                    if (!m_planeCargoBuildings.Contains(buildingId))
                    {
                        m_planeCargoBuildings.Add(buildingId);
                        Logger.Log($"OutsideConnectionInfo: Registering B{buildingId} as plane cargo building, count={m_planeCargoBuildings.Count}");
                    }

                    break;

                case ItemClass.SubService.PublicTransportShip:
                    if (!m_shipCargoBuildings.Contains(buildingId))
                    {
                        m_shipCargoBuildings.Add(buildingId);
                        Logger.Log($"OutsideConnectionInfo: Registering B{buildingId} as ship cargo building, count={m_shipCargoBuildings.Count}");
                    }

                    break;

                case ItemClass.SubService.PublicTransportTrain:
                    if (!m_trainCargoBuildings.Contains(buildingId))
                    {
                        m_trainCargoBuildings.Add(buildingId);
                        Logger.Log($"OutsideConnectionInfo: Registering B{buildingId} as train cargo building, count={m_trainCargoBuildings.Count}");
                    }

                    break;

                default:
                    break;
            }
        }
    }
}
