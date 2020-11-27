namespace EnhancedDistrictServices
{
    public static class CopyPaste
    {
        public static ushort BuildingTemplate = 0;

        public static bool CopyPolicyTo(ushort building)
        {
            Constraints.ReleaseBuilding(building);

            Constraints.SetInternalSupplyReserve(building, Constraints.InternalSupplyBuffer(BuildingTemplate));

            Constraints.SetAllInputLocalAreas(building, Constraints.InputAllLocalAreas(BuildingTemplate));
            Constraints.SetAllOutputLocalAreas(building, Constraints.OutputAllLocalAreas(BuildingTemplate));

            Constraints.SetAllInputOutsideConnections(building, Constraints.AllInputOutsideConnections(BuildingTemplate));
            Constraints.SetInputOutsideConnectionIds(building, Constraints.InputOutsideConnectionIds(BuildingTemplate));
            Constraints.SetAllOutputOutsideConnections(building, Constraints.AllOutputOutsideConnections(BuildingTemplate));
            Constraints.SetOutputOutsideConnectionIds(building, Constraints.OutputOutsideConnectionIds(BuildingTemplate));

            var inputDistrictsServed = Constraints.InputDistrictParkServiced(BuildingTemplate);
            if (inputDistrictsServed != null)
            {
                foreach (var districtPark in inputDistrictsServed)
                {
                    Constraints.AddInputDistrictParkServiced(building, districtPark);
                }
            }

            var outputDistrictsServed = Constraints.OutputDistrictParkServiced(BuildingTemplate);
            if (outputDistrictsServed != null)
            {
                foreach (var districtPark in outputDistrictsServed)
                {
                    Constraints.AddOutputDistrictParkServiced(building, districtPark);
                }
            }

            bool copySucceeded = true;

            var supplySources = Constraints.SupplySources(BuildingTemplate);
            for (int index = 0; index < supplySources?.Count; index++)
            {
                if (TransferManagerInfo.IsValidSupplyChainLink((ushort)supplySources[index], building))
                {
                    Constraints.AddSupplyChainConnection((ushort)supplySources[index], building);
                }
                else
                {
                    copySucceeded = false;
                }
            }

            var supplyDestinations = Constraints.SupplyDestinations(BuildingTemplate);
            for (int index = 0; index < supplyDestinations?.Count; index++)
            {
                if (TransferManagerInfo.IsValidSupplyChainLink(building, (ushort)supplyDestinations[index]))
                {
                    Constraints.AddSupplyChainConnection(building, (ushort)supplyDestinations[index]);
                }
                else
                {
                    copySucceeded = false;
                }
            }

            VehicleManagerMod.ReleaseBuilding(building);
            VehicleManagerMod.SetBuildingUseDefaultVehicles(building, VehicleManagerMod.BuildingUseDefaultVehicles[BuildingTemplate]);
            if (VehicleManagerMod.BuildingToVehicles[BuildingTemplate] != null && VehicleManagerMod.BuildingToVehicles[BuildingTemplate].Count > 0)
            {
                foreach (var prefabIndex in VehicleManagerMod.BuildingToVehicles[BuildingTemplate])
                {
                    VehicleManagerMod.AddCustomVehicle(building, prefabIndex);
                }                    
            }

            return copySucceeded;
        }
    }
}
