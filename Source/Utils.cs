using ColossalFramework.DataBinding;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Misc helper methods to help better debug the code.
    /// </summary>
    public static class Utils
    {
        public static void DisplayMessage(string str1, string str2, string str3)
        {
            try
            {
                var uiComponent = UIView.library.ShowModal("ExceptionPanel");
                if ((UnityEngine.Object)uiComponent != (UnityEngine.Object)null)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    var component = uiComponent.GetComponent<BindPropertyByKey>();
                    if (component != null)
                    {
                        component.SetProperties(TooltipHelper.Format("title", str1, "message", str2, "img", str3));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public static T FindObject<T>(string name) where T : UnityEngine.Object
        {
            var objects = Resources.FindObjectsOfTypeAll<T>();
            foreach (var o in objects)
            {
                if (o.name == name)
                {
                    return (T)o;
                }
            }

            return null;
        }

        /// <summary>
        /// Helper struct for sorting buildings by their names, to make debugging nicer.
        /// </summary>
        private struct Building : IComparable<Building>
        {
            public string Name { get; set; }
            public ushort Id { get; set; }

            public int CompareTo(Building other)
            {
                if (Name == null)
                {
                    return -1;
                }
                else if (other.Name == null)
                {
                    return +1;
                }
                else if (!string.Equals(Name, other.Name))
                {
                    return Name.CompareTo(other.Name);
                }
                else
                {
                    return Id.CompareTo(other.Id);
                }
            }
        }

        /// <summary>
        /// Lists all buildings that can be configured using ESD.
        /// </summary>
        /// <returns></returns>
        public static List<ushort> GetSupportedServiceBuildings()
        {
            var bs = new List<Building>();
            for (ushort buildingId = 0; buildingId < BuildingManager.MAX_BUILDING_COUNT; buildingId++)
            {
                if (TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
                {
                    bs.Add(new Building
                    {
                        Name = TransferManagerInfo.GetBuildingName(buildingId),
                        Id = buildingId
                    });
                }
            }

            bs.Sort();
            return bs.Select(b => b.Id).ToList();
        }

        /// <summary>
        /// Helper method for dumping the contents of an offer, for debugging purposes.
        /// </summary>
        /// <param name="offer"></param>
        /// <param name="material"></param>
        /// <returns></returns>
        public static string ToString(ref TransferManager.TransferOffer offer, TransferManager.TransferReason material)
        {
            var outsideOfferText = TransferManagerInfo.IsOutsideOffer(ref offer) ? "(O)" : "";

            if (offer.NetSegment != 0)
            {
                return $"Id=S{offer.NetSegment}, (Amt,Mat,Pri,Exc,Act)=({offer.Amount},{material},{offer.Priority},{offer.Exclude},{offer.Active})";
            }

            if (offer.Vehicle != 0)
            {
                var homeBuilding = VehicleManager.instance.m_vehicles.m_buffer[offer.Vehicle].m_sourceBuilding;
                return $"Id=V{offer.Vehicle}, Home=B{homeBuilding}{outsideOfferText}, (Amt,Mat,Pri,Exc,Act)=({offer.Amount},{material},{offer.Priority},{offer.Exclude},{offer.Active})";
            }

            if (offer.Citizen != 0)
            {
                var homeBuilding = CitizenManager.instance.m_citizens.m_buffer[offer.Citizen].m_homeBuilding;
                return $"Id=C{offer.Citizen}, Home=B{homeBuilding}{outsideOfferText}, (Amt,Mat,Pri,Exc,Act)=({offer.Amount},{material},{offer.Priority},{offer.Exclude},{offer.Active})";
            }

            if (offer.Building != 0)
            {
                return $"Id=B{offer.Building}{outsideOfferText}, (Amt,Mat,Pri,Exc,Act)=({offer.Amount},{material},{offer.Priority},{offer.Exclude},{offer.Active})";
            }

            return $"Id=0, (Amt,Mat,Pri,Exc,Act)=({offer.Amount},{material},{offer.Priority},{offer.Exclude},{offer.Active})";
        }
    }
}
