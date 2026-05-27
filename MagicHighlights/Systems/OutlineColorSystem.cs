// File: Systems/OutlineColorSystem.cs
// Purpose: Apply user-chosen outline color + fill/outline alpha to the game's selection highlight.
// Also auto-overrides to vanilla cyan while the player is using Bulldoze or Net (road) tools so
// invisible-alpha settings don't make the targeted house / road preview impossible to see.
//
// Surfaces written (one color choice covers all of them, two alpha sliders control opacity):
//   - RenderingSettingsData.m_HoveredColor.RGB   ← Outline RGB (lot-pattern tint on hovered building)
//   - RenderingSettingsData.m_OwnerColor.RGB     ← Outline RGB (parent/owner objects of placed building)
//   - Material _OuterColor.RGB                   ← Outline RGB (the visible halo edge color)
//   - Material _OuterColor.a                     ← OutlineA   (halo edge opacity)
//   - Material _InnerColor.RGB                   ← Outline RGB (color of fill overlay inside silhouette)
//   - Material _InnerColor.a                     ← FillA      (fill overlay opacity)
//
// Tool override: when ToolSystem.activeTool is BulldozeToolSystem or NetToolSystem we substitute
// the vanilla cyan defaults (Outline R=0.502 G=0.869 B=1.0 A=0.855, Fill A=0) so the player can
// see their target. The dirty-flag tracks the *effective* values so an idle bulldozer session is
// still ~free per frame.
//
// Performance contract (matters because this system runs every Rendering tick):
//   - The HDRP CustomPassVolume / OutlinesWorldUIPass / Material refs are found ONCE and cached.
//     Calling Object.FindObjectsOfType<CustomPassVolume>() every frame is what would tank FPS.
//   - Last-applied 5-float snapshot is kept, so OnUpdate early-returns (5 compares + return) when
//     neither the sliders nor the active-tool override flag changed.
//   - Cache invalidates only when the Material reference goes destroyed-null (e.g. scene reload).

using CS2Shared.RiverMochi;
using Game;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using HighlightsOpacity.Settings;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace HighlightsOpacity.Systems
{
    public partial class OutlineColorSystem : GameSystemBase
    {
        // Vanilla cyan defaults applied during Bulldoze / Net tool override.
        // Keep in sync with HighlightsOpacitySettings.SetDefaults().
        private const float VanillaR = 0.502f;
        private const float VanillaG = 0.869f;
        private const float VanillaB = 1f;
        private const float VanillaOutlineA = 0.855f;
        private const float VanillaFillA = 0f;

        private EntityQuery m_RenderSettingsQuery;
        private ToolSystem? m_ToolSystem;

        // Cached HDRP outline material. UnityEngine.Object operator!= detects destroyed-but-not-null.
        private Material? m_OutlineMaterial;

        // Last-applied EFFECTIVE values (after tool-override decision).
        private float m_LastR, m_LastG, m_LastB, m_LastOutlineA, m_LastFillA;
        private bool m_Applied;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_RenderSettingsQuery = GetEntityQuery(ComponentType.ReadWrite<RenderingSettingsData>());
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            LogUtils.Info(() => $"{Mod.ModTag} OutlineColorSystem created");
        }

        protected override void OnUpdate()
        {
            HighlightsOpacitySettings? settings = Mod.Settings;
            if (settings == null)
            {
                return;
            }

            // Tool-override decision: while the player is targeting things with bulldoze or net
            // (road) tools, ignore user settings and use vanilla cyan so the target stays visible.
            bool toolOverride = m_ToolSystem != null
                && m_ToolSystem.activeTool is (BulldozeToolSystem or NetToolSystem);

            float r, g, b, outlineA, fillA;
            if (toolOverride)
            {
                r = VanillaR;
                g = VanillaG;
                b = VanillaB;
                outlineA = VanillaOutlineA;
                fillA = VanillaFillA;
            }
            else
            {
                r = settings.OutlineR;
                g = settings.OutlineG;
                b = settings.OutlineB;
                outlineA = settings.OutlineA;
                fillA = settings.FillA;
            }

            // Hot-path: neither effective slider value nor the override flag has shifted.
            if (m_Applied
                && r == m_LastR
                && g == m_LastG
                && b == m_LastB
                && outlineA == m_LastOutlineA
                && fillA == m_LastFillA)
            {
                return;
            }

            bool ecsOk = ApplyRenderingSettingsColors(r, g, b);
            bool matOk = ApplyOutlineMaterialColors(r, g, b, outlineA, fillA);

            // Only cache the snapshot when BOTH writes land — otherwise retry next frame.
            if (ecsOk && matOk)
            {
                m_LastR = r;
                m_LastG = g;
                m_LastB = b;
                m_LastOutlineA = outlineA;
                m_LastFillA = fillA;
                m_Applied = true;
            }
        }

        // ECS singleton: lot-pattern tint for hovered + owner objects. Alpha is force-pinned to 0.25
        // inside BuildingLotRenderJob, so writing anything other than 1f to .a here is pointless;
        // we keep it at 1f for documentation clarity.
        private bool ApplyRenderingSettingsColors(float r, float g, float b)
        {
            if (m_RenderSettingsQuery.IsEmptyIgnoreFilter)
            {
                return false;
            }

            Entity entity = m_RenderSettingsQuery.GetSingletonEntity();
            RenderingSettingsData data = EntityManager.GetComponentData<RenderingSettingsData>(entity);

            Color rgb = new Color(r, g, b, 1f);
            data.m_HoveredColor = rgb;
            data.m_OwnerColor = rgb;

            EntityManager.SetComponentData(entity, data);
            return true;
        }

        // HDRP material: same RGB to both inner and outer (so the color covers the halo edge AND
        // the fill overlay inside the silhouette). Two distinct alphas:
        //   _OuterColor.a = outlineA (halo edge opacity)
        //   _InnerColor.a = fillA    (fill overlay opacity inside the silhouette)
        private bool ApplyOutlineMaterialColors(float r, float g, float b, float outlineA, float fillA)
        {
            if (!TryResolveOutlineMaterial())
            {
                return false;
            }

            Color outer = new Color(r, g, b, outlineA);
            Color inner = new Color(r, g, b, fillA);

            m_OutlineMaterial!.SetColor("_OuterColor", outer);
            m_OutlineMaterial.SetColor("_InnerColor", inner);
            return true;
        }

        // Locates the OutlinesWorldUIPass material once per scene and caches it.
        // Re-scans only when the cached Material is destroyed (Unity operator!= detects that).
        private bool TryResolveOutlineMaterial()
        {
            if (m_OutlineMaterial != null)
            {
                return true;
            }

            CustomPassVolume[] volumes = Object.FindObjectsOfType<CustomPassVolume>();
            for (int i = 0; i < volumes.Length; i++)
            {
                CustomPassVolume volume = volumes[i];
                if (volume == null || volume.customPasses == null)
                {
                    continue;
                }

                for (int j = 0; j < volume.customPasses.Count; j++)
                {
                    if (volume.customPasses[j] is OutlinesWorldUIPass pass && pass.m_FullscreenOutline != null)
                    {
                        m_OutlineMaterial = pass.m_FullscreenOutline;
                        LogUtils.Info(() => $"{Mod.ModTag} OutlinesWorldUIPass material cached");
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
