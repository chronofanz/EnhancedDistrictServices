﻿using EnhancedDistrictServices;
using ICities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace CitiesMod
{
    /// <summary>
    /// The game code automatically calls OnLoadData and OnSaveData on classes that extend 
    /// SerializableDataExtensionBase.
    /// </summary>
    public class EnhancedDistrictServicesSerializableData : SerializableDataExtensionBase
    {
        /// <summary>
        /// Data that we are serializing to the save game file.
        /// </summary>
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
                if (this.LoadData(EnhancedDistrictServicesId, out Data data))
                {
                    Logger.Log("EnhancedDistrictServicesSerializableData::OnLoadData: Loading data ...");
                    Constraints.LoadData(data);
                }
            }
        }

        public override void OnSaveData()
        {
            base.OnSaveData();

            if (managers.loading.currentMode == AppMode.Game)
            {
                Logger.Log("EnhancedDistrictServicesSerializableData::OnSaveData: Saving data ...");

                var data = Constraints.SaveData();
                this.SaveData(EnhancedDistrictServicesId, data);
            }
        }

        private bool LoadData<T>(string id, out T target) where T : class
        {
            if (!serializableDataManager.EnumerateData().Contains(id))
            {
                Logger.Log($"EnhancedDistrictServicesSerializableData::LoadData: Data does not contain data with id {id}");
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
                    Logger.LogWarning($"EnhancedDistrictServicesSerializableData::LoadData: Data failed to load with id {id}");
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
