using Harmony;
using ICities;
using System.Reflection;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Main mod class that specifies metadata about the mod.
    /// </summary>
    public class EnhancedDistrictServicesMod : IUserMod, ILoadingExtension
    {
        public const string version = "1.0.2";
        public string Name => "EnhancedDistrictServices 1.0.2";
        public string Description => "EnhancedDistrictServices mod for Cities Skylines, which allows more granular control of services and supply chains.";

        public void OnCreated(ILoading loading)
        {
            var harmony = HarmonyInstance.Create("com.pachang.enhanceddistrictservices");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void OnLevelLoaded(LoadMode mode)
        {
        }

        public void OnLevelUnloading()
        {
        }

        public void OnReleased()
        {
        }
    }
}
