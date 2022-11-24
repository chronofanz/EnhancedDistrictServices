using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// The main panel that the user interacts with.
    /// </summary>
    public class EnhancedDistrictServicesUIPanel : EnhancedDistrictServicesUIPanelBase<EnhancedDistrictServicesUIPanel>
    {
        private enum InputMode
        {
            OUTGOING = 0,
            INCOMING = 1,
            GLOBAL = 3
        }

        /// <summary>
        /// Mapping of dropdown index to DistrictPark. 
        /// </summary>
        private readonly List<EDSDistrictPark> m_districtParkMapping = new List<EDSDistrictPark>(capacity: EDSDistrictPark.MAX_DISTRICT_PARK_COUNT);

        /// <summary>
        /// Mapping of dropdown index to prefab index to vehicle info.
        /// </summary>
        private readonly List<int> m_vehicleMapping = new List<int>();

        /// <summary>
        /// Current input mode
        /// </summary>
        private InputMode m_inputMode = InputMode.OUTGOING;

        /// <summary>
        /// Current building whose policies we are editing.
        /// </summary>
        private ushort m_currBuildingId = 0;

        /// <summary>
        /// Hookup all the event handlers.
        /// </summary>
        public override void Start()
        {
            Logger.Log("EnhancedDistrictServicedUIPanel::Start");

            base.Start();

            UITitle.eventClicked += (c, p) =>
            {
                if (m_currBuildingId == 0)
                {
                    return;
                }

                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    SetBuilding(m_currBuildingId);
                    UpdatePositionToBuilding(m_currBuildingId);
                });
            };

            UIBuildingIdLabel.tooltip = "Click to cycle through all buildings of the same service type.";
            UIBuildingIdLabel.eventClicked += (c, p) =>
            {
                if (m_currBuildingId == 0)
                {
                    return;
                }

                Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIBuildingIdLabel Clicked");

                var info = BuildingManager.instance.m_buildings.m_buffer[m_currBuildingId].Info;
                var service = info.GetService();
                var subService = info.GetSubService();
                var aiType = info.GetAI().GetType();

                var nextBuildingId = FindSimilarBuilding(m_currBuildingId, service, subService, aiType);
                if (!TransferManagerInfo.IsDistrictServicesBuilding(nextBuildingId))
                {
                    return;
                }

                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    SetBuilding((ushort)nextBuildingId);
                    UpdatePositionToBuilding((ushort)nextBuildingId);
                });
            };

            UIBuildingId.eventClicked += (c, p) => 
            {
                Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIBuildingId Clicked");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    UIBuildingId.text = "";
                });
            };

            UIBuildingId.eventTextCancelled += (c, p) =>
            {
                Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIBuildingId TextCancelled");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    UpdateUIBuildingId();
                });
            };

            UIBuildingId.eventTextSubmitted += (c, p) =>
            {
                Logger.LogVerbose($"EnhancedDistrictServicedUIPanel::UIBuildingId TextSubmitted {p}");
                if (ushort.TryParse(p, out ushort buildingId) && TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
                {
                    Singleton<SimulationManager>.instance.AddAction(() =>
                    {
                        SetBuilding(buildingId);
                        UpdatePositionToBuilding(buildingId);
                    });
                }
                else
                {
                    Utils.DisplayMessage(
                        str1: "Enhanced District Services",
                        str2: $"Invalid building {p}!",
                        str3: "IconMessage");

                    Singleton<SimulationManager>.instance.AddAction(() =>
                    {
                        UpdateUIBuildingId();
                    });
                }
            };

            if (Settings.enableSelectOutsideConnection.value)
            {
                UIServices.tooltip = "(Experimental) Click to select outside connection.";
                UIServices.eventClicked += (c, p) =>
                {
                    Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIServices Clicked");

                    var nextBuildingId = FindSimilarBuilding(m_currBuildingId, ItemClass.Service.None, ItemClass.SubService.None, typeof(OutsideConnectionAI));
                    if (!TransferManagerInfo.IsDistrictServicesBuilding(nextBuildingId))
                    {
                        return;
                    }

                    Singleton<SimulationManager>.instance.AddAction(() =>
                    {
                        SetBuilding((ushort)nextBuildingId);
                        UpdatePositionToBuilding((ushort)nextBuildingId);
                    });
                };
            }

            UIOutgoingTab.eventClicked += (c, p) =>
            {
                Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIOutgoingTab Clicked");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    UpdateUIInputMode(InputMode.OUTGOING);
                });
            };

            UIIncomingTab.eventClicked += (c, p) =>
            {
                Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIIncomingTab Clicked");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    UpdateUIInputMode(InputMode.INCOMING);
                });
            };

            UIGlobalTab.eventClicked += (c, p) =>
            {
                Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIGlobalTab Clicked");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    UpdateUIInputMode(InputMode.GLOBAL);
                });
            };

            UISupplyReserve.eventClicked += (c, p) =>
            {
                Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyReserve Clicked");
            };

            UISupplyReserve.eventTextCancelled += (c, p) =>
            {
                Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyReserve TextCancelled");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    UpdateUISupplyReserve();
                });
            };

            UISupplyReserve.eventTextSubmitted += (c, p) =>
            {
                Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyReserve TextSubmitted");
                try
                {
                    if (!TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId))
                    {
                        return;
                    }

                    if (string.IsNullOrEmpty(UISupplyReserve.text.Trim()))
                    {
                        return;
                    }
                    
                    var amount = ushort.Parse(UISupplyReserve.text);
                    Constraints.SetInternalSupplyReserve(m_currBuildingId, amount);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
                finally
                {
                    Singleton<SimulationManager>.instance.AddAction(() =>
                    {
                        UpdateUISupplyReserve();
                    });
                }
            };

            UIAllLocalAreasCheckBox.eventCheckChanged += (c, t) =>
            {
                if (m_currBuildingId == 0 || 
                    (m_inputMode == InputMode.INCOMING && t == Constraints.InputAllLocalAreas(m_currBuildingId)) ||
                    (m_inputMode == InputMode.OUTGOING && t == Constraints.OutputAllLocalAreas(m_currBuildingId)))
                {
                    return;
                }

                Logger.LogVerbose($"EnhancedDistrictServicedUIPanel::UIAllLocalAreasCheckBox CheckChanged {t}");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    if (m_inputMode == InputMode.INCOMING)
                    {
                        Constraints.SetAllInputLocalAreas(m_currBuildingId, t);
                    }

                    if (m_inputMode == InputMode.OUTGOING)
                    {
                        Constraints.SetAllOutputLocalAreas(m_currBuildingId, t);
                    }

                    UpdateUIAllLocalAreasCheckBox();

                    UpdateUISupplyChain();
                    UpdateUIDistrictsSummary();
                });
            };

            UIAllOutsideConnectionsCheckBox.eventCheckChanged += (c, t) =>
            {
                if (m_currBuildingId == 0 ||
                    (m_inputMode == InputMode.INCOMING && t == Constraints.InputOutsideConnections(m_currBuildingId)) ||
                    (m_inputMode == InputMode.OUTGOING && t == Constraints.OutputOutsideConnections(m_currBuildingId)))
                {
                    return;
                }

                Logger.LogVerbose($"EnhancedDistrictServicedUIPanel::UIAllOutsideConnectionsCheckBox CheckChanged {t}");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    if (m_inputMode == InputMode.INCOMING)
                    {
                        Constraints.SetAllInputOutsideConnections(m_currBuildingId, t);
                    }

                    if (m_inputMode == InputMode.OUTGOING)
                    {
                        Constraints.SetAllOutputOutsideConnections(m_currBuildingId, t);
                    }

                    UpdateUISupplyChain();
                    UpdateUIDistrictsSummary();
                });
            };

            UISupplyChain.eventClicked += (c, p) =>
            {
                Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyChain Clicked");
            };

            UISupplyChain.eventTextCancelled += (c, p) =>
            {
                Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyChain TextCancelled");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    UpdateUISupplyChain();
                });
            };

            UISupplyChain.eventTextSubmitted += (c, p) =>
            {
                Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyChain TextSubmitted");
                try
                {
                    if (!TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId))
                    {
                        return;
                    }

                    if (string.IsNullOrEmpty(UISupplyChain.text.Trim()))
                    {
                        if (m_inputMode == InputMode.INCOMING)
                        {
                            Constraints.RemoveAllSupplyChainConnectionsToDestination(m_currBuildingId);
                        }

                        if (m_inputMode == InputMode.OUTGOING)
                        {
                            Constraints.RemoveAllSupplyChainConnectionsFromSource(m_currBuildingId);
                        }
                    }
                    else
                    {
                        // TODO, FIXME: Do this in a single transaction and clean up hacky implementation below.
                        var buildings = UISupplyChain.text.Split(',').Select(s => ushort.Parse(s));

                        if (m_inputMode == InputMode.INCOMING)
                        {
                            foreach (var building in buildings)
                            {
                                if (!TransferManagerInfo.IsValidSupplyChainLink(building, m_currBuildingId))
                                {
                                    Utils.DisplayMessage(
                                        str1: "Enhanced District Services",
                                        str2: $"Could not specify building {building} as supply chain in restriction!",
                                        str3: "IconMessage");

                                    return;
                                }
                            }

                            Constraints.RemoveAllSupplyChainConnectionsToDestination(m_currBuildingId);

                            foreach (var building in buildings)
                            {
                                Constraints.AddSupplyChainConnection(building, m_currBuildingId);
                            }
                        }

                        if (m_inputMode == InputMode.OUTGOING)
                        {
                            foreach (var building in buildings)
                            {
                                if (!TransferManagerInfo.IsValidSupplyChainLink(m_currBuildingId, building))
                                {
                                    Utils.DisplayMessage(
                                        str1: "Enhanced District Services",
                                        str2: $"Could not specify building {building} as supply chain out restriction!",
                                        str3: "IconMessage");

                                    return;
                                }
                            }

                            Constraints.RemoveAllSupplyChainConnectionsFromSource(m_currBuildingId);

                            foreach (var building in buildings)
                            {
                                Constraints.AddSupplyChainConnection(m_currBuildingId, building);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
                finally
                {
                    Singleton<SimulationManager>.instance.AddAction(() =>
                    {
                        UpdateUISupplyChain();
                        UpdateUIDistrictsSummary();
                    });
                }
            };

            UIDistrictsDropDown.eventCheckedChanged += (c, t) =>
            {
                if (m_currBuildingId == 0 || m_districtParkMapping == null)
                {
                    return;
                }

                if (m_inputMode == InputMode.INCOMING && UIDistrictsDropDown.GetChecked(t) == Constraints.InputDistrictParkServiced(m_currBuildingId)?.Contains(m_districtParkMapping[t]))
                {
                    return;
                }

                if (m_inputMode == InputMode.OUTGOING && UIDistrictsDropDown.GetChecked(t) == Constraints.OutputDistrictParkServiced(m_currBuildingId)?.Contains(m_districtParkMapping[t]))
                {
                    return;
                }

                Logger.LogVerbose($"EnhancedDistrictServicedUIPanel::UIDistrictsDropDown CheckChanged: {t}");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    try
                    {
                        if (m_inputMode == InputMode.INCOMING)
                        {
                            if (UIDistrictsDropDown.GetChecked(t))
                            {
                                Constraints.AddInputDistrictParkServiced(m_currBuildingId, m_districtParkMapping[t]);
                            }
                            else
                            {
                                Constraints.RemoveInputDistrictParkServiced(m_currBuildingId, m_districtParkMapping[t]);
                            }
                        }

                        if (m_inputMode == InputMode.OUTGOING)
                        {
                            if (UIDistrictsDropDown.GetChecked(t))
                            {
                                Constraints.AddOutputDistrictParkServiced(m_currBuildingId, m_districtParkMapping[t]);
                            }
                            else
                            {
                                Constraints.RemoveOutputDistrictParkServiced(m_currBuildingId, m_districtParkMapping[t]);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex);
                    }
                    finally
                    {
                        UpdateUISupplyChain();
                        UpdateUIDistrictsSummary();
                    }
                });
            };

            GlobalIntensity.eventTextCancelled += (c, p) =>
            {
                Logger.LogVerbose("UITitlePanel::GlobalIntensity TextCancelled");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    UpdateGlobalIntensity();
                });
            };

            GlobalIntensity.eventTextSubmitted += (c, p) =>
            {
                Logger.LogVerbose("UITitlePanel::GlobalIntensity TextSubmitted");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    if (string.IsNullOrEmpty(GlobalIntensity.text.Trim()))
                    {
                        UpdateGlobalIntensity();
                        return;
                    }
                    else
                    {
                        try
                        {
                            var amount = ushort.Parse(GlobalIntensity.text);
                            Constraints.SetGlobalOutsideConnectionIntensity(amount);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogException(ex);
                        }
                    }

                    UpdateGlobalIntensity();
                });
            };

            GlobalOutsideToOutsideMaxPerc.eventTextCancelled += (c, p) =>
            {
                Logger.LogVerbose("UITitlePanel::GlobalOutsideToOutsideMaxPerc TextCancelled");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    UpdateGlobalOutsideToOutsideMaxPerc();
                });
            };

            GlobalOutsideToOutsideMaxPerc.eventTextSubmitted += (c, p) =>
            {
                Logger.LogVerbose("UITitlePanel::GlobalOutsideToOutsideMaxPerc TextSubmitted");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    if (string.IsNullOrEmpty(GlobalOutsideToOutsideMaxPerc.text.Trim()))
                    {
                        UpdateGlobalOutsideToOutsideMaxPerc();
                        return;
                    }
                    else
                    {
                        try
                        {
                            var amount = ushort.Parse(GlobalOutsideToOutsideMaxPerc.text);
                            Constraints.SetGlobalOutsideToOutsideMaxPerc(amount);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogException(ex);
                        }
                    }

                    UpdateGlobalOutsideToOutsideMaxPerc();
                });
            };
        }

        public override void OnEnable()
        {
            if (UIDistrictsDropDown == null)
            {
                return;
            }

            base.OnEnable();

            Singleton<SimulationManager>.instance.AddAction(() =>
            {
                UpdateUIDistrictsDropdownDistrictItems();
                SetBuilding(0);
            });
        }

        public void SetBuilding(ushort building)
        {
            Logger.LogVerbose($"EnhancedDistrictServicedUIPanel::SetBuilding: buildingId={building}");
            if (TransferManagerInfo.IsDistrictServicesBuilding(building))
            {
                m_currBuildingId = building;
            }
            else
            {
                m_currBuildingId = 0;
            }

            if (TransferManagerInfo.IsOutsideBuilding(building, TransferManager.TransferReason.None))
            {
                // Need to enable this so that the user can roughly determine where the connection is ...
                Camera.main.GetComponent<CameraController>().m_unlimitedCamera = true;
            }

            UpdateUITitle();
            UpdateUIBuildingId();
            UpdateUIHomeDistrict();
            UpdateUIServices();

            UpdateUIInputModeTabs();
          
            if (m_currBuildingId != 0)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        private void UpdateUITitle()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UITitle Update");
            if (m_currBuildingId != 0)
            {
                UITitle.text = TransferManagerInfo.GetBuildingName(m_currBuildingId);
            }
            else
            {
                UITitle.text = "(Enhanced District Services Tool)";
            }

            UITitle.tooltip = "Click to move camera to building";
        }

        private void UpdateUIBuildingId()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIBuildingId Update");

            UIBuildingId.text = m_currBuildingId != 0 ? $"{m_currBuildingId}" : string.Empty;
            UIBuildingId.tooltip = "Enter a new building id to configure that building";
        }

        private void UpdateUIHomeDistrict()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIHomeDistrict Update");

            if (m_currBuildingId != 0)
            {
                UIHomeDistrict.text = TransferManagerInfo.GetDistrictParkText(m_currBuildingId);
            }
            else
            {
                UIHomeDistrict.text = "Home district:";
            }
        }

        private void UpdateUIServices()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIServices Update");

            if (m_currBuildingId != 0)
            {
                UIServices.text = TransferManagerInfo.GetServicesText(m_currBuildingId);
            }
            else
            {
                UIServices.text = "Service:";
            }
        }

        private void UpdateUIInputModeTabs()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIInput Update");

            void ShowTab(string tabName, bool show)
            {
                if (show)
                {
                    UIInputMode.ShowTab(tabName);
                }
                else
                {
                    UIInputMode.HideTab(tabName);
                }
            }

            var inputType = TransferManagerInfo.GetBuildingInputType(m_currBuildingId);
            ShowTab("Outgoing", (inputType & InputType.OUTGOING) != InputType.NONE);
            ShowTab("Incoming", (inputType & InputType.INCOMING) != InputType.NONE);
            ShowTab("Global", m_currBuildingId != 0);

            if ((inputType & InputType.OUTGOING) != InputType.NONE)
            {
                UpdateUIInputMode(InputMode.OUTGOING);
            }
            else if ((inputType & InputType.INCOMING) != InputType.NONE)
            {
                UpdateUIInputMode(InputMode.INCOMING);
            }
            else
            {
                UpdateUIInputMode(InputMode.GLOBAL);
            }
        }

        private void UpdateUIInputMode(InputMode inputMode)
        {
            m_inputMode = inputMode;

            if ((int)m_inputMode != UIInputMode.selectedIndex)
            {
                UIInputMode.selectedIndex = (int)m_inputMode;
            }

            ClearTabContainerElements();

            switch (m_inputMode)
            {
                case InputMode.OUTGOING:
                    AddTabContainerRow();
                    AddElementToTabContainerRow(UIAllLocalAreasCheckBox);

                    var info = BuildingManager.instance.m_buildings.m_buffer[m_currBuildingId].Info;
                    if (TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId) || info.GetAI() is FishFarmAI || info.GetAI() is FishingHarborAI)
                    {
                        AddElementToTabContainerRow(UIAllOutsideConnectionsCheckBox);
                    }
                    else
                    {
                        ShowComponent(UIAllOutsideConnectionsCheckBox, false);
                    }

                    if (TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId))
                    {
                        AddTabContainerRow();
                        AddElementToTabContainerRow(UISupplyReserve);
                        AddElementToTabContainerRow(UISupplyReserveLabel);

                        AddTabContainerRow();
                        AddElementToTabContainerRow(UISupplyChain);
                        AddElementToTabContainerRow(UISupplyChainLabel);
                    }
                    else
                    {
                        ShowComponent(UISupplyReserve, false);
                        ShowComponent(UISupplyReserveLabel, false);
                        ShowComponent(UISupplyChain, false);
                        ShowComponent(UISupplyChainLabel, false);
                    }

                    AddTabContainerRow();
                    AddElementToTabContainerRow(UIDistrictsSummary);
                    AddElementToTabContainerRow(UIDistrictsDropDown);

                    ShowComponent(GlobalIntensity, false);
                    ShowComponent(GlobalIntensityLabel, false);
                    ShowComponent(GlobalOutsideToOutsideMaxPerc, false);
                    ShowComponent(GlobalOutsideToOutsideMaxPercLabel, false);
                    break;

                case InputMode.INCOMING:
                    AddTabContainerRow();
                    AddElementToTabContainerRow(UIAllLocalAreasCheckBox);
                    AddElementToTabContainerRow(UIAllOutsideConnectionsCheckBox);

                    ShowComponent(UISupplyReserve, false);
                    ShowComponent(UISupplyReserveLabel, false);

                    AddTabContainerRow();
                    AddElementToTabContainerRow(UISupplyChain);
                    AddElementToTabContainerRow(UISupplyChainLabel);

                    AddTabContainerRow();
                    AddElementToTabContainerRow(UIDistrictsSummary);
                    AddElementToTabContainerRow(UIDistrictsDropDown);

                    ShowComponent(GlobalIntensity, false);
                    ShowComponent(GlobalIntensityLabel, false);
                    ShowComponent(GlobalOutsideToOutsideMaxPerc, false);
                    ShowComponent(GlobalOutsideToOutsideMaxPercLabel, false);

                    break;

                case InputMode.GLOBAL:
                    ShowComponent(UIAllLocalAreasCheckBox, false);
                    ShowComponent(UIAllOutsideConnectionsCheckBox, false);
                    ShowComponent(UISupplyReserve, false);
                    ShowComponent(UISupplyReserveLabel, false);
                    ShowComponent(UISupplyChain, false);
                    ShowComponent(UISupplyChainLabel, false);
                    ShowComponent(UIDistrictsSummary, false);
                    ShowComponent(UIDistrictsDropDown, false);

                    AddTabContainerRow();
                    AddElementToTabContainerRow(GlobalIntensity);
                    AddElementToTabContainerRow(GlobalIntensityLabel);
                    AddTabContainerRow();
                    AddElementToTabContainerRow(GlobalOutsideToOutsideMaxPerc);
                    AddElementToTabContainerRow(GlobalOutsideToOutsideMaxPercLabel);
                    break;

                default:
                    throw new Exception($"Unknown input mode {m_inputMode}");
            }

            UpdateUIAllLocalAreasCheckBox();
            UpdateUIAllOutsideConnectionsCheckBox();
            UpdateUISupplyReserve();
            UpdateUISupplyChain();
            UpdateUIDistrictsDropdown();
            UpdateUIDistrictsSummary();
            UpdateGlobalIntensity();
            UpdateGlobalOutsideToOutsideMaxPerc();
        }

        private void UpdateUIAllLocalAreasCheckBox()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIAllLocalAreasCheckBox Update");

            if (m_currBuildingId == 0 || !(m_inputMode == InputMode.INCOMING || m_inputMode == InputMode.OUTGOING))
            {
                return;
            }

            if (m_inputMode == InputMode.INCOMING)
            {
                UIAllLocalAreasCheckBox.isChecked = Constraints.InputAllLocalAreas(m_currBuildingId);
            }

            if (m_inputMode == InputMode.OUTGOING)
            {
                UIAllLocalAreasCheckBox.isChecked = Constraints.OutputAllLocalAreas(m_currBuildingId);
            }

            switch (m_inputMode)
            {
                case InputMode.OUTGOING:
                    if (TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId))
                    {
                        ShowComponent(UISupplyReserve, !UIAllLocalAreasCheckBox.isChecked);
                        ShowComponent(UISupplyReserveLabel, !UIAllLocalAreasCheckBox.isChecked);
                        ShowComponent(UISupplyChain, !UIAllLocalAreasCheckBox.isChecked);
                        ShowComponent(UISupplyChainLabel, !UIAllLocalAreasCheckBox.isChecked);
                    }

                    ShowComponent(UIDistrictsSummary, !UIAllLocalAreasCheckBox.isChecked);
                    ShowComponent(UIDistrictsDropDown, !UIAllLocalAreasCheckBox.isChecked);

                    break;

                case InputMode.INCOMING:
                    ShowComponent(UISupplyChain, !UIAllLocalAreasCheckBox.isChecked);
                    ShowComponent(UISupplyChainLabel, !UIAllLocalAreasCheckBox.isChecked);
                    ShowComponent(UIDistrictsSummary, !UIAllLocalAreasCheckBox.isChecked);
                    ShowComponent(UIDistrictsDropDown, !UIAllLocalAreasCheckBox.isChecked);

                    break;

                default:
                    throw new Exception($"Unknown input mode {m_inputMode}");
            }

            UIAllLocalAreasCheckBox.label.text = "All Local Areas: ";
            UIAllLocalAreasCheckBox.tooltip = "If enabled, serves all local areas.  Disable to specify Supply Chain or Districts Served restrictions.";
        }

        private void UpdateUIAllOutsideConnectionsCheckBox()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIAllOutsideConnectionsCheckBox Update");

            if (m_currBuildingId == 0 || !(m_inputMode == InputMode.INCOMING || m_inputMode == InputMode.OUTGOING))
            {
                return;
            }

            var info = BuildingManager.instance.m_buildings.m_buffer[m_currBuildingId].Info;
            if (!(TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId) || info.GetAI() is FishFarmAI || info.GetAI() is FishingHarborAI))
            {
                return;
            }

            if (m_inputMode == InputMode.INCOMING)
            {
                UIAllOutsideConnectionsCheckBox.isChecked = Constraints.InputOutsideConnections(m_currBuildingId);
            }

            if (m_inputMode == InputMode.OUTGOING)
            {
                UIAllOutsideConnectionsCheckBox.isChecked = Constraints.OutputOutsideConnections(m_currBuildingId);
            }

            UIAllOutsideConnectionsCheckBox.tooltip = "If enabled, serves all outside connections.";
            UIAllOutsideConnectionsCheckBox.label.text = "All Outside Connections: ";
        }

        private void UpdateUISupplyReserve()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyReserve Update");

            if (m_currBuildingId == 0 || !(m_inputMode == InputMode.INCOMING || m_inputMode == InputMode.OUTGOING) || !TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId))
            {
                return;
            }

            var tooltipText = "(Supply Chain Buildings Only):\nThe percentage of goods to reserve for allowed districts and supply out buildings.\nEnter a value between 0 and 100 inclusive.";

            UISupplyReserve.text = Constraints.InternalSupplyBuffer(m_currBuildingId).ToString();
            UISupplyReserve.tooltip = tooltipText;
            UISupplyReserveLabel.tooltip = tooltipText;
        }

        private void UpdateUISupplyChain()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyChain Update");

            if (m_currBuildingId == 0 || !(m_inputMode == InputMode.INCOMING || m_inputMode == InputMode.OUTGOING) || !TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId))
            {
                return;
            }

            if (m_inputMode == InputMode.INCOMING)
            {
                var tooltipText = "(Supply Chain Buildings Only):\nEnter a comma delimited list of building ids to incoming shipments from those buildings.";
                if (Constraints.SupplySources(m_currBuildingId)?.Count > 0)
                {
                    UISupplyChain.text = string.Join(",", Constraints.SupplySources(m_currBuildingId).Select(b => b.ToString()).ToArray());
                    UISupplyChain.tooltip = TransferManagerInfo.GetSupplyBuildingSourcesText(m_currBuildingId);
                }
                else
                {
                    UISupplyChain.text = "";
                    UISupplyChain.tooltip = tooltipText;
                }

                UISupplyChainLabel.text = "Supply Chain:";
                UISupplyChainLabel.tooltip = tooltipText;
            }

            if (m_inputMode == InputMode.OUTGOING)
            {
                var tooltipText = "(Supply Chain Buildings Only):\nEnter a comma delimited list of building ids to restrict outgoing shipments to those buildings.\nClear to enable districts served restrictions.";

                if (Constraints.SupplyDestinations(m_currBuildingId)?.Count > 0)
                {
                    UISupplyChain.text = string.Join(",", Constraints.SupplyDestinations(m_currBuildingId).Select(b => b.ToString()).ToArray());
                    UISupplyChain.tooltip = TransferManagerInfo.GetSupplyBuildingDestinationsText(m_currBuildingId);
                }
                else
                {
                    UISupplyChain.text = "";
                    UISupplyChain.tooltip = tooltipText;
                }

                UISupplyChainLabel.text = "Supply Chain:";
                UISupplyChainLabel.tooltip = tooltipText;
            }
        }

        private void UpdateUIDistrictsDropdownDistrictItems()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIDistrictsDropdownDistrictItems Update");

            UIDistrictsDropDown.Clear();
            m_districtParkMapping.Clear();

            var districtParks = EDSDistrictPark.GetAllDistrictParks();
            foreach (var districtPark in districtParks)
            {
                if (!Settings.showCampusDistricts.value && districtPark.IsCampus)
                {
                    continue;
                }

                if (!Settings.showIndustryDistricts.value && districtPark.IsIndustry)
                {
                    continue;
                }

                if (!Settings.showParkDistricts.value && districtPark.IsPark)
                {
                    continue;
                }

                UIDistrictsDropDown.AddItem(districtPark.Name, isChecked: false);
                m_districtParkMapping.Add(districtPark);
            }

            Logger.LogVerbose($"EnhancedDistrictServicedUIPanel::UIDistrictsDropdownDistrictItems Found {m_districtParkMapping.Count} districts.");
        }

        private void UpdateUIDistrictsDropdown()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIDistrictsDropdown Update");

            if (m_currBuildingId == 0 || !(m_inputMode == InputMode.INCOMING || m_inputMode == InputMode.OUTGOING))
            {
                return;
            }

            List<EDSDistrictPark> districtParkServed = null;
            if (m_inputMode == InputMode.INCOMING)
            {
                districtParkServed = Constraints.InputDistrictParkServiced(m_currBuildingId);
            }
            if (m_inputMode == InputMode.OUTGOING)
            {
                districtParkServed = Constraints.OutputDistrictParkServiced(m_currBuildingId);
            }

            void SetChecked(int i, bool ischecked)
            {
                if (UIDistrictsDropDown.GetChecked(i) != ischecked)
                {
                    UIDistrictsDropDown.SetChecked(i, ischecked);
                }
            }

            if (districtParkServed != null)
            {
                // Do not used UICheckboxDropDown::SetChecked(bool[] isChecked) because it replaces the underlying array.
                for (int i = 0; i < m_districtParkMapping.Count; i++)
                {
                    SetChecked(i, districtParkServed.Contains(m_districtParkMapping[i]));
                }
            }
            else
            {
                // Do not used UICheckboxDropDown::SetChecked(bool[] isChecked) because it replaces the underlying array.
                for (int i = 0; i < m_districtParkMapping.Count; i++)
                {
                    SetChecked(i, false);
                }
            }
        }

        private void UpdateUIDistrictsSummary()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIDistrictsSummary Update");

            if (m_currBuildingId == 0 || !(m_inputMode == InputMode.INCOMING || m_inputMode == InputMode.OUTGOING))
            {
                UIDistrictsSummary.text = string.Empty;
                UIDistrictsDropDown.triggerButton.tooltip = string.Empty;
                return;
            }

            if (m_inputMode == InputMode.INCOMING)
            {
                var homeDistrictPark = TransferManagerInfo.GetDistrictPark(m_currBuildingId);
                var districtParkServed = Constraints.InputDistrictParkServiced(m_currBuildingId);

                var tooltipText = TransferManagerInfo.GetSupplyBuildingSourcesText(m_currBuildingId);

                if (districtParkServed == null || districtParkServed.Count == 0)
                {
                    UIDistrictsSummary.text = "Shipments from Districts: None";
                    UIDistrictsDropDown.triggerButton.tooltip = tooltipText;
                }
                // Note that using List::Contains is the wrong thing to do, since the districtParkServed array is 
                // guaranteed to contain elements that refer to either 1 district or 1 park, but not both, while a building
                // might belong to both the district or park ...
                else if (!homeDistrictPark.IsEmpty && homeDistrictPark.IsServedBy(districtParkServed))
                {
                    if (districtParkServed.Count == 1)
                    {
                        UIDistrictsSummary.text = $"Shipments from Districts: Home only";
                    }
                    else
                    {
                        UIDistrictsSummary.text = $"Shipments from Districts: Home + {districtParkServed.Count - 1} others";
                    }

                    UIDistrictsDropDown.triggerButton.tooltip = tooltipText;
                }
                else
                {
                    UIDistrictsSummary.text = $"Shipments from Districts: {districtParkServed.Count} others";
                    UIDistrictsDropDown.triggerButton.tooltip = tooltipText;
                }
            }

            if (m_inputMode == InputMode.OUTGOING)
            {
                var homeDistrictPark = TransferManagerInfo.GetDistrictPark(m_currBuildingId);
                var districtParkServed = Constraints.OutputDistrictParkServiced(m_currBuildingId);

                var buildingType = TransferManagerInfo.GetBuildingInputType(m_currBuildingId);
                var tooltipText = 
                    (buildingType & InputType.SUPPLY_CHAIN) == InputType.NONE ? TransferManagerInfo.GetOutputDistrictsServedText(m_currBuildingId) : TransferManagerInfo.GetSupplyBuildingDestinationsText(m_currBuildingId);

                if (districtParkServed == null || districtParkServed.Count == 0)
                {
                    UIDistrictsSummary.text = "Districts served: None";
                    UIDistrictsDropDown.triggerButton.tooltip = tooltipText;
                }
                // Note that using List::Contains is the wrong thing to do, since the districtParkServed array is 
                // guaranteed to contain elements that refer to either 1 district or 1 park, but not both, while a building
                // might belong to both the district or park ...
                else if (!homeDistrictPark.IsEmpty && homeDistrictPark.IsServedBy(districtParkServed))
                {
                    if (districtParkServed.Count == 1)
                    {
                        UIDistrictsSummary.text = $"Districts served: Home only";
                    }
                    else
                    {
                        UIDistrictsSummary.text = $"Districts served: Home + {districtParkServed.Count - 1} others";
                    }

                    UIDistrictsDropDown.triggerButton.tooltip = tooltipText;
                }
                else
                {
                    UIDistrictsSummary.text = $"Districts served: {districtParkServed.Count} others";
                    UIDistrictsDropDown.triggerButton.tooltip = tooltipText;
                }
            }
        }

        private void UpdateGlobalIntensity()
        {
            if (m_currBuildingId == 0 || m_inputMode != InputMode.GLOBAL)
            {
                return;
            }

            GlobalIntensity.text = Constraints.GlobalOutsideConnectionIntensity().ToString();

            var tooltipText = "The intensity controls the amount of supply chain traffic entering the city, between 0 and 1000\nWARNING: Do not set this too high, otherwise your traffic will become overwhelmed with traffic!";
            GlobalIntensity.tooltip = tooltipText;
            GlobalIntensityLabel.tooltip = tooltipText;
        }

        private void UpdateGlobalOutsideToOutsideMaxPerc()
        {
            if (m_currBuildingId == 0 || m_inputMode != InputMode.GLOBAL)
            {
                return;
            }

            GlobalOutsideToOutsideMaxPerc.text = Constraints.GlobalOutsideToOutsidePerc().ToString();

            var tooltipText = "Max percentage of traffic allowed for dummy outside to outside cargo transfers, between 0 and 100";
            GlobalOutsideToOutsideMaxPerc.tooltip = tooltipText;
            GlobalOutsideToOutsideMaxPercLabel.tooltip = tooltipText;
        }

        #region Helper methods

        /// <summary>
        /// Used by UIBuildingIdLabel.eventClicked to find another building that is in the given service category.
        /// </summary>
        private static int FindSimilarBuilding(int currBuildingId, ItemClass.Service service, ItemClass.SubService subService, Type aiType)
        {
            try
            {
                bool IsSameBuildingType(int buildingId)
                {
                    var other_info = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info;
                    if (aiType == typeof(OutsideConnectionAI))
                    {
                        return other_info?.GetAI()?.GetType() == typeof(OutsideConnectionAI);
                    }
                    else
                    {
                        return
                            other_info?.GetService() == service &&
                            other_info?.GetSubService() == subService &&
                            other_info?.GetAI()?.GetType() == aiType;
                    }
                }

                for (int buildingId = currBuildingId + 1; buildingId < BuildingManager.MAX_BUILDING_COUNT; buildingId++)
                {
                    if (IsSameBuildingType(buildingId))
                    {
                        return buildingId;
                    }
                }

                for (int buildingId = 1; buildingId < currBuildingId; buildingId++)
                {
                    if (IsSameBuildingType(buildingId))
                    {
                        return buildingId;
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return 0;
            }
        }

        #endregion
    }
}
