using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

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
        public int GlobalOutsideConnectionIntensity = 15;

        private static readonly string m_id = "EnhancedDistrictServices_v2";

        private class Datav2Binder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                return typeof(Datav2);
            }
        }

        public string Id
        {
            get
            {
                return m_id;
            }
        }

        public static bool TryLoadData(EnhancedDistrictServicesSerializableData loader, out Datav3 data)
        {
            if (loader.TryLoadData(m_id, new Datav2Binder(), out Datav2 target))
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

        public Datav3 Upgrade()
        {
            Logger.Log("Datav2::Upgrade");

            // Default settings for v3 data.
            var defaultInputBuildingToAllLocalAreas = new bool[BuildingManager.MAX_BUILDING_COUNT];
            for (int b = 0; b < defaultInputBuildingToAllLocalAreas.Length; b++)
            {
                defaultInputBuildingToAllLocalAreas[b] = true;
            }

            var defaultInputBuildingToOutsideConnections = new bool[BuildingManager.MAX_BUILDING_COUNT];
            for (int b = 0; b < defaultInputBuildingToOutsideConnections.Length; b++)
            {
                defaultInputBuildingToOutsideConnections[b] = true;
            }

            var defaultInputBuildingToDistrictServiced = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

            // Note that input data is only relevant to supply buildings.
            // The only previous input restriction we had in v2 was from the building to building serviced data.
            // We're going to try and create default settings for supply buildings based on these input restrictions.
            var buildingInSeen = new HashSet<int>();
            for (int b = 0; b < BuildingToBuildingServiced.Length; b++)
            {
                if (BuildingToBuildingServiced[b]?.Count > 0)
                {
                    // But previously, the understanding was that if a supply chain out restriction was placed, then 
                    // no other districts would be serviced ...
                    this.BuildingToAllLocalAreas[b] = false;
                    this.BuildingToOutsideConnections[b] = false;
                    this.BuildingToDistrictServiced[b] = null;

                    for (int i = 0; i < BuildingToBuildingServiced[b].Count; i++)
                    {
                        var buildingIn = BuildingToBuildingServiced[b][i];
                        if (!buildingInSeen.Contains(buildingIn))
                        {
                            buildingInSeen.Add(buildingIn);

                            Logger.Log($"Datav2::Upgrade: building {buildingIn} has input restriction ...");
                            defaultInputBuildingToAllLocalAreas[buildingIn] = false;
                            defaultInputBuildingToOutsideConnections[buildingIn] = false;
                        }
                    }
                }
            }

            return new Datav3
            {
                InputBuildingToAllLocalAreas = defaultInputBuildingToAllLocalAreas,
                InputBuildingToOutsideConnections = defaultInputBuildingToOutsideConnections,
                InputBuildingToDistrictServiced = defaultInputBuildingToDistrictServiced,

                OutputBuildingToAllLocalAreas = this.BuildingToAllLocalAreas,
                OutputBuildingToOutsideConnections = this.BuildingToOutsideConnections,
                OutputBuildingToDistrictServiced = this.BuildingToDistrictServiced,

                BuildingToInternalSupplyBuffer = this.BuildingToInternalSupplyBuffer,
                BuildingToBuildingServiced = this.BuildingToBuildingServiced,
                GlobalOutsideConnectionIntensity = this.GlobalOutsideConnectionIntensity
            };
        }
    }
}
