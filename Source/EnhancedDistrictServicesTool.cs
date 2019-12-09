using ColossalFramework;
using ColossalFramework.UI;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// The EnhancedDistrictServicesTool.
    /// </summary>
    public class EnhancedDistrictServicesTool : DefaultTool
    {
        #region MonoBehavior

        [UsedImplicitly]
        protected override void Awake()
        {
            base.Awake();

            name = "EnhancedDistrictServicesTool";

            UITitlePanel.Create();
            EnhancedDistrictServicesUIPanel.Create();

            BuildingManager.instance.EventBuildingCreated += Constraints.CreateBuilding;
            BuildingManager.instance.EventBuildingReleased += Constraints.ReleaseBuilding;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EnhancedDistrictServicesUIPanel.Destroy();

            BuildingManager.instance.EventBuildingCreated -= Constraints.CreateBuilding;
            BuildingManager.instance.EventBuildingReleased -= Constraints.ReleaseBuilding;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            UITitlePanel.Instance?.OnEnable();
            EnhancedDistrictServicesUIPanel.Instance?.OnEnable();
            EnhancedDistrictServicesUIPanel.Instance?.UpdatePanelToBuilding(0);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UITitlePanel.Instance?.Hide();
            EnhancedDistrictServicesUIPanel.Instance?.UIDistrictsDropDown?.ClosePopup();
            EnhancedDistrictServicesUIPanel.Instance?.Hide();
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

            var building = m_hoverInstance.Building;
            if (m_toolController.IsInsideUI || !Cursor.visible || building == 0)
            {
                ShowToolInfo(false, null, Vector3.zero);
                return;
            }

            // Don't show info for dummy or sub buildings.
            if (BuildingManager.instance.m_buildings.m_buffer[building].Info.GetAI() is DummyBuildingAI)
            {
                ShowToolInfo(false, null, Vector3.zero);
                return;
            }

            var position = BuildingManager.instance.m_buildings.m_buffer[building].m_position;
            var txt = GetBuildingInfoText(building);
            ShowToolInfo(true, txt, position);
        }

        protected override void OnToolGUI(Event e)
        {
            try
            {
                WorldInfoPanel.HideAllWorldInfoPanels();

                var hoverInstance = this.m_hoverInstance;

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

                            Singleton<SimulationManager>.instance.AddAction(() =>
                            {
                                var panel = EnhancedDistrictServicesUIPanel.Instance;
                                panel.SetBuilding(hoverInstance.Building);
                                panel.UpdatePositionToBuilding(hoverInstance.Building);
                                panel.UpdatePanelToBuilding(hoverInstance.Building);
                                panel.opacity = 1f;

                                Singleton<GuideManager>.instance.m_worldInfoNotUsed.Disable();
                            });

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"EnhancedDistrictServicesTool::OnToolGUI: Could not open district services world info panel!");
                Logger.LogException(ex);
            }
        }

        /// <summary>
        /// Helper method for displaying information, including district and supply chain constraints, about the 
        /// building with given building id.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        private static string GetBuildingInfoText(ushort building)
        {
            var txtItems = new List<string>();

            txtItems.Add($"{TransferManagerInfo.GetBuildingName(building)} ({building})");
            txtItems.Add(TransferManagerInfo.GetDistrictParkText(building));

            // Early return.  Rest of info pertains to building types that we deal with in the mod.
            if (!TransferManagerInfo.IsDistrictServicesBuilding(building))
            {
                return string.Join("\n", txtItems.ToArray());
            }

            txtItems.Add(TransferManagerInfo.GetServicesText(building));

            if (TransferManagerInfo.IsSupplyChainBuilding(building))
            {
                txtItems.Add($"Supply Reserve: {Constraints.InternalSupplyBuffer(building)}");
            }

            if (Constraints.SupplySources(building)?.Count > 0)
            {
                txtItems.Add("");
                txtItems.Add(TransferManagerInfo.GetSupplySourcesText(building));
            }

            if (!Constraints.AllLocalAreas(building) && Constraints.SupplyDestinations(building)?.Count > 0)
            {
                txtItems.Add("");
                txtItems.Add(TransferManagerInfo.GetSupplyDestinationsText(building));
            }

            txtItems.Add("");
            txtItems.Add(TransferManagerInfo.GetDistrictsServedText(building));

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
