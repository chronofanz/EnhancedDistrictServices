﻿using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using HarmonyLib;
using ICities;
using System;
using System.Reflection;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Main mod class that specifies metadata about the mod.
    /// </summary>
    public class EnhancedDistrictServicesMod : IUserMod, ILoadingExtension
    {
        public const string version = "1.0.31";
        public string Name => $"Enhanced District Services {version}";
        public string Description => "Enhanced District Services mod for Cities Skylines, which allows more granular control of services and supply chains.";
        public Harmony Harmony { get; private set; }

        public EnhancedDistrictServicesMod()
        {
            try
            {
                GameSettings.AddSettingsFile(new SettingsFile()
                {
                    fileName = "EnhancedDistrictServices"
                });
            }
            catch (Exception ex)
            {
                Logger.Log("EnhancedDistrictServicesMod::(ctor): Could not load or create the settings file.");
                Logger.LogException(ex);
            }
        }

        public void OnCreated(ILoading loading)
        {
            Harmony = new Harmony("com.pachang.enhanceddistrictservices");
            Harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void OnLevelLoaded(LoadMode mode)
        {
            if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame)
            {
                Logger.Log("EnhancedDistrictServicesMod::OnLevelLoaded: Listing other enabled mods ...");

                try
                {
                    foreach (var pluginInfo in PluginManager.instance.GetPluginsInfo())
                    {
                        if (pluginInfo.isEnabled)
                        {
                            Logger.Log($"  {pluginInfo.name}: {pluginInfo.assembliesString}");
                        }
                    }
                }
                catch (Exception ex)
                { 
                    Logger.LogException(ex);
                }
            }
        }

        public void OnLevelUnloading()
        {
        }

        public void OnReleased()
        {
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            try
            {
                UIHelper uiHelper = helper.AddGroup(this.Name) as UIHelper;
                UIPanel self = uiHelper.self as UIPanel;

                ((UIComponent)uiHelper.AddCheckbox(
                    "Enable ability to customize industries supply chain",
                    Settings.enableIndustriesControl,
                    b => Settings.enableIndustriesControl.value = b))
                    .tooltip = "By default, the mod offers the ability to control your industries supply chain.  Disable this option if you want the base game to control the supply chain.";

                ((UIComponent)uiHelper.AddCheckbox(
                    "Show campus districts in district dropdown menu",
                    Settings.showCampusDistricts,
                    b => Settings.showCampusDistricts.value = b))
                    .tooltip = "Disable this option if you do not wish to be able to see campus districts in the dropdown menu.";

                ((UIComponent)uiHelper.AddCheckbox(
                    "Show industry districts in district dropdown menu",
                    Settings.showIndustryDistricts,
                    b => Settings.showIndustryDistricts.value = b))
                    .tooltip = "Disable this option if you do not wish to be able to see industry districts in the dropdown menu.";

                ((UIComponent)uiHelper.AddCheckbox(
                    "Show park districts in district dropdown menu", 
                    Settings.showParkDistricts, 
                    b => Settings.showParkDistricts.value = b))
                    .tooltip = "Disable this option if you do not wish to be able to see park districts in the dropdown menu.";

                ((UIComponent)uiHelper.AddCheckbox(
                    "Show welcome message",
                    Settings.showWelcomeMessage,
                    b => Settings.showWelcomeMessage.value = b))
                    .tooltip = "Automatically disabled upon first viewing the welcome message.";

                uiHelper.AddSpace(10);

                self.gameObject.AddComponent<UIOptionsKeymapping>();
            }
            catch (Exception ex)
            {
                Logger.Log("EnhancedDistrictServicesMod::OnSettingsUI failed");
                Logger.LogException(ex);
            }
        }
    }
}
