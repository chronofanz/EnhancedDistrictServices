using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using System;
using UnityEngine;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Purpose: Prevent overloading network with vehicles ...
    /// </summary>
    [HarmonyPatch(typeof(ResidentialBuildingAI))]
    [HarmonyPatch("GetHomeReason")]
    public class ResidentialBuildingAIGetHomeReasonPatch
    {
        private static readonly MyRandomizer m_randomizer = new MyRandomizer(1);

        /// <summary>
        /// Don't kill off that many people ...
        /// </summary>
        public static bool Prefix(ResidentialBuildingAI __instance, ushort buildingID, ref Building buildingData, ref Randomizer r, ref TransferManager.TransferReason __result)
        {
            __result = GetHomeReason(__instance, ref buildingData);
            return false;
        }

        private static TransferManager.TransferReason GetHomeReason(ResidentialBuildingAI instance, ref Building buildingData)
        {
            if ((instance.m_info.m_class.isResidentialLowGeneric ? 1 : (instance.m_info.m_class.isResidentialLowEco ? 1 : 0)) == (m_randomizer.Int32(10U) != 0 ? 1 : 0))
            {
                switch (buildingData.m_level)
                {
                    case 0:
                        return TransferManager.TransferReason.Family0;
                    case 1:
                        return TransferManager.TransferReason.Family1;
                    case 2:
                        return TransferManager.TransferReason.Family2;
                    case 3:
                        return TransferManager.TransferReason.Family3;
                    case 4:
                        return TransferManager.TransferReason.Family3;
                    default:
                        return TransferManager.TransferReason.Family0;
                }
            }
            else if (m_randomizer.Int32(2U) == 0)
            {
                switch (m_randomizer.Int32(5U))
                {
                    case 0:
                        return TransferManager.TransferReason.Single0;
                    case 1:
                        return TransferManager.TransferReason.Single1;
                    case 2:
                        return TransferManager.TransferReason.Single2;
                    case 3:
                        return TransferManager.TransferReason.Single3;
                    case 4:
                        return TransferManager.TransferReason.Single3;
                    default:
                        return TransferManager.TransferReason.Single0;
                }
            }
            else
            {
                switch (m_randomizer.Int32(5U))
                {
                    case 0:
                        return TransferManager.TransferReason.Single0B;
                    case 1:
                        return TransferManager.TransferReason.Single1B;
                    case 2:
                        return TransferManager.TransferReason.Single2B;
                    case 3:
                        return TransferManager.TransferReason.Single3B;
                    case 4:
                        return TransferManager.TransferReason.Single3B;
                    default:
                        return TransferManager.TransferReason.Single0B;
                }
            }
        }
    }
}
