using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnhancedDistrictServices
{
    public class EnhancedDistrictServicesUIPanel : EnhancedDistrictServicesUIPanelBase<EnhancedDistrictServicesUIPanel>
    {
        // Mapping of dropdown index to district number.
        private readonly List<int> m_districtsMapping = new List<int>(capacity: DistrictManager.MAX_DISTRICT_COUNT);

        // Store current building id.
        private ushort m_currBuildingId = 0;

        public override void Start()
        {
            base.Start();

            UITitle.tooltip = "Click on service building to configure";
            UIBuildingId.tooltip = "Enter a new building id to configure that building";

            UIBuildingId.eventClicked += (c, p) =>
            {
                UIBuildingId.text = "";
            };

            UIBuildingId.eventTextCancelled += (c, p) =>
            {
                UpdateBuildingId(m_currBuildingId);
            };

            UIBuildingId.eventTextSubmitted += (c, p) =>
            {
                if (ushort.TryParse(UIBuildingId.text, out ushort buildingId2) &&
                    TransferManagerInfo.IsDistrictServicesBuilding(buildingId2))
                {
                    var position = BuildingManager.instance.m_buildings.m_buffer[buildingId2].m_position;

                    var buildingInstanceID = new InstanceID
                    {
                        Building = buildingId2
                    };

                    CameraController.SetTarget(buildingInstanceID, position, false);
                    SetTarget(position, buildingId2);
                }
                else
                {
                    UpdateBuildingId(m_currBuildingId);
                }
            };

            UISupplyChainIn.tooltip = "(Supply Chain Buildings Only):\nEnter a comma delimited list of building ids to restrict incoming shipments to those buildings.";
            UISupplyChainIn.eventClicked += (c, p) =>
            {
            };

            UISupplyChainIn.eventTextCancelled += (c, p) =>
            {
                UpdateSupplyChainIn(m_currBuildingId);
            };

            UISupplyChainIn.eventTextSubmitted += (c, p) =>
            {
                if (!TransferManagerInfo.IsSupplyChainBuilding((ushort)m_currBuildingId))
                {
                    UpdateSupplyChainIn(m_currBuildingId);
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

                UpdateSupplyChainIn(m_currBuildingId);
            };

            UISupplyChainOut.tooltip = "(Supply Chain Buildings Only):\nEnter a comma delimited list of building ids to restrict outgoing shipments to those buildings.\nOverrides all other options below.";
            UISupplyChainOut.eventClicked += (c, p) =>
            {
            };

            UISupplyChainOut.eventTextCancelled += (c, p) =>
            {
                UpdateSupplyChainOut(m_currBuildingId);
            };

            UISupplyChainOut.eventTextSubmitted += (c, p) =>
            {
                if (!TransferManagerInfo.IsSupplyChainBuilding((ushort)m_currBuildingId))
                {
                    UpdateSupplyChainOut(m_currBuildingId);
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

                UpdateSupplyChainOut(m_currBuildingId);
            };

            UIAllLocalAreasCheckBox.tooltip = "If enabled, serves all local areas.  Overrides Districts Served restrictons below.";
            UIAllLocalAreasCheckBox.label.text = "All Local Areas";

            UIAllLocalAreasCheckBox.eventCheckChanged += (c, t) =>
            {
                Constraints.SetAllLocalAreas(m_currBuildingId, t, true);
                UpdateRestrictionSummary(m_currBuildingId);
            };

            UIAllOutsideConnectionsCheckBox.tooltip = "If enabled, serves all outside connections.";
            UIAllOutsideConnectionsCheckBox.label.text = "All Outside Connections";

            UIAllOutsideConnectionsCheckBox.eventCheckChanged += (c, t) =>
            {
                Constraints.SetAllOutsideConnections(m_currBuildingId, t, true);
                UpdateRestrictionSummary(m_currBuildingId);
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

                UpdateRestrictionSummary(m_currBuildingId);
            };
        }

        public void Activate()
        {
            if (UIDistrictsDropDown == null)
            {
                return;
            }

            UIDistrictsDropDown.Clear();
            m_districtsMapping.Clear();

            var districtNames = new SortedDictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

            // TODO: Add in alphabetical order!
            for (int district = 1; district < DistrictManager.MAX_DISTRICT_COUNT; district++)
            {
                if ((DistrictManager.instance.m_districts.m_buffer[district].m_flags & District.Flags.Created) != 0)
                {
                    var districtName = DistrictManager.instance.GetDistrictName(district);
                    districtNames.Add(districtName, district);
                }
            }

            foreach (var kvp in districtNames)
            {
                UIDistrictsDropDown.AddItem(kvp.Key, isChecked: false);
                m_districtsMapping.Add(kvp.Value);
            }

            SetTarget(Vector3.zero, 0);
        }

        public void SetTarget(Vector3 worldMousePosition, ushort building)
        {
            if (TransferManagerInfo.IsDistrictServicesBuilding(building))
            {
                UISupplyChainIn.readOnly = !TransferManagerInfo.IsSupplyChainBuilding(building);
                UISupplyChainOut.readOnly = !TransferManagerInfo.IsSupplyChainBuilding(building);
                UIAllLocalAreasCheckBox.readOnly = false;
                UIAllOutsideConnectionsCheckBox.readOnly = false;
                UIDistrictsDropDown.triggerButton.Enable();

                UpdatePositionToBuilding(worldMousePosition, building);
                UpdatePanel(worldMousePosition, building);
            }
            else
            {
                UISupplyChainIn.readOnly = true;
                UISupplyChainOut.readOnly = true;
                UIAllLocalAreasCheckBox.readOnly = true;
                UIAllOutsideConnectionsCheckBox.readOnly = true;
                UIDistrictsDropDown.triggerButton.Disable();

                UpdatePanel(worldMousePosition, 0);
            }

            Show();
        }

        private void UpdatePanel(Vector3 worldMousePosition, ushort building)
        {
            if (TransferManagerInfo.IsDistrictServicesBuilding(building))
            {
                UITitle.text = TransferManagerInfo.GetBuildingName(building);

                UpdateBuildingId(building);
                UpdateHomeDistrict(building);
                UpdateSupplyChainIn(building);
                UpdateSupplyChainOut(building);
                UpdateRestrictionSummary(building);

                m_currBuildingId = building;

                UIAllLocalAreasCheckBox.isChecked = Constraints.BuildingToAllLocalAreas[building];
                UIAllOutsideConnectionsCheckBox.isChecked = Constraints.BuildingToOutsideConnections[building];

                var restrictions = Constraints.BuildingToDistrictServiced[building];
                if (restrictions != null)
                {
                    for (int index = 0; index < m_districtsMapping.Count; index++)
                    {
                        UIDistrictsDropDown.SetChecked(m_districtsMapping.Select(district => restrictions.Contains(district)).ToArray());
                    }
                }
                else
                {
                    for (int index = 0; index < m_districtsMapping.Count; index++)
                    {
                        UIDistrictsDropDown.SetChecked(index, false);
                    }
                }
            }
            else
            {
                UITitle.text = "(Enhanced District Services Tool)";
                UIBuildingId.text = "";
                UIHomeDistrict.text = $"Home district:";
                UISupplyChainIn.text = "";
                UISupplyChainOut.text = "";
                UIDistrictsSummary.text = string.Empty;

                m_currBuildingId = 0;

                for (int index = 0; index < m_districtsMapping.Count; index++)
                {
                    UIDistrictsDropDown.SetChecked(index, false);
                }
            }
        }

        private void UpdateBuildingId(int buildingId)
        {
            if (buildingId != 0)
            {
                UIBuildingId.text = $"{buildingId}";
            }
            else
            {
                UIBuildingId.text = $"";
            }
        }

        private void UpdateHomeDistrict(int buildingId)
        {
            var position = BuildingManager.instance.m_buildings.m_buffer[buildingId].m_position;
            var homeDistrict = DistrictManager.instance.GetDistrict(position);

            if (homeDistrict != 0)
            {
                var homeDistrictName = DistrictManager.instance.GetDistrictName((int)homeDistrict);
                UIHomeDistrict.text = $"Home district: {homeDistrictName}";
            }
            else
            {
                UIHomeDistrict.text = $"Home district:";
            }
        }

        private void UpdateSupplyChainIn(int buildingId)
        {
            string buildingNameList()
            {
                if (Constraints.SupplySources[buildingId]?.Count > 0)
                {
                    var buildingNames = Constraints.SupplySources[buildingId]
                        .Select(b => TransferManagerInfo.GetBuildingName(b))
                        .OrderBy(s => s);

                    var sb = new StringBuilder();
                    sb.AppendLine("Incoming Shipments Only From:");
                    foreach (var buildingName in buildingNames)
                    {
                        sb.AppendLine(buildingName);
                    }

                    return sb.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }

            if (Constraints.SupplySources[buildingId]?.Count > 0)
            {
                UISupplyChainIn.text = string.Join(",", Constraints.SupplySources[buildingId].Select(b => b.ToString()).ToArray());
                UISupplyChainIn.tooltip = buildingNameList();
            }
            else
            {
                UISupplyChainIn.text = "";
                UISupplyChainIn.tooltip = "(Supply Chain Buildings Only):\nEnter a comma delimited list of building ids to restrict incoming shipments to those buildings.";
            }
        }

        private void UpdateSupplyChainOut(int buildingId)
        {
            string buildingNameList()
            {
                if (Constraints.SupplyDestinations[buildingId]?.Count > 0)
                {
                    var buildingNames = Constraints.SupplyDestinations[buildingId]
                        .Select(b => TransferManagerInfo.GetBuildingName(b))
                        .OrderBy(s => s);

                    var sb = new StringBuilder();
                    sb.AppendLine("Outgoing Shipments Only To:");
                    foreach (var buildingName in buildingNames)
                    {
                        sb.AppendLine(buildingName);
                    }

                    return sb.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }

            if (Constraints.SupplyDestinations[buildingId]?.Count > 0)
            {
                UISupplyChainOut.text = string.Join(",", Constraints.SupplyDestinations[buildingId].Select(b => b.ToString()).ToArray());
                UISupplyChainOut.tooltip = buildingNameList();
            }
            else
            {
                UISupplyChainOut.text = "";
                UISupplyChainOut.tooltip = "(Supply Chain Buildings Only):\nEnter a comma delimited list of building ids to restrict outgoing shipments to those buildings.\nOverrides all other options below.";
            }
        }

        private void UpdateRestrictionSummary(int buildingId)
        {
            string districtNameList()
            {
                var districts = Constraints.BuildingToDistrictServiced[buildingId];
                if (districts?.Count == 0)
                {
                    return string.Empty;
                }

                if (districts.Count == 1)
                {
                    return $"Districts served: {DistrictManager.instance.GetDistrictName(districts[0])}";
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Districts served:");

                    foreach (var districtName in districts.Select(d => DistrictManager.instance.GetDistrictName(d)).OrderBy(d => d))
                    {
                        sb.AppendLine(districtName);
                    }

                    return sb.ToString();
                }
            }

            var position = BuildingManager.instance.m_buildings.m_buffer[buildingId].m_position;
            var homeDistrict = DistrictManager.instance.GetDistrict(position);

            var restrictions = Constraints.BuildingToDistrictServiced[buildingId];
            if (Constraints.BuildingToAllLocalAreas[buildingId])
            {
                UIDistrictsSummary.text = "Districts served: All local areas";
                UIDistrictsDropDown.triggerButton.tooltip = "Districts served: All local areas";
            }
            else if (restrictions == null || restrictions.Count == 0)
            {
                UIDistrictsSummary.text = "Districts served: None";
                UIDistrictsDropDown.triggerButton.tooltip = "";
            }
            else if (homeDistrict != 0 && restrictions.Contains(homeDistrict))
            {
                if (restrictions.Count == 1)
                {
                    UIDistrictsSummary.text = $"Districts served: home only";
                    UIDistrictsDropDown.triggerButton.tooltip = districtNameList();
                }
                else
                {
                    UIDistrictsSummary.text = $"Districts served: home + {restrictions.Count - 1} others";
                    UIDistrictsDropDown.triggerButton.tooltip = districtNameList();
                }
            }
            else
            {
                UIDistrictsSummary.text = $"Districts served: {restrictions.Count} others";
                UIDistrictsDropDown.triggerButton.tooltip = districtNameList();
            }
        }
    }
}
