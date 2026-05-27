// File: Mod.cs
// Purpose: Mod entrypoint; registers settings, schedules systems, and configures the mod logger.

namespace HoverPower
{
    using Colossal;
    using Colossal.IO.AssetDatabase;
    using Colossal.Localization;
    using Colossal.Logging;
    using CS2Shared.RiverMochi;
    using Game;
    using Game.Input;
    using Game.Modding;
    using Game.SceneFlow;
    using HoverPower.Localization;
    using HoverPower.Settings;
    using HoverPower.Systems;
    using HoverPower.UI;
    using System;
    using System.Reflection;
    using Unity.Entities;
    using UnityEngine.InputSystem;

    public sealed class Mod : IMod
    {
        public const string ModName = "Hover Power";
        public const string ModId = "HoverPower";
        public const string ModTag = "[HP]";
        public const string kTogglePanelActionName = "TogglePanel";

        public static readonly string ModVersion =
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.5.0";

        public static readonly ILog s_Log =
            LogManager.GetLogger(ModId).SetShowsErrorsInUI(false);

        public static HoverPowerSettings? Settings { get; private set; }

        private static bool s_BannerLogged;

        [System.Diagnostics.Conditional("DEBUG")]
        internal static void DebugLog(string message)
        {
            LogUtils.Info(() => message);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        internal static void DebugLog(Func<string> messageFactory)
        {
            LogUtils.Info(messageFactory);
        }

        public void OnLoad(UpdateSystem updateSystem)
        {
            LogUtils.Configure(ModId, s_Log);
            LogStartupBanner();

            if (GameManager.instance == null)
            {
                LogUtils.Warn(() => $"{ModTag} GameManager.instance is null; {ModName} cannot initialize.");
                return;
            }

            HoverPowerSettings setting = new HoverPowerSettings(this);
            Settings = setting;

            try
            {
                AssetDatabase.global.LoadSettings(ModId, setting, new HoverPowerSettings(this));
            }
            catch (Exception ex)
            {
                LogUtils.Error(() => $"{ModTag} Settings load failed: {ex.GetType().Name}: {ex.Message}", ex);
            }

            try
            {
                setting.RegisterInOptionsUI();
            }
            catch (Exception ex)
            {
                LogUtils.Error(() => $"{ModTag} Options UI registration failed: {ex.GetType().Name}: {ex.Message}", ex);
            }

            // Register localization sources before the Options UI reads the dictionary so labels resolve.
            AddLocaleSource("en-US", new LocaleEN(setting));

            try
            {
                setting.RegisterKeyBindings();
                EnableTogglePanelAction(setting);
            }
            catch (Exception ex)
            {
                LogUtils.Error(() => $"{ModTag} Keybinding registration failed: {ex.GetType().Name}: {ex.Message}", ex);
            }

            try
            {
                ScheduleSystems(updateSystem);
            }
            catch (Exception ex)
            {
                LogUtils.Error(() => $"{ModTag} System scheduling failed: {ex.GetType().Name}: {ex.Message}", ex);
            }
        }

        // Wires the H hotkey to HoverPowerUISystem.TogglePanel(). Pattern matches CityWatchdog:
        //   1. setting.RegisterKeyBindings() in OnLoad (above)
        //   2. setting.GetAction(actionName) to fetch the ProxyAction created by the attribute
        //   3. shouldBeEnabled = true so the action receives input
        //   4. onInteraction += handler so we get called when the key fires
        private static void EnableTogglePanelAction(HoverPowerSettings setting)
        {
            try
            {
                ProxyAction action = setting.GetAction(kTogglePanelActionName);
                if (action == null)
                {
                    LogUtils.Warn(() => $"{ModTag} ProxyAction '{kTogglePanelActionName}' not found.");
                    return;
                }

                action.shouldBeEnabled = true;
                action.onInteraction += OnTogglePanelInteraction;
            }
            catch (Exception ex)
            {
                LogUtils.Warn(() => $"{ModTag} EnableTogglePanelAction failed: {ex.GetType().Name}: {ex.Message}", ex);
            }
        }

        private static void OnTogglePanelInteraction(ProxyAction _, InputActionPhase phase)
        {
            // Only react to the press (Performed) phase, not the press-down / release frames.
            if (phase == InputActionPhase.Performed)
            {
                HoverPowerUISystem.TogglePanel();
            }
        }

        private static bool AddLocaleSource(string localeId, IDictionarySource source)
        {
            if (string.IsNullOrEmpty(localeId))
            {
                return false;
            }

            LocalizationManager? lm = GameManager.instance?.localizationManager;
            if (lm == null)
            {
                LogUtils.Warn(() => $"{ModTag} AddLocaleSource: no LocalizationManager; '{localeId}' not registered.");
                return false;
            }

            try
            {
                lm.AddSource(localeId, source);
                return true;
            }
            catch (Exception ex)
            {
                LogUtils.Warn(() => $"{ModTag} AddLocaleSource('{localeId}') failed: {ex.GetType().Name}: {ex.Message}", ex);
                return false;
            }
        }

        public void OnDispose()
        {
            DebugLog(() => $"{ModTag} Mod Dispose");

            HoverPowerSettings? setting = Settings;
            if (setting != null)
            {
                try
                {
                    setting.UnregisterInOptionsUI();
                }
                catch (Exception ex)
                {
                    LogUtils.Warn(() => $"{ModTag} UnregisterInOptionsUI failed: {ex.GetType().Name}: {ex.Message}", ex);
                }
            }

            Settings = null;
        }

        private static void ScheduleSystems(UpdateSystem updateSystem)
        {
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<OutlineColorSystem>();
            updateSystem.UpdateAt<OutlineColorSystem>(SystemUpdatePhase.Rendering);

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<GuidelineColorSystem>();
            updateSystem.UpdateAt<GuidelineColorSystem>(SystemUpdatePhase.Rendering);

            updateSystem.UpdateAt<HoverPowerUISystem>(SystemUpdatePhase.UIUpdate);
        }

        private static void LogStartupBanner()
        {
            if (s_BannerLogged)
            {
                return;
            }

            s_BannerLogged = true;
            LogUtils.Info(() => $"{ModName} v{ModVersion} {ModTag} loaded");
        }
    }
}
