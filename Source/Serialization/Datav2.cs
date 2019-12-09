using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnhancedDistrictServices.Serialization
{
    [Serializable]
    public class Datav2
    {
        public bool[] BuildingToAllLocalAreas = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public bool[] BuildingToOutsideConnections = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public int[] BuildingToInternalSupplyBuffer = new int[BuildingManager.MAX_BUILDING_COUNT];
        public List<int>[] BuildingToDistrictServiced = new List<int>[BuildingManager.MAX_BUILDING_COUNT];
        public List<int>[] BuildingToBuildingServiced = new List<int>[BuildingManager.MAX_BUILDING_COUNT];
        public int GlobalOutsideConnectionIntensity = 5;

        private static readonly string m_id = "EnhancedDistrictServices_v2";

        public string Id
        {
            get
            {
                return m_id;
            }
        }

        public static bool TryLoadData(EnhancedDistrictServicesSerializableData loader, out Datav2 data)
        {
            if (loader.TryLoadData(m_id, null, out Datav2 target))
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
    }
}
