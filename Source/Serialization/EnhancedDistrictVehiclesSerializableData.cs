using ICities;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace EnhancedDistrictServices.Serialization
{
    /// <summary>
    /// The game code automatically calls OnLoadData and OnSaveData on classes that extend 
    /// SerializableDataExtensionBase.
    /// </summary>
    public class EnhancedDistrictVehiclesSerializableData : SerializableDataExtensionBase
    {
        public override void OnLoadData()
        {
            base.OnLoadData();

            if (managers.loading.currentMode == AppMode.Game)
            {
                try
                {
                    // Always try to load the latest version if possible.
                    if (Vehiclesv1.TryLoadData(this, out Vehiclesv1 data))
                    {
                        VehicleManagerMod.LoadData(data);
                    }
                    else
                    {
                        VehicleManagerMod.Clear();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }
        }

        public override void OnSaveData()
        {
            base.OnSaveData();

            if (managers.loading.currentMode == AppMode.Game)
            {
                var data = VehicleManagerMod.SaveData();
                this.SaveData(data.Id, data);
            }
        }

        /// <summary>
        /// Helper method called by Datav* classes to load data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="binder"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool TryLoadData<T>(string id, SerializationBinder binder, out T target) where T : class
        {
            if (!serializableDataManager.EnumerateData().Contains(id))
            {
                target = null;
                return false;
            }

            Logger.Log($"EnhancedDistrictVehiclesSerializableData::LoadData: version {id}");
            var data = serializableDataManager.LoadData(id);

            var memStream = new MemoryStream();
            memStream.Write(data, 0, data.Length);
            memStream.Position = 0;

            var binaryFormatter = new BinaryFormatter();
            if (binder != null)
            {
                binaryFormatter.Binder = binder;
            }

            try
            {
                target = (T)binaryFormatter.Deserialize(memStream);
                if (target == null)
                {
                    Logger.LogWarning($"EnhancedDistrictVehiclesSerializableData::LoadData: Data failed to load version {id}");
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

        /// <summary>
        /// Helper method called by Datav* classes to save their data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="target"></param>
        public void SaveData<T>(string id, T target)
        {
            Logger.Log($"EnhancedDistrictVehiclesSerializableData::SaveData: version {id}");

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
