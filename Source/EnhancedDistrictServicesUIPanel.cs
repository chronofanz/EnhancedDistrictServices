using System;
using System.Collections.Generic;
using System.Linq;

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

            UIBuildingId.eventClicked += (c, p) => 
            {
                UIBuildingId.text = "";
            };

            UIBuildingId.eventTextCancelled += (c, p) =>
            {
                UpdateUIBuildingId();
            };

            UIBuildingId.eventTextSubmitted += (c, p) =>
            {
                if (ushort.TryParse(UIBuildingId.text, out ushort buildingId) &&
                    TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
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
            };

            UISupplyChainIn.eventClicked += (c, p) =>
            {
            };

            UISupplyChainIn.eventTextCancelled += (c, p) =>
            {
                UpdateUISupplyChainIn();
            };

            UISupplyChainIn.eventTextSubmitted += (c, p) =>
            {
                if (!TransferManagerInfo.IsSupplyChainBuilding((ushort)m_currBuildingId))
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
                        // TODO, FIXME: Do this in a single transaction.
                        Constraints.RemoveAllSupplyChainConnectionsToDestination(m_currBuildingId);

                        var sources = UISupplyChainIn.text.Split(',').Select(s => ushort.Parse(s));
                        foreach (var source in sources)
                        {
                            if (TransferManagerInfo.IsSupplyChainBuilding(source))
                            {
                                Constraints.AddSupplyChainConnection(source, m_currBuildingId);
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                UpdateUISupplyChainIn();
                UpdateUIDistrictsDropdown();
                UpdateUIDistrictsSummary();
            };

            UISupplyChainOut.eventClicked += (c, p) =>
            {
            };

            UISupplyChainOut.eventTextCancelled += (c, p) =>
            {
                UpdateUISupplyChainOut();
            };

            UISupplyChainOut.eventTextSubmitted += (c, p) =>
            {
                if (!TransferManagerInfo.IsSupplyChainBuilding((ushort)m_currBuildingId))
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
                        // TODO, FIXME: Do this in a single transaction.
                        Constraints.RemoveAllSupplyChainConnectionsFromSource(m_currBuildingId);

                        var destinations = UISupplyChainOut.text.Split(',').Select(s => ushort.Parse(s));
                        foreach (var destination in destinations)
                        {
                            if (TransferManagerInfo.IsSupplyChainBuilding(destination))
                            {
                                Constraints.AddSupplyChainConnection(m_currBuildingId, destination);
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                UpdateUISupplyChainOut();
                UpdateUIDistrictsDropdown();
                UpdateUIDistrictsSummary();
            };

            UIAllLocalAreasCheckBox.eventCheckChanged += (c, t) =>
            {
                Constraints.SetAllLocalAreas(m_currBuildingId, t);
                UpdateUIDistrictsDropdown();
                UpdateUIDistrictsSummary();
            };

            UIAllOutsideConnectionsCheckBox.eventCheckChanged += (c, t) =>
            {
                Constraints.SetAllOutsideConnections(m_currBuildingId, t);
                UpdateUIDistrictsSummary();
            };

            UIDistrictsDropDown.eventCheckedChanged += (c, t) =>
            {
                if (UIDistrictsDropDown.GetChecked(t))
                {
                    Constraints.AddDistrictServiced(m_currBuildingId, m_districtsMapping[t]);
                }
                else
                {
                    Constraints.RemoveDistrictServiced(m_currBuildingId, m_districtsMapping[t]);
                }

                UpdateUIDistrictsSummary();
            };
        }

        public override void OnEnable()
        {
            if (UIDistrictsDropDown == null)
            {
                return;
            }

            base.OnEnable();
            UpdateUIDistrictsDropdownDistrictItems();
            SetBuilding(0);
        }

        public void SetBuilding(ushort building)
        {
            if (TransferManagerInfo.IsDistrictServicesBuilding(building))
            {
                m_currBuildingId = building;
            }
            else
            {
                m_currBuildingId = 0;
            }

            UpdateUITitle();
            UpdateUIBuildingId();
            UpdateUIHomeDistrict();
            UpdateUIServices();
            UpdateUISupplyChainIn();
            UpdateUISupplyChainOut();
            UpdateUIAllLocalAreasCheckBox();
            UpdateUIAllOutsideConnectionsCheckBox();
            UpdateUIDistrictsDropdown();

            UpdateUIDistrictsSummary();
            Show();
        }

        private void UpdateUITitle()
        {
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
            UIBuildingId.text = m_currBuildingId != 0 ? $"{m_currBuildingId}" : string.Empty;
            UIBuildingId.tooltip = "Enter a new building id to configure that building";
        }

        private void UpdateUIHomeDistrict()
        {
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
            if (m_currBuildingId != 0)
            {
                UIServices.text = TransferManagerInfo.GetServicesText(m_currBuildingId);
            }
            else
            {
                UIServices.text = "Service:";
            }
        }

        private void UpdateUISupplyChainIn()
        {
            if (m_currBuildingId != 0 && TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId))
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
            else
            {
                UISupplyChainIn.readOnly = true;

                UISupplyChainIn.text = "";
                UISupplyChainIn.tooltip = "(Supply Chain Buildings Only):\nEnter a comma delimited list of building ids to restrict incoming shipments to those buildings.";
            }
        }

        private void UpdateUISupplyChainOut()
        {
            if (m_currBuildingId != 0 && TransferManagerInfo.IsSupplyChainBuilding(m_currBuildingId))
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
                    UISupplyChainOut.tooltip = "(Supply Chain Buildings Only):\nEnter a comma delimited list of building ids to restrict outgoing shipments to those buildings.\nOverrides all other options below.";
                }
            }
            else
            {
                UISupplyChainOut.readOnly = true;

                UISupplyChainOut.text = "";
                UISupplyChainOut.tooltip = "(Supply Chain Buildings Only):\nEnter a comma delimited list of building ids to restrict outgoing shipments to those buildings.\nOverrides all other options below.";
            }
        }

        private void UpdateUIAllLocalAreasCheckBox()
        {
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
            UIAllLocalAreasCheckBox.tooltip = "If enabled, serves all local areas.  Overrides Districts Served restrictions below.";
        }

        private void UpdateUIAllOutsideConnectionsCheckBox()
        {
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

        private void UpdateUIDistrictsDropdown()
        {
            if (m_currBuildingId == 0 || Constraints.AllLocalAreas(m_currBuildingId) || Constraints.SupplyDestinations(m_currBuildingId)?.Count > 0)
            {
                UIDistrictsDropDown.triggerButton.Disable();
                UIDistrictsDropDown.SetChecked(m_districtsMapping.Select(district => false).ToArray());
            }
            else
            {
                UIDistrictsDropDown.triggerButton.Enable();

                var districtsServiced = Constraints.DistrictServiced(m_currBuildingId);
                if (districtsServiced != null)
                {
                    UIDistrictsDropDown.SetChecked(m_districtsMapping.Select(district => districtsServiced.Contains(district)).ToArray());
                }
                else
                {
                    UIDistrictsDropDown.SetChecked(m_districtsMapping.Select(district => false).ToArray());
                }
            }

            UIDistrictsDropDown.triggerButton.tooltip = TransferManagerInfo.GetDistrictsServedText(m_currBuildingId);
        }

        private void UpdateUIDistrictsDropdownDistrictItems()
        {
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
        }

        private void UpdateUIDistrictsSummary()
        {
            var homeDistrict = TransferManagerInfo.GetDistrict(m_currBuildingId);
            var districtsServed = Constraints.DistrictServiced(m_currBuildingId);

            if (Constraints.AllLocalAreas(m_currBuildingId))
            {
                UIDistrictsSummary.text = "Districts served: All local areas";
            }
            else if (districtsServed == null || districtsServed.Count == 0)
            {
                UIDistrictsSummary.text = "Districts served: None";
            }
            else if (homeDistrict != 0 && districtsServed.Contains(homeDistrict))
            {
                if (districtsServed.Count == 1)
                {
                    UIDistrictsSummary.text = $"Districts served: home only";
                }
                else
                {
                    UIDistrictsSummary.text = $"Districts served: home + {districtsServed.Count - 1} others";
                }
            }
            else
            {
                UIDistrictsSummary.text = $"Districts served: {districtsServed.Count} others";
            }
        }
    }
}
