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
    [HarmonyPatch("UpdateHealth")]
    public class ResidentAIUpdateHealthPatch
    {
        /// <summary>
        /// Don't kill off that many people ...
        /// </summary>
        public static bool Prefix(uint citizenID, ref Citizen data, ref bool __result)
        {
            if (data.m_homeBuilding == (ushort)0)
            {
                __result = false;
                return false;
            }
                
            int num1 = 20;
            BuildingManager instance1 = Singleton<BuildingManager>.instance;
            BuildingInfo info = instance1.m_buildings.m_buffer[(int)data.m_homeBuilding].Info;
            Vector3 position = instance1.m_buildings.m_buffer[(int)data.m_homeBuilding].m_position;
            DistrictManager instance2 = Singleton<DistrictManager>.instance;
            byte district = instance2.GetDistrict(position);
            DistrictPolicies.Services servicePolicies = instance2.m_districts.m_buffer[(int)district].m_servicePolicies;
            DistrictPolicies.CityPlanning planningPolicies = instance2.m_districts.m_buffer[(int)district].m_cityPlanningPolicies;
            if ((servicePolicies & DistrictPolicies.Services.SmokingBan) != DistrictPolicies.Services.None)
                num1 += 10;
            if (data.Age >= 180 && (planningPolicies & DistrictPolicies.CityPlanning.AntiSlip) != DistrictPolicies.CityPlanning.None)
                num1 += 10;
            int amount;
            info.m_buildingAI.GetMaterialAmount(data.m_homeBuilding, ref instance1.m_buildings.m_buffer[(int)data.m_homeBuilding], TransferManager.TransferReason.Garbage, out amount, out int _);
            int num2 = amount / 1000;
            if (num2 <= 2)
                num1 += 12;
            else if (num2 >= 4)
                num1 -= num2 - 3;
            int healthCareRequirement = Citizen.GetHealthCareRequirement(Citizen.GetAgePhase(data.EducationLevel, data.Age));
            int local1;
            int total1;
            Singleton<ImmaterialResourceManager>.instance.CheckResource(ImmaterialResourceManager.Resource.HealthCare, position, out local1, out total1);
            int local2;
            int total2;
            Singleton<ImmaterialResourceManager>.instance.CheckResource(ImmaterialResourceManager.Resource.ElderCare, position, out local2, out total2);
            int local3;
            int total3;
            Singleton<ImmaterialResourceManager>.instance.CheckResource(ImmaterialResourceManager.Resource.ChildCare, position, out local3, out total3);
            if (healthCareRequirement != 0)
            {
                if (local1 != 0)
                    num1 += ImmaterialResourceManager.CalculateResourceEffect(local1, healthCareRequirement, 500, 20, 40);
                if (total1 != 0)
                    num1 += ImmaterialResourceManager.CalculateResourceEffect(total1, healthCareRequirement >> 1, 250, 5, 20);
                CitizenManager instance3 = Singleton<CitizenManager>.instance;
                if (UnitHasChild(data.GetContainingUnit(citizenID, instance1.m_buildings.m_buffer[(int)data.m_homeBuilding].m_citizenUnits, CitizenUnit.Flags.Home)))
                {
                    if (local3 != 0)
                    {
                        int resourceEffect = ImmaterialResourceManager.CalculateResourceEffect(local3, healthCareRequirement, 500, 20, 40);
                        num1 += !IsChild(citizenID) ? resourceEffect / 10 : resourceEffect;
                    }
                    if (total3 != 0)
                    {
                        int resourceEffect = ImmaterialResourceManager.CalculateResourceEffect(total3, healthCareRequirement >> 1, 250, 5, 20);
                        num1 += !IsChild(citizenID) ? resourceEffect / 10 : resourceEffect;
                    }
                }
                if (IsSenior(citizenID))
                {
                    if (local2 != 0)
                        num1 += ImmaterialResourceManager.CalculateResourceEffect(local2, healthCareRequirement, 500, 30, 60);
                    if (total2 != 0)
                        num1 += ImmaterialResourceManager.CalculateResourceEffect(total2, healthCareRequirement >> 1, 250, 10, 40);
                }
            }
            int local4;
            Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(ImmaterialResourceManager.Resource.NoisePollution, position, out local4);
            if (local4 != 0)
            {
                if (info.m_class.m_subService == ItemClass.SubService.ResidentialLowEco || info.m_class.m_subService == ItemClass.SubService.ResidentialHighEco)
                    num1 -= local4 * 150 / (int)byte.MaxValue;
                else
                    num1 -= local4 * 100 / (int)byte.MaxValue;
            }
            int local5;
            Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(ImmaterialResourceManager.Resource.CrimeRate, position, out local5);
            if (local5 > 3)
            {
                if (local5 <= 30)
                    num1 -= 2;
                else if (local5 <= 70)
                    num1 -= 5;
                else
                    num1 -= 15;
            }
            bool water;
            bool sewage;
            byte waterPollution;
            Singleton<WaterManager>.instance.CheckWater(position, out water, out sewage, out waterPollution);
            if (water)
            {
                num1 += 12;
                data.NoWater = 0;
            }
            else
            {
                int noWater = data.NoWater;
                if (noWater < 2)
                    data.NoWater = noWater + 1;
                else
                    num1 -= 5;
            }
            if (sewage)
            {
                num1 += 12;
                data.NoSewage = 0;
            }
            else
            {
                int noSewage = data.NoSewage;
                if (noSewage < 2)
                    data.NoSewage = noSewage + 1;
                else
                    num1 -= 5;
            }
            int num3 = waterPollution >= (byte)35 ? num1 - ((int)waterPollution * 2 - 35) : num1 - (int)waterPollution;
            byte groundPollution;
            Singleton<NaturalResourceManager>.instance.CheckPollution(position, out groundPollution);
            if (groundPollution != (byte)0)
            {
                if (info.m_class.m_subService == ItemClass.SubService.ResidentialLowEco || info.m_class.m_subService == ItemClass.SubService.ResidentialHighEco)
                    num3 -= (int)groundPollution * 200 / (int)byte.MaxValue;
                else
                    num3 -= (int)groundPollution * 100 / (int)byte.MaxValue;
            }
            if (data.m_workBuilding != (ushort)0)
            {
                byte park = instance2.GetPark(instance1.m_buildings.m_buffer[(int)data.m_workBuilding].m_position);
                DistrictPolicies.Park parkPolicies = instance2.m_parks.m_buffer[(int)park].m_parkPolicies;
                if ((parkPolicies & DistrictPolicies.Park.WorkSafety) != DistrictPolicies.Park.None && instance1.m_buildings.m_buffer[(int)data.m_workBuilding].Info.m_class.m_service == ItemClass.Service.PlayerIndustry)
                    num3 += 10;
                if ((parkPolicies & DistrictPolicies.Park.StudentHealthcare) != DistrictPolicies.Park.None && instance1.m_buildings.m_buffer[(int)data.m_workBuilding].Info.m_class.m_service == ItemClass.Service.PlayerEducation)
                    num3 += 10;
            }
            int num4 = Mathf.Clamp(num3, 0, 100);
            data.m_health = (byte)num4;
            int num5 = 0;
            if (num4 <= 10)
            {
                int badHealth = data.BadHealth;
                if (badHealth < 3)
                {
                    num5 = 15;
                    data.BadHealth = badHealth + 1;
                }
                else
                    num5 = !IsSenior(citizenID) && !IsChild(citizenID) && total1 == 0 || IsSenior(citizenID) && total2 == 0 && total1 == 0 || IsChild(citizenID) && total3 == 0 && total1 == 0 ? 75 : 50;
            }
            else if (num4 <= 25)
            {
                data.BadHealth = 0;
                num5 += 10;
            }
            else if (num4 <= 50)
            {
                data.BadHealth = 0;
                num5 += 3;
            }
            else
                data.BadHealth = 0;
            if (data.Sick && IsChild(citizenID) && local3 != 0 && Singleton<SimulationManager>.instance.m_randomizer.Int32(1000U) < num4)
                data.Sick = false;
            if (data.Sick && IsSenior(citizenID) && local2 != 0 && Singleton<SimulationManager>.instance.m_randomizer.Int32(1000U) < num4)
                data.Sick = false;
            if (data.CurrentLocation != Citizen.Location.Moving && data.m_vehicle == (ushort)0 && num5 != 0 && Singleton<SimulationManager>.instance.m_randomizer.Int32(100U) < num5)
            {
                /*
                 * pachang: Don't kill off the person
                if (Singleton<SimulationManager>.instance.m_randomizer.Int32(3U) == 0)
                {
                    this.Die(citizenID, ref data);
                    if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2U) == 0)
                    {
                        Singleton<CitizenManager>.instance.ReleaseCitizen(citizenID);
                        return true;
                    }
                }
                else
                */
                    data.Sick = true;
            }

            __result = false;
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
