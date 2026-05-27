// File: Systems/GuidelineColorSystem.cs
// Purpose: Scale the alpha of the in-game guideline overlay colors by a single user-controlled
// multiplier. The guideline overlay is what the game draws while placing roads/zones/props
// (high/medium/low/very-low priority arrows + the positive-feedback green).
//
// Discovery credit: yenyang's HighlightsAndGuidelinesTweaks pointed at GuideLineSettingsData on
// the rendering-settings prefab. We instead write to the runtime singleton each time the slider
// moves, which:
//   - applies live without needing the player to reload the city, and
//   - is read every frame by Game.Rendering.GuideLinesSystem
//     (m_RenderingSettingsQuery.GetSingleton<GuideLineSettingsData>()).
//
// Performance contract — same as OutlineColorSystem.cs:
//   - Capture the game's default alphas ONCE (first successful read of the singleton). The
//     multiplier scales these defaults; defaults themselves are never modified by us, so
//     the per-priority alpha relationships the artists picked are preserved.
//   - Compare current slider value against last-applied value at the top of OnUpdate and
//     early-return when the user isn't moving the slider. Idle cost = one float compare.

using CS2Shared.RiverMochi;
using Game;
using Game.Prefabs;
using HighlightsOpacity.Settings;
using Unity.Entities;
using UnityEngine;

namespace HighlightsOpacity.Systems
{
    public partial class GuidelineColorSystem : GameSystemBase
    {
        private EntityQuery m_Query;

        // Snapshot of the game's default alphas, captured the first time we successfully read the
        // singleton. The multiplier scales these, never the previously-modified values.
        private float m_DefAlphaVeryLow;
        private float m_DefAlphaLow;
        private float m_DefAlphaMedium;
        private float m_DefAlphaHigh;
        private float m_DefAlphaPositive;
        private bool m_DefaultsCaptured;

        // Last applied opacity (0..1). NaN sentinel ensures the first apply always runs.
        private float m_LastOpacity = float.NaN;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Query = GetEntityQuery(ComponentType.ReadWrite<GuideLineSettingsData>());
            LogUtils.Info(() => $"{Mod.ModTag} GuidelineColorSystem created");
        }

        protected override void OnUpdate()
        {
            HighlightsOpacitySettings? settings = Mod.Settings;
            if (settings == null)
            {
                return;
            }

            float opacity = Mathf.Clamp01(settings.GuidelineOpacityPercent / 100f);

            // Hot-path early-return: defaults captured AND slider unchanged since last apply.
            if (m_DefaultsCaptured && opacity == m_LastOpacity)
            {
                return;
            }

            if (m_Query.IsEmptyIgnoreFilter)
            {
                return;
            }

            Entity entity = m_Query.GetSingletonEntity();
            GuideLineSettingsData data = EntityManager.GetComponentData<GuideLineSettingsData>(entity);

            if (!m_DefaultsCaptured)
            {
                m_DefAlphaVeryLow = data.m_VeryLowPriorityColor.a;
                m_DefAlphaLow = data.m_LowPriorityColor.a;
                m_DefAlphaMedium = data.m_MediumPriorityColor.a;
                m_DefAlphaHigh = data.m_HighPriorityColor.a;
                m_DefAlphaPositive = data.m_PositiveFeedbackColor.a;
                m_DefaultsCaptured = true;
                LogUtils.Info(() => $"{Mod.ModTag} GuidelineColorSystem captured default alphas " +
                    $"(VL={m_DefAlphaVeryLow:F3} L={m_DefAlphaLow:F3} M={m_DefAlphaMedium:F3} " +
                    $"H={m_DefAlphaHigh:F3} P={m_DefAlphaPositive:F3})");
            }

            data.m_VeryLowPriorityColor = WithAlpha(data.m_VeryLowPriorityColor, m_DefAlphaVeryLow * opacity);
            data.m_LowPriorityColor = WithAlpha(data.m_LowPriorityColor, m_DefAlphaLow * opacity);
            data.m_MediumPriorityColor = WithAlpha(data.m_MediumPriorityColor, m_DefAlphaMedium * opacity);
            data.m_HighPriorityColor = WithAlpha(data.m_HighPriorityColor, m_DefAlphaHigh * opacity);
            data.m_PositiveFeedbackColor = WithAlpha(data.m_PositiveFeedbackColor, m_DefAlphaPositive * opacity);

            EntityManager.SetComponentData(entity, data);
            m_LastOpacity = opacity;
        }

        private static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
