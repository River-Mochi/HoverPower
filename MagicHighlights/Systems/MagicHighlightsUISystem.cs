// File: Systems/HighlightsOpacityUISystem.cs
// Purpose: Bridges Mod settings to/from the cs2/api bindings the UI (cohtml) layer reads.
// Post-redesign binding shape:
//   Getters (settings -> UI):
//     OutlineR, OutlineG, OutlineB, OutlineA, FillA
//   Triggers (UI -> settings):
//     SetOutlineColor(r, g, b, a)   — fired by the vanilla ColorField onChange
//     SetFillAlpha(a)               — fired by the Fill alpha slider onChange
// OutlineColorSystem (Rendering phase) picks up changes via its dirty-flag.

using Colossal.UI.Binding;
using Game.UI;
using HighlightsOpacity.Settings;

namespace HighlightsOpacity.UI
{
    public partial class HighlightsOpacityUISystem : UISystemBase
    {
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

            AddBinding(new TriggerBinding<float, float, float, float>(
                Mod.ModId,
                "SetOutlineColor",
                (r, g, b, a) =>
                {
                    HighlightsOpacitySettings? settings = Mod.Settings;
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
                    HighlightsOpacitySettings? settings = Mod.Settings;
                    if (settings == null) return;

                    settings.FillA = a;
                    settings.ApplyAndSave();
                }));
        }
    }
}
