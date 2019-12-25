using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using UnityEngine;

namespace EnhancedDistrictServices
{
    public class UIOptionsKeymapping : UICustomControl
    {
        private static readonly string kKeyBindingTemplate = "KeyBindingTemplate";
        private SavedInputKey m_EditingBinding;

        private void Awake()
        {
            this.AddKeymapping("Toggle Main Tool", Settings.toggleMainTool);
            this.AddKeymapping("Copy building policy hotkey", Settings.keyCopy);
            this.AddKeymapping("Paste building policy hotkey", Settings.keyPaste);
        }

        private void AddKeymapping(string label, SavedInputKey savedInputKey)
        {
            UIPanel uiPanel = this.component.AttachUIComponent(UITemplateManager.GetAsGameObject(UIOptionsKeymapping.kKeyBindingTemplate)) as UIPanel;
            UILabel uiLabel = uiPanel.Find<UILabel>("Name");
            UIButton uiButton = uiPanel.Find<UIButton>("Binding");
            uiButton.eventKeyDown += new KeyPressHandler(this.OnBindingKeyDown);
            uiButton.eventMouseDown += new MouseEventHandler(this.OnBindingMouseDown);
            uiLabel.text = label;
            uiButton.text = savedInputKey.ToLocalizedString("KEYNAME");
            uiButton.objectUserData = (object)savedInputKey;
        }

        private void OnEnable()
        {
            LocaleManager.eventLocaleChanged += new LocaleManager.LocaleChangedHandler(this.OnLocaleChanged);
        }

        private void OnDisable()
        {
            LocaleManager.eventLocaleChanged -= new LocaleManager.LocaleChangedHandler(this.OnLocaleChanged);
        }

        private void OnLocaleChanged()
        {
            this.RefreshBindableInputs();
        }

        private bool IsModifierKey(KeyCode code)
        {
            if (code != KeyCode.LeftControl && code != KeyCode.RightControl && (code != KeyCode.LeftShift && code != KeyCode.RightShift) && code != KeyCode.LeftAlt)
                return code == KeyCode.RightAlt;
            return true;
        }

        private bool IsControlDown()
        {
            if (!Input.GetKey(KeyCode.LeftControl))
                return Input.GetKey(KeyCode.RightControl);
            return true;
        }

        private bool IsShiftDown()
        {
            if (!Input.GetKey(KeyCode.LeftShift))
                return Input.GetKey(KeyCode.RightShift);
            return true;
        }

        private bool IsAltDown()
        {
            if (!Input.GetKey(KeyCode.LeftAlt))
                return Input.GetKey(KeyCode.RightAlt);
            return true;
        }

        private bool IsUnbindableMouseButton(UIMouseButton code)
        {
            if (code != UIMouseButton.Left)
                return code == UIMouseButton.Right;
            return true;
        }

        private KeyCode ButtonToKeycode(UIMouseButton button)
        {
            switch (button)
            {
                case UIMouseButton.Left:
                    return KeyCode.Mouse0;
                case UIMouseButton.Right:
                    return KeyCode.Mouse1;
                case UIMouseButton.Middle:
                    return KeyCode.Mouse2;
                case UIMouseButton.Special0:
                    return KeyCode.Mouse3;
                case UIMouseButton.Special1:
                    return KeyCode.Mouse4;
                case UIMouseButton.Special2:
                    return KeyCode.Mouse5;
                case UIMouseButton.Special3:
                    return KeyCode.Mouse6;
                default:
                    return KeyCode.None;
            }
        }

        private void OnBindingKeyDown(UIComponent comp, UIKeyEventParameter p)
        {
            if (!((SavedValue)this.m_EditingBinding != (SavedValue)null) || this.IsModifierKey(p.keycode))
                return;
            p.Use();
            UIView.PopModal();
            KeyCode keycode = p.keycode;
            InputKey inputKey = p.keycode == KeyCode.Escape ? this.m_EditingBinding.value : SavedInputKey.Encode(keycode, p.control, p.shift, p.alt);
            if (p.keycode == KeyCode.Backspace)
                inputKey = SavedInputKey.Empty;
            this.m_EditingBinding.value = inputKey;
            (p.source as UITextComponent).text = this.m_EditingBinding.ToLocalizedString("KEYNAME");
            this.m_EditingBinding = (SavedInputKey)null;
        }

        private void OnBindingMouseDown(UIComponent comp, UIMouseEventParameter p)
        {
            if ((SavedValue)this.m_EditingBinding == (SavedValue)null)
            {
                p.Use();
                this.m_EditingBinding = (SavedInputKey)p.source.objectUserData;
                UIButton source = p.source as UIButton;
                source.buttonsMask = UIMouseButton.Left | UIMouseButton.Right | UIMouseButton.Middle | UIMouseButton.Special0 | UIMouseButton.Special1 | UIMouseButton.Special2 | UIMouseButton.Special3;
                source.text = ColossalFramework.Globalization.Locale.Get("KEYMAPPING_PRESSANYKEY");
                p.source.Focus();
                UIView.PushModal(p.source);
            }
            else
            {
                if (this.IsUnbindableMouseButton(p.buttons))
                    return;
                p.Use();
                UIView.PopModal();
                this.m_EditingBinding.value = SavedInputKey.Encode(this.ButtonToKeycode(p.buttons), this.IsControlDown(), this.IsShiftDown(), this.IsAltDown());
                UIButton source = p.source as UIButton;
                source.text = this.m_EditingBinding.ToLocalizedString("KEYNAME");
                source.buttonsMask = UIMouseButton.Left;
                this.m_EditingBinding = (SavedInputKey)null;
            }
        }

        private void RefreshBindableInputs()
        {
            foreach (UIComponent componentsInChild in this.component.GetComponentsInChildren<UIComponent>())
            {
                UITextComponent uiTextComponent = componentsInChild.Find<UITextComponent>("Binding");
                if (uiTextComponent != null)
                {
                    SavedInputKey objectUserData = uiTextComponent.objectUserData as SavedInputKey;
                    if ((SavedValue)objectUserData != (SavedValue)null)
                        uiTextComponent.text = objectUserData.ToLocalizedString("KEYNAME");
                }
                UILabel uiLabel = componentsInChild.Find<UILabel>("Name");
                if (uiLabel != null)
                    uiLabel.text = ColossalFramework.Globalization.Locale.Get("KEYMAPPING", uiLabel.stringUserData);
            }
        }
    }
}
