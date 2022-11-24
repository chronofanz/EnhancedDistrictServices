using ColossalFramework;
using HarmonyLib;
using System;
using UnityEngine;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Purpose: Prevent overloading network with vehicles ...
    /// </summary>
    [HarmonyPatch(typeof(ResidentAI))]
    [HarmonyPatch("FindHospital")]
    public class ResidentAIFindHospitalPatch
    {
        /// <summary>
        /// Don't kill off that many people ...
        /// </summary>
        public static bool Prefix(uint citizenID, ushort sourceBuilding, TransferManager.TransferReason reason, ref bool __result)
        {
            __result = FindHospital(citizenID, sourceBuilding, reason);
            return false;
        }

        private static bool FindHospital(uint citizenID, ushort sourceBuilding, TransferManager.TransferReason reason)
        {
            if (reason == TransferManager.TransferReason.Dead)
            {
                if (Singleton<UnlockManager>.instance.Unlocked(UnlockManager.Feature.DeathCare))
                    return true;
                Singleton<CitizenManager>.instance.ReleaseCitizen(citizenID);
                return false;
            }
            if (Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.HealthCare))
            {
                BuildingManager instance1 = Singleton<BuildingManager>.instance;
                DistrictManager instance2 = Singleton<DistrictManager>.instance;
                Vector3 position = instance1.m_buildings.m_buffer[(int)sourceBuilding].m_position;
                byte district = instance2.GetDistrict(position);
                DistrictPolicies.Services servicePolicies = instance2.m_districts.m_buffer[(int)district].m_servicePolicies;
                TransferManager.TransferOffer offer = new TransferManager.TransferOffer();
                offer.Priority = 6;
                offer.Citizen = citizenID;
                offer.Position = position;
                offer.Amount = 1;
                bool flag = false;
                if (Singleton<CitizenManager>.exists && (UnityEngine.Object)Singleton<CitizenManager>.instance != (UnityEngine.Object)null && Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].m_health >= (byte)40 && (IsChild(citizenID) || IsSenior(citizenID)))
                {
                    FastList<ushort> serviceBuildings = Singleton<BuildingManager>.instance.GetServiceBuildings(ItemClass.Service.HealthCare);
                    for (int i = 0; i < serviceBuildings.m_size; ++i)
                    {
                        BuildingInfo info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)serviceBuildings[i]].Info;
                        if (info != null)
                        {
                            if (IsChild(citizenID) && info.m_class.m_level == ItemClass.Level.Level4)
                            {
                                reason = TransferManager.TransferReason.ChildCare;
                                flag = true;
                            }
                            else if (IsSenior(citizenID) && info.m_class.m_level == ItemClass.Level.Level5)
                            {
                                reason = TransferManager.TransferReason.ElderCare;
                                flag = true;
                            }
                        }
                    }
                }
                if (flag) // pachang: don't randomize ... && Singleton<SimulationManager>.instance.m_randomizer.Int32(2U) == 0)
                {
                    offer.Active = true;
                    Singleton<TransferManager>.instance.AddIncomingOffer(reason, offer);
                }
                else if ((servicePolicies & DistrictPolicies.Services.HelicopterPriority) != DistrictPolicies.Services.None)
                {
                    instance2.m_districts.m_buffer[(int)district].m_servicePoliciesEffect |= DistrictPolicies.Services.HelicopterPriority;
                    offer.Active = false;
                    Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Sick2, offer);
                }
                else if ((instance1.m_buildings.m_buffer[(int)sourceBuilding].m_flags & Building.Flags.RoadAccessFailed) != Building.Flags.None || Singleton<SimulationManager>.instance.m_randomizer.Int32(20U) == 0)
                {
                    offer.Active = false;
                    Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Sick2, offer);
                }
                else
                {
                    offer.Active = Singleton<SimulationManager>.instance.m_randomizer.Int32(2U) == 0;
                    Singleton<TransferManager>.instance.AddOutgoingOffer(reason, offer);
                }

                return true;
            }

            Singleton<CitizenManager>.instance.ReleaseCitizen(citizenID);
            return false;
        }

        private static bool IsChild(uint citizenID) => Citizen.GetAgeGroup(Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].Age) == Citizen.AgeGroup.Child || Citizen.GetAgeGroup(Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].Age) == Citizen.AgeGroup.Teen;
        private static bool IsSenior(uint citizenID) => Citizen.GetAgeGroup(Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].Age) == Citizen.AgeGroup.Senior;

        private static bool UnitHasChild(uint unitID)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            return IsChild(instance.m_units.m_buffer[unitID].m_citizen0) || IsChild(instance.m_units.m_buffer[unitID].m_citizen1) || IsChild(instance.m_units.m_buffer[unitID].m_citizen2) || IsChild(instance.m_units.m_buffer[unitID].m_citizen3) || IsChild(instance.m_units.m_buffer[unitID].m_citizen4);
        }

    }
}
