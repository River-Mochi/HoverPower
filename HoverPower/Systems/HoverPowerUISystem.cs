// File: Systems/HoverPowerUISystem.cs
// Purpose: Bridges Mod settings to/from the cs2/api bindings the UI (cohtml) layer reads,
// and owns the shared panel-open flag toggled by both the GTL button and the H hotkey.
//
// Binding shape:
//   Getters (UISystem -> UI):
//     OutlineR, OutlineG, OutlineB, OutlineA, FillA, GuidelineOpacityPercent, PanelOpen
//   Triggers (UI -> UISystem):
//     SetOutlineColor(r, g, b, a)         — vanilla ColorField onChange
//     SetFillAlpha(a)                     — Fill alpha slider onChange
//     SetGuidelineOpacity(percent)        — in-city Guidelines slider (Options-UI slider is fallback)
//     SetPanelOpen(open)                  — GTL button onSelect flips the shared panel flag
//
// OutlineColorSystem + GuidelineColorSystem (Rendering phase) pick up settings changes via dirty-flag.
// The hotkey (Setting.TogglePanelBinding "H") fires a ProxyAction handled in Mod.cs which calls
// the static TogglePanel() helper below — the GTL button reads the same flag via PanelOpen.

using Colossal.UI.Binding;
using Game.UI;
using HoverPower.Settings;

namespace HoverPower.UI
{
    public partial class HoverPowerUISystem : UISystemBase
    {
        // Shared panel-open flag. Toggled by either the GTL button (SetPanelOpen trigger) or the
        // hotkey (TogglePanel() static called from Mod.cs onInteraction handler).
        private static bool s_PanelOpen;

        // Called from Mod.cs when the TogglePanel ProxyAction fires.
        public static void TogglePanel()
        {
            s_PanelOpen = !s_PanelOpen;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            AddUpdateBinding(new GetterValueBinding<float>(
                Mod.ModId, "OutlineR",
                () => Mod.Settings?.OutlineR ?? 0.502f));

            AddUpdateBinding(new GetterValueBinding<float>(
                Mod.ModId, "OutlineG",
                () => Mod.Settings?.OutlineG ?? 0.869f));

            AddUpdateBinding(new GetterValueBinding<float>(
                Mod.ModId, "OutlineB",
                () => Mod.Settings?.OutlineB ?? 1f));

            AddUpdateBinding(new GetterValueBinding<float>(
                Mod.ModId, "OutlineA",
                () => Mod.Settings?.OutlineA ?? 0.855f));

            AddUpdateBinding(new GetterValueBinding<float>(
                Mod.ModId, "FillA",
                () => Mod.Settings?.FillA ?? 0f));

            AddUpdateBinding(new GetterValueBinding<int>(
                Mod.ModId, "GuidelineOpacityPercent",
                () => Mod.Settings?.GuidelineOpacityPercent ?? 100));

            AddUpdateBinding(new GetterValueBinding<bool>(
                Mod.ModId, "PanelOpen",
                () => s_PanelOpen));

            AddBinding(new TriggerBinding<float, float, float, float>(
                Mod.ModId,
                "SetOutlineColor",
                (r, g, b, a) =>
                {
                    HoverPowerSettings? settings = Mod.Settings;
                    if (settings == null) return;

                    settings.OutlineR = r;
                    settings.OutlineG = g;
                    settings.OutlineB = b;
                    settings.OutlineA = a;
                    settings.ApplyAndSave();
                }));

            AddBinding(new TriggerBinding<float>(
                Mod.ModId,
                "SetFillAlpha",
                a =>
                {
                    HoverPowerSettings? settings = Mod.Settings;
                    if (settings == null) return;

                    settings.FillA = a;
                    settings.ApplyAndSave();
                }));

            AddBinding(new TriggerBinding<int>(
                Mod.ModId,
                "SetGuidelineOpacity",
                percent =>
                {
                    HoverPowerSettings? settings = Mod.Settings;
                    if (settings == null) return;

                    if (percent < 0) percent = 0;
                    if (percent > 100) percent = 100;

                    settings.GuidelineOpacityPercent = percent;
                    settings.ApplyAndSave();
                }));

            AddBinding(new TriggerBinding<bool>(
                Mod.ModId,
                "SetPanelOpen",
                open => s_PanelOpen = open));
        }
    }
}
