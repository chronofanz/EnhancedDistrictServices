using ColossalFramework;
using ColossalFramework.Math;
using ICities;
using UnityEngine;

namespace EnhancedDistrictServices
{
    public class EnhancedDistrictServicesThreadingExtension : ThreadingExtensionBase
    {
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.D))
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
            }
        }
    }
}
