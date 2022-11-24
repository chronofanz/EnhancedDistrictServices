using ColossalFramework;
using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using static TransferManager;

namespace EnhancedDistrictServices
{
    public static class OfferTracker
    {
        /// <summary>
        /// The private inner class that writes logs that are output by this mod, as well as the game itself, to the 
        /// log file.
        /// </summary>
        private class MyLogger : MonoBehaviour
        {
            private static readonly string m_logFilename = Path.Combine(Application.dataPath, "EDSOfferTracker.csv");
            private readonly StreamWriter m_logFile = null;

            public MyLogger()
            {
                m_logFile = File.CreateText(m_logFilename);
                m_logFile.AutoFlush = true;
            }

            public void WriteHeader(params string[] args)
            {
                lock (m_logFile)
                {
                    m_logFile.WriteLine(string.Join(",", args));
                }
            }

            public void WriteRecord(
                string @event,
                string offerID,
                InstanceType instanceType,
                string homeID,
                byte isLocalPark,
                bool outside,
                int amount,
                TransferManager.TransferReason material,
                int priority,
                bool exclude,
                bool active)
            {
                var now = DateTime.Now;
                lock (m_logFile)
                {
                    m_logFile.WriteLine($"{now},{@event},{offerID},{instanceType},{homeID},{isLocalPark},{outside},{amount},{material},{priority},{exclude},{active}");
                }
            }
        }

        /// <summary>
        /// Singleton instance of MyLogger.
        /// </summary>
        private static readonly MyLogger m_instance;

        static OfferTracker()
        {
            m_instance = new MyLogger();
            m_instance.WriteHeader("DateTime", "Event", "OfferID", "InstanceType", "HomeID", "IsLocalPark", "Outside", "Amount", "Material", "Priority", "Exclude", "Active");
        }

        /// <summary>
        /// Logs outgoing offer
        /// </summary>
        /// <param name="msg"></param>
        [Conditional("VERBOSE")]
        public static void LogEvent(string @event, ref TransferManager.TransferOffer offer, TransferManager.TransferReason material)
        {
            var offerID = GetOfferID(ref offer);
            var instanceType = offer.m_object.Type;
            var homeID = GetHomeID(ref offer);
            var isLocalPark = offer.m_isLocalPark;
            var outside = TransferManagerInfo.IsOutsideOffer(ref offer);

            var amount = offer.Amount;
            //var material = material;
            var priority = offer.Priority;
            var exclude = offer.Exclude;
            var active = offer.Active;

            m_instance.WriteRecord(
                @event: @event,
                offerID: offerID,
                instanceType: instanceType,
                homeID: homeID,
                isLocalPark: isLocalPark,
                outside: outside,

                amount: amount,
                material: material,
                priority: priority,
                exclude: exclude,
                active: active);
        }

        private static string GetOfferID(ref TransferManager.TransferOffer offer)
        {

            if (offer.NetSegment != 0)
            {
                return $"S{offer.NetSegment}";
            }

            if (offer.Vehicle != 0)
            {
                return $"V{offer.Vehicle}";
            }

            if (offer.Citizen != 0)
            {
                return $"C{offer.Citizen}";
            }

            if (offer.Building != 0)
            {
                return $"B{offer.Building}";
            }

            if (offer.Park != 0)
            {
                return $"P{offer.Park}";
            }

            return $"NULL";
        }

        private static string GetHomeID(ref TransferManager.TransferOffer offer)
        {

            if (offer.Vehicle != 0)
            {
                var homeBuildingId = VehicleManager.instance.m_vehicles.m_buffer[offer.Vehicle].m_sourceBuilding;
                return $"B{homeBuildingId}";
            }

            if (offer.Citizen != 0)
            {
                var homeBuildingId = Singleton<CitizenManager>.instance.m_citizens.m_buffer[offer.Citizen].m_homeBuilding;
                return $"B{homeBuildingId}";
            }

            return $"NULL";
        }
    }
}
