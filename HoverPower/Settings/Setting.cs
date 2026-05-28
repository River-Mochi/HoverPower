// File: Settings/Setting.cs
// Purpose: Defines Hover Power settings, persistent storage, and the Options UI surface.
// Layout: 2 tabs (Actions, About) following CityWatchdog/EasyZoning convention.
// Note: the eight outline RGBA floats are NOT decorated for Options UI — they are read by the
// in-city color-picker panel via cs2/api bindings (see Systems/HoverPowerUISystem.cs).

namespace HoverPower.Settings
{
    using Colossal.IO.AssetDatabase;
    using CS2Shared.RiverMochi;
    using Game.Input;
    using Game.Modding;
    using Game.Settings;
    using Game.UI;
    using System;
    using UnityEngine;

    [FileLocation("ModsSettings/HoverPower/HoverPower")]
    [SettingsUITabOrder(Actions, About)]
    [SettingsUIGroupOrder(KeyBindings, Guidelines, AboutInfo, AboutLinks)]
    [SettingsUIShowGroupName(KeyBindings, Guidelines)]
    public class HoverPowerSettings : ModSetting
    {
        // Tab IDs
        internal const string Actions = nameof(Actions);
        internal const string About = nameof(About);

        // Group IDs
        internal const string Guidelines = nameof(Guidelines);
        internal const string KeyBindings = nameof(KeyBindings);
        internal const string AboutInfo = nameof(AboutInfo);
        internal const string AboutLinks = nameof(AboutLinks);

        private const string AboutLinksRow = nameof(AboutLinksRow);
        // Same Paradox URL pattern as CityWatchdog — lands on River-Mochi's author page filtered to CS2.
        private const string UrlParadox =
            "https://mods.paradoxplaza.com/authors/River-mochi/cities_skylines_2?games=cities_skylines_2&orderBy=desc&sortBy=best&time=alltime";

        // -----------------------------------------------------------------------
        // In-city color-picker bindings (driven by Systems/HoverPowerUISystem)
        // Not decorated for Options UI — these are data fields the cs2/api bindings read/write
        // and the OutlineColorSystem applies. Field layout after the post-alpha redesign:
        //   - OutlineR/G/B  → outline halo edge color + fill overlay color + lot-pattern tint
        //     (one color choice drives every visible surface so the panel only needs one swatch)
        //   - OutlineA     → outline halo edge opacity  (material _OuterColor.a)
        //   - AreaBorderA  → owner / area-border opacity (RenderingSettingsData.m_OwnerColor.a)
        //   - FillA        → fill overlay opacity inside the silhouette (material _InnerColor.a)
        // The dropped OutlineInner*/OutlineOuter* fields from the early alpha are gone — their
        // saved values from the .coc file are ignored and replaced by SetDefaults() on next load.
        // -----------------------------------------------------------------------

        public float OutlineR { get; set; }
        public float OutlineG { get; set; }
        public float OutlineB { get; set; }
        public float OutlineA { get; set; }
        public float AreaBorderA { get; set; }
        public float FillA { get; set; }

        // -----------------------------------------------------------------------
        // Actions tab — Guidelines
        // -----------------------------------------------------------------------
        // Slider is 0..100 (percent) so the SettingsUISlider can use kPercentage units.
        // GuidelineColorSystem divides by 100 and multiplies the game's default per-priority
        // alphas, so 100 = no change, 50 = half as visible, 0 = fully invisible guidelines.

        [SettingsUISection(Actions, KeyBindings)]
        [SettingsUIKeyboardBinding(BindingKeyboard.H, Mod.kTogglePanelActionName)]
        public ProxyBinding TogglePanelBinding { get; set; }
        [SettingsUISlider(min = 0, max = 100, step = 5, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(Actions, Guidelines)]
        public int GuidelineOpacityPercent { get; set; }

        // -----------------------------------------------------------------------
        // About tab
        // -----------------------------------------------------------------------

        [SettingsUISection(About, AboutInfo)]
        public string NameText => Mod.ModName;

        [SettingsUISection(About, AboutInfo)]
        public string VersionText =>
#if DEBUG
            Mod.ModVersion + " (DEBUG)";
#else
            Mod.ModVersion;
#endif

        [SettingsUIButtonGroup(AboutLinksRow)]
        [SettingsUIButton]
        [SettingsUISection(About, AboutLinks)]
        public bool OpenParadox
        {
            set
            {
                if (value)
                {
                    TryOpenUrl(UrlParadox);
                }
            }
        }

        // -----------------------------------------------------------------------
        // Construction + defaults
        // -----------------------------------------------------------------------

        public HoverPowerSettings(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        public override void SetDefaults()
        {
            // Vanilla cyan-blue from the OutlinesWorldUIPass material defaults
            // (confirmed by Mert's testing in https://github.com/Flashbond/Mert-OutlineColor).
            OutlineR = 0.502f;
            OutlineG = 0.869f;
            OutlineB = 1f;
            OutlineA = 0.855f;
            AreaBorderA = 0.702f;

            // FillA=0 matches vanilla CS2: no extra silhouette overlay until the player turns it up.
            FillA = 0f;

            // 100 = no change from game defaults. Lower = more transparent guidelines.
            GuidelineOpacityPercent = 40;
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static void TryOpenUrl(string url)
        {
            try
            {
                Application.OpenURL(url);
            }
            catch (Exception ex)
            {
                LogUtils.WarnOnce(
                    "open-url-" + url,
                    () => $"Failed to open URL '{url}': {ex.GetType().Name}: {ex.Message}",
                    ex);
            }
        }
    }
}
