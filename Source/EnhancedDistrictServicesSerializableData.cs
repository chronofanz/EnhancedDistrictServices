using EnhancedDistrictServices;
using ICities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace CitiesMod
{
    public class EnhancedDistrictServicesSerializableData : SerializableDataExtensionBase
    {
        [Serializable]
        public class Data
        {
            public bool[] BuildingToAllLocalAreas = new bool[BuildingManager.MAX_BUILDING_COUNT];
            public bool[] BuildingToOutsideConnections = new bool[BuildingManager.MAX_BUILDING_COUNT];
            public List<int>[] BuildingToDistrictServiced = new List<int>[BuildingManager.MAX_BUILDING_COUNT];
            public List<int>[] BuildingToBuildingServiced = new List<int>[BuildingManager.MAX_BUILDING_COUNT];
        }

        public static readonly string EnhancedDistrictServicesId = "EnhancedDistrictServices_vchronofanz";

        public override void OnLoadData()
        {
            base.OnLoadData();

            if (managers.loading.currentMode == AppMode.Game)
            {
                Logger.Log("EnhancedDistrictServicesSerializableData::OnLoadData: Loading data ...");

                Constraints.Clear();

                if (this.LoadData(EnhancedDistrictServicesId, out Data data))
                {
                    for (int buildingId = 0; buildingId < BuildingManager.MAX_BUILDING_COUNT; buildingId++)
                    {
                        var restrictions = data.BuildingToAllLocalAreas[buildingId];
                        Constraints.SetAllLocalAreas(buildingId, restrictions, false);
                    }

                    for (int buildingId = 0; buildingId < BuildingManager.MAX_BUILDING_COUNT; buildingId++)
                    {
                        var restrictions = data.BuildingToOutsideConnections[buildingId];
                        Constraints.SetAllOutsideConnections(buildingId, restrictions, false);
                    }

                    for (int buildingId = 0; buildingId < BuildingManager.MAX_BUILDING_COUNT; buildingId++)
                    {
                        var restrictions = data.BuildingToBuildingServiced[buildingId];
                        
                        if (restrictions != null)
                        {
                            foreach (var destination in restrictions)
                            {
                                Constraints.AddSupplyChainConnection(buildingId, destination);
                            }
                        }                        
                    }

                    for (int buildingId = 0; buildingId < BuildingManager.MAX_BUILDING_COUNT; buildingId++)
                    {
                        var restrictions = data.BuildingToDistrictServiced[buildingId];

                        if (restrictions != null)
                        {
                            foreach (var district in restrictions)
                            {
                                Constraints.AddDistrictServiced(buildingId, district);
                            }
                        }
                    }
                }
            }
        }

        public override void OnSaveData()
        {
            base.OnSaveData();

            if (managers.loading.currentMode == AppMode.Game)
            {
                Logger.Log("EnhancedDistrictServicesSerializableData::OnSaveData: Saving data ...");

                var data = new Data
                {
                    BuildingToAllLocalAreas = Constraints.BuildingToAllLocalAreas,
                    BuildingToOutsideConnections = Constraints.BuildingToOutsideConnections,
                    BuildingToBuildingServiced = Constraints.SupplyDestinations,
                    BuildingToDistrictServiced = Constraints.BuildingToDistrictServiced
                };

                this.SaveData(EnhancedDistrictServicesId, data);
            }
        }

        public bool LoadData<T>(string id, out T target) where T : class
        {
            if (!serializableDataManager.EnumerateData().Contains(id))
            {
                Logger.Log($"SerializableDataWithHelperMethods::LoadData: Data does not contain data with id {id}");
                target = null;
                return false;
            }

            var data = serializableDataManager.LoadData(id);

            var memStream = new MemoryStream();
            memStream.Write(data, 0, data.Length);
            memStream.Position = 0;

            var binaryFormatter = new BinaryFormatter();
            try
            {
                target = (T)binaryFormatter.Deserialize(memStream);
                if (target == null)
                {
                    Logger.LogWarning($"SerializableDataWithHelperMethods::LoadData: Data failed to load with id {id}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                target = null;
                return false;
            }
            finally
            {
                memStream.Close();
            }
        }

        private void SaveData<T>(string id, T target)
        {
            var binaryFormatter = new BinaryFormatter();
            var memStream = new MemoryStream();
            try
            {
                binaryFormatter.Serialize(memStream, target);
                serializableDataManager.SaveData(id, memStream.ToArray());
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            finally
            {
                memStream.Close();
            }
        }
    }
}
