using ColossalFramework;
using System.Collections.Generic;
using System.Linq;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Misc helper methods for classifying buildings and offers.
    /// </summary>
    public static class TransferManagerInfo
    {
        /// <summary>
        /// Returns the building id associated with the offer, if specified.
        /// If a citizen is associated with the offer, returns the citizen's home building id.
        /// If a service vehicle is associated with the offer, returns that vehicle's service building.
        /// </summary>
        /// <param name="offer"></param>
        /// <returns></returns>
        public static ushort GetHomeBuilding(ref TransferManager.TransferOffer offer)
        {
            if (offer.Building != 0)
            {
                return offer.Building;
            }

            if (offer.Citizen != 0)
            {
                return Singleton<CitizenManager>.instance.m_citizens.m_buffer[offer.Citizen].m_homeBuilding;
            }

            if (offer.Vehicle != 0)
            {
                return VehicleManager.instance.m_vehicles.m_buffer[offer.Vehicle].m_sourceBuilding;
            }

            return 0;
        }

        /// <summary>
        /// Returns the name of the building.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetBuildingName(int building)
        {
            return Singleton<BuildingManager>.instance.GetBuildingName((ushort)building, InstanceID.Empty);
        }

        /// <summary>
        /// Returns the district of the building.
        /// Should return 0 if thebuilding is not in a district.
        /// </summary>
        /// <returns></returns>
        public static byte GetDistrict(int building)
        {
            if (building != 0)
            {
                var position = BuildingManager.instance.m_buildings.m_buffer[building].m_position;
                return DistrictManager.instance.GetDistrict(position);
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns a descriptive text indicating the home district of the specified building.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetDistrictText(ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            var district = GetDistrict(building);
            if (district != 0)
            {
                var districtName = DistrictManager.instance.GetDistrictName((int)district);
                return $"Home district: {districtName}";
            }
            else
            {
                return $"Home district: (Not in a district)";
            }
        }

        /// <summary>
        /// Returns a descriptive text indicating the districts that are served by the specified building.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetDistrictsServedText(ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            var txtItems = new List<string>();
            txtItems.Add($"<<DistrictsServed>>");

            if (Constraints.SupplyDestinations(building)?.Count > 0)
            {
                txtItems.Add($"Supply chain restricted, only serves specified Supply Chain Out buildings!");
                return string.Join("\n", txtItems.ToArray());
            }

            bool addedText = false;
            if (Constraints.OutsideConnections(building))
            {              
                txtItems.Add($"All outside connections served");
                addedText = true;
            }

            if (Constraints.AllLocalAreas(building))
            {
                txtItems.Add($"All local areas served");
                addedText = true;
            }
            else if (Constraints.DistrictServiced(building)?.Count > 0)
            {
                var districtNames = Constraints.DistrictServiced(building)
                    .Select(d => DistrictManager.instance.GetDistrictName(d))
                    .OrderBy(s => s);

                foreach (var districtName in districtNames)
                {
                    txtItems.Add(districtName);
                }

                addedText = true;
            }

            if (!addedText)
            {
                txtItems.Add($"No districts served!");
            }

            return string.Join("\n", txtItems.ToArray());
        }

        /// <summary>
        /// Returns a descriptive text about the type of service provided by the building.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetServicesText(ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            var buildingInfo = BuildingManager.instance.m_buildings.m_buffer[building].Info;
            var service = buildingInfo.GetService();
            var subService = buildingInfo.GetSubService();
            if (service == ItemClass.Service.PlayerIndustry)
            {
                if (buildingInfo.GetAI() is ExtractingFacilityAI extractingFacilityAI)
                {
                    return $"Service: {service} ({extractingFacilityAI.m_outputResource})";
                }
                else if (buildingInfo.GetAI() is ProcessingFacilityAI processingFacilityAI)
                {
                    return $"Service: {service} ({processingFacilityAI.m_outputResource})";
                }
                else if (buildingInfo.GetAI() is WarehouseAI warehouseAI)
                {
                    return $"Service: {service} ({warehouseAI.m_storageType})";
                }
                else
                {
                    return $"Service: {service}";
                }
            }
            else
            {
                return $"Service: {service}";
            }
        }

        /// <summary>
        /// Returns a descriptive text indicating the supply chain destination buildings that the given building
        /// will ship to.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetSupplyDestinationsText(ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            var txtItems = new List<string>();
            txtItems.Add($"<<Supply Chain Shipments Only To>>");

            var buildingNames = Constraints.SupplyDestinations(building)
                .Select(b => TransferManagerInfo.GetBuildingName(b))
                .OrderBy(s => s);

            foreach (var buildingName in buildingNames)
            {
                txtItems.Add(buildingName);
            }

            return string.Join("\n", txtItems.ToArray());
        }

        /// <summary>
        /// Returns a descriptive text indicating the supply chain source buildings that the given building
        /// will receive shipments from.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetSupplySourcesText(ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            var txtItems = new List<string>();
            txtItems.Add($"<<Supply Chain Shipments Only From>>");

            var buildingNames = Constraints.SupplySources(building)
                .Select(b => TransferManagerInfo.GetBuildingName(b))
                .OrderBy(s => s);

            foreach (var buildingName in buildingNames)
            {
                txtItems.Add(buildingName);
            }

            return string.Join("\n", txtItems.ToArray());
        }

        /// <summary>
        /// Returns true if the building's service is a supported district-only service.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static bool IsDistrictServicesBuilding(int building)
        {
            if (building == 0)
            {
                return false;
            }

            var instance = Singleton<BuildingManager>.instance;

            if ((instance.m_buildings.m_buffer[building].m_flags & Building.Flags.Created) != Building.Flags.None)
            {
                var info = instance.m_buildings.m_buffer[building].Info;
                switch (info?.GetService())
                {
                    case ItemClass.Service.Education:
                    case ItemClass.Service.FireDepartment:
                    case ItemClass.Service.Garbage:
                    case ItemClass.Service.HealthCare:
                    case ItemClass.Service.PoliceDepartment:
                        return !(
                            info.GetAI() is HelicopterDepotAI ||
                            info.GetAI() is LibraryAI ||
                            info.GetAI() is SaunaAI);

                    case ItemClass.Service.PlayerEducation:
                        return !(
                            info.GetSubService() == ItemClass.SubService.PlayerEducationLiberalArts ||
                            info.GetSubService() == ItemClass.SubService.PlayerEducationTradeSchool ||
                            info.GetSubService() == ItemClass.SubService.PlayerEducationUniversity);

                    case ItemClass.Service.PublicTransport:
                        return info.GetSubService() == ItemClass.SubService.PublicTransportPost;

                    case ItemClass.Service.PlayerIndustry:
                        return !(
                            info.GetAI() is MainIndustryBuildingAI ||
                            info.GetAI() is AuxiliaryBuildingAI);

                    default:
                        return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the building's service is a supported supply chain service.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static bool IsSupplyChainBuilding(int building)
        {
            if (building == 0)
            {
                return false;
            }

            var instance = Singleton<BuildingManager>.instance;

            if ((instance.m_buildings.m_buffer[building].m_flags & Building.Flags.Created) != Building.Flags.None)
            {
                var info = instance.m_buildings.m_buffer[building].Info;
                switch (info?.GetService())
                {
                    case ItemClass.Service.PublicTransport:
                        return info.GetSubService() == ItemClass.SubService.PublicTransportPost;

                    case ItemClass.Service.PlayerIndustry:
                        return true;

                    default:
                        return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the offer concerns a city service that should be restricted within a district.
        /// </summary>
        /// <param name="material"></param>
        /// <returns></returns>
        public static bool IsDistrictOffer(TransferManager.TransferReason material)
        {
            return
                material == TransferManager.TransferReason.Garbage ||
                material == TransferManager.TransferReason.Crime ||
                material == TransferManager.TransferReason.CriminalMove ||
                material == TransferManager.TransferReason.Sick ||
                material == TransferManager.TransferReason.Dead ||
                material == TransferManager.TransferReason.Fire ||
                material == TransferManager.TransferReason.Mail ||

                material == TransferManager.TransferReason.Student1 ||
                material == TransferManager.TransferReason.Student2 ||

                material == TransferManager.TransferReason.UnsortedMail;
        }

        /// <summary>
        /// Returns true if the offer concerns a supported supply chain material.
        /// </summary>
        /// <param name="material"></param>
        /// <returns></returns>
        public static bool IsSupplyChainOffer(TransferManager.TransferReason material)
        {
            return
                material == TransferManager.TransferReason.Coal ||
                material == TransferManager.TransferReason.Food ||
                material == TransferManager.TransferReason.Petrol ||
                material == TransferManager.TransferReason.Lumber ||

                material == TransferManager.TransferReason.Logs ||
                material == TransferManager.TransferReason.Paper ||
                material == TransferManager.TransferReason.PlanedTimber ||

                material == TransferManager.TransferReason.Grain ||
                material == TransferManager.TransferReason.Flours ||
                material == TransferManager.TransferReason.AnimalProducts ||

                material == TransferManager.TransferReason.Oil ||
                material == TransferManager.TransferReason.Petroleum ||
                material == TransferManager.TransferReason.Plastics ||

                material == TransferManager.TransferReason.Ore ||
                material == TransferManager.TransferReason.Glass ||
                material == TransferManager.TransferReason.Metals ||

                material == TransferManager.TransferReason.LuxuryProducts ||
                material == TransferManager.TransferReason.SortedMail;
        }
        
        /// <summary>
        /// Returns true if the offer was given from an outside connection.
        /// </summary>
        /// <param name="offer"></param>
        /// <returns></returns>
        public static bool IsOutsideOffer(ref TransferManager.TransferOffer offer)
        {
            return offer.Building != 0 && Singleton<BuildingManager>.instance.m_buildings.m_buffer[offer.Building].Info.m_buildingAI is OutsideConnectionAI;
        }
    }
}
