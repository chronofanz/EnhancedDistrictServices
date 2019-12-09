using ColossalFramework;
using ColossalFramework.UI;
using System;
using UnityEngine;

namespace EnhancedDistrictServices
{
    public class UITitlePanel : UIPanel
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static UITitlePanel Instance { get; private set; }

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
                Instance = (UITitlePanel)view.AddUIComponent(typeof(UITitlePanel));
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

        public UILabel UITitle;
        public UILabel GlobalIntensityLabel;
        public UITextField GlobalIntensity;

        /// <summary>
        /// Layout the GUI.
        /// </summary>
        public override void Start()
        {
            base.Start();

            var uiView = component?.GetUIView();
            if (uiView == null)
            {
                return;
            }

            var screenSize = uiView.GetScreenResolution();

            name = GetType().Name;
            backgroundSprite = "InfoPanelBack";
            size = new Vector2(298, 48);
            relativePosition = new Vector3(3 * (screenSize.x - size.x) / 4, 10);

            UITitle = AddUIComponent<UILabel>();
            UITitle.text = "Enhanced District Services Tool";
            UITitle.textScale = 1.1f;
            UITitle.relativePosition = new Vector3(3, 3);
            UITitle.size = new Vector2(298, 28);
            UITitle.autoSize = false;
            UITitle.textAlignment = UIHorizontalAlignment.Center;
            UITitle.verticalAlignment = UIVerticalAlignment.Middle;
            UITitle.tooltip = "Click on service building to configure";

            GlobalIntensityLabel = AttachUILabelTo(this, 3, 29, text: "Outside Connection Intensity:");
            GlobalIntensityLabel.tooltip = "The intensity controls the amount of supply chain traffic entering the city, between 0 and 100\nWARNING: Do not set this too high, otherwise your traffic will become overwhelmed with traffic!";

            GlobalIntensity = AttachUITextFieldTo(this, 3, 29, 213);
            GlobalIntensity.eventTextCancelled += (c, p) =>
            {
                Logger.LogVerbose("UITitlePanel::GlobalIntensity TextCancelled");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    UpdatePanel();
                });
            };

            GlobalIntensity.eventTextSubmitted += (c, p) =>
            {
                Logger.LogVerbose("UITitlePanel::GlobalIntensity TextSubmitted");
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    if (string.IsNullOrEmpty(GlobalIntensity.text.Trim()))
                    {
                        UpdatePanel();
                        return;
                    }
                    else
                    {
                        try
                        {
                            // TODO, FIXME: Do this in a single transaction and clean up hacky implementation below.
                            var amount = ushort.Parse(GlobalIntensity.text);
                            Constraints.SetGlobalOutsideConnectionIntensity(amount);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogException(ex);
                        }
                    }

                    UpdatePanel();
                });
            };
        }

        public override void OnEnable()
        {
            base.OnEnable();

            Singleton<SimulationManager>.instance.AddAction(() =>
            {
                UpdatePanel();
            });

            Show();
        }

        private void UpdatePanel()
        {
            GlobalIntensity.text = Constraints.GlobalOutsideConnectionIntensity().ToString();
        }

        private UILabel AttachUILabelTo(UIComponent parent, int x, int y, int height = 20, string text = "")
        {
            var label = parent.AddUIComponent<UILabel>();
            label.text = text;
            label.relativePosition = new Vector3(x, y);
            label.size = new Vector2(size.x, height);
            label.autoSize = false;
            label.textAlignment = UIHorizontalAlignment.Left;
            label.verticalAlignment = UIVerticalAlignment.Middle;
            label.textScale = 0.8f;
            return label;
        }

        private UITextField AttachUITextFieldTo(UIComponent parent, int x, int y, int xOffset)
        {
            x = x + xOffset;
            xOffset = (int)size.x - 3 - (xOffset + 3);

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
    }
}
