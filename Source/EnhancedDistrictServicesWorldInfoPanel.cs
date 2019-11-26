using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnhancedDistrictServices
{
    public class EnhancedDistrictServicesWorldInfoPanel : EnhancedDistrictServicesUIPanelBase<EnhancedDistrictServicesWorldInfoPanel>
    {
        private const int m_componentPadding = 3;
        private const int m_componentWidth = 274;
        private const int m_componentHeight = 490;
        private const int m_listScrollbarWidth = 20;

        private Transform m_CameraTransform;
        private UIComponent m_FullscreenContainer;

        public UILabel UITitle;
        public UITextField UIBuildingId;
        public UILabel UIHomeDistrict;
        public UITextField UISupplyChainIn;
        public UITextField UISupplyChainOut;
        public UICheckBox UIAllLocalAreasCheckBox;
        public UICheckBox UIAllOutsideConnectionsCheckBox;
        public UILabel UIDistrictsSummary;
        public UICheckboxDropDown UIDistrictsDropDown;

        // Mapping of dropdown index to district number.
        private readonly List<int> m_districtsMapping = new List<int>(capacity: DistrictManager.MAX_DISTRICT_COUNT);

        // Store current building id.
        private ushort m_currBuildingId = 0;

        public override void Start()
        {
            base.Start();

            name = GetType().Name;
            backgroundSprite = "InfoPanelBack";
            size = new Vector2(m_componentWidth + 2 * m_componentPadding, 200);

            UITitle = AttachUILabelTo(this, 3, 3);
            UITitle.textAlignment = UIHorizontalAlignment.Center;
            UITitle.tooltip = "Click on service building to configure";

            UIBuildingId = AttachUICompositeTextFieldTo(this, 3, 23, 78, $"Building Id: ");
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

            UIHomeDistrict = AttachUILabelTo(this, 3, 43);

            UISupplyChainIn = AttachUICompositeTextFieldTo(this, 3, 63, 111, $"Supply Chain In: ");
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

            UISupplyChainOut = AttachUICompositeTextFieldTo(this, 3, 83, 123, $"Supply Chain Out: ");
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

            UIAllLocalAreasCheckBox = AttachUICheckBoxTo(this, 3, 103);
            UIAllLocalAreasCheckBox.tooltip = "If enabled, serves all local areas.  Overrides Districts Served restrictons below.";
            UIAllLocalAreasCheckBox.label.text = "All Local Areas";

            UIAllLocalAreasCheckBox.eventCheckChanged += (c, t) =>
            {
                Constraints.SetAllLocalAreas(m_currBuildingId, t, true);
                UpdateRestrictionSummary(m_currBuildingId);
            };

            UIAllOutsideConnectionsCheckBox = AttachUICheckBoxTo(this, 3, 123);
            UIAllOutsideConnectionsCheckBox.tooltip = "If enabled, serves all outside connections.";
            UIAllOutsideConnectionsCheckBox.label.text = "All Outside Connections";

            UIAllOutsideConnectionsCheckBox.eventCheckChanged += (c, t) =>
            {
                Constraints.SetAllOutsideConnections(m_currBuildingId, t, true);
                UpdateRestrictionSummary(m_currBuildingId);
            };

            UIDistrictsSummary = AttachUILabelTo(this, 3, 146);
            UIDistrictsSummary.zOrder = 0;

            UIDistrictsDropDown = AttachUICheckboxDropDownTo(this, 3, 3 + 146);
            UIDistrictsDropDown.eventDropdownOpen += DistrictsDropDown_eventDropdownOpen;
            UIDistrictsDropDown.eventDropdownClose += DistrictsDropDown_eventDropdownClose;

            UIDistrictsDropDown.eventCheckedChanged += (c, t) =>
            {
                if (UIDistrictsDropDown.GetChecked(t))
                {
                    Constraints.AddDistrictRestriction(m_currBuildingId, m_districtsMapping[t]);
                }
                else
                {
                    Constraints.RemoveDistrictRestriction(m_currBuildingId, m_districtsMapping[t]);
                }

                UpdateRestrictionSummary(m_currBuildingId);
            };

            UIDistrictsDropDown.eventSizeChanged += (c, t) =>
            {
                UIDistrictsDropDown.triggerButton.size = t;
                UIDistrictsDropDown.listWidth = (int)t.x;
            };

            m_FullscreenContainer = UIView.Find("FullScreenContainer");
            m_FullscreenContainer.AttachUIComponent(gameObject);

            if (Camera.main != null)
            {
                m_CameraTransform = Camera.main.transform;
            }
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

                UpdatePosition(worldMousePosition, building);
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

        private void DistrictsDropDown_eventDropdownOpen(UICheckboxDropDown checkboxdropdown, UIScrollablePanel popup, ref bool overridden)
        {
            popup.verticalScrollbar.isVisible = true;
        }

        private void DistrictsDropDown_eventDropdownClose(UICheckboxDropDown checkboxdropdown, UIScrollablePanel popup, ref bool overridden)
        {
            popup.verticalScrollbar.isVisible = false;
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

        private void UpdatePosition(Vector3 worldMousePosition, ushort building)
        {
            if (m_CameraTransform == null)
                return;

            if (InstanceManager.GetPosition(new InstanceID { Building = building }, out Vector3 position, out Quaternion rotation, out Vector3 size))
                position.y += size.y * 0.8f;
            else
                position = worldMousePosition;

            Vector3 vector3_1 = Camera.main.WorldToScreenPoint(position) * Mathf.Sign(Vector3.Dot(position - m_CameraTransform.position, m_CameraTransform.forward));
            UIView uiView = component.GetUIView();
            Vector2 vector2 = m_FullscreenContainer == null ? uiView.GetScreenResolution() : this.m_FullscreenContainer.size;
            Vector3 vector3_2 = vector3_1 / uiView.inputScale;
            Vector3 transform = component.pivot.UpperLeftToTransform(component.size, component.arbitraryPivotOffset);
            Vector3 vector3_3 = uiView.ScreenPointToGUI(vector3_2) + new Vector2(transform.x, transform.y);
            if (vector3_3.x < 0.0)
                vector3_3.x = 0.0f;
            if (vector3_3.y < 0.0)
                vector3_3.y = 0.0f;
            if (vector3_3.x + (double)component.width > vector2.x)
                vector3_3.x = vector2.x - component.width;
            if (vector3_3.y + (double)m_componentHeight > vector2.y)
                vector3_3.y = vector2.y - m_componentHeight;
            component.relativePosition = vector3_3;
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

        #region Graphical elements setup

        private static UICheckBox AttachUICheckBoxTo(UIComponent parent, int x, int y)
        {
            var checkBox = parent.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsCheckBoxTemplate")) as UICheckBox;
            checkBox.relativePosition = new Vector3(x, y);
            checkBox.text = "";
            checkBox.size = new Vector2(m_componentWidth, 20f);
            checkBox.autoSize = false;

            checkBox.label = AttachUILabelTo(checkBox, 20, 3);

            return checkBox;
        }

        private static UICheckboxDropDown AttachUICheckboxDropDownTo(UIComponent parent, int x, int y)
        {
            var dropDown = parent.AddUIComponent<UICheckboxDropDown>();
            dropDown.relativePosition = new Vector3(x, y);
            dropDown.size = new Vector2(m_componentWidth - m_listScrollbarWidth, 20f);
            dropDown.listBackground = "GenericPanelLight";
            dropDown.itemHeight = 20;
            dropDown.itemHover = "ListItemHover";
            dropDown.itemHighlight = "ListItemHighlight";
            dropDown.checkedSprite = "InfoIconDistrictsFocused";
            dropDown.uncheckedSprite = "";
            dropDown.normalBgSprite = "ButtonMenu";
            dropDown.disabledBgSprite = "ButtonMenuDisabled";
            dropDown.hoveredBgSprite = "ButtonMenuHovered";
            dropDown.focusedBgSprite = "ButtonMenu";
            dropDown.listWidth = (int)dropDown.size.x;
            dropDown.listHeight = 400;
            dropDown.foregroundSpriteMode = UIForegroundSpriteMode.Stretch;
            dropDown.popupColor = new Color32(45, 52, 61, byte.MaxValue);
            dropDown.popupTextColor = new Color32(170, 170, 170, byte.MaxValue);
            dropDown.zOrder = 2;
            dropDown.textScale = 0.8f;
            dropDown.verticalAlignment = UIVerticalAlignment.Middle;
            dropDown.horizontalAlignment = UIHorizontalAlignment.Left;
            dropDown.textFieldPadding = new RectOffset(4, 0, 4, 0);
            dropDown.itemPadding = new RectOffset(10, 0, 4, 0);

            var button = dropDown.AddUIComponent<UIButton>();
            button.text = "";
            button.size = dropDown.size;
            button.relativePosition = new Vector3(0.0f, 0.0f);
            button.textVerticalAlignment = UIVerticalAlignment.Middle;
            button.textHorizontalAlignment = UIHorizontalAlignment.Left;
            button.normalFgSprite = "IconDownArrow";
            button.hoveredFgSprite = "IconDownArrowHovered";
            button.pressedFgSprite = "IconDownArrowPressed";
            button.focusedFgSprite = "IconDownArrowFocused";
            button.disabledFgSprite = "IconDownArrowDisabled";
            button.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            button.horizontalAlignment = UIHorizontalAlignment.Right;
            button.verticalAlignment = UIVerticalAlignment.Middle;
            button.zOrder = 1;
            button.textScale = 0.8f;
            dropDown.triggerButton = button;

            var listScrollbar = dropDown.AddUIComponent<UIScrollbar>();
            listScrollbar.orientation = UIOrientation.Vertical;
            listScrollbar.minValue = 0f;
            listScrollbar.maxValue = dropDown.listHeight;
            listScrollbar.incrementAmount = 1f;
            listScrollbar.width = m_listScrollbarWidth;
            listScrollbar.height = dropDown.listHeight;
            listScrollbar.pivot = UIPivotPoint.BottomLeft;
            listScrollbar.AlignTo(dropDown, UIAlignAnchor.TopRight);
            listScrollbar.isVisible = false;
            listScrollbar.zOrder = 0;
            dropDown.listScrollbar = listScrollbar;

            var tracSprite = listScrollbar.AddUIComponent<UISlicedSprite>();
            tracSprite.relativePosition = Vector2.zero;
            tracSprite.autoSize = true;
            tracSprite.size = tracSprite.parent.size;
            tracSprite.fillDirection = UIFillDirection.Vertical;
            tracSprite.spriteName = "ScrollbarTrack";
            listScrollbar.trackObject = tracSprite;

            UISlicedSprite thumbSprite = tracSprite.AddUIComponent<UISlicedSprite>();
            thumbSprite.relativePosition = Vector2.zero;
            thumbSprite.fillDirection = UIFillDirection.Vertical;
            thumbSprite.autoSize = true;
            thumbSprite.width = thumbSprite.parent.width - 8;
            thumbSprite.spriteName = "ScrollbarThumb";
            listScrollbar.thumbObject = thumbSprite;

            return dropDown;
        }

        private static UITextField AttachUICompositeTextFieldTo(UIComponent parent, int x, int y, int textFieldOffset, string labelText)
        {
            var label = AttachUILabelTo(parent, x, y);
            label.text = labelText;

            var textField = AttachUITextField(parent, x + textFieldOffset, y + 2, m_componentWidth - (textFieldOffset + 3));
            return textField;
        }

        private static UILabel AttachUILabelTo(UIComponent parent, int x, int y)
        {
            var label = parent.AddUIComponent<UILabel>();
            label.relativePosition = new Vector3(x, y);
            label.size = new Vector2(m_componentWidth, 20f);
            label.autoSize = false;
            label.textAlignment = UIHorizontalAlignment.Left;
            label.verticalAlignment = UIVerticalAlignment.Middle;
            label.textScale = 0.8f;
            return label;
        }

        private static UITextField AttachUITextField(UIComponent parent, int x, int y, int width)
        {
            var textField = parent.AddUIComponent<UITextField>();
            textField.relativePosition = new Vector3(x, y);
            textField.size = new Vector2(width, 14f);
            textField.autoSize = false;

            textField.builtinKeyNavigation = true;
            textField.readOnly = false;
            textField.canFocus = true;
            textField.isInteractive = true;
            textField.enabled = true;
            textField.color = Color.white;
            textField.bottomColor = Color.white;
            textField.textColor = Color.black;
            textField.cursorBlinkTime = 0.45f;
            textField.cursorWidth = 1;
            textField.selectionSprite = "EmptySprite";
            textField.normalBgSprite = "TextFieldPanel";
            textField.hoveredBgSprite = "TextFieldPanelHovered";
            textField.focusedBgSprite = "TextFieldPanel";
            textField.textScale = 0.8f;
            textField.horizontalAlignment = UIHorizontalAlignment.Left;
            textField.verticalAlignment = UIVerticalAlignment.Middle;

            return textField;
        }


        #endregion
    }
}
