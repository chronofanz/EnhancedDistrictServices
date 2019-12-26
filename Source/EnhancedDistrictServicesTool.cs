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
            BuildingManager.instance.EventBuildingCreated += TaxiMod.RegisterTaxiBuilding;
            BuildingManager.instance.EventBuildingReleased += Constraints.ReleaseBuilding;
            BuildingManager.instance.EventBuildingReleased += TaxiMod.DeregisterTaxiBuilding;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EnhancedDistrictServicesUIPanel.Destroy();

            BuildingManager.instance.EventBuildingCreated -= Constraints.CreateBuilding;
            BuildingManager.instance.EventBuildingCreated -= TaxiMod.RegisterTaxiBuilding;
            BuildingManager.instance.EventBuildingReleased -= Constraints.ReleaseBuilding;
            BuildingManager.instance.EventBuildingReleased -= TaxiMod.DeregisterTaxiBuilding;
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

            CopyPaste.BuildingTemplate = 0;
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

            if (TransferManagerInfo.IsDistrictServicesBuilding(building))
            {
                var position = BuildingManager.instance.m_buildings.m_buffer[building].m_position;
                var txt = GetBuildingInfoText(building);
                ShowToolInfo(true, txt, position);
            }
        }

        protected override void OnToolGUI(Event e)
        {
            try
            {
                var hoverInstance = this.m_hoverInstance;
                var building = hoverInstance.Building;

                WorldInfoPanel.HideAllWorldInfoPanels();

                if (Settings.keyCopy.IsPressed(e))
                {
                    if (building == 0 || 
                        BuildingManager.instance.m_buildings.m_buffer[building].Info.GetAI() is DummyBuildingAI ||
                        !TransferManagerInfo.IsDistrictServicesBuilding(building))
                    {
                        Utils.DisplayMessage(
                            str1: "Enhanced District Services",
                            str2: $"Cannot copy policy from this building!",
                            str3: "IconMessage");
                        return;
                    }

                    CopyPaste.BuildingTemplate = building;
                }

                if (Settings.keyPaste.IsPressed(e))
                {
                    if (CopyPaste.BuildingTemplate == 0)
                    {
                        Utils.DisplayMessage(
                            str1: "Enhanced District Services",
                            str2: $"Please hover over a valid building and press Ctrl-C to copy its policy first!",
                            str3: "IconMessage");
                        return;
                    }

                    if (building == 0 ||
                        BuildingManager.instance.m_buildings.m_buffer[building].Info.GetAI() is DummyBuildingAI ||
                        !TransferManagerInfo.IsDistrictServicesBuilding(building))
                    {
                        Utils.DisplayMessage(
                            str1: "Enhanced District Services",
                            str2: $"Cannot copy policy to this unsupported building!",
                            str3: "IconMessage");
                        return;
                    }

                    var inputType1 = TransferManagerInfo.GetBuildingInputType(CopyPaste.BuildingTemplate);
                    var inputType2 = TransferManagerInfo.GetBuildingInputType(building);

                    if (inputType1 != inputType2)
                    {
                        Utils.DisplayMessage(
                            str1: "Enhanced District Services",
                            str2: $"Can only copy-paste policy between buildings of the same policy type!",
                            str3: "IconMessage");
                        return;
                    }

                    var success = CopyPaste.CopyPolicyTo(building);
                    if (!success)
                    {
                        Utils.DisplayMessage(
                            str1: "Enhanced District Services",
                            str2: $"Could not copy certain supply chain restrictions.  Please check results of copy operation!",
                            str3: "IconMessage");
                        return;
                    }

                    var position = BuildingManager.instance.m_buildings.m_buffer[building].m_position;
                    var txt = GetBuildingInfoText(building);
                    ShowToolInfo(true, txt, position);
                }

                if (!m_toolController.IsInsideUI && e.type == UnityEngine.EventType.MouseDown && e.button == 0)
                {
                    if (!TransferManagerInfo.IsDistrictServicesBuilding(building))
                    {
                        return;
                    }

                    if (this.m_selectErrors == ToolBase.ToolErrors.None || this.m_selectErrors == ToolBase.ToolErrors.RaycastFailed)
                    {
                        Vector3 mousePosition = this.m_mousePosition;
                        UIInput.MouseUsed();

                        if (!Singleton<InstanceManager>.instance.SelectInstance(hoverInstance))
                        {
                            return;
                        }

                        SimulationManager.instance.AddAction(() =>
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
            catch (Exception ex)
            {
                Logger.LogWarning($"EnhancedDistrictServicesTool::OnToolGUI: ...");
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

            txtItems.Add(TransferManagerInfo.GetBuildingInputTypeText(building));
            txtItems.Add(TransferManagerInfo.GetServicesText(building));

            if (!TransferManagerInfo.IsSupplyChainBuilding(building))
            {
                txtItems.Add("");
                txtItems.Add(TransferManagerInfo.GetOutputDistrictsServedText(building));

                return string.Join("\n", txtItems.ToArray());
            }

            // From this point forth, we know this is a supply chain building ...
            var inputType = TransferManagerInfo.GetBuildingInputType(building);

            txtItems.Add($"Supply Reserve: {Constraints.InternalSupplyBuffer(building)}");

            if ((inputType & InputType.INCOMING) != InputType.NONE)
            {
                txtItems.Add("");
                txtItems.Add(TransferManagerInfo.GetSupplyBuildingSourcesText(building));
            }

            if ((inputType & InputType.OUTGOING) != InputType.NONE)
            {
                txtItems.Add("");
                txtItems.Add(TransferManagerInfo.GetSupplyBuildingDestinationsText(building));
            }

            var problemText = TransferManagerInfo.GetSupplyBuildingProblemsText(building);
            if (problemText != string.Empty)
            {
                txtItems.Add("");
                txtItems.Add($"<<WARNING: Cannot find the following materials to procure!>>");
                txtItems.Add(problemText);
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
