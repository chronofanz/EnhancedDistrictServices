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
        /// Mapping of dropdown index to district number. 
        /// </summary>
        private readonly List<int> m_districtsMapping = new List<int>(capacity: DistrictManager.MAX_DISTRICT_COUNT);

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

            UIAllLocalAreasCheckBox.eventCheckChanged += (c, t) =>
            {
                if (m_currBuildingId == 0 || t == Constraints.AllLocalAreas(m_currBuildingId))
                {
                    return;
                }

                Logger.LogVerbose($"EnhancedDistrictServicedUIPanel::UIAllLocalAreasCheckBox CheckChanged {t}");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    Constraints.SetAllLocalAreas(m_currBuildingId, t);
                    UpdateUISupplyChainOut();
                    UpdateUIDistrictsDropdown(updateChecked: false);
                    UpdateUIDistrictsSummary();
                });
            };

            UIAllOutsideConnectionsCheckBox.eventCheckChanged += (c, t) =>
            {
                if (m_currBuildingId == 0 || t == Constraints.OutsideConnections(m_currBuildingId))
                {
                    return;
                }

                Logger.LogVerbose($"EnhancedDistrictServicedUIPanel::UIAllOutsideConnectionsCheckBox CheckChanged {t}");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    Constraints.SetAllOutsideConnections(m_currBuildingId, t);
                    UpdateUIDistrictsSummary();
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
                if (m_currBuildingId == 0 || m_districtsMapping == null)
                {
                    return;
                }

                if (UIDistrictsDropDown.GetChecked(t) == Constraints.DistrictServiced(m_currBuildingId)?.Contains(m_districtsMapping[t]))
                {
                    return;
                }

                Logger.LogVerbose($"EnhancedDistrictServicedUIPanel::UIDistrictsDropDown CheckChanged: {t}");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    if (UIDistrictsDropDown.GetChecked(t))
                    {
                        Constraints.AddDistrictServiced(m_currBuildingId, m_districtsMapping[t]);
                    }
                    else
                    {
                        Constraints.RemoveDistrictServiced(m_currBuildingId, m_districtsMapping[t]);
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
            UpdateUISupplyChainIn();
            UpdateUISupplyChainOut();
            UpdateUIDistrictsDropdown(updateChecked: true);

            UpdateUIDistrictsSummary();
            Show();
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
                UIHomeDistrict.text = TransferManagerInfo.GetDistrictText(m_currBuildingId);
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
                UIAllLocalAreasCheckBox.isChecked = Constraints.AllLocalAreas(m_currBuildingId);
                UIAllLocalAreasCheckBox.readOnly = false;
            }
            else
            {
                UIAllLocalAreasCheckBox.isChecked = false;
                UIAllLocalAreasCheckBox.readOnly = true;
            }

            UIAllLocalAreasCheckBox.label.text = "All Local Areas";
            UIAllLocalAreasCheckBox.tooltip = "If enabled, serves all local areas.  Overrides Supply Chain Out and Districts Served restrictions.";
        }

        private void UpdateUIAllOutsideConnectionsCheckBox()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIAllOutsideConnectionsCheckBox Update");
            if (m_currBuildingId != 0)
            {
                UIAllOutsideConnectionsCheckBox.isChecked = Constraints.OutsideConnections(m_currBuildingId);
                UIAllOutsideConnectionsCheckBox.readOnly = false;
            }
            else
            {
                UIAllOutsideConnectionsCheckBox.isChecked = false;
                UIAllOutsideConnectionsCheckBox.readOnly = true;
            }

            UIAllOutsideConnectionsCheckBox.tooltip = "If enabled, serves all outside connections.";
            UIAllOutsideConnectionsCheckBox.label.text = "All Outside Connections";
        }

        private void UpdateUISupplyChainIn()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyChainIn Update");
            if (m_currBuildingId == 0 || !TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId))
            {
                UISupplyChainIn.readOnly = true;

                UISupplyChainIn.text = "(Disabled)";
                UISupplyChainIn.tooltip = "This policy is not applicable for non-supply chain buildings.";
            }
            else
            {
                UISupplyChainIn.readOnly = false;

                if (Constraints.SupplySources(m_currBuildingId)?.Count > 0)
                {
                    UISupplyChainIn.text = string.Join(",", Constraints.SupplySources(m_currBuildingId).Select(b => b.ToString()).ToArray());
                    UISupplyChainIn.tooltip = TransferManagerInfo.GetSupplySourcesText(m_currBuildingId);
                }
                else
                {
                    UISupplyChainIn.text = "";
                    UISupplyChainIn.tooltip = "(Supply Chain Buildings Only):\nEnter a comma delimited list of building ids to restrict incoming shipments to those buildings.";
                }
            }
        }

        private void UpdateUISupplyChainOut()
        {
            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UISupplyChainOut Update");
            if (m_currBuildingId == 0 || !TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId))
            {
                UISupplyChainOut.readOnly = true;

                UISupplyChainOut.text = "(Disabled)";
                UISupplyChainOut.tooltip = "This policy is not applicable for non-supply chain buildings.";
            }
            else if (Constraints.AllLocalAreas(m_currBuildingId))
            {
                UISupplyChainOut.readOnly = true;

                UISupplyChainOut.text = "(Disabled)";
                UISupplyChainOut.tooltip = "All Local Areas enabled.  This policy will not be applied if all local areas are enabled.";
            }
            else
            {
                UISupplyChainOut.readOnly = false;

                if (Constraints.SupplyDestinations(m_currBuildingId)?.Count > 0)
                {
                    UISupplyChainOut.text = string.Join(",", Constraints.SupplyDestinations(m_currBuildingId).Select(b => b.ToString()).ToArray());
                    UISupplyChainOut.tooltip = TransferManagerInfo.GetSupplyDestinationsText(m_currBuildingId);
                }
                else
                {
                    UISupplyChainOut.text = "";
                    UISupplyChainOut.tooltip = "(Supply Chain Buildings Only):\nEnter a comma delimited list of building ids to restrict outgoing shipments to those buildings.\nOverrides Districts Served restrictions.";
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

            if (m_currBuildingId == 0 || Constraints.AllLocalAreas(m_currBuildingId) || Constraints.SupplyDestinations(m_currBuildingId)?.Count > 0)
            {
                UIDistrictsDropDown.triggerButton.Disable();

                if (updateChecked)
                {
                    // Do not used UICheckboxDropDown::SetChecked(bool[] isChecked) because it replaces the underlying array.
                    for (int i = 0; i < m_districtsMapping.Count; i++)
                    {
                        SetChecked(i, false);
                    }
                }
            }
            else
            {
                UIDistrictsDropDown.triggerButton.Enable();

                if (updateChecked)
                {
                    var districtsServed = Constraints.DistrictServiced(m_currBuildingId);
                    if (districtsServed != null)
                    {
                        // Do not used UICheckboxDropDown::SetChecked(bool[] isChecked) because it replaces the underlying array.
                        for (int i = 0; i < m_districtsMapping.Count; i++)
                        {
                            SetChecked(i, districtsServed.Contains(m_districtsMapping[i]));
                        }
                    }
                    else
                    {
                        // Do not used UICheckboxDropDown::SetChecked(bool[] isChecked) because it replaces the underlying array.
                        for (int i = 0; i < m_districtsMapping.Count; i++)
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
            var districtNames = new SortedDictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
            for (byte district = 1; district < DistrictManager.MAX_DISTRICT_COUNT; district++)
            {
                if ((DistrictManager.instance.m_districts.m_buffer[district].m_flags & District.Flags.Created) != 0)
                {
                    var districtName = DistrictManager.instance.GetDistrictName(district);
                    districtNames.Add(districtName, district);
                }
            }

            UIDistrictsDropDown.Clear();
            m_districtsMapping.Clear();

            foreach (var kvp in districtNames)
            {
                UIDistrictsDropDown.AddItem(kvp.Key, isChecked: false);
                m_districtsMapping.Add(kvp.Value);
            }

            Logger.LogVerbose($"EnhancedDistrictServicedUIPanel::UIDistrictsDropdownDistrictItems Found {m_districtsMapping.Count} districts.");
        }

        private void UpdateUIDistrictsSummary()
        {
            if (m_currBuildingId == 0)
            {
                return;
            }

            Logger.LogVerbose("EnhancedDistrictServicedUIPanel::UIDistrictsSummary Update");
            var homeDistrict = TransferManagerInfo.GetDistrict(m_currBuildingId);
            var districtsServed = Constraints.DistrictServiced(m_currBuildingId);

            if (Constraints.AllLocalAreas(m_currBuildingId))
            {
                UIDistrictsSummary.text = "Districts served: (Disabled)";
                UIDistrictsDropDown.triggerButton.tooltip = "All Local Areas enabled.  This policy will not be applied if all local areas are enabled.";

            }
            else if (Constraints.SupplyDestinations(m_currBuildingId)?.Count > 0)
            {
                UIDistrictsSummary.text = "Districts served: (Disabled)";
                UIDistrictsDropDown.triggerButton.tooltip = "Supply Chain Out enabled.  This policy will not be applied if supply chain out specified.";
            }
            else if (districtsServed == null || districtsServed.Count == 0)
            {
                UIDistrictsSummary.text = "Districts served: None";
                UIDistrictsDropDown.triggerButton.tooltip = TransferManagerInfo.GetDistrictsServedText(m_currBuildingId);
            }
            else if (homeDistrict != 0 && districtsServed.Contains(homeDistrict))
            {
                if (districtsServed.Count == 1)
                {
                    UIDistrictsSummary.text = $"Districts served: Home only";
                }
                else
                {
                    UIDistrictsSummary.text = $"Districts served: Home + {districtsServed.Count - 1} others";
                }

                UIDistrictsDropDown.triggerButton.tooltip = TransferManagerInfo.GetDistrictsServedText(m_currBuildingId);
            }
            else
            {
                UIDistrictsSummary.text = $"Districts served: {districtsServed.Count} others";
                UIDistrictsDropDown.triggerButton.tooltip = TransferManagerInfo.GetDistrictsServedText(m_currBuildingId);
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
