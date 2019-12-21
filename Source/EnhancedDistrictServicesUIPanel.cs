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
        /// <summary>
        /// Mapping of dropdown index to DistrictPark. 
        /// </summary>
        private readonly List<DistrictPark> m_districtParkMapping = new List<DistrictPark>(capacity: DistrictPark.MAX_DISTRICT_PARK_COUNT);

        /// <summary>
        /// Current building whose policies we are editing.
        /// </summary>
        private ushort m_currBuildingId = 0;

        /// <summary>
        /// Hookup all the event handlers.
        /// </summary>
        public override void Start()
        {
            base.Start();

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
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    if (ushort.TryParse(p, out ushort buildingId) && TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
                    {
                        var position = BuildingManager.instance.m_buildings.m_buffer[buildingId].m_position;
                        CameraController.SetTarget(new InstanceID { Building = buildingId }, position, false);

                        SetBuilding(buildingId);
                        UpdatePositionToBuilding(buildingId);
                    }
                    else
                    {
                        UpdateUIBuildingId();
                    }
                });
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

            UIAllLocalAreasCheckBox.eventCheckChanged += (c, t) =>
            {
                if (m_currBuildingId == 0 || t == Constraints.OutputAllLocalAreas(m_currBuildingId))
                {
                    return;
                }

                Logger.LogVerbose($"EnhancedDistrictServicedUIPanel::UIAllLocalAreasCheckBox CheckChanged {t}");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    Constraints.SetAllOutputLocalAreas(m_currBuildingId, t);
                    UpdateUISupplyChainOut();
                    UpdateUIDistrictsDropdown(updateChecked: false);
                    UpdateUIDistrictsSummary();
                });
            };

            UIAllOutsideConnectionsCheckBox.eventCheckChanged += (c, t) =>
            {
                if (m_currBuildingId == 0 || t == Constraints.OutputOutsideConnections(m_currBuildingId))
                {
                    return;
                }

                Logger.LogVerbose($"EnhancedDistrictServicedUIPanel::UIAllOutsideConnectionsCheckBox CheckChanged {t}");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    Constraints.SetAllOutputOutsideConnections(m_currBuildingId, t);
                    UpdateUIDistrictsSummary();
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
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    if (!TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId))
                    {
                        UpdateUISupplyReserve();
                        return;
                    }

                    if (string.IsNullOrEmpty(UISupplyReserve.text.Trim()))
                    {
                        UpdateUISupplyReserve();
                        return;
                    }
                    else
                    {
                        try
                        {
                            // TODO, FIXME: Do this in a single transaction and clean up hacky implementation below.
                            var amount = ushort.Parse(UISupplyReserve.text);
                            Constraints.SetInternalSupplyReserve(m_currBuildingId, amount);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogException(ex);
                        }
                    }

                    UpdateUISupplyReserve();
                });
            };

            UISupplyChainIn.eventClicked += (c, p) =>
            {
                Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyChainIn Clicked");
            };

            UISupplyChainIn.eventTextCancelled += (c, p) =>
            {
                Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyChainIn TextCancelled");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    UpdateUISupplyChainIn();
                });
            };

            UISupplyChainIn.eventTextSubmitted += (c, p) =>
            {
                Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyChainIn TextSubmitted");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    if (!TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId))
                    {
                        UpdateUISupplyChainIn();
                        return;
                    }

                    if (string.IsNullOrEmpty(UISupplyChainIn.text.Trim()))
                    {
                        Constraints.RemoveAllSupplyChainConnectionsToDestination(m_currBuildingId);
                    }
                    else
                    {
                        try
                        {
                            // TODO, FIXME: Do this in a single transaction and clean up hacky implementation below.
                            var sources = UISupplyChainIn.text.Split(',').Select(s => ushort.Parse(s));

                            foreach (var source in sources)
                            {
                                if (TransferManagerInfo.IsSupplyChainBuilding(source))
                                {
                                    Constraints.AddSupplyChainConnection(source, m_currBuildingId);
                                }
                            }

                            Constraints.RemoveAllSupplyChainConnectionsToDestination(m_currBuildingId);
                            foreach (var source in sources)
                            {
                                if (TransferManagerInfo.IsSupplyChainBuilding(source))
                                {
                                    Constraints.AddSupplyChainConnection(source, m_currBuildingId);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogException(ex);
                        }
                    }

                    UpdateUISupplyChainIn();
                    UpdateUIDistrictsDropdown(updateChecked: false);
                    UpdateUIDistrictsSummary();
                });
            };

            UISupplyChainOut.eventClicked += (c, p) =>
            {
                Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyChainOut Clicked");
            };

            UISupplyChainOut.eventTextCancelled += (c, p) =>
            {
                Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyChainOut TextCancelled");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    UpdateUISupplyChainOut();
                });
            };

            UISupplyChainOut.eventTextSubmitted += (c, p) =>
            {
                Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyChainOut TextSubmitted");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    if (!TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId))
                    {
                        UpdateUISupplyChainOut();
                        return;
                    }

                    if (string.IsNullOrEmpty(UISupplyChainOut.text.Trim()))
                    {
                        Constraints.RemoveAllSupplyChainConnectionsFromSource(m_currBuildingId);
                    }
                    else
                    {
                        try
                        {
                            // TODO, FIXME: Do this in a single transaction and clean up hacky implementation below.
                            var destinations = UISupplyChainOut.text.Split(',').Select(s => ushort.Parse(s));

                            foreach (var destination in destinations)
                            {
                                if (TransferManagerInfo.IsSupplyChainBuilding(destination))
                                {
                                    Constraints.AddSupplyChainConnection(m_currBuildingId, destination);
                                }
                            }

                            Constraints.RemoveAllSupplyChainConnectionsFromSource(m_currBuildingId);
                            foreach (var destination in destinations)
                            {
                                if (TransferManagerInfo.IsSupplyChainBuilding(destination))
                                {
                                    Constraints.AddSupplyChainConnection(m_currBuildingId, destination);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogException(ex);
                        }
                    }

                    UpdateUISupplyChainOut();
                    UpdateUIDistrictsDropdown(updateChecked: false);
                    UpdateUIDistrictsSummary();
                });
            };

            UIDistrictsDropDown.eventCheckedChanged += (c, t) =>
            {
                if (m_currBuildingId == 0 || m_districtParkMapping == null)
                {
                    return;
                }

                if (UIDistrictsDropDown.GetChecked(t) == Constraints.OutputDistrictParkServiced(m_currBuildingId)?.Contains(m_districtParkMapping[t]))
                {
                    return;
                }

                Logger.LogVerbose($"EnhancedDistrictServicedUIPanel::UIDistrictsDropDown CheckChanged: {t}");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    if (UIDistrictsDropDown.GetChecked(t))
                    {
                        Constraints.AddOutputDistrictParkServiced(m_currBuildingId, m_districtParkMapping[t]);
                    }
                    else
                    {
                        Constraints.RemoveOutputDistrictParkServiced(m_currBuildingId, m_districtParkMapping[t]);
                    }
                });
            };

            UIDistrictsDropDown.eventDropdownClose += UIDistrictsDropDown_eventDropdownClose;
        }

        private void UIDistrictsDropDown_eventDropdownClose(ColossalFramework.UI.UICheckboxDropDown checkboxdropdown, ColossalFramework.UI.UIScrollablePanel popup, ref bool overridden)
        {
            UpdateUIDistrictsSummary();
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
            Logger.Log($"EnhancedDistrictServicedUIPanel::SetBuilding: buildingId={building}");
            if (TransferManagerInfo.IsDistrictServicesBuilding(building))
            {
                m_currBuildingId = building;
            }
            else
            {
                m_currBuildingId = 0;
            }

            if (TransferManagerInfo.IsOutsideBuilding(building))
            {
                // Need to enable this so that the user can roughly determine where the connection is ...
                Camera.main.GetComponent<CameraController>().m_unlimitedCamera = true;
            }

            UpdateUITitle();
            UpdateUIBuildingId();
            UpdateUIHomeDistrict();
            UpdateUIServices();
            UpdateUIAllLocalAreasCheckBox();
            UpdateUIAllOutsideConnectionsCheckBox();
            UpdateUISupplyReserve();
            UpdateUISupplyChainIn();
            UpdateUISupplyChainOut();
            UpdateUIDistrictsDropdown(updateChecked: true);

            UpdateUIDistrictsSummary();

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

            UITitle.tooltip = "Click on service building to configure";
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

        private void UpdateUIAllLocalAreasCheckBox()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIAllLocalAreasCheckBox Update");
            if (m_currBuildingId != 0)
            {
                UIAllLocalAreasCheckBox.isChecked = Constraints.OutputAllLocalAreas(m_currBuildingId);
                UIAllLocalAreasCheckBox.readOnly = false;
            }
            else
            {
                UIAllLocalAreasCheckBox.isChecked = false;
                UIAllLocalAreasCheckBox.readOnly = true;
            }

            UIAllLocalAreasCheckBox.label.text = "All Local Areas: ";
            UIAllLocalAreasCheckBox.tooltip = "If enabled, serves all local areas.  Disable to specify Supply Chain Out or Districts Served restrictions.";
        }

        private void UpdateUIAllOutsideConnectionsCheckBox()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIAllOutsideConnectionsCheckBox Update");
            if (m_currBuildingId != 0 && TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId))
            {
                UIAllOutsideConnectionsCheckBox.Show();
                UIAllOutsideConnectionsCheckBox.isChecked = Constraints.OutputOutsideConnections(m_currBuildingId);
                UIAllOutsideConnectionsCheckBox.readOnly = false;
            }
            else
            {
                UIAllOutsideConnectionsCheckBox.Hide();
                UIAllOutsideConnectionsCheckBox.isChecked = false;
                UIAllOutsideConnectionsCheckBox.readOnly = true;
            }

            UIAllOutsideConnectionsCheckBox.tooltip = "If enabled, serves all outside connections.";
            UIAllOutsideConnectionsCheckBox.label.text = "All Outside Connections: ";
        }

        private void UpdateUISupplyReserve()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyReserve Update");
            if (m_currBuildingId == 0 || !TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId))
            {
                UISupplyReserve.Hide();
                UISupplyReserveLabel.Hide();
                UISupplyReserve.readOnly = true;
                UISupplyReserve.text = "(Disabled)";
                UISupplyReserve.tooltip = "This policy is not applicable for non-supply chain buildings.";
            }
            else
            {
                UISupplyReserve.Show();
                UISupplyReserveLabel.Show();
                UISupplyReserve.readOnly = false;

                var tooltipText = "(Supply Chain Buildings Only):\nThe percentage of goods to reserve for allowed districts and supply out buildings.\nEnter a value between 0 and 100 inclusive.";

                UISupplyReserve.text = Constraints.InternalSupplyBuffer(m_currBuildingId).ToString();
                UISupplyReserve.tooltip = tooltipText;
                UISupplyReserveLabel.tooltip = tooltipText;
            }
        }

        private void UpdateUISupplyChainIn()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyChainIn Update");
            if (m_currBuildingId == 0 || !TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId))
            {
                UISupplyChainIn.Hide();
                UISupplyChainInLabel.Hide();
                UISupplyChainIn.readOnly = true;

                UISupplyChainIn.text = "(Disabled)";
                UISupplyChainIn.tooltip = "This policy is not applicable for non-supply chain buildings.";
            }
            else
            {
                UISupplyChainIn.Show();
                UISupplyChainInLabel.Show();
                UISupplyChainIn.readOnly = false;

                var tooltipText = "(Supply Chain Buildings Only):\nEnter a comma delimited list of building ids to incoming shipments from those buildings.";
                if (Constraints.SupplySources(m_currBuildingId)?.Count > 0)
                {
                    UISupplyChainIn.text = string.Join(",", Constraints.SupplySources(m_currBuildingId).Select(b => b.ToString()).ToArray());
                    UISupplyChainIn.tooltip = TransferManagerInfo.GetSupplySourcesText(m_currBuildingId);
                    UISupplyChainInLabel.tooltip = tooltipText;
                }
                else
                {
                    UISupplyChainIn.text = "";
                    UISupplyChainIn.tooltip = tooltipText;
                    UISupplyChainInLabel.tooltip = tooltipText;
                }
            }
        }

        private void UpdateUISupplyChainOut()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyChainOut Update");
            if (m_currBuildingId == 0 || !TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId))
            {
                UISupplyChainOut.Hide();
                UISupplyChainOutLabel.Hide();
                UISupplyChainOut.readOnly = true;

                UISupplyChainOut.text = "(Disabled)";
                UISupplyChainOut.tooltip = "This policy is not applicable for non-supply chain buildings.";
            }
            else if (Constraints.OutputAllLocalAreas(m_currBuildingId))
            {
                UISupplyChainOut.Hide();
                UISupplyChainOutLabel.Hide();
                UISupplyChainOut.readOnly = true;

                UISupplyChainOut.text = "(Disabled)";
                UISupplyChainOut.tooltip = "All Local Areas enabled.  This policy will not be applied if all local areas are enabled.";
            }
            else
            {
                UISupplyChainOut.Show();
                UISupplyChainOutLabel.Show();
                UISupplyChainOut.readOnly = false;

                var tooltipText = "(Supply Chain Buildings Only):\nEnter a comma delimited list of building ids to restrict outgoing shipments to those buildings.\nClear to enable districts served restrictions.";

                if (Constraints.SupplyDestinations(m_currBuildingId)?.Count > 0)
                {
                    UISupplyChainOut.text = string.Join(",", Constraints.SupplyDestinations(m_currBuildingId).Select(b => b.ToString()).ToArray());
                    UISupplyChainOut.tooltip = TransferManagerInfo.GetSupplyDestinationsText(m_currBuildingId);
                    UISupplyChainOutLabel.tooltip = tooltipText;
                }
                else
                {
                    UISupplyChainOut.text = "";
                    UISupplyChainOut.tooltip = tooltipText;
                    UISupplyChainOutLabel.tooltip = tooltipText;
                }
            }
        }

        private void UpdateUIDistrictsDropdown(bool updateChecked)
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIDistrictsDropdown Update");

            void SetChecked(int i, bool ischecked)
            {
                if (UIDistrictsDropDown.GetChecked(i) != ischecked)
                {
                    UIDistrictsDropDown.SetChecked(i, ischecked);
                }
            }

            if (m_currBuildingId == 0 || Constraints.OutputAllLocalAreas(m_currBuildingId) || Constraints.SupplyDestinations(m_currBuildingId)?.Count > 0)
            {
                UIDistrictsDropDown.Hide();
                UIDistrictsSummary.Hide();
                UIDistrictsDropDown.triggerButton.Disable();

                if (updateChecked)
                {
                    // Do not used UICheckboxDropDown::SetChecked(bool[] isChecked) because it replaces the underlying array.
                    for (int i = 0; i < m_districtParkMapping.Count; i++)
                    {
                        SetChecked(i, false);
                    }
                }
            }
            else
            {
                if (m_currBuildingId != 0 && TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId))
                {
                    UIDistrictsDropDown.relativePosition = new Vector3(3, 168 + 3);
                    UIDistrictsSummary.relativePosition = new Vector3(3, 168);
                }
                else
                {
                    UIDistrictsDropDown.relativePosition = new Vector3(3, 108 + 3);
                    UIDistrictsSummary.relativePosition = new Vector3(3, 108);
                }

                UIDistrictsDropDown.Show();
                UIDistrictsSummary.Show();
                UIDistrictsDropDown.triggerButton.Enable();

                if (updateChecked)
                {
                    var districtParkServed = Constraints.OutputDistrictParkServiced(m_currBuildingId);
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
            }
        }

        private void UpdateUIDistrictsDropdownDistrictItems()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIDistrictsDropdownDistrictItems Update");
            var districtParks = DistrictPark.GetAllDistrictParks();

            UIDistrictsDropDown.Clear();
            m_districtParkMapping.Clear();

            foreach (var districtPark in districtParks)
            {
                if (!Settings.showParkDistricts.value && districtPark.IsPark)
                {
                    continue;
                }

                UIDistrictsDropDown.AddItem(districtPark.Name, isChecked: false);
                m_districtParkMapping.Add(districtPark);
            }

            Logger.LogVerbose($"EnhancedDistrictServicedUIPanel::UIDistrictsDropdownDistrictItems Found {m_districtParkMapping.Count} districts.");
        }

        private void UpdateUIDistrictsSummary()
        {
            if (m_currBuildingId == 0)
            {
                UIDistrictsSummary.text = "Districts served:";
                UIDistrictsDropDown.triggerButton.tooltip = "";
                return;
            }

            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIDistrictsSummary Update");
            var homeDistrictPark = TransferManagerInfo.GetDistrictPark(m_currBuildingId);
            var districtParkServed = Constraints.OutputDistrictParkServiced(m_currBuildingId);

            if (Constraints.OutputAllLocalAreas(m_currBuildingId))
            {
                UIDistrictsSummary.text = "Districts served: (Disabled)";
                UIDistrictsDropDown.triggerButton.tooltip = "All Local Areas enabled.  This policy will not be applied if all local areas are enabled.";

            }
            else if (districtParkServed == null || districtParkServed.Count == 0)
            {
                UIDistrictsSummary.text = "Districts served: None";
                UIDistrictsDropDown.triggerButton.tooltip = TransferManagerInfo.GetOutputDistrictsServedText(m_currBuildingId);
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

                UIDistrictsDropDown.triggerButton.tooltip = TransferManagerInfo.GetOutputDistrictsServedText(m_currBuildingId);
            }
            else
            {
                UIDistrictsSummary.text = $"Districts served: {districtParkServed.Count} others";
                UIDistrictsDropDown.triggerButton.tooltip = TransferManagerInfo.GetOutputDistrictsServedText(m_currBuildingId);
            }
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
