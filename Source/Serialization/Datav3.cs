using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EnhancedDistrictServices.Serialization
{
    [Serializable]
    public class Datav3
    {
        public bool[] InputBuildingToAllLocalAreas = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public bool[] InputBuildingToOutsideConnections = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public List<int>[] InputBuildingToDistrictServiced = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

        public bool[] OutputBuildingToAllLocalAreas = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public bool[] OutputBuildingToOutsideConnections = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public List<int>[] OutputBuildingToDistrictServiced = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

        public int[] BuildingToInternalSupplyBuffer = new int[BuildingManager.MAX_BUILDING_COUNT];
        public List<int>[] BuildingToBuildingServiced = new List<int>[BuildingManager.MAX_BUILDING_COUNT];
        public int GlobalOutsideConnectionIntensity = 15;

        private static readonly string m_id = "EnhancedDistrictServices_v3";

        private class Datav3Binder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                return typeof(Datav3);
            }
        }

        public string Id
        {
            get
            {
                return m_id;
            }
        }

        public static bool TryLoadData(EnhancedDistrictServicesSerializableData loader, out Datav4 data)
        {
            if (loader.TryLoadData(m_id, new Datav3Binder(), out Datav3 target))
            {
                if (target != null)
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
            else
            {
                data = null;
                return false;
            }
        }

        public Datav4 Upgrade()
        {
            Logger.Log("Datav3::Upgrade");

            return new Datav4
            {
                InputBuildingToAllLocalAreas = this.InputBuildingToAllLocalAreas,
                InputBuildingToOutsideConnections = this.InputBuildingToOutsideConnections,
                InputBuildingToDistrictServiced = this.InputBuildingToDistrictServiced,

                OutputBuildingToAllLocalAreas = this.OutputBuildingToAllLocalAreas,
                OutputBuildingToOutsideConnections = this.OutputBuildingToOutsideConnections,
                OutputBuildingToDistrictServiced = this.OutputBuildingToDistrictServiced,

                BuildingToInternalSupplyBuffer = this.BuildingToInternalSupplyBuffer,
                BuildingToBuildingServiced = this.BuildingToBuildingServiced,
                GlobalOutsideConnectionIntensity = this.GlobalOutsideConnectionIntensity
            };
        }
    }
}
