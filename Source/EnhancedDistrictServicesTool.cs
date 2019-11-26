using ColossalFramework;
using ColossalFramework.UI;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnhancedDistrictServices
{
    public class EnhancedDistrictServicesTool : DefaultTool
    {
        #region MonoBehavior

        [UsedImplicitly]
        protected override void Awake()
        {
            base.Awake();

            name = "EnhancedDistrictServicesTool";

            EnhancedDistrictServicesWorldInfoPanel.Create();

            BuildingManager.instance.EventBuildingCreated += Instance_EventBuildingCreated;
            BuildingManager.instance.EventBuildingReleased += Instance_EventBuildingReleased;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            EnhancedDistrictServicesWorldInfoPanel.Destroy();

            BuildingManager.instance.EventBuildingCreated -= Instance_EventBuildingCreated;
            BuildingManager.instance.EventBuildingReleased -= Instance_EventBuildingReleased;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnhancedDistrictServicesWorldInfoPanel.Instance?.Activate();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EnhancedDistrictServicesWorldInfoPanel.Instance.Hide();
        }

        #endregion

        #region Game Loop

        public override void SimulationStep()
        {
            base.SimulationStep();

            if (m_mouseRayValid)
            {
                var defaultService = new RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
                var input = new RaycastInput(m_mouseRay, m_mouseRayLength)
                {
                    m_rayRight = m_rayRight,
                    m_netService = defaultService,
                    m_ignoreCitizenFlags = CitizenInstance.Flags.All,
                    m_ignoreNodeFlags = NetNode.Flags.All,
                    m_ignoreSegmentFlags = NetSegment.Flags.All
                };

                if (RayCast(input, out RaycastOutput output))
                {
                    if (output.m_building != 0)
                    {
                        m_hoverInstance.Building = output.m_building;
                    }
                }
                else
                {
                    m_hoverInstance.Building = 0;
                }
            }
        }

        protected override void OnToolUpdate()
        {
            base.OnToolUpdate();

            var buildingId = m_hoverInstance.Building;
            if (m_toolController.IsInsideUI || !Cursor.visible || buildingId == 0)
            {
                ShowToolInfo(false, null, Vector3.zero);
                return;
            }

            var position = BuildingManager.instance.m_buildings.m_buffer[buildingId].m_position;
            var txt = GetBuildingInfoText(buildingId);
            ShowToolInfo(true, txt, position);
        }

        protected override void OnToolGUI(Event e)
        {
            try
            {
                WorldInfoPanel.HideAllWorldInfoPanels();

                InstanceID hoverInstance = this.m_hoverInstance;

                if (!m_toolController.IsInsideUI && e.type == UnityEngine.EventType.MouseDown && e.button == 0)
                {
                    if (this.m_selectErrors == ToolBase.ToolErrors.None || this.m_selectErrors == ToolBase.ToolErrors.RaycastFailed)
                    {
                        Vector3 mousePosition = this.m_mousePosition;
                        UIInput.MouseUsed();

                        if (TransferManagerInfo.IsDistrictServicesBuilding(hoverInstance.Building))
                        {
                            if (!Singleton<InstanceManager>.instance.SelectInstance(hoverInstance))
                            {
                                return;
                            }

                            var panel = EnhancedDistrictServicesWorldInfoPanel.Instance;
                            panel.SetTarget(mousePosition, hoverInstance.Building);
                            panel.opacity = 1f;
                        }

                        if (!hoverInstance.IsEmpty)
                        {
                            Singleton<SimulationManager>.instance.AddAction(() => Singleton<GuideManager>.instance.m_worldInfoNotUsed.Disable());
                        }
                    }
                }

                if (m_toolController.m_developerUI == null || !this.m_toolController.m_developerUI.enabled || !Cursor.visible)
                {
                    return;
                }

                string text = null;
                if (hoverInstance.Building != 0)
                {
                    ushort building1 = m_hoverInstance.Building;
                    BuildingManager instance = Singleton<BuildingManager>.instance;

                    if ((instance.m_buildings.m_buffer[building1].m_flags & Building.Flags.Created) != Building.Flags.None)
                    {
                        BuildingInfo info = instance.m_buildings.m_buffer[building1].Info;
                        if (info != null)
                        {
                            text = StringUtils.SafeFormat("{0} ({1})", info.gameObject.name, building1);
                            string debugString = info.m_buildingAI.GetDebugString(building1, ref instance.m_buildings.m_buffer[building1]);
                            if (debugString != null)
                                text = text + "\n" + debugString;
                        }
                    }
                }

                if (text == null)
                    return;

                if (!InstanceManager.GetPosition(m_hoverInstance, out Vector3 position, out Quaternion rotation, out Vector3 size))
                {
                    position = this.m_mousePosition;
                }

                Vector3 screenPoint = Camera.main.WorldToScreenPoint(position);
                screenPoint.y = Screen.height - screenPoint.y;
                Color color = GUI.color;
                GUI.color = Color.cyan;
                DeveloperUI.LabelOutline(new Rect(screenPoint.x, screenPoint.y, 500f, 500f), text, Color.black, Color.cyan, GUI.skin.label, 2f);
                GUI.color = color;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not open district services world info panel!");
                Logger.LogException(ex);
            }
        }

        private void Instance_EventBuildingCreated(ushort building)
        {
            var position = BuildingManager.instance.m_buildings.m_buffer[building].m_position;
            var homeDistrict = DistrictManager.instance.GetDistrict(position);

            if (homeDistrict != 0)
            {
                DistrictServicesTable.AddDistrictRestriction(building, homeDistrict);
                DistrictServicesTable.SetAllLocalAreas(building, false, true);
                DistrictServicesTable.SetAllOutsideConnections(building, false, true);
            }
            else
            {
                DistrictServicesTable.SetAllLocalAreas(building, true, true);
                DistrictServicesTable.SetAllOutsideConnections(building, true, true);
            }
        }

        private void Instance_EventBuildingReleased(ushort building)
        {
            DistrictServicesTable.RemoveBuilding(building);
            SupplyChainTable.RemoveBuilding(building);
        }

        public static string GetBuildingInfoText(int buildingId)
        {
            var txtItems = new List<string>();

            txtItems.Add($"{TransferManagerInfo.GetBuildingName(buildingId)} ({buildingId})");

            var position = BuildingManager.instance.m_buildings.m_buffer[buildingId].m_position;
            var district = DistrictManager.instance.GetDistrict(position);
            if (district != 0)
            {
                var districtName = DistrictManager.instance.GetDistrictName((int)district);
                txtItems.Add($"District: {districtName}");
            }
            else
            {
                txtItems.Add($"District: (Not in a district)");
            }

            var buildingInfo = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info;
            var service = buildingInfo.GetService();
            var subService = buildingInfo.GetSubService();
            if (service == ItemClass.Service.PlayerIndustry)
            {
                if (buildingInfo.GetAI() is WarehouseAI warehouseAI)
                {
                    txtItems.Add($"Service: {service} ({warehouseAI.m_storageType})");
                }
                else
                {
                    txtItems.Add($"Service: {service}");
                }
            }
            else if (subService == ItemClass.SubService.None)
            {
                txtItems.Add($"Service: {service}");
            }
            else
            {
                txtItems.Add($"Service: {service} ({subService})");
            }

            // Early return.  Rest of info pertains to building types that we deal with in the mod.
            if (!TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
            {
                return string.Join("\n", txtItems.ToArray());
            }

            if (SupplyChainTable.IncomingOfferRestricted[buildingId]?.Count > 0)
            {
                txtItems.Add($"<<Supply Chain In>>");

                var buildingNames = SupplyChainTable.IncomingOfferRestricted[buildingId]
                    .Select(b => TransferManagerInfo.GetBuildingName(b))
                    .OrderBy(s => s);

                foreach (var buildingName in buildingNames)
                {
                    txtItems.Add(buildingName);
                }
            }

            if (SupplyChainTable.BuildingToBuildingServiced[buildingId] != null)
            {
                txtItems.Add($"<<Supply Chain Out>>");

                var buildingNames = SupplyChainTable.BuildingToBuildingServiced[buildingId]
                    .Select(b => TransferManagerInfo.GetBuildingName(b))
                    .OrderBy(s => s);

                foreach (var buildingName in buildingNames)
                {
                    txtItems.Add(buildingName);
                }
            }

            txtItems.Add($"<<DistrictsServed>>");

            if (DistrictServicesTable.BuildingToOutsideConnections[buildingId])
            {
                txtItems.Add($"All outside connections served");
            }

            if (DistrictServicesTable.BuildingToAllLocalAreas[buildingId])
            {
                txtItems.Add($"All local areas served");
            }
            else if (DistrictServicesTable.BuildingToDistrictServiced[buildingId]?.Count > 0)
            {
                var districtNames = DistrictServicesTable.BuildingToDistrictServiced[buildingId]
                    .Select(d => DistrictManager.instance.GetDistrictName(d))
                    .OrderBy(s => s);

                foreach (var districtName in districtNames)
                {
                    txtItems.Add(districtName);
                }
            }

            return string.Join("\n", txtItems.ToArray());
        }

        #endregion

        #region Ignore Flags

        public override NetNode.Flags GetNodeIgnoreFlags()
        {
            return NetNode.Flags.All;
        }

        public override NetSegment.Flags GetSegmentIgnoreFlags(out bool nameOnly)
        {
            nameOnly = false;
            return NetSegment.Flags.All;
        }

        public override Building.Flags GetBuildingIgnoreFlags()
        {
            return Building.Flags.Deleted;
        }

        public override TreeInstance.Flags GetTreeIgnoreFlags()
        {
            return TreeInstance.Flags.All;
        }

        public override PropInstance.Flags GetPropIgnoreFlags()
        {
            return PropInstance.Flags.All;
        }

        public override Vehicle.Flags GetVehicleIgnoreFlags()
        {
            return Vehicle.Flags.Created;
        }

        public override VehicleParked.Flags GetParkedVehicleIgnoreFlags()
        {
            return VehicleParked.Flags.All;
        }

        public override CitizenInstance.Flags GetCitizenIgnoreFlags()
        {
            return CitizenInstance.Flags.All;
        }

        public override TransportLine.Flags GetTransportIgnoreFlags()
        {
            return TransportLine.Flags.All;
        }

        public override District.Flags GetDistrictIgnoreFlags()
        {
            return District.Flags.All;
        }

        public override bool GetTerrainIgnore()
        {
            return true;
        }

        #endregion
    }
}
