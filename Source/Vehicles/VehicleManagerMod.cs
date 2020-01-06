using ColossalFramework.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnhancedDistrictServices
{
    public static class VehicleManagerMod
    {
        /// <summary>
        /// Map of building id to bool indicating whether to use game (or other mod) logic in selecting vehicles.
        /// </summary>
        public static readonly bool[] BuildingUseDefaultVehicles = new bool[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Mapping of building id to list of supported vehicles.  Set to null if we want the game code to select the 
        /// vehicle.
        /// </summary>
        public static readonly List<int>[] BuildingToVehicles = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Mapping of name of prefab to index in prefab array.
        /// </summary>
        public static readonly Dictionary<string, int> PrefabMapping = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Mapping of index in prefab array to name of prefab.
        /// </summary>
        public static readonly Dictionary<int, string> PrefabNames = new Dictionary<int, string>();

        /// <summary>
        /// Unfortunately, the call to GetRandomVehicleInfo doesn't include a source building parameter.  We hack this
        /// by setting this field at the beginning of TransferManagerMod::StartTransfer and then resetting this field
        /// at the end of TransferManagerMod::StartTransfer.
        /// 
        /// TODO: Make this less hacky.
        /// </summary>
        public static ushort CurrentSourceBuilding = 0;

        /// <summary>
        /// Reset all data structures.
        /// </summary>
        public static void Clear()
        {
            for (ushort buildingId = 0; buildingId < BuildingManager.MAX_BUILDING_COUNT; buildingId++)
            {
                ReleaseBuilding(buildingId);
            }

            PrefabMapping.Clear();
            PrefabNames.Clear();

            int num1 = PrefabCollection<VehicleInfo>.PrefabCount();
            for (int index = 0; index < num1; ++index)
            {
                var prefab = PrefabCollection<VehicleInfo>.GetPrefab((uint)index);
                if (prefab != null && prefab.m_class.m_service != ItemClass.Service.None && prefab.m_placementStyle == ItemClass.Placement.Automatic)
                {
                    Logger.Log($"VehicleManagerMod::Clear: Loading vehicle={prefab.name}, service={prefab.m_class.m_service}, subService={prefab.m_class.m_subService}, level={prefab.m_class.m_level}, type={prefab.m_vehicleType}");
                    PrefabMapping[prefab.name] = index;
                    PrefabNames[index] = prefab.name;
                }
            }
        }

        /// <summary>
        /// Load data from given object.
        /// </summary>
        /// <param name="data"></param>
        public static void LoadData(Serialization.Vehiclesv1 data)
        {
            Logger.Log($"VehicleManagerMod::LoadData: version {data.Id}");
            Clear();

            var buildings = Utils.GetSupportedServiceBuildings();
            foreach (var building in buildings)
            {
                SetBuildingUseDefaultVehicles(building, data.BuildingUseDefaultVehicles[building]);

                foreach (var prefab in data.BuildingToVehicles[building])
                {
                    if (PrefabMapping.TryGetValue(prefab, out int prefabIndex))
                    {
                        AddCustomVehicle(building, prefabIndex);
                    }
                    else
                    {
                        Logger.LogWarning($"VehicleManagerMod::LoadData: Could not load vehicle prefab {prefab}!!!");
                    }
                }
            }
        }

        /// <summary>
        /// Saves a copy of the data in this object, for serialization.
        /// </summary>
        /// <returns></returns>
        public static Serialization.Vehiclesv1 SaveData()
        {
            List<string> Convert(List<int> prefabs)
            {
                if (prefabs == null || prefabs.Count == 0)
                {
                    return null;
                }

                return prefabs.Select(index => PrefabNames[index]).ToList();
            }

            return new Serialization.Vehiclesv1
            {
                BuildingUseDefaultVehicles = BuildingUseDefaultVehicles,
                BuildingToVehicles = BuildingToVehicles.Select(prefabs => Convert(prefabs)).ToArray()
            };
        }

        /// <summary>
        /// Called when a building is first created.
        /// </summary>
        /// <param name="buildingId"></param>
        public static void CreateBuilding(ushort buildingId)
        {
            BuildingUseDefaultVehicles[buildingId] = true;
            BuildingToVehicles[buildingId] = null;
        }

        /// <summary>
        /// Called when a building is destroyed.
        /// </summary>
        /// <param name="buildingId"></param>
        public static void ReleaseBuilding(ushort buildingId)
        {
            BuildingUseDefaultVehicles[buildingId] = true;
            BuildingToVehicles[buildingId] = null;
        }

        public static void AddCustomVehicle(int buildingId, int prefabIndex)
        {
            if (BuildingToVehicles[buildingId] == null)
            {
                BuildingToVehicles[buildingId] = new List<int>();
            }

            if (!BuildingToVehicles[buildingId].Contains(prefabIndex))
            {
                BuildingToVehicles[buildingId].Add(prefabIndex);

                var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
                var prefabName = PrefabNames[prefabIndex];
                Logger.Log($"VehicleManagerMod::AddCustomVehicle: {buildingName} ({buildingId}) => {prefabName} ...");
            }
        }

        public static void RemoveCustomVehicle(int buildingId, int prefabIndex)
        {
            if (BuildingToVehicles[buildingId] == null)
            {
                return;
            }

            if (BuildingToVehicles[buildingId].Contains(prefabIndex))
            {
                BuildingToVehicles[buildingId].Remove(prefabIndex);

                var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
                var prefabName = PrefabNames[prefabIndex];
                Logger.Log($"VehicleManagerMod::RemoveCustomVehicle: {buildingName} ({buildingId}) => {prefabName} ...");
            }

            if (BuildingToVehicles[buildingId].Count == 0)
            {
                BuildingToVehicles[buildingId] = null;
            }
        }

        public static void SetBuildingUseDefaultVehicles(int buildingId, bool status)
        {
            var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
            Logger.LogVerbose($"VehicleManagerMod::SetBuildingUseDefaultVehicles: {buildingName} ({buildingId}) => {status} ...");

            BuildingUseDefaultVehicles[buildingId] = status;
        }

        public static IEnumerable<int> GetPrefabs(ushort buildingId)
        {
            var info = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info;
            var service = info.GetService();
            var subService = info.GetSubService();

            // Special handling for player industry ... follows logic in WarehouseAI::GetTransferVehicleService.
            if (service == ItemClass.Service.PlayerIndustry)
            {
                service = ItemClass.Service.Industrial;
                subService = ItemClass.SubService.None;

                var material = TransferManagerInfo.GetSupplyBuildingOutputMaterial(buildingId);
                switch (material)
                {
                    case TransferManager.TransferReason.Oil:
                    case TransferManager.TransferReason.Petroleum:
                    case TransferManager.TransferReason.Plastics:
                        subService = ItemClass.SubService.IndustrialOil;
                        break;
                    case TransferManager.TransferReason.Ore:
                    case TransferManager.TransferReason.Coal:
                    case TransferManager.TransferReason.Glass:
                    case TransferManager.TransferReason.Metals:
                        subService = ItemClass.SubService.IndustrialOre;
                        break;
                    case TransferManager.TransferReason.Logs:
                    case TransferManager.TransferReason.Paper:
                    case TransferManager.TransferReason.PlanedTimber:
                        subService = ItemClass.SubService.IndustrialForestry;
                        break;
                    case TransferManager.TransferReason.Grain:
                    case TransferManager.TransferReason.Flours:
                        subService = ItemClass.SubService.IndustrialFarming;
                        break;
                    case TransferManager.TransferReason.Goods:
                        subService = ItemClass.SubService.IndustrialGeneric;
                        break;
                    case TransferManager.TransferReason.AnimalProducts:
                        service = ItemClass.Service.PlayerIndustry;
                        subService = ItemClass.SubService.PlayerIndustryFarming;
                        break;
                    case TransferManager.TransferReason.LuxuryProducts:
                        service = ItemClass.Service.PlayerIndustry;
                        break;
                    default:
                        if (material != TransferManager.TransferReason.Petrol)
                        {
                            if (material != TransferManager.TransferReason.Food)
                            {
                                if (material != TransferManager.TransferReason.Lumber)
                                {
                                    service = ItemClass.Service.None;
                                    break;
                                }
                                goto case TransferManager.TransferReason.Logs;
                            }
                            else
                                goto case TransferManager.TransferReason.Grain;
                        }
                        else
                            goto case TransferManager.TransferReason.Oil;
                }
            }

            // Spit out the prefab names ...
            foreach (var prefabIndex in PrefabNames.Keys)
            {
                var prefab = PrefabCollection<VehicleInfo>.GetPrefab((uint)prefabIndex);
                if (prefab.m_class.m_service == service && prefab.m_class.m_subService == subService)
                {
                    yield return prefabIndex;
                }
            }
        }

        public static VehicleInfo GetRandomVehicleInfo(
            ref Randomizer r,
            ItemClass.Service service,
            ItemClass.SubService subService,
            ItemClass.Level level)
        {
            if (CurrentSourceBuilding == 0 || BuildingUseDefaultVehicles[CurrentSourceBuilding] || BuildingToVehicles[CurrentSourceBuilding] == null || BuildingToVehicles[CurrentSourceBuilding].Count == 0)
            {
                return null;
            }

            /*
            if (service == ItemClass.Service.PlayerIndustry)
            {
                service = ItemClass.Service.Industrial;
            }

            var buildingInfo = BuildingManager.instance.m_buildings.m_buffer[CurrentSourceBuilding].Info;
            var buildingService = buildingInfo.GetService();
            var buildingSubService = buildingInfo.GetSubService();
            if (buildingService != service || buildingSubService != subService)
            {
                return null;
            }
            */

            int index = r.Int32((uint)BuildingToVehicles[CurrentSourceBuilding].Count);
            var prefab = PrefabCollection<VehicleInfo>.GetPrefab((uint)BuildingToVehicles[CurrentSourceBuilding][index]);
            Logger.LogVerbose($"VehicleManagerMod::GetRandomVehicleInfo: Selected {prefab.name}");
            return prefab;
        }

        public static VehicleInfo GetRandomVehicleInfo(
            ref Randomizer r,
            ItemClass.Service service,
            ItemClass.SubService subService,
            ItemClass.Level level,
            VehicleInfo.VehicleType type)
        {
            if (CurrentSourceBuilding == 0 || BuildingUseDefaultVehicles[CurrentSourceBuilding] || BuildingToVehicles[CurrentSourceBuilding] == null || BuildingToVehicles[CurrentSourceBuilding].Count == 0)
            {
                return null;
            }

            /*
            if (service == ItemClass.Service.PlayerIndustry)
            {
                service = ItemClass.Service.Industrial;
            }

            var buildingInfo = BuildingManager.instance.m_buildings.m_buffer[CurrentSourceBuilding].Info;
            var buildingService = buildingInfo.GetService();
            var buildingSubService = buildingInfo.GetSubService();
            if (buildingService != service || buildingSubService != subService)
            {
                return null;
            }
            */

            var vehicles = BuildingToVehicles[CurrentSourceBuilding].Where(index => PrefabCollection<VehicleInfo>.GetPrefab((uint)index).m_vehicleType == type).ToArray();
            if (vehicles.Length == 0)
            {
                return null;
            }

            int index1 = r.Int32((uint)vehicles.Length);
            var prefab = PrefabCollection<VehicleInfo>.GetPrefab((uint)vehicles[index1]);
            Logger.LogVerbose($"VehicleManagerMod::GetRandomVehicleInfo: Selected {prefab.name}");
            return prefab;
        }
    }
}
