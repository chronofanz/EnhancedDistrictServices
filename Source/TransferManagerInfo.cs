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
            if (offer.Vehicle != 0)
            {
                return VehicleManager.instance.m_vehicles.m_buffer[offer.Vehicle].m_sourceBuilding;
            }

            if (offer.Citizen != 0)
            {
                return CitizenManager.instance.m_citizens.m_buffer[offer.Citizen].m_homeBuilding;
            }

            if (offer.Building != 0)
            {
                return offer.Building;
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
        /// Returns the input type of the building, used to determine which GUI elements are shown in the main panel.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static InputType GetBuildingInputType(int building)
        {
            if (building == 0)
            {
                return InputType.NONE;
            }

            var result = InputType.NONE;

            // The only building type for which we will not show an outgoing tab is coal power plants.
            var info = BuildingManager.instance.m_buildings.m_buffer[building].Info;
            if (!(info?.GetService() == ItemClass.Service.Electricity && info?.GetAI() is PowerPlantAI))
            {
                result |= InputType.OUTGOING;
            }

            if (TransferManagerInfo.IsSupplyChainBuilding(building))
            {
                result |= InputType.SUPPLY_CHAIN;

                if (!(info?.GetAI() is ExtractingFacilityAI))
                {
                    result |= InputType.INCOMING;
                }
            }

            return result;
        }

        public static string GetBuildingInputTypeText(int building)
        {
            var inputType = GetBuildingInputType(building);

            var txtItems = new List<string>();
            if ((inputType & InputType.SUPPLY_CHAIN) == InputType.NONE)
            {
                txtItems.Add($"Services");
            }
            else
            {
                txtItems.Add($"Supply Chain");

                if ((inputType & InputType.INCOMING) != InputType.NONE)
                {
                    txtItems.Add($"Incoming");
                }

                if ((inputType & InputType.OUTGOING) != InputType.NONE)
                {
                    txtItems.Add($"Outgoing");
                }
            }

            return $"Building Type: {string.Join(", ", txtItems.ToArray())}";
        }

        /// <summary>
        /// Returns the district of the offer's home building or segment.
        /// Should return 0 if the offer does not originate from a district.
        /// </summary>
        /// <returns></returns>
        public static DistrictPark GetDistrictPark(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)
        {
            if (offer.NetSegment != 0)
            {
                var position = NetManager.instance.m_segments.m_buffer[offer.NetSegment].m_middlePosition;
                return DistrictPark.FromPosition(position);
            }
            else if ((material == TransferManager.TransferReason.Sick || material == TransferManager.TransferReason.Taxi) && offer.Citizen != 0)
            {
                return DistrictPark.FromPosition(offer.Position);
            }
            else
            {
                return GetDistrictPark(GetHomeBuilding(ref offer));
            }
        }

        /// <summary>
        /// Returns the district and/or park of the building.
        /// Should return 0 if the building is not in a district and/or park.
        /// </summary>
        /// <returns></returns>
        public static DistrictPark GetDistrictPark(int building)
        {
            if (building != 0)
            {
                var position = BuildingManager.instance.m_buildings.m_buffer[building].m_position;
                return DistrictPark.FromPosition(position);
            }
            else
            {
                return new DistrictPark();
            }
        }

        /// <summary>
        /// Returns a descriptive text indicating the home district and/or park of the specified building.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetDistrictParkText(ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            var districtPark = GetDistrictPark(building);
            if (!districtPark.IsEmpty)
            {
                return $"Home district: {districtPark.Name}";
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
        public static string GetOutputDistrictsServedText(ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }


            var txtItems = new List<string>();
            txtItems.Add($"<<Districts served>>");

            if (Constraints.OutputAllLocalAreas(building))
            {
                txtItems.Add($"All local areas");
            }
            else if (Constraints.OutputDistrictParkServiced(building) == null || Constraints.OutputDistrictParkServiced(building).Count == 0)
            {
                return $"<<No districts served!>>";
            }
            else
            {
                var districtParkNames = Constraints.OutputDistrictParkServiced(building)
                    .Select(dp => dp.Name)
                    .OrderBy(s => s);

                foreach (var districtParkName in districtParkNames)
                {
                    txtItems.Add(districtParkName);
                }
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
            if (buildingInfo.GetAI() is OutsideConnectionAI)
            {
                if (buildingInfo.GetService() == ItemClass.Service.Road)
                {
                    return $"Service: OutsideConnection (Road)";
                }
                else
                {
                    return $"Service: OutsideConnection ({subService})";
                }
            }
            else if (service == ItemClass.Service.PlayerIndustry)
            {
                var resource = GetSupplyBuildingOutputMaterial(building);
                if (resource != TransferManager.TransferReason.None)
                {
                    return $"Service: {service} ({resource})";
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
        public static string GetSupplyBuildingDestinationsText(ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            var txtItems = new List<string>();
            txtItems.Add($"<<Supply Chain Shipments To>>");

            if (Constraints.OutputAllLocalAreas(building))
            {
                txtItems.Add($"All local areas");
            }

            if (Constraints.OutputOutsideConnections(building))
            {
                txtItems.Add($"All outside connections");
            }

            if (Constraints.OutputAllLocalAreas(building))
            {
                return string.Join("\n", txtItems.ToArray());
            }

            // Next, list output building names
            var supply = Constraints.SupplyDestinations(building);
            if (supply?.Count > 0)
            {
                var buildingNames = supply
                    .Select(b => $"{GetBuildingName(b)} ({b})")
                    .OrderBy(s => s);

                foreach (var buildingName in buildingNames)
                {
                    txtItems.Add(buildingName);
                }
            }

            var districts = Constraints.OutputDistrictParkServiced(building);
            if (districts?.Count > 0)
            {
                // Then add district names
                var districtParkNames = districts
                    .Select(dp => dp.Name)
                    .OrderBy(s => s);

                foreach (var districtParkName in districtParkNames)
                {
                    txtItems.Add($"{districtParkName} (DISTRICT)");
                }
            }

            if (txtItems.Count == 1)
            {
                return $"<<WARNING: No supply chain shipments output!>>";
            }
            else
            {
                return string.Join("\n", txtItems.ToArray());
            }
        }

        /// <summary>
        /// Returns a descriptive text indicating the supply chain source buildings that the given building
        /// will receive shipments from.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetSupplyBuildingSourcesText(ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            var txtItems = new List<string>();
            txtItems.Add($"<<Supply Chain Shipments From>>");

            if (Constraints.InputAllLocalAreas(building))
            {
                txtItems.Add($"All local areas");
            }

            if (Constraints.InputOutsideConnections(building))
            {
                txtItems.Add($"All outside connections");
            }

            if (Constraints.InputAllLocalAreas(building))
            {
                return string.Join("\n", txtItems.ToArray());
            }

            // Next, list input building names
            var supply = Constraints.SupplySources(building);
            if (supply?.Count > 0)
            {
                var buildingNames = supply
                    .Select(b => $"{GetBuildingName(b)} ({b})")
                    .OrderBy(s => s);

                foreach (var buildingName in buildingNames)
                {
                    txtItems.Add(buildingName);
                }
            }

            // Then add district names
            var districts = Constraints.InputDistrictParkServiced(building);
            if (districts?.Count > 0)
            {
                var districtParkNames = districts
                    .Select(dp => dp.Name)
                    .OrderBy(s => s);

                foreach (var districtParkName in districtParkNames)
                {
                    txtItems.Add($"{districtParkName} (DISTRICT)");
                }
            }

            if (txtItems.Count == 1)
            {
                return $"<<WARNING: No supply chain shipments accepted!>>";
            }
            else
            {
                return string.Join("\n", txtItems.ToArray());
            }
        }

        /// <summary>
        /// Returns a descriptive text describing problems with this supply chain building ...
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetSupplyBuildingProblemsText(ushort building)
        {
            if (building == 0 || !TransferManagerInfo.IsSupplyChainBuilding(building))
            {
                return string.Empty;
            }

            bool FindSourceBuilding(TransferManager.TransferReason material)
            {
                if (material == TransferManager.TransferReason.None)
                {
                    return true;
                }

                // Assume for now that the outside connection can supply the building with the materials it needs.
                if (Constraints.InputOutsideConnections(building))
                {
                    return true;
                }

                for (ushort buildingIn = 1; buildingIn < BuildingManager.MAX_BUILDING_COUNT; buildingIn++)
                {
                    if (!TransferManagerInfo.IsSupplyChainBuilding(building))
                    {
                        continue;
                    }

                    if (GetSupplyBuildingOutputMaterial(buildingIn) == material)
                    {
                        // Check if a supply link exists ...
                        if (Constraints.SupplyDestinations(buildingIn)?.Count > 0 && Constraints.SupplyDestinations(buildingIn).Contains(building))
                        {
                            return true;
                        }

                        var requestDistrictPark = TransferManagerInfo.GetDistrictPark(building);
                        var responseDistrictPark = TransferManagerInfo.GetDistrictPark(buildingIn);

                        if (!Constraints.InputAllLocalAreas(building))
                        {
                            var requestDistrictParksServed = Constraints.InputDistrictParkServiced(building);
                            if (!responseDistrictPark.IsServedBy(requestDistrictParksServed))
                            {
                                continue;
                            }
                        }

                        if (Constraints.OutputAllLocalAreas(buildingIn))
                        {
                            return true;
                        }
                        else
                        {
                            // The call to TransferManagerInfo.GetDistrict applies to offers that are come from buildings, service 
                            // vehicles, citizens, AND segments.  The latter needs to be considered for road maintenance.
                            var responseDistrictParksServed = Constraints.OutputDistrictParkServiced(buildingIn);
                            if (requestDistrictPark.IsServedBy(responseDistrictParksServed))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            // Detect if we do have an other building that can supply materials to this building.
            List<TransferManager.TransferReason> notFound = new List<TransferManager.TransferReason>();
            switch (BuildingManager.instance.m_buildings.m_buffer[building].Info?.GetAI())
            {
                case ProcessingFacilityAI processingFacilityAI:
                    if (!FindSourceBuilding(processingFacilityAI.m_inputResource1))
                    {
                        notFound.Add(processingFacilityAI.m_inputResource1);
                    }

                    if (!FindSourceBuilding(processingFacilityAI.m_inputResource2))
                    {
                        notFound.Add(processingFacilityAI.m_inputResource2);
                    }

                    if (!FindSourceBuilding(processingFacilityAI.m_inputResource3))
                    {
                        notFound.Add(processingFacilityAI.m_inputResource3);
                    }

                    if (!FindSourceBuilding(processingFacilityAI.m_inputResource4))
                    {
                        notFound.Add(processingFacilityAI.m_inputResource4);
                    }

                    break;

                default:
                    break;
            }

            if (notFound.Count > 0)
            {
                return string.Join(",", notFound.Select(x => x.ToString()).ToArray());
            }
            else
            {
                return string.Empty;
            }
        }

        public static int GetSupplyBuildingAmount(int buildingId)
        {
            switch (BuildingManager.instance.m_buildings.m_buffer[buildingId].Info?.GetAI())
            {
                case ExtractingFacilityAI extractingFacilityAI:
                    int outputBufferSize1 = extractingFacilityAI.GetOutputBufferSize((ushort)buildingId, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId]);
                    return (int)(((double)Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId].m_customBuffer1) * 100 / (outputBufferSize1));

                case ProcessingFacilityAI processingFacilityAI:
                    int outputBufferSize2 = processingFacilityAI.GetOutputBufferSize((ushort)buildingId, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId]);
                    return (int)(((double)Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId].m_customBuffer1) * 100 / (outputBufferSize2));

                case WarehouseAI warehouseAI:
                    return (int)(((double)Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId].m_customBuffer1) * 100 * 100 / warehouseAI.m_storageCapacity);
            }

            return 0;
        }

        public static TransferManager.TransferReason GetSupplyBuildingOutputMaterial(ushort buildingId)
        {
            var ai = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info?.GetAI();
            switch (ai)
            {
                case ExtractingFacilityAI extractingFacilityAI:
                    return extractingFacilityAI.m_outputResource;

                case ProcessingFacilityAI processingFacilityAI:
                    return processingFacilityAI.m_outputResource;

                case WarehouseAI warehouseAI:
                    return warehouseAI.GetTransferReason(buildingId, ref BuildingManager.instance.m_buildings.m_buffer[buildingId]);

                default:
                    return TransferManager.TransferReason.None;
            }
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
                    case ItemClass.Service.Beautification:
                        return info.GetAI() is MaintenanceDepotAI;

                    case ItemClass.Service.Disaster:
                    case ItemClass.Service.Education:
                    case ItemClass.Service.FireDepartment:
                    case ItemClass.Service.Garbage:
                    case ItemClass.Service.HealthCare:
                    case ItemClass.Service.PoliceDepartment:
                        return !(
                            info.GetAI() is DummyBuildingAI ||
                            info.GetAI() is LibraryAI ||
                            info.GetAI() is SaunaAI);

                    case ItemClass.Service.Electricity:
                        return (
                            info.GetAI() is PowerPlantAI);

                    case ItemClass.Service.PlayerEducation:
                        return !(
                            info.GetSubService() == ItemClass.SubService.PlayerEducationLiberalArts ||
                            info.GetSubService() == ItemClass.SubService.PlayerEducationTradeSchool ||
                            info.GetSubService() == ItemClass.SubService.PlayerEducationUniversity);

                    case ItemClass.Service.PlayerIndustry:
                        return !(
                            info.GetAI() is AuxiliaryBuildingAI ||
                            info.GetAI() is DummyBuildingAI ||
                            info.GetAI() is MainIndustryBuildingAI);

                    case ItemClass.Service.PublicTransport:
                        return (
                            (Settings.enableSelectOutsideConnection && info.GetAI() is OutsideConnectionAI) ||
                            info.GetSubService() == ItemClass.SubService.PublicTransportPost ||
                            info.GetSubService() == ItemClass.SubService.PublicTransportTaxi);

                    case ItemClass.Service.Road:
                        return (
                            info.GetAI() is MaintenanceDepotAI ||
                            info.GetAI() is OutsideConnectionAI ||
                            info.GetAI() is SnowDumpAI);

                    case ItemClass.Service.Water:
                        return (
                            info.GetAI() is WaterFacilityAI waterFacilityAI &&
                            waterFacilityAI.m_pumpingVehicles > 0);

                    default:
                        return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the building corresponds to an outside connection.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static bool IsOutsideBuilding(int building)
        {
            return building != 0 && Singleton<BuildingManager>.instance.m_buildings.m_buffer[building].Info.m_buildingAI is OutsideConnectionAI;
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
                    case ItemClass.Service.Electricity:
                        return (
                            info.GetAI() is PowerPlantAI);

                    case ItemClass.Service.PlayerIndustry:
                        return !(
                            info.GetAI() is AuxiliaryBuildingAI ||
                            info.GetAI() is DummyBuildingAI ||
                            info.GetAI() is MainIndustryBuildingAI);

                    case ItemClass.Service.PublicTransport:
                        return (
                            (Settings.enableSelectOutsideConnection && info.GetAI() is OutsideConnectionAI) ||
                            info.GetSubService() == ItemClass.SubService.PublicTransportPost);

                    case ItemClass.Service.Road:
                        return (
                            info.GetAI() is OutsideConnectionAI);

                    default:
                        return false;
                }
            }

            return false;
        }

        public static bool IsValidSupplyChainLink(ushort source, ushort destination)
        {
            var sourceMaterial = GetSupplyBuildingOutputMaterial(source);
            if (sourceMaterial == TransferManager.TransferReason.None)
            {
                return false;
            }

            var info = BuildingManager.instance.m_buildings.m_buffer[destination].Info;
            if (info?.GetService() == ItemClass.Service.Electricity && info?.GetAI() is PowerPlantAI)
            {
                return sourceMaterial == TransferManager.TransferReason.Coal;
            }

            if (info?.GetService() == ItemClass.Service.PlayerIndustry && info?.GetAI() is ProcessingFacilityAI processingFacilityAI)
            {
                return
                    processingFacilityAI.m_inputResource1 == sourceMaterial ||
                    processingFacilityAI.m_inputResource2 == sourceMaterial ||
                    processingFacilityAI.m_inputResource3 == sourceMaterial ||
                    processingFacilityAI.m_inputResource4 == sourceMaterial;
            }

            if (info?.GetService() == ItemClass.Service.PlayerIndustry && info?.GetAI() is WarehouseAI warehouseAI)
            {
                return
                    GetSupplyBuildingOutputMaterial(destination) == sourceMaterial;
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

                material == TransferManager.TransferReason.ParkMaintenance ||
                material == TransferManager.TransferReason.RoadMaintenance ||
                material == TransferManager.TransferReason.Snow ||

                material == TransferManager.TransferReason.ForestFire ||
                material == TransferManager.TransferReason.Collapsed ||
                material == TransferManager.TransferReason.Collapsed2 ||
                material == TransferManager.TransferReason.Fire2 ||
                material == TransferManager.TransferReason.Sick2 ||
                material == TransferManager.TransferReason.FloodWater ||
                material == TransferManager.TransferReason.EvacuateA ||
                material == TransferManager.TransferReason.EvacuateB ||
                material == TransferManager.TransferReason.EvacuateC ||
                material == TransferManager.TransferReason.EvacuateD ||
                material == TransferManager.TransferReason.EvacuateVipA ||
                material == TransferManager.TransferReason.EvacuateVipB ||
                material == TransferManager.TransferReason.EvacuateVipC ||
                material == TransferManager.TransferReason.EvacuateVipD ||

                material == TransferManager.TransferReason.Student1 ||
                material == TransferManager.TransferReason.Student2 ||
                material == TransferManager.TransferReason.Taxi ||

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
            return IsOutsideBuilding(GetHomeBuilding(ref offer));
        }
    }
}
