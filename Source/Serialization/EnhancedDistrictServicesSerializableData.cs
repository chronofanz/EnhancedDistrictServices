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
    public class EnhancedDistrictServicesSerializableData : SerializableDataExtensionBase
    {
        public override void OnLoadData()
        {
            base.OnLoadData();

            if (managers.loading.currentMode == AppMode.Game)
            {
                try
                {
                    Datav3 data;

                    // Always to try the latest version if possible.
                    if (Datav3.TryLoadData(this, out data))
                    {
                        Constraints.LoadData(data);
                    }
                    else if (Datav2.TryLoadData(this, out data))
                    {
                        Constraints.LoadData(data);
                    }
                    else if (Datav1.TryLoadData(this, out data))
                    {
                        Constraints.LoadData(data);
                    }

                    // Update Taxi buildings cache ...
                    // TODO, FIXME: Make this less hacky ...
                    var buildings = Utils.GetSupportedServiceBuildings();
                    foreach (var building in buildings)
                    {
                        TaxiMod.RegisterTaxiBuilding(building);
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
                var data = Constraints.SaveData();
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

            Logger.Log($"EnhancedDistrictServicesSerializableData::LoadData: version {id}");
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
                    Logger.LogWarning($"EnhancedDistrictServicesSerializableData::LoadData: Data failed to load version {id}");
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
            Logger.Log($"EnhancedDistrictServicesSerializableData::SaveData: version {id}");

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
