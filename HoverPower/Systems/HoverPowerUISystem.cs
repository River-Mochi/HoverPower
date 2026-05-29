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
using System.Collections.Generic;

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
        private ProxyAction? m_ToggleSurfaceToolAreasAction;
        private ValueBinding<float> m_OutlineRBinding = null!;
        private ValueBinding<float> m_OutlineGBinding = null!;
        private ValueBinding<float> m_OutlineBBinding = null!;
        private ValueBinding<float> m_OutlineABinding = null!;
        private ValueBinding<float> m_FillABinding = null!;
        private ValueBinding<int> m_GuidelineOpacityBinding = null!;
        private ValueBinding<bool> m_PanelOpenBinding = null!;
        private ValueBinding<bool> m_SurfaceToolAreasSuppressedBinding = null!;
        private ValueBinding<bool> m_VanillaOutlineActiveBinding = null!;

        protected override void OnCreate()
        {
            base.OnCreate();
            LogUtils.Info(() => $"{Mod.ModTag} HoverPowerUISystem created");

            InitializeKeybindActions();

            RegisterValueBindings();

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
                    SyncValueBindings();
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
                    SyncValueBindings();
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
                    SyncValueBindings();
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
                    settings.FillA = OutlineColorSystem.CapturedFillA;
                    settings.ApplyAndSave();
                    SyncValueBindings();
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
                    SyncValueBindings();
                }));

            AddBinding(new TriggerBinding<bool>(
                Mod.ModId,
                "SetPanelOpen",
                SetPanelOpen));

            AddBinding(new TriggerBinding(
                Mod.ModId,
                "ToggleSurfaceToolAreas",
                ToggleSurfaceToolAreas));
        }

        protected override void OnUpdate()
        {
            // CWD-style push bindings: do not call base.OnUpdate() because there are no
            // GetterValueBindings to poll. This keeps the panel idle unless a value actually changes.
            SyncValueBindings();

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
                SyncValueBindings();
            }

            if (m_ToggleSurfaceToolAreasAction?.WasReleasedThisFrame() == true)
            {
                ToggleSurfaceToolAreas();
            }
        }

        private void RegisterValueBindings()
        {
            HoverPowerSettings? settings = Mod.Settings;
            m_OutlineRBinding = AddValueBinding("OutlineR", settings?.OutlineR ?? 0.502f);
            m_OutlineGBinding = AddValueBinding("OutlineG", settings?.OutlineG ?? 0.869f);
            m_OutlineBBinding = AddValueBinding("OutlineB", settings?.OutlineB ?? 1f);
            m_OutlineABinding = AddValueBinding("OutlineA", settings?.OutlineA ?? 0.855f);
            m_FillABinding = AddValueBinding("FillA", settings?.FillA ?? 0f);
            m_GuidelineOpacityBinding = AddValueBinding("GuidelineOpacityPercent", settings?.GuidelineOpacityPercent ?? 40);
            m_PanelOpenBinding = AddValueBinding("PanelOpen", s_PanelOpen);
            m_SurfaceToolAreasSuppressedBinding = AddValueBinding("SurfaceToolAreasSuppressed", SurfaceToolOverlaySystem.SuppressSurfaceToolAreas);
            m_VanillaOutlineActiveBinding = AddValueBinding("VanillaOutlineActive", IsVanillaOutlineActive());
        }

        private ValueBinding<T> AddValueBinding<T>(string name, T initialValue)
        {
            ValueBinding<T> binding = new ValueBinding<T>(Mod.ModId, name, initialValue);
            AddBinding(binding);
            return binding;
        }

        private void SyncValueBindings()
        {
            HoverPowerSettings? settings = Mod.Settings;
            UpdateIfChanged(m_OutlineRBinding, settings?.OutlineR ?? 0.502f);
            UpdateIfChanged(m_OutlineGBinding, settings?.OutlineG ?? 0.869f);
            UpdateIfChanged(m_OutlineBBinding, settings?.OutlineB ?? 1f);
            UpdateIfChanged(m_OutlineABinding, settings?.OutlineA ?? 0.855f);
            UpdateIfChanged(m_FillABinding, settings?.FillA ?? 0f);
            UpdateIfChanged(m_GuidelineOpacityBinding, settings?.GuidelineOpacityPercent ?? 40);
            UpdateIfChanged(m_PanelOpenBinding, s_PanelOpen);
            UpdateIfChanged(m_SurfaceToolAreasSuppressedBinding, SurfaceToolOverlaySystem.SuppressSurfaceToolAreas);
            UpdateIfChanged(m_VanillaOutlineActiveBinding, IsVanillaOutlineActive());
        }

        private static void UpdateIfChanged<T>(ValueBinding<T> binding, T value)
        {
            if (EqualityComparer<T>.Default.Equals(binding.value, value))
            {
                return;
            }

            binding.Update(value);
        }

        private void SetPanelOpen(bool open)
        {
            s_PanelOpen = open;
            UpdateIfChanged(m_PanelOpenBinding, s_PanelOpen);
        }

        private void ToggleSurfaceToolAreas()
        {
            SurfaceToolOverlaySystem.ToggleSuppression();
            UpdateIfChanged(m_SurfaceToolAreasSuppressedBinding, SurfaceToolOverlaySystem.SuppressSurfaceToolAreas);
        }

        private static bool IsVanillaOutlineActive()
        {
            HoverPowerSettings? settings = Mod.Settings;
            return settings != null
                && OutlineColorSystem.MatchesCapturedVanillaProfile(
                    settings.OutlineR,
                    settings.OutlineG,
                    settings.OutlineB,
                    settings.OutlineA,
                    settings.FillA);
        }

        private void InitializeKeybindActions()
        {
            m_TogglePanelAction = EnableAction(Mod.kTogglePanelActionName);
            m_ToggleSurfaceToolAreasAction = EnableAction(Mod.kToggleSurfaceToolAreasActionName);
        }

        private void RefreshKeybindActions()
        {
            if (m_TogglePanelAction == null)
            {
                m_TogglePanelAction = EnableAction(Mod.kTogglePanelActionName);
            }

            if (m_ToggleSurfaceToolAreasAction == null)
            {
                m_ToggleSurfaceToolAreasAction = EnableAction(Mod.kToggleSurfaceToolAreasActionName);
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
