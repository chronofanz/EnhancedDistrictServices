using ICities;

namespace EnhancedDistrictServices
{
    public class EnhancedDistrictServicesThreadingExtension : ThreadingExtensionBase
    {
        /// <summary>
        /// Allows the user to activate the EnhancedDistrictServices tool by pressing Ctrl-D.
        /// </summary>
        /// <param name="realTimeDelta"></param>
        /// <param name="simulationTimeDelta"></param>
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            var current = UnityEngine.Event.current;
            if (Settings.toggleMainTool.IsPressed(current))
            {
                var tool = ToolsModifierControl.toolController.gameObject.GetComponent<EnhancedDistrictServicesTool>();
                if (ToolsModifierControl.toolController.CurrentTool == tool)
                {
                    ToolsModifierControl.toolController.CurrentTool = ToolsModifierControl.GetTool<DefaultTool>();                    
                }
                else
                {
                    ToolsModifierControl.toolController.CurrentTool = tool;
                }

                if (Settings.showWelcomeMessage)
                {
                    Settings.showWelcomeMessage.value = false;

                    Utils.DisplayMessage(
                        str1: "Enhanced District Services",
                        str2: $"Please check the EDS mod page for latest updates, including copy-paste policy functionality and new incoming supply chain restrictions!",
                        str3: "IconMessage");
                }
            }
        }
    }
}
