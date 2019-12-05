using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Each building/citizen/segment may simultaneously belong to a district and/or a park.
    /// </summary>
    public struct DistrictPark : IComparable<DistrictPark>
    {
        /// <summary>
        /// Max number of districts + parks.
        /// </summary>
        public const int MAX_DISTRICT_PARK_COUNT = DistrictManager.MAX_DISTRICT_COUNT + DistrictManager.MAX_DISTRICT_COUNT;

        public byte District;
        public byte Park;

        /// <summary>
        /// Constructs a DistrictPark from the given district.
        /// </summary>
        /// <param name="district"></param>
        /// <returns></returns>
        public static DistrictPark FromDistrict(byte district)
        {
            return new DistrictPark
            {
                District = district
            };
        }

        /// <summary>
        /// Constructs a DistrictPark from the given park.
        /// </summary>
        /// <param name="park"></param>
        /// <returns></returns>
        public static DistrictPark FromPark(byte park)
        {
            return new DistrictPark
            {
                Park = park
            };
        }

        /// <summary>
        /// Returns the district and/or park covering the given position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static DistrictPark FromPosition(Vector3 position)
        {
            return new DistrictPark
            {
                District = DistrictManager.instance.GetDistrict(position),
                Park = DistrictManager.instance.GetPark(position)
            };
        }

        /// <summary>
        /// Lists all current districts and parks, sorted by alphabetical order.
        /// </summary>
        /// <returns></returns>
        public static List<DistrictPark> GetAllDistrictParks()
        {
            var dps = new List<DistrictPark>();

            for (byte district = 1; district < DistrictManager.MAX_DISTRICT_COUNT; district++)
            {
                if ((DistrictManager.instance.m_districts.m_buffer[district].m_flags & global::District.Flags.Created) != 0)
                {
                    dps.Add(new DistrictPark
                    {
                        District = district
                    });
                }
            }

            for (byte park = 1; park < DistrictManager.MAX_DISTRICT_COUNT; park++)
            {
                if ((DistrictManager.instance.m_parks.m_buffer[park].m_flags & global::DistrictPark.Flags.Created) != 0)
                {
                    dps.Add(new DistrictPark
                    {
                        Park = park
                    });
                }
            }

            dps.Sort();
            return dps;
        }

        /// <summary>
        /// Returns true if this struct does not refer to a valid district or park.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return District == 0 && Park == 0;
            }
        }

        /// <summary>
        /// The name of the district and/or park.
        /// </summary>
        public string Name
        {
            get
            {
                if (District != 0 && Park != 0)
                {
                    var districtName = DistrictManager.instance.GetDistrictName(District);
                    var parkName = DistrictManager.instance.GetParkName(Park);
                    return $"{districtName}/{parkName}";
                }
                else if (District != 0)
                {
                    var districtName = DistrictManager.instance.GetDistrictName(District);
                    return $"{districtName}";
                }
                else if (Park != 0)
                {
                    var parkName = DistrictManager.instance.GetParkName(Park);
                    return $"{parkName}";
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Returns true if the other struct has either a district and/or park in common with this one.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsServedBy(DistrictPark other)
        {
            return District == other.District || Park == other.Park;
        }

        /// <summary>
        /// Returns true if this object "is served by" one of the elements in the given collection.
        /// </summary>
        /// <param name="districtParks"></param>
        /// <returns></returns>
        public bool IsServedBy(IEnumerable<DistrictPark> districtParks)
        {
            if (districtParks == null)
            {
                return false;
            }

            foreach (var districtPark in districtParks)
            {
                if (IsServedBy(districtPark))
                {
                    return true;
                }
            }

            return false;
        }

        #region Equality/Comparison

        /// <summary>
        /// For sorting ... we do this by name ...
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(DistrictPark other)
        {
            return Name.CompareTo(other.Name);
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is DistrictPark other)
            {
                return District == other.District && Park == other.Park;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Hash code.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return District + Park;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// The lower 8 bytes contains the district.  The upper 8 bytes contains the park.
        /// </summary>
        /// <param name="districtPark"></param>
        /// <returns></returns>
        public static DistrictPark FromSerializedInt(int districtPark)
        {
            return new DistrictPark
            {
                District = (byte)(districtPark & 255),
                Park = (byte)(districtPark >> 8)
            };
        }

        /// <summary>
        /// Packs this struct as an int.
        /// </summary>
        /// <returns></returns>
        public int ToSerializedInt()
        {
            return ((int)District) | (((int)Park) << 8);
        }

        #endregion
    }
}
