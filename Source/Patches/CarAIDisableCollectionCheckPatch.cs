using ColossalFramework;
using Harmony;
using System;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Purpose: Better matching has a consequence - a tsunami of vehicles waiting to spawn at outside connections.
    ///          Disable the collision check if we are close to an outside connection.
    /// </summary>
    //[HarmonyPatch(typeof(CarAI))]
    //[HarmonyPatch("DisableCollisionCheck")]
    public class CarAIDisableCollectionCheckPatch
    {
        public static void Enable(HarmonyInstance harmony)
        {
            var original = typeof(CarAI).GetMethod("DisableCollisionCheck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (original == null)
            {
                throw new InvalidOperationException("Could not find CarAI::DisableCollisionCheck!");
            }

            var postfix = typeof(CarAIDisableCollectionCheckPatch).GetMethod("Postfix");
            if (postfix == null)
            {
                throw new InvalidOperationException("Could not find CarAIPatchDisableCollectionCheck::Postfix!");
            }

            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
        }

        public static void Postfix(ushort vehicleID, ref Vehicle vehicleData, ref bool __result)
        {
            // Coords near the edge of map have x or z > 8000 in abs value.
            var a = vehicleData.m_segment.a;
            var b = vehicleData.m_segment.b;
            if ((Math.Abs(a.x) < 8625 && Math.Abs(a.z) < 8625 && Math.Abs(b.x) < 8625 && Math.Abs(b.z) < 8625) &&
                (Math.Abs(a.x) > 8525 || Math.Abs(a.z) > 8525 || Math.Abs(b.x) > 8525 || Math.Abs(b.z) > 8525))
            {
                __result = true;
            }
        }
    }
}
