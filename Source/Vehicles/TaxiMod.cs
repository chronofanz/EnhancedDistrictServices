using System.Collections.Generic;
using UnityEngine;

namespace EnhancedDistrictServices
{
    public static class TaxiMod
    {
        private static List<ushort> m_taxiBuildings = new List<ushort>();

        public static void ClearTaxiBuildings()
        {
            m_taxiBuildings.Clear();
        }

        public static void RegisterTaxiBuilding(ushort building)
        {
            if (BuildingManager.instance.m_buildings.m_buffer[building].Info?.GetSubService() == ItemClass.SubService.PublicTransportTaxi)
            {
                if (!m_taxiBuildings.Contains(building))
                {
                    m_taxiBuildings.Add(building);
                }
            }
        }

        public static void DeregisterTaxiBuilding(ushort building)
        {
            m_taxiBuildings.Remove(building);
        }

        public static bool CanUseTaxis(Vector3 startPosition, Vector3 endPosition)
        {
            var startDistrictPark = DistrictPark.FromPosition(startPosition);
            var endDistrictPark = DistrictPark.FromPosition(endPosition);

            // Now see whether any taxi buildings serve this position.
            for (int i = 0; i < m_taxiBuildings.Count; i++)
            {
                var buildingId = m_taxiBuildings[i];
                if (BuildingManager.instance.m_buildings.m_buffer[buildingId].Info?.GetSubService() == ItemClass.SubService.PublicTransportTaxi)
                {
                    var districtParkServed = Constraints.OutputDistrictParkServiced((ushort)buildingId);
                    if (startDistrictPark.IsServedBy(districtParkServed) && endDistrictPark.IsServedBy(districtParkServed))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
