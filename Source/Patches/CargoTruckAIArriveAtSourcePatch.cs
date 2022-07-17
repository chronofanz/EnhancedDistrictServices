using ColossalFramework;
using HarmonyLib;
using System;
using UnityEngine;

namespace EnhancedDistrictServices
{
    [HarmonyPatch(typeof(CargoTruckAI))]
    [HarmonyPatch("ArriveAtSource")]
    [HarmonyPatch(new Type[] { typeof(ushort), typeof(Vehicle) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref })]
    public class CargoTruckAIArriveAtSourcePatch
    {
        public static bool Prefix(ushort vehicleID, ref Vehicle data)
        {
            if ((data.m_flags & Vehicle.Flags.GoingBack) != 0)
            {
                // Apparently this flag controls whether excess goods are transferred back to source or not ...
                data.m_flags |= Vehicle.Flags.TransferToSource;
            }

            return true;
        }
    }
}
