// File: Systems/HoverPowerUISystem.cs
// Bridges Mod.Settings to cs2/api bindings, owns shared panel-open flag,
// and polls the H hotkey each frame (CityWatchdog pattern).

using Colossal.UI.Binding;
using CS2Shared.RiverMochi;
using Game;
using Game.Input;
using Game.SceneFlow;
using Game.UI;
using HoverPower.Settings;
using HoverPower.Systems;
using System;

namespace HoverPower.UI
{
    public partial class HoverPowerUISystem : UISystemBase
    {
        private static bool s_PanelOpen;

        // Toggle target for both the GTL button (via SetPanelOpen trigger) and the H hotkey poll below.
        public static void TogglePanel() => s_PanelOpen = !s_PanelOpen;

        // ProxyAction for the H key, fetched after Setting.RegisterKeyBindings() ran in Mod.OnLoad.
        // Polled via WasReleasedThisFrame() each tick — matches CityWatchdog (no event handlers).
        private ProxyAction? m_TogglePanelAction;

        protected override void OnCreate()
        {
            base.OnCreate();
            LogUtils.Info(() => $"{Mod.ModTag} HoverPowerUISystem created");

            InitializeKeybindActions();

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
                Mod.ModId, "AreaBorderA",
                () => Mod.Settings?.AreaBorderA ?? 0.702f));

            AddUpdateBinding(new GetterValueBinding<float>(
                Mod.ModId, "FillA",
                () => Mod.Settings?.FillA ?? 0f));

            AddUpdateBinding(new GetterValueBinding<int>(
                Mod.ModId, "GuidelineOpacityPercent",
                () => Mod.Settings?.GuidelineOpacityPercent ?? 40));

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
                "SetAreaAlpha",
                a =>
                {
                    HoverPowerSettings? settings = Mod.Settings;
                    if (settings == null) return;

                    settings.AreaBorderA = a;
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

            AddBinding(new TriggerBinding(
                Mod.ModId,
                "ResetToVanilla",
                () =>
                {
                    HoverPowerSettings? settings = Mod.Settings;
                    if (settings == null) return;

                    UnityEngine.Color hovered = OutlineColorSystem.CapturedHoveredColor;
                    settings.OutlineR = hovered.r;
                    settings.OutlineG = hovered.g;
                    settings.OutlineB = hovered.b;
                    settings.OutlineA = OutlineColorSystem.CapturedOutlineA;
                    settings.AreaBorderA = OutlineColorSystem.CapturedAreaBorderA;
                    settings.FillA = OutlineColorSystem.CapturedFillA;
                    settings.ApplyAndSave();
                }));

            AddBinding(new TriggerBinding(
                Mod.ModId,
                "ResetOutlineToVanilla",
                () =>
                {
                    HoverPowerSettings? settings = Mod.Settings;
                    if (settings == null) return;

                    UnityEngine.Color hovered = OutlineColorSystem.CapturedHoveredColor;
                    settings.OutlineR = hovered.r;
                    settings.OutlineG = hovered.g;
                    settings.OutlineB = hovered.b;
                    settings.OutlineA = OutlineColorSystem.CapturedOutlineA;
                    settings.ApplyAndSave();
                }));

            AddBinding(new TriggerBinding(
                Mod.ModId,
                "ResetAreaToVanilla",
                () =>
                {
                    HoverPowerSettings? settings = Mod.Settings;
                    if (settings == null) return;

                    settings.AreaBorderA = OutlineColorSystem.CapturedAreaBorderA;
                    settings.ApplyAndSave();
                }));

            AddBinding(new TriggerBinding<bool>(
                Mod.ModId,
                "SetPanelOpen",
                open => s_PanelOpen = open));
        }

        protected override void OnUpdate()
        {
            // CRITICAL: base.OnUpdate() ticks the m_UpdateBindings list (PanelOpen, OutlineR, etc).
            // Without this call, every GetterValueBinding we registered goes silent — UI never sees
            // settings changes, button stays selected=false even after click, hotkey toggles the
            // C# flag but the panel never opens. CWD avoids this trap by using ValueBinding (push)
            // instead of GetterValueBinding (pull); we use the pull style, so we must call base.
            base.OnUpdate();

            // Re-fetch if the action wasn't ready at OnCreate (RegisterKeyBindings race) or got dropped.
            RefreshKeybindActions();

            // Don't fire hotkeys in main menu / editor.
            if (!IsInGame())
            {
                return;
            }

            // Read current shared state and flip it — works whether button or previous hotkey set it.
            if (m_TogglePanelAction?.WasReleasedThisFrame() == true)
            {
                TogglePanel();
            }
        }

        private void InitializeKeybindActions()
        {
            m_TogglePanelAction = EnableAction(Mod.kTogglePanelActionName);
        }

        private void RefreshKeybindActions()
        {
            if (m_TogglePanelAction == null)
            {
                m_TogglePanelAction = EnableAction(Mod.kTogglePanelActionName);
            }
        }

        // CWD-style: fetch the ProxyAction registered by the [SettingsUIKeyboardBinding] attribute
        // and flip shouldBeEnabled so it actually receives input. Returns null on miss.
        private static ProxyAction? EnableAction(string actionName)
        {
            try
            {
                ProxyAction? action = Mod.Settings?.GetAction(actionName);
                if (action != null)
                {
                    action.shouldBeEnabled = true;
                }
                return action;
            }
            catch (Exception ex)
            {
                LogUtils.WarnOnce(
                    "missing-keybind-" + actionName,
                    () => $"{Mod.ModTag} Keybinding '{actionName}' unavailable: {ex.GetType().Name}: {ex.Message}",
                    ex);
                return null;
            }
        }

        private static bool IsInGame()
        {
            return GameManager.instance != null && GameManager.instance.gameMode == GameMode.Game;
        }
    }
}
