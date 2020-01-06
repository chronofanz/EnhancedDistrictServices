using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EnhancedDistrictServices.Serialization
{
    [Serializable]
    public class Vehiclesv1
    {
        public bool[] BuildingUseDefaultVehicles = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public List<string>[] BuildingToVehicles = new List<string>[BuildingManager.MAX_BUILDING_COUNT];

        private static readonly string m_id = "EnhancedDistrictVehicles_v1";

        private class Vehiclesv1Binder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                return typeof(Vehiclesv1);
            }
        }

        public string Id
        {
            get
            {
                return m_id;
            }
        }

        public static bool TryLoadData(EnhancedDistrictVehiclesSerializableData loader, out Vehiclesv1 data)
        {
            if (loader.TryLoadData(m_id, new Vehiclesv1Binder(), out Vehiclesv1 target))
            {
                if (target != null)
                {
                    data = target;
                    return true;
                }
                else
                {
                    data = null;
                    return false;
                }
            }
            else
            {
                data = null;
                return false;
            }
        }
    }
}
