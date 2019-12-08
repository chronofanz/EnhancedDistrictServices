using ColossalFramework.UI;
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
            size = new Vector2(298, 28);
            relativePosition = new Vector3((screenSize.x - size.x) / 2, 10);

            var label = AddUIComponent<UILabel>();
            label.text = "Enhanced District Services Tool";
            label.textScale = 1.1f;
            label.relativePosition = new Vector3(3, 3);
            label.size = new Vector2(298, 28);
            label.autoSize = false;
            label.textAlignment = UIHorizontalAlignment.Center;
            label.verticalAlignment = UIVerticalAlignment.Middle;

            label.tooltip = "Click on service building to configure";
        }

        public override void OnEnable()
        {
            base.OnEnable();
            Show();
        }
    }
}
