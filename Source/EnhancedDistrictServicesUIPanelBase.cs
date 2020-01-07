using ColossalFramework.UI;
using UnityEngine;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// This class contain all the GUI elements that define the world info panel that users use to interact with the
    /// tool.  This is perhaps not the neatest way to separate the GUI from the underlying logic, but on the other hand
    /// MVVM is probably overkill for our purposes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnhancedDistrictServicesUIPanelBase<T> : UIPanel where T : UIPanel
    {
        #region Static methods and properties

        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static T Instance { get; private set; }

        /// <summary>
        /// Reference to the main camera.
        /// </summary>
        private static CameraController m_cameraController;
        public static CameraController CameraController
        {
            get
            {
                if (m_cameraController == null)
                {
                    GameObject gameObjectWithTag = GameObject.FindGameObjectWithTag("MainCamera");
                    if (gameObjectWithTag != null)
                    {
                        m_cameraController = gameObjectWithTag.GetComponent<CameraController>();
                    }
                }

                return m_cameraController;
            }
        }

        /// <summary>
        /// Create the singleton instance.
        /// </summary>
        public static void Create()
        {
            if (Instance == null)
            {
                var view = UIView.GetAView();
                Instance = (T)view.AddUIComponent(typeof(T));
                Instance.Hide();
            }
        }

        /// <summary>
        /// Destroy the singleton instance.
        /// </summary>
        public static void Destroy()
        {
            if (Instance != null && Instance.gameObject != null)
            {
                Destroy(Instance.gameObject);
                Instance = null;
            }
        }

        #endregion

        #region Dynamic GUI arranging

        private int m_shownElements = 0;

        public void AddTabContainerRow()
        {
            m_shownElements++;
        }

        public void AddElementToTabContainerRow(UIComponent component)
        {
            int y = 92 + 20 * m_shownElements;
            component.relativePosition = new Vector3(component.relativePosition.x, y);
            component.Show();
        }

        public void ClearTabContainerElements()
        {
            m_shownElements = 0;
        }

        public void ShowComponent(UIComponent component, bool show)
        {
            if (show)
            {
                component.Show();
            }
            else
            {
                component.Hide();
            }
        }

        #endregion

        #region GUI layout

        private const int m_componentPadding = 3;
        private const int m_componentWidth = 400;
        private const int m_componentHeight = 800;
        private const int m_listScrollbarWidth = 20;

        private Transform m_CameraTransform;
        private UIComponent m_FullscreenContainer;

        public UILabel UITitle;
        public UIButton UICloseButton;
        public UILabel UIBuildingIdLabel;
        public UITextField UIBuildingId;
        public UILabel UIHomeDistrict;
        public UILabel UIServices;

        public UITabstrip UIInputMode;
        public UIButton UIIncomingTab;
        public UIButton UIOutgoingTab;
        public UIButton UIVehiclesTab;
        public UIButton UIGlobalTab;

        public UICheckBox UIAllLocalAreasCheckBox;
        public UICheckBox UIAllOutsideConnectionsCheckBox;
        public UILabel UISupplyReserveLabel;
        public UITextField UISupplyReserve;
        public UILabel UISupplyChainLabel;
        public UITextField UISupplyChain;
        public UILabel UIDistrictsSummary;
        public UICheckboxDropDown UIDistrictsDropDown;

        public UICheckBox UIVehicleDefaultsCheckBox;
        public UILabel UIVehiclesSummary;
        public UICheckboxDropDown UIVehiclesDropDown;

        public UILabel GlobalIntensityLabel;
        public UITextField GlobalIntensity;

        /// <summary>
        /// Layout the GUI.
        /// </summary>
        public override void Start()
        {
            base.Start();

            name = GetType().Name;
            backgroundSprite = "MenuPanel2";
            size = new Vector2(m_componentWidth + 2 * m_componentPadding, 200);

            UITitle = AttachUILabelTo(this, 3, 3, height: 25);
            UITitle.textAlignment = UIHorizontalAlignment.Center;
            UITitle.textScale = 1.0f;

            UICloseButton = AttachUIButtonTo(this, 372, 0);

            UIBuildingIdLabel = AttachUILabelTo(this, 3, 28, text: $"Building Id: ");
            UIBuildingId = AttachUITextFieldTo(this, 3, 28, 78);
            UIHomeDistrict = AttachUILabelTo(this, 3, 48);
            UIServices = AttachUILabelTo(this, 3, 68);

            var buttonTemplate = GetUITabstripButtonTemplate(this);
            UIInputMode = AttachUITabstripTo(this, 3, 88);
            UIOutgoingTab = UIInputMode.AddTab("Outgoing", buttonTemplate, true);
            UIIncomingTab = UIInputMode.AddTab("Incoming", buttonTemplate, true);
            if (Settings.enableCustomVehicles)
                UIVehiclesTab = UIInputMode.AddTab("Vehicles", buttonTemplate, true);
            UIGlobalTab = UIInputMode.AddTab("Global", buttonTemplate, true);

            UIAllLocalAreasCheckBox = AttachUICheckBoxTo(this, 113, 88);
            UIAllLocalAreasCheckBox.label = AttachUILabelTo(UIAllLocalAreasCheckBox, -110, 0);
            UIAllOutsideConnectionsCheckBox = AttachUICheckBoxTo(this, 336, 88);
            UIAllOutsideConnectionsCheckBox.label = AttachUILabelTo(UIAllOutsideConnectionsCheckBox, -173, 0);
            UISupplyReserveLabel = AttachUILabelTo(this, 3, 108, text: $"Supply Reserve: ");
            UISupplyReserve = AttachUITextFieldTo(this, 3, 108, 112);
            UISupplyChainLabel = AttachUILabelTo(this, 3, 128, text: $"Supply Chain: ");
            UISupplyChain = AttachUITextFieldTo(this, 3, 128, 111);

            UIDistrictsSummary = AttachUILabelTo(this, 3, 148);
            UIDistrictsSummary.zOrder = 0;
            UIDistrictsDropDown = AttachUICheckboxDropDownTo(this, 3, 148 + 3);
            UIDistrictsDropDown.eventDropdownOpen += UIEventDropdownOpen;
            UIDistrictsDropDown.eventDropdownClose += UIEventDropdownClose;

            UIVehicleDefaultsCheckBox = AttachUICheckBoxTo(this, 141, 88);
            UIVehicleDefaultsCheckBox.label = AttachUILabelTo(UIVehicleDefaultsCheckBox, -138, 0);
            UIVehiclesSummary = AttachUILabelTo(this, 3, 148);
            UIVehiclesSummary.zOrder = 0;
            UIVehiclesDropDown = AttachUICheckboxDropDownTo(this, 3, 148 + 3);
            UIVehiclesDropDown.eventDropdownOpen += UIEventDropdownOpen;
            UIVehiclesDropDown.eventDropdownClose += UIEventDropdownClose;

            GlobalIntensityLabel = AttachUILabelTo(this, 3, 88, text: "Outside Connection Intensity:");
            GlobalIntensity = AttachUITextFieldTo(this, 3, 29, 213);

            m_FullscreenContainer = UIView.Find("FullScreenContainer");
            m_FullscreenContainer.AttachUIComponent(gameObject);

            if (Camera.main != null)
            {
                m_CameraTransform = Camera.main.transform;
            }
        }

        /// <summary>
        /// Cleanup UI objects on destroy.
        /// </summary>
        public override void OnDestroy()
        {
            foreach (var c in GetComponentsInChildren<UIComponent>())
            {
                Destroy(c.gameObject);
            }

            base.OnDestroy();
        }

        /// <summary>
        /// Shift the policies panel to the building.
        /// </summary>
        /// <param name="building"></param>
        public void UpdatePanelToBuilding(ushort building)
        {
            if (m_CameraTransform == null)
            {
                return;
            }

            var component = GetComponent<UIComponent>();

            if (InstanceManager.GetPosition(new InstanceID { Building = building }, out Vector3 position, out Quaternion _, out Vector3 size))
            {
                position.y += size.y * 0.8f;
            }
            else
            {
                // Shift the GUI out of the way a little bit.
                // TODO: Make a better GUI with a close button ...
                component.relativePosition = new Vector3(m_componentWidth, 0); 
                return;
            }

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

        /// <summary>
        /// Move the main camera to the building's position.
        /// </summary>
        /// <param name="building"></param>
        public void UpdatePositionToBuilding(ushort building)
        {
            if (TransferManagerInfo.IsDistrictServicesBuilding(building))
            {
                var position = BuildingManager.instance.m_buildings.m_buffer[building].m_position;
                CameraController.SetTarget(new InstanceID { Building = building }, position, false);
            }
        }

        private void UIEventDropdownOpen(UICheckboxDropDown checkboxdropdown, UIScrollablePanel popup, ref bool overridden)
        {
            checkboxdropdown?.triggerButton?.Hide();

            if (popup != null && popup.verticalScrollbar != null)
            {
                popup.verticalScrollbar.isVisible = true;
            }
        }

        private void UIEventDropdownClose(UICheckboxDropDown checkboxdropdown, UIScrollablePanel popup, ref bool overridden)
        {
            checkboxdropdown?.triggerButton?.Show();

            if (popup != null && popup.verticalScrollbar != null)
            {
                popup.verticalScrollbar.isVisible = false;
            }
        }

        #endregion

        #region Graphical elements setup

        private static UIButton AttachUIButtonTo(UIComponent parent, int x, int y)
        {
            var uiButton = parent.AddUIComponent<UIButton>();
            uiButton.relativePosition = new Vector3(x, y);
            uiButton.size = new Vector2(28f, 28f);
            uiButton.normalFgSprite = "buttonclose";
            uiButton.hoveredFgSprite = "buttonclosehover";
            uiButton.pressedFgSprite = "buttonclosepressed";
            uiButton.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            uiButton.horizontalAlignment = UIHorizontalAlignment.Right;
            uiButton.verticalAlignment = UIVerticalAlignment.Middle;
            uiButton.zOrder = 1;

            uiButton.text = "";
            uiButton.textVerticalAlignment = UIVerticalAlignment.Middle;
            uiButton.textHorizontalAlignment = UIHorizontalAlignment.Left;
            uiButton.textScale = 0.8f;

            uiButton.normalBgSprite = "ButtonMenu";
            uiButton.disabledBgSprite = "ButtonMenuDisabled";
            uiButton.hoveredBgSprite = "ButtonMenuHovered";
            uiButton.focusedBgSprite = "ButtonMenu";
            uiButton.pressedBgSprite = "ButtonMenuPressed";

            uiButton.eventClick += (c, p) =>
            {
                uiButton?.parent?.Hide();
            };

            return uiButton;
        }

        private static UICheckBox AttachUICheckBoxTo(UIComponent parent, int x, int y)
        {
            var checkBox = parent.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsCheckBoxTemplate")) as UICheckBox;
            checkBox.relativePosition = new Vector3(x, y);
            checkBox.text = "";
            checkBox.size = new Vector2(14f, 14f);
            checkBox.autoSize = false;

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
            button.relativePosition = new Vector3(0.0f, -2.0f);
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

        private static UILabel AttachUILabelTo(UIComponent parent, int x, int y, int height = 20, string text = "")
        {
            var label = parent.AddUIComponent<UILabel>();
            label.text = text;
            label.relativePosition = new Vector3(x, y);
            label.size = new Vector2(m_componentWidth, height);
            label.autoSize = false;
            label.textAlignment = UIHorizontalAlignment.Left;
            label.verticalAlignment = UIVerticalAlignment.Middle;
            label.textScale = 0.8f;
            return label;
        }

        private static UITabstrip AttachUITabstripTo(UIComponent parent, int x, int y)
        {
            var tabstrip = parent.AddUIComponent<UITabstrip>();
            tabstrip.relativePosition = new Vector3(x, y);
            tabstrip.size = new Vector2(m_componentWidth, 20);
            return tabstrip;
        }

        private static UITextField AttachUITextFieldTo(UIComponent parent, int x, int y, int xOffset)
        {
            x = x + xOffset;
            xOffset = m_componentWidth - (xOffset + 3);

            var textField = parent.AddUIComponent<UITextField>();
            textField.relativePosition = new Vector3(x, y);
            textField.size = new Vector2(xOffset, 14f);
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

        private static UIButton GetUITabstripButtonTemplate(UIComponent parent)
        {
            var buttonTemplate = parent.AddUIComponent<UIButton>();
            buttonTemplate.Hide();

            buttonTemplate.text = "";
            buttonTemplate.size = new Vector2(23 * (m_componentWidth - m_listScrollbarWidth) / 100, 20f);
            buttonTemplate.normalFgSprite = "GenericTab";
            buttonTemplate.hoveredFgSprite = "GenericTabHovered";
            buttonTemplate.pressedFgSprite = "GenericTabPressed";
            buttonTemplate.focusedFgSprite = "GenericTabFocused";
            buttonTemplate.textVerticalAlignment = UIVerticalAlignment.Top;
            buttonTemplate.textHorizontalAlignment = UIHorizontalAlignment.Left;
            buttonTemplate.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            buttonTemplate.zOrder = 1;
            buttonTemplate.textScale = 0.8f;

            return buttonTemplate;
        }

        #endregion
    }
}
