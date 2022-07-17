using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnhancedDistrictServices
{
    /*
    [HarmonyPatch(typeof(HumanAI))]
    [HarmonyPatch("StartMoving")]
    [HarmonyPatch(new Type[] { typeof(uint), typeof(Citizen), typeof(ushort), typeof(TransferManager.TransferOffer) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal })]
    public class HumanAIStartMovingPatch
    {
        public static void Postfix(
            uint citizenID,
            ref Citizen data,
            ushort sourceBuilding,
            TransferManager.TransferOffer offer,
            ref bool __result)
        {
            if (__result == false)
            {
                Logger.LogWarning($"HumanAI::StartMoving: failed to move C{citizenID}, B{sourceBuilding} to B{offer.Building}");
            }
        }
    }
    */
}
