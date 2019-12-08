using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace EnhancedDistrictServices.Serialization
{
    [Serializable]
    public class Datav1
    {
        public bool[] BuildingToAllLocalAreas = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public bool[] BuildingToOutsideConnections = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public List<int>[] BuildingToDistrictServiced = new List<int>[BuildingManager.MAX_BUILDING_COUNT];
        public List<int>[] BuildingToBuildingServiced = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

        private static readonly string m_id = "EnhancedDistrictServices_vchronofanz";

        private class Datav1Binder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                return typeof(Datav1);
            }
        }

        public string Id
        {
            get
            {
                return m_id;
            }
        }

        public static bool TryLoadData(EnhancedDistrictServicesSerializableData loader, out Datav2 data)
        {
            if (loader.TryLoadData(m_id, new Datav1Binder(), out Datav1 target))
            {
                data = target.Upgrade();
                return true;
            }
            else
            {
                data = null;
                return false;
            }
        }

        public Datav2 Upgrade()
        {
            var defaultBuildingToInteralSupplyBuffer = new int[BuildingManager.MAX_BUILDING_COUNT];
            for (int b = 0; b < defaultBuildingToInteralSupplyBuffer.Length; b++)
            {
                defaultBuildingToInteralSupplyBuffer[b] = 100;
            }

            return new Datav2
            {
                BuildingToAllLocalAreas = this.BuildingToAllLocalAreas,
                BuildingToOutsideConnections = this.BuildingToOutsideConnections,
                BuildingToInternalSupplyBuffer = defaultBuildingToInteralSupplyBuffer,
                BuildingToDistrictServiced = this.BuildingToDistrictServiced,
                BuildingToBuildingServiced = this.BuildingToBuildingServiced
            };
        }
    }
}
