using ColossalFramework;
using ColossalFramework.UI;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
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
            EnhancedDistrictServicesUIPanel.Instance?.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EnhancedDistrictServicesUIPanel.Instance.Hide();
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

                            var panel = EnhancedDistrictServicesUIPanel.Instance;
                            panel.SetBuilding(hoverInstance.Building);
                            panel.opacity = 1f;
                        }

                        if (!hoverInstance.IsEmpty)
                        {
                            Singleton<SimulationManager>.instance.AddAction(() => Singleton<GuideManager>.instance.m_worldInfoNotUsed.Disable());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not open district services world info panel!");
                Logger.LogException(ex);
            }
        }

        /// <summary>
        /// Helper method for displaying information, including district and supply chain constraints, about the 
        /// building with given building id.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        private static string GetBuildingInfoText(ushort buildingId)
        {
            var txtItems = new List<string>();

            txtItems.Add($"{TransferManagerInfo.GetBuildingName(buildingId)} ({buildingId})");
            txtItems.Add(TransferManagerInfo.GetDistrictText(buildingId));

            // Early return.  Rest of info pertains to building types that we deal with in the mod.
            if (!TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
            {
                return string.Join("\n", txtItems.ToArray());
            }

            txtItems.Add(TransferManagerInfo.GetServicesText(buildingId));

            if (Constraints.SupplySources(buildingId)?.Count > 0)
            {
                txtItems.Add("");
                txtItems.Add(TransferManagerInfo.GetSupplySourcesText(buildingId));
            }

            if (Constraints.SupplyDestinations(buildingId)?.Count > 0)
            {
                txtItems.Add("");
                txtItems.Add(TransferManagerInfo.GetSupplyDestinationsText(buildingId));
            }

            txtItems.Add("");
            txtItems.Add(TransferManagerInfo.GetDistrictsServedText(buildingId));

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
