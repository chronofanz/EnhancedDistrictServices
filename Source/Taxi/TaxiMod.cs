using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnhancedDistrictServices
{
    public static class TaxiMod
    {
        private static List<ushort> m_taxiBuildings = new List<ushort>();

        public static void AddTaxiBuilding(ushort building)
        {
            if (BuildingManager.instance.m_buildings.m_buffer[building].Info?.GetSubService() == ItemClass.SubService.PublicTransportTaxi)
            {
                if (!m_taxiBuildings.Contains(building))
                {
                    m_taxiBuildings.Add(building);
                }
            }
        }

        public static void RemoveTaxiBuilding(ushort building)
        {
            m_taxiBuildings.Remove(building);
        }

        // public static int LastCitizenId { get; set; }
        // public static Vector3 LastCitizenPosition { get; set; }

        public static bool CanUseTaxis(Vector3 position)
        {
            var districtPark = DistrictPark.FromPosition(position);

            // Now see whether any taxi buildings serve this position.
            for (int i = 0; i < m_taxiBuildings.Count; i++)
            {
                var buildingId = m_taxiBuildings[i];
                if (BuildingManager.instance.m_buildings.m_buffer[buildingId].Info?.GetSubService() == ItemClass.SubService.PublicTransportTaxi)
                {
                    var districtParkServed = Constraints.DistrictParkServiced((ushort)buildingId);
                    if (districtPark.IsServedBy(districtParkServed))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
