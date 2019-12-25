using ICities;

namespace EnhancedDistrictServices
{
    public class EnhancedDistrictServicesLoadingExtension : LoadingExtensionBase
    {
        /// <summary>
        /// Create the EnhancedDistrictServicesTool object. 
        /// </summary>
        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame)
            {
                var tool = ToolsModifierControl.toolController.gameObject.GetComponent<EnhancedDistrictServicesTool>();
                if (tool == null)
                {
                    ToolsModifierControl.toolController.gameObject.AddComponent<EnhancedDistrictServicesTool>();
                    ToolsModifierControl.toolController.CurrentTool = ToolsModifierControl.GetTool<DefaultTool>();
                }
            }
        }

        /// <summary>
        /// Destroy the EnhancedDistrictServicesTool object. 
        /// </summary>
        public override void OnReleased()
        {
            if (ToolsModifierControl.toolController != null && ToolsModifierControl.toolController.gameObject != null)
            {
                ToolsModifierControl.toolController.CurrentTool = ToolsModifierControl.GetTool<DefaultTool>();
                var tool = ToolsModifierControl.toolController.gameObject.GetComponent<EnhancedDistrictServicesTool>();
                if (tool != null)
                {
                    UnityEngine.Object.Destroy(tool);
                }
            }
        }
    }
}
