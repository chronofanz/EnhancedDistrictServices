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

        #region GUI layout

        private const int m_componentPadding = 3;
        private const int m_componentWidth = 274;
        private const int m_componentHeight = 530;
        private const int m_listScrollbarWidth = 20;

        private Transform m_CameraTransform;
        private UIComponent m_FullscreenContainer;

        public UILabel UITitle;
        public UITextField UIBuildingId;
        public UILabel UIHomeDistrict;
        public UILabel UIServices;
        public UITextField UISupplyChainIn;
        public UITextField UISupplyChainOut;
        public UICheckBox UIAllLocalAreasCheckBox;
        public UICheckBox UIAllOutsideConnectionsCheckBox;
        public UILabel UIDistrictsSummary;
        public UICheckboxDropDown UIDistrictsDropDown;

        private UIComponent m_component;
        public UIComponent component
        {
            get
            {
                if (m_component == null)
                    m_component = GetComponent<UIComponent>();
                return m_component;
            }
        }

        /// <summary>
        /// Layout the GUI.
        /// </summary>
        public override void Start()
        {
            base.Start();

            name = GetType().Name;
            backgroundSprite = "InfoPanelBack";
            size = new Vector2(m_componentWidth + 2 * m_componentPadding, 200);

            UITitle = AttachUILabelTo(this, 3, 3);
            UITitle.textAlignment = UIHorizontalAlignment.Center;

            UIBuildingId = AttachUICompositeTextFieldTo(this, 3, 23, 78, $"Building Id: ");
            UIHomeDistrict = AttachUILabelTo(this, 3, 43);
            UIServices = AttachUILabelTo(this, 3, 63);
            UISupplyChainIn = AttachUICompositeTextFieldTo(this, 3, 83, 111, $"Supply Chain In: ");
            UISupplyChainOut = AttachUICompositeTextFieldTo(this, 3, 103, 123, $"Supply Chain Out: ");
            UIAllLocalAreasCheckBox = AttachUICheckBoxTo(this, 3, 123);
            UIAllOutsideConnectionsCheckBox = AttachUICheckBoxTo(this, 3, 143);

            UIDistrictsSummary = AttachUILabelTo(this, 3, 166);
            UIDistrictsSummary.zOrder = 0;

            UIDistrictsDropDown = AttachUICheckboxDropDownTo(this, 3, 3 + 166);
            UIDistrictsDropDown.eventDropdownOpen += UIDistrictsDropDown_eventDropdownOpen;
            UIDistrictsDropDown.eventDropdownClose += UIDistrictsDropDown_eventDropdownClose;
            UIDistrictsDropDown.eventSizeChanged += UIDistrictsDropDown_eventSizeChanged;

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
        /// Move the main camera to the building's position.
        /// </summary>
        /// <param name="worldMousePosition"></param>
        /// <param name="building"></param>
        public void UpdatePositionToBuilding(ushort building)
        {
            if (m_CameraTransform == null)
            {
                return;
            }

            if (InstanceManager.GetPosition(new InstanceID { Building = building }, out Vector3 position, out Quaternion _, out Vector3 size))
            {
                position.y += size.y * 0.8f;
            }
            else
            {
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

        private void UIDistrictsDropDown_eventDropdownOpen(UICheckboxDropDown checkboxdropdown, UIScrollablePanel popup, ref bool overridden)
        {
            popup.verticalScrollbar.isVisible = true;
        }

        private void UIDistrictsDropDown_eventDropdownClose(UICheckboxDropDown checkboxdropdown, UIScrollablePanel popup, ref bool overridden)
        {
            popup.verticalScrollbar.isVisible = false;
        }

        private void UIDistrictsDropDown_eventSizeChanged(UIComponent component, Vector2 value)
        {
            UIDistrictsDropDown.triggerButton.size = value;
            UIDistrictsDropDown.listWidth = (int)value.x;
        }

        #endregion

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
