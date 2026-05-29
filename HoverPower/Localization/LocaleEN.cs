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
                { m_Settings.GetOptionGroupLocaleID(HoverPowerSettings.ToolColors), "Tool Color Behavior" },
                { m_Settings.GetOptionGroupLocaleID(HoverPowerSettings.KeyBindings), "Key bindings" },
                { m_Settings.GetOptionGroupLocaleID(HoverPowerSettings.Guidelines), "Guidelines" },
                // AboutInfo + AboutLinks intentionally have empty group headers.
                { m_Settings.GetOptionGroupLocaleID(HoverPowerSettings.AboutInfo), string.Empty },
                { m_Settings.GetOptionGroupLocaleID(HoverPowerSettings.AboutLinks), string.Empty },

                // Tool color behavior
                { m_Settings.GetOptionLabelLocaleID(nameof(HoverPowerSettings.ToolColorMode)), "Bulldozer + Roads" },
                { m_Settings.GetOptionDescLocaleID(nameof(HoverPowerSettings.ToolColorMode)),
                    "Controls temporary outline colors while bulldozer, or road tools are active.\n\n" +
                    "**1. Recommended** uses game's WarningColor for demolition and a softer vanilla blue for roads.\n" +
                    "**2. Vanilla tool colors** restores the game's normal vanilla blue while those tools are active.\n" +
                    "**3. Keep my custom color** uses your chosen color everywhere.\n\n" +
                    "This does not overwrite your automatically saved custom color in the color picker.\n"+
                    "This feature exists because some users find their custom color hard to see while bulldozing, and wanted stronger color outlines back on during tool usage. 
                },
                { m_Settings.GetToolColorModeLocaleID("Recommended"), "1. Recommended" },
                { m_Settings.GetToolColorModeLocaleID("Vanilla"), "2. Vanilla tool colors" },
                { m_Settings.GetToolColorModeLocaleID("Custom"), "3. Keep my custom color" },

                // Guidelines opacity slider
                { m_Settings.GetOptionLabelLocaleID(nameof(HoverPowerSettings.GuidelineOpacityPercent)), "Guidelines opacity" },
                { m_Settings.GetOptionDescLocaleID(nameof(HoverPowerSettings.GuidelineOpacityPercent)),
                    "Scales the in-game guideline overlay (the colored arrows/lines shown while placing roads, " +
                    "zones, props, etc.) relative to the game's defaults.\n\n" +
                    "**100%** keeps vanilla default look.\n" +
                    "**Lower** makes guidelines more transparent.\n" +
                    "**0%** hides them entirely - <Not recommended>.\n" +           
                    "Recommend stay above 15% or it's hard to see what is happening\n" +
                    "The same slider lives on the city mod panel. They are both synced;\n" +
                    "if you change this one, the one in-city conveniently changes."
                },

                // Keybinds
                { m_Settings.GetOptionLabelLocaleID(nameof(HoverPowerSettings.TogglePanelBinding)), "Toggle panel (H)" },
                { m_Settings.GetOptionDescLocaleID(nameof(HoverPowerSettings.TogglePanelBinding)),
                    "Keyboard shortcut to open / close the in-city Hover objects Color Panel." },
                { m_Settings.GetBindingKeyLocaleID(Mod.kTogglePanelActionName), "Toggle Hover Power panel" },

                { m_Settings.GetOptionLabelLocaleID(nameof(HoverPowerSettings.ToggleSurfaceToolAreasBinding)), "Toggle Surface tool lines (L)" },
                { m_Settings.GetOptionDescLocaleID(nameof(HoverPowerSettings.ToggleSurfaceToolAreasBinding)),
                    "Keyboard shortcut to hide or restore active Surface tool boundary preview lines while placing surfaces." },
                { m_Settings.GetBindingKeyLocaleID(Mod.kToggleSurfaceToolAreasActionName), "Toggle Surface tool lines" },

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
