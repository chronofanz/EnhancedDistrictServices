using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnhancedDistrictServices
{
    public class OfferTracker
    {
        private struct Offer : IComparable<Offer>
        {
            public ushort Building;
            public float Distance;
            public int SubIndex;

            public int CompareTo(Offer other)
            {
                if (Distance != other.Distance)
                {
                    return Distance.CompareTo(other.Distance);
                }

                return SubIndex.CompareTo(other.SubIndex);
            }
        }

        private readonly List<Offer> m_offers = new List<Offer>();

        public int Count => m_offers.Count;

        public int RoadConnectionCount
        {
            get
            {
                int count = 0;
                for (int offerIndex = 0; offerIndex < m_offers.Count; offerIndex++)
                {
                    if (OutsideConnectionInfo.IsOutsideRoadConnection(m_offers[offerIndex].Building))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void AddOffer(ushort building, float distance, int subIndex)
        {
            m_offers.Add(new Offer
            {
                Building = building,
                Distance = distance,
                SubIndex = subIndex
            });

            m_offers.Sort();
        }

        public void Clear()
        {
            m_offers.Clear();
        }

        public int GetSubIndex(int offerIndex)
        {
            return m_offers[offerIndex].SubIndex;
        }
    }
}
