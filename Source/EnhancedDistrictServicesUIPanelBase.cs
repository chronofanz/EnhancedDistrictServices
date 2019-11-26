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

        #endregion
    }
}
