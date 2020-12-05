using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnhancedDistrictServices
{
    public class UIEDSButton : UIButton
    {
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

        public override void Start()
        {
            LoadResources();

            name = "EDS";
            tooltip = "Left-click to enable tool.  Right-click to move EDS button.";

            normalFgSprite = "EDS";
            disabledFgSprite = "EDSDisabled";
            focusedFgSprite = "EDSFocused";
            hoveredFgSprite = "EDSHovered";
            pressedFgSprite = "EDSPressed";

            clickSound = Utils.FindObject<AudioClip>("button_click");
            if (clickSound == null)
            {
                Logger.LogWarning($"UIEDSButton::Start: Could not find button_click audio clip ...");
            }

            playAudioEvents = true;

            size = new Vector2(48, 38);

            var uiView = component?.GetUIView();
            if (uiView == null)
            {
                return;
            }

            var screenSize = uiView.GetScreenResolution();

            if (Settings.savedEDSButtonX.value == -1000)
            {
                absolutePosition = new Vector2(3 * (screenSize.x - size.x) / 4, 10);
            }
            else
            {
                absolutePosition = new Vector2(Settings.savedEDSButtonX.value, Settings.savedEDSButtonY.value);
            }
        }

        protected override void OnClick(UIMouseEventParameter p)
        {
            UIView.playSoundDelegate(this.clickSound, 1f);

            var tool = ToolsModifierControl.toolController.gameObject.GetComponent<EnhancedDistrictServicesTool>();
            if (p.buttons.IsFlagSet(UIMouseButton.Left) && tool != null)
            {
                if (Settings.modVersion.value != EnhancedDistrictServicesMod.version)
                {
                    Settings.modVersion.value = EnhancedDistrictServicesMod.version;
                    Settings.showWelcomeMessage.value = true;
                }

                if (Settings.showWelcomeMessage)
                {
                    Settings.showWelcomeMessage.value = false;

                    Utils.DisplayMessage(
                        str1: "Enhanced District Services",
                        str2: $"Please check the EDS mod page for latest updates.  Please note that the Global Outside Connection Intensity value is now recommended to be at least 500!  Please change this setting in your games to allow more outside traffic.",
                        str3: "IconMessage");
                }

                if (ToolsModifierControl.toolController.CurrentTool == tool)
                {
                    ToolsModifierControl.toolController.CurrentTool = ToolsModifierControl.GetTool<DefaultTool>();
                }
                else
                {
                    ToolsModifierControl.infoViewsPanel.CloseAllPanels();
                    ToolsModifierControl.mainToolbar.CloseEverything();

                    ToolsModifierControl.toolController.CurrentTool = tool;
                }
            }
        }

        private Vector3 m_deltaPos;
        protected override void OnMouseDown(UIMouseEventParameter p)
        {
            if (p.buttons.IsFlagSet(UIMouseButton.Right))
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.y = m_OwnerView.fixedHeight - mousePos.y;

                m_deltaPos = absolutePosition - mousePos;
                BringToFront();
            }
        }

        protected override void OnMouseMove(UIMouseEventParameter p)
        {
            if (p.buttons.IsFlagSet(UIMouseButton.Right))
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.y = m_OwnerView.fixedHeight - mousePos.y;

                absolutePosition = mousePos + m_deltaPos;
                Settings.savedEDSButtonX.value = (int)absolutePosition.x;
                Settings.savedEDSButtonY.value = (int)absolutePosition.y;
            }
        }

        public override void Update()
        {
            var tool = ToolsModifierControl.toolController.gameObject.GetComponent<EnhancedDistrictServicesTool>();
            if (tool != null && tool.enabled)
            {
                normalFgSprite = "EDSFocused";
                hoveredFgSprite = "EDSFocused";
            }
            else
            {
                normalFgSprite = "EDS";
                hoveredFgSprite = "EDSHovered";
            }
        }

        public void OnGUI()
        {
            if (!UIView.HasModalInput() && !UIView.HasInputFocus() && Settings.toggleMainTool.IsPressed(Event.current))
            {
                SimulateClick();
            }
        }

        private void LoadResources()
        {
            var spriteNames = new string[]
            {
                "EDS",
                "EDSDisabled",
                "EDSFocused",
                "EDSHovered",
                "EDSPressed"
            };

            atlas = ResourceLoader.CreateTextureAtlas("EDS", spriteNames, "EnhancedDistrictServices.Source.Icons.");
        }
    }
}
