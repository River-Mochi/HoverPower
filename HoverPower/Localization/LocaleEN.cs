// File: Localization/LocaleEN.cs
// Purpose: English (en-US) strings for the Options UI (ESC -> Options -> Hover Power).
// Registered in Mod.OnLoad via GameManager.instance.localizationManager.AddSource("en-US", ...).
// Strings for the in-city cohtml panel live separately in L10n/lang/en-US.json.

namespace HoverPower.Localization
{
    using Colossal;
    using HoverPower.Settings;
    using System.Collections.Generic;

    public sealed class LocaleEN : IDictionarySource
    {
        private readonly HoverPowerSettings m_Settings;

        public LocaleEN(HoverPowerSettings settings)
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
                { m_Settings.GetOptionTabLocaleID(HoverPowerSettings.Actions), "Actions" },
                { m_Settings.GetOptionTabLocaleID(HoverPowerSettings.About), "About" },

                // Groups
                { m_Settings.GetOptionGroupLocaleID(HoverPowerSettings.KeyBindings), "Key bindings" },
                { m_Settings.GetOptionGroupLocaleID(HoverPowerSettings.Guidelines), "Guidelines" },
                // AboutInfo + AboutLinks intentionally have empty group headers.
                { m_Settings.GetOptionGroupLocaleID(HoverPowerSettings.AboutInfo), string.Empty },
                { m_Settings.GetOptionGroupLocaleID(HoverPowerSettings.AboutLinks), string.Empty },


                // Guidelines opacity slider
                { m_Settings.GetOptionLabelLocaleID(nameof(HoverPowerSettings.GuidelineOpacityPercent)), "Guidelines opacity" },
                { m_Settings.GetOptionDescLocaleID(nameof(HoverPowerSettings.GuidelineOpacityPercent)),
                    "Scales the in-game guideline overlay (the colored arrows/lines shown while placing roads, " +
                    "zones, props, etc.) relative to the game's defaults.\n\n" +
                    "**100%** keeps vanilla default look.\n" +
                    "**Lower** makes guidelines more transparent.\n" +
                    "**0%** hides them entirely - <Not recommended>.\n" +           
                    "Recommend not going lower than 10%" },

                // Keybinds
                { m_Settings.GetOptionLabelLocaleID(nameof(HoverPowerSettings.TogglePanelBinding)), "Toggle panel (H)" },
                { m_Settings.GetOptionDescLocaleID(nameof(HoverPowerSettings.TogglePanelBinding)),
                    "Keyboard shortcut to open / close the in-city Hover Power Color Panel panel." },

                // About — name + version
                { m_Settings.GetOptionLabelLocaleID(nameof(HoverPowerSettings.NameText)), "Mod" },
                { m_Settings.GetOptionDescLocaleID(nameof(HoverPowerSettings.NameText)), string.Empty },

                { m_Settings.GetOptionLabelLocaleID(nameof(HoverPowerSettings.VersionText)), "Version" },
                { m_Settings.GetOptionDescLocaleID(nameof(HoverPowerSettings.VersionText)), string.Empty },

                // About — Paradox Mods link button (matches CityWatchdog phrasing)
                { m_Settings.GetOptionLabelLocaleID(nameof(HoverPowerSettings.OpenParadox)), "Paradox Mods" },
                { m_Settings.GetOptionDescLocaleID(nameof(HoverPowerSettings.OpenParadox)), "Open the author's Paradox Mods page." },
            };
        }

        public void Unload()
        {
        }
    }
}
