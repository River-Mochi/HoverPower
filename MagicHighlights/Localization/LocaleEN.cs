// File: Localization/LocaleEN.cs
// Purpose: English (en-US) strings for the Options UI (ESC -> Options -> Magic Highlights).
// Registered in Mod.OnLoad via GameManager.instance.localizationManager.AddSource("en-US", ...).
// Strings for the in-city cohtml panel live separately in L10n/lang/en-US.json.

namespace HighlightsOpacity.Localization
{
    using Colossal;
    using HighlightsOpacity.Settings;
    using System.Collections.Generic;

    public sealed class LocaleEN : IDictionarySource
    {
        private readonly HighlightsOpacitySettings m_Settings;

        public LocaleEN(HighlightsOpacitySettings settings)
        {
            m_Settings = settings;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts)
        {
            string title = Mod.ModName;
            if (!string.IsNullOrEmpty(Mod.ModVersion))
            {
                title += " (" + Mod.ModVersion + ")";
            }

            return new Dictionary<string, string>
            {
                // Mod title in the left rail of the Options menu.
                { m_Settings.GetSettingsLocaleID(), title },

                // Tabs
                { m_Settings.GetOptionTabLocaleID(HighlightsOpacitySettings.Actions), "Actions" },
                { m_Settings.GetOptionTabLocaleID(HighlightsOpacitySettings.About), "About" },

                // Groups
                { m_Settings.GetOptionGroupLocaleID(HighlightsOpacitySettings.Guidelines), "Guidelines" },
                // AboutInfo + AboutLinks intentionally have empty group headers.
                { m_Settings.GetOptionGroupLocaleID(HighlightsOpacitySettings.AboutInfo), string.Empty },
                { m_Settings.GetOptionGroupLocaleID(HighlightsOpacitySettings.AboutLinks), string.Empty },

                // Guidelines opacity slider
                { m_Settings.GetOptionLabelLocaleID(nameof(HighlightsOpacitySettings.GuidelineOpacityPercent)), "Guidelines opacity" },
                { m_Settings.GetOptionDescLocaleID(nameof(HighlightsOpacitySettings.GuidelineOpacityPercent)),
                    "Scales the in-game guideline overlay (the colored arrows/lines shown while placing roads, " +
                    "zones, props, etc.) relative to the game's defaults.\n\n" +
                    "**100%** keeps vanilla default look.\n" +
                    "**Lower** makes guidelines more transparent.\n" +
                    "**0%** hides them entirely - <Not recommended>.\n" +           
                    "Recommend not going lower than 10%" },

                // About — name + version
                { m_Settings.GetOptionLabelLocaleID(nameof(HighlightsOpacitySettings.NameText)), "Mod" },
                { m_Settings.GetOptionDescLocaleID(nameof(HighlightsOpacitySettings.NameText)), string.Empty },

                { m_Settings.GetOptionLabelLocaleID(nameof(HighlightsOpacitySettings.VersionText)), "Version" },
                { m_Settings.GetOptionDescLocaleID(nameof(HighlightsOpacitySettings.VersionText)), string.Empty },

                // About — Paradox Mods link button (matches CityWatchdog phrasing)
                { m_Settings.GetOptionLabelLocaleID(nameof(HighlightsOpacitySettings.OpenParadox)), "Paradox Mods" },
                { m_Settings.GetOptionDescLocaleID(nameof(HighlightsOpacitySettings.OpenParadox)), "Open the author's Paradox Mods page." },
            };
        }

        public void Unload()
        {
        }
    }
}
