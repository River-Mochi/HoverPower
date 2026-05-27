// File: Mod.cs
// Purpose: Mod entrypoint; registers settings, schedules systems, and configures the mod logger.

namespace HighlightsOpacity
{
    using Colossal;
    using Colossal.IO.AssetDatabase;
    using Colossal.Localization;
    using Colossal.Logging;
    using CS2Shared.RiverMochi;
    using Game;
    using Game.Modding;
    using Game.SceneFlow;
    using HighlightsOpacity.Localization;
    using HighlightsOpacity.Settings;
    using HighlightsOpacity.Systems;
    using HighlightsOpacity.UI;
    using System;
    using System.Reflection;
    using Unity.Entities;

    public sealed class Mod : IMod
    {
        public const string ModName = "Highlights + Opacity Tuner";
        public const string ModId = "HighlightsOpacity";
        public const string ModTag = "[HOT]";

        public static readonly string ModVersion =
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.5.0";

        public static readonly ILog s_Log =
            LogManager.GetLogger(ModId).SetShowsErrorsInUI(false);

        public static HighlightsOpacitySettings? Settings { get; private set; }

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

            HighlightsOpacitySettings setting = new HighlightsOpacitySettings(this);
            Settings = setting;

            try
            {
                AssetDatabase.global.LoadSettings(ModId, setting, new HighlightsOpacitySettings(this));
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
                ScheduleSystems(updateSystem);
            }
            catch (Exception ex)
            {
                LogUtils.Error(() => $"{ModTag} System scheduling failed: {ex.GetType().Name}: {ex.Message}", ex);
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

            HighlightsOpacitySettings? setting = Settings;
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

            updateSystem.UpdateAt<HighlightsOpacityUISystem>(SystemUpdatePhase.UIUpdate);
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
