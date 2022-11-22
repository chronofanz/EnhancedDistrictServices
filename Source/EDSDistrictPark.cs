using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Each building/citizen/segment may simultaneously belong to a district and/or a park.
    /// </summary>
    public struct EDSDistrictPark : IComparable<EDSDistrictPark>
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
        public static EDSDistrictPark FromDistrict(byte district)
        {
            return new EDSDistrictPark
            {
                District = district
            };
        }

        /// <summary>
        /// Constructs a DistrictPark from the given park.
        /// </summary>
        /// <param name="park"></param>
        /// <returns></returns>
        public static EDSDistrictPark FromPark(byte park)
        {
            return new EDSDistrictPark
            {
                Park = park
            };
        }

        /// <summary>
        /// Returns the district and/or park covering the given position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static EDSDistrictPark FromPosition(Vector3 position)
        {
            return new EDSDistrictPark
            {
                District = DistrictManager.instance.GetDistrict(position),
                Park = DistrictManager.instance.GetPark(position)
            };
        }

        /// <summary>
        /// Lists all current districts and parks, sorted by alphabetical order.
        /// </summary>
        /// <returns></returns>
        public static List<EDSDistrictPark> GetAllDistrictParks()
        {
            var dps = new List<EDSDistrictPark>();

            for (byte district = 1; district < DistrictManager.MAX_DISTRICT_COUNT; district++)
            {
                if ((DistrictManager.instance.m_districts.m_buffer[district].m_flags & global::District.Flags.Created) != 0)
                {
                    dps.Add(new EDSDistrictPark
                    {
                        District = district
                    });
                }
            }

            for (byte park = 1; park < DistrictManager.MAX_DISTRICT_COUNT; park++)
            {
                if ((DistrictManager.instance.m_parks.m_buffer[park].m_flags & global::DistrictPark.Flags.Created) != 0)
                {
                    dps.Add(new EDSDistrictPark
                    {
                        Park = park
                    });
                }
            }

            dps.Sort();
            return dps;
        }

        /// <summary>
        /// Returns true if this specification includes a reference to a district.
        /// </summary>
        public bool IsDistrict
        {
            get
            {
                return District != 0;
            }
        }

        /// <summary>
        /// Returns false if the district or park no longer exist.
        /// </summary>
        public bool Exists
        {
            get
            {
                if (District != 0 && (DistrictManager.instance.m_districts.m_buffer[District].m_flags & global::District.Flags.Created) == global::District.Flags.None)
                {
                    return false;
                }

                if (Park != 0 && (DistrictManager.instance.m_parks.m_buffer[Park].m_flags & global::DistrictPark.Flags.Created) == global::DistrictPark.Flags.None)
                {
                    return false;
                }

                return true;
            }
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
        /// Returns true if this specification includes a reference to a campus.
        /// </summary>
        public bool IsCampus
        {
            get
            {
                if (Park != 0)
                {
                    var type = DistrictManager.instance.m_parks.m_buffer[Park].m_parkType;
                    return
                        type == global::DistrictPark.ParkType.GenericCampus ||
                        type == global::DistrictPark.ParkType.TradeSchool ||
                        type == global::DistrictPark.ParkType.LiberalArts ||
                        type == global::DistrictPark.ParkType.University;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns true if this specification includes a reference to a campus.
        /// </summary>
        public bool IsIndustry
        {
            get
            {
                if (Park != 0)
                {
                    var type = DistrictManager.instance.m_parks.m_buffer[Park].m_parkType;
                    return
                        type == global::DistrictPark.ParkType.Industry ||
                        type == global::DistrictPark.ParkType.Farming ||
                        type == global::DistrictPark.ParkType.Forestry ||
                        type == global::DistrictPark.ParkType.Ore ||
                        type == global::DistrictPark.ParkType.Oil;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns true if this specification includes a reference to a park.
        /// </summary>
        public bool IsPark
        {
            get
            {
                if (Park != 0)
                {
                    var type = DistrictManager.instance.m_parks.m_buffer[Park].m_parkType;
                    return
                        type == global::DistrictPark.ParkType.Generic ||
                        type == global::DistrictPark.ParkType.AmusementPark ||
                        type == global::DistrictPark.ParkType.Zoo ||
                        type == global::DistrictPark.ParkType.NatureReserve;
                }
                else
                {
                    return false;
                }
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
        public bool IsServedBy(EDSDistrictPark other)
        {
            if (IsEmpty && other.IsEmpty)
            {
                return true;
            }

            return
                (District == other.District && District != 0) ||
                (Park == other.Park && Park != 0);
        }

        /// <summary>
        /// Returns true if this object "is served by" one of the elements in the given collection.
        /// </summary>
        /// <param name="districtParks"></param>
        /// <returns></returns>
        public bool IsServedBy(List<EDSDistrictPark> districtParks)
        {
            if (districtParks == null)
            {
                return false;
            }

            for (int index = 0; index < districtParks.Count; index++)
            {
                if (IsServedBy(districtParks[index]))
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
        public int CompareTo(EDSDistrictPark other)
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
            if (obj is EDSDistrictPark other)
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
        public static EDSDistrictPark FromSerializedInt(int districtPark)
        {
            return new EDSDistrictPark
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
