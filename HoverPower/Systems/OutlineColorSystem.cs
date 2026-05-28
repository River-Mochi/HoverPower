// File: Systems/OutlineColorSystem.cs
// Purpose: Apply user-chosen outline color + fill/outline alpha to the game's selection highlight.
// Also auto-overrides to vanilla cyan while the player is using Bulldoze or Net (road) tools so
// invisible-alpha settings don't make the targeted house / road preview impossible to see.
//
// Surfaces written (one color choice covers all of them, two alpha sliders control opacity):
//   - RenderingSettingsData.m_HoveredColor.RGB   ← Outline RGB (lot-pattern tint on hovered building)
//   - RenderingSettingsData.m_OwnerColor.RGB     ← Outline RGB (parent/owner/area objects share same color)
//   - RenderingSettingsData.m_OwnerColor.a       ← AreaBorderA (district / extractor / area-border opacity)
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

namespace HoverPower.Systems
{
    using CS2Shared.RiverMochi;
    using Game;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using HoverPower.Settings;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Rendering.HighDefinition;

    public partial class OutlineColorSystem : GameSystemBase
    {
        // Vanilla cyan defaults applied during Bulldoze / Net tool override.
        // Keep in sync with HoverPowerSettings.SetDefaults().
        private const float VanillaR = 0.502f;
        private const float VanillaG = 0.869f;
        private const float VanillaB = 1f;
        private const float VanillaOutlineA = 0.855f;
        private const float VanillaFillA = 0f;
        private const float VanillaOwnerR = 0.247f;
        private const float VanillaOwnerG = 0.981f;
        private const float VanillaOwnerB = 0.247f;
        private const float VanillaOwnerA = 0.702f;

        public static Color CapturedHoveredColor { get; private set; } = new Color(VanillaR, VanillaG, VanillaB, VanillaOutlineA);
        public static Color CapturedOwnerColor { get; private set; } = new Color(VanillaOwnerR, VanillaOwnerG, VanillaOwnerB, VanillaOwnerA);
        public static Color CapturedOuterColor { get; private set; } = new Color(1f, 1f, 1f, VanillaOutlineA);
        public static Color CapturedInnerColor { get; private set; } = new Color(1f, 1f, 1f, VanillaFillA);
        public static float CapturedOutlineA { get; private set; } = VanillaOutlineA;
        public static float CapturedAreaBorderA { get; private set; } = VanillaOwnerA;
        public static float CapturedFillA { get; private set; } = VanillaFillA;
        public static bool HasCapturedVanillaDefaults { get; private set; }

        private EntityQuery m_RenderSettingsQuery;
        private ToolSystem? m_ToolSystem;
        private PrefabSystem? m_PrefabSystem;
        private readonly PrefabID m_RenderingSettingsPrefab = new(nameof(m_RenderingSettingsPrefab), "RenderingSettings");

        // Cached HDRP outline material. UnityEngine.Object operator!= detects destroyed-but-not-null.
        private Material? m_OutlineMaterial;
        private bool m_PrefabDefaultsCaptured;
        private bool m_RenderingDefaultsCaptured;
        private bool m_MaterialDefaultsCaptured;
        private bool m_CaptureLogged;

        // Last-applied EFFECTIVE values (after tool-override decision).
        private float m_LastR, m_LastG, m_LastB, m_LastOutlineA, m_LastAreaBorderA, m_LastFillA;
        private bool m_LastUsedVanillaPalette;
        private bool m_Applied;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_RenderSettingsQuery = GetEntityQuery(ComponentType.ReadWrite<RenderingSettingsData>());
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            LogUtils.Info(() => $"{Mod.ModTag} OutlineColorSystem created");
        }

        protected override void OnUpdate()
        {
            HoverPowerSettings? settings = Mod.Settings;
            if (settings == null)
            {
                return;
            }

            TryCaptureVanillaDefaults();

            // Tool-override decision: while the player is targeting things with bulldoze or net
            // (road) tools, ignore user settings and use vanilla cyan so the target stays visible.
            bool toolOverride = m_ToolSystem != null
                && m_ToolSystem.activeTool is (BulldozeToolSystem or NetToolSystem);

            float r, g, b, outlineA, areaBorderA, fillA;
            bool useVanillaPalette;
            if (toolOverride)
            {
                Color hovered = CapturedHoveredColor;
                r = hovered.r;
                g = hovered.g;
                b = hovered.b;
                outlineA = CapturedOutlineA;
                areaBorderA = CapturedAreaBorderA;
                fillA = CapturedFillA;
                useVanillaPalette = true;
            }
            else
            {
                r = settings.OutlineR;
                g = settings.OutlineG;
                b = settings.OutlineB;
                outlineA = settings.OutlineA;
                areaBorderA = settings.AreaBorderA;
                fillA = settings.FillA;
                useVanillaPalette = MatchesCapturedVanillaProfile(r, g, b, outlineA, areaBorderA, fillA);
            }

            // Hot-path: neither effective slider value nor the override flag has shifted.
            if (m_Applied
                && r == m_LastR
                && g == m_LastG
                && b == m_LastB
                && outlineA == m_LastOutlineA
                && areaBorderA == m_LastAreaBorderA
                && fillA == m_LastFillA
                && useVanillaPalette == m_LastUsedVanillaPalette)
            {
                return;
            }

            bool ecsOk = ApplyRenderingSettingsColors(r, g, b, outlineA, areaBorderA, useVanillaPalette);
            bool matOk = ApplyOutlineMaterialColors(r, g, b, outlineA, fillA, useVanillaPalette);

            // Only cache the snapshot when BOTH writes land — otherwise retry next frame.
            if (ecsOk && matOk)
            {
                m_LastR = r;
                m_LastG = g;
                m_LastB = b;
                m_LastOutlineA = outlineA;
                m_LastAreaBorderA = areaBorderA;
                m_LastFillA = fillA;
                m_LastUsedVanillaPalette = useVanillaPalette;
                m_Applied = true;
            }
        }

        private void TryCaptureVanillaDefaults()
        {
            if (!m_PrefabDefaultsCaptured && m_PrefabSystem != null
                && m_PrefabSystem.TryGetPrefab(m_RenderingSettingsPrefab, out PrefabBase prefab)
                && m_PrefabSystem.TryGetEntity(prefab, out Entity prefabEntity)
                && EntityManager.HasComponent<RenderingSettingsData>(prefabEntity))
            {
                RenderingSettingsData prefabData = EntityManager.GetComponentData<RenderingSettingsData>(prefabEntity);
                if (!m_RenderingDefaultsCaptured)
                {
                    CapturedHoveredColor = prefabData.m_HoveredColor;
                    CapturedOwnerColor = prefabData.m_OwnerColor;
                    CapturedAreaBorderA = prefabData.m_OwnerColor.a;
                    m_RenderingDefaultsCaptured = true;
                }

                m_PrefabDefaultsCaptured = true;
            }

            if (!m_RenderingDefaultsCaptured && !m_RenderSettingsQuery.IsEmptyIgnoreFilter)
            {
                Entity entity = m_RenderSettingsQuery.GetSingletonEntity();
                RenderingSettingsData data = EntityManager.GetComponentData<RenderingSettingsData>(entity);
                CapturedHoveredColor = data.m_HoveredColor;
                CapturedOwnerColor = data.m_OwnerColor;
                CapturedAreaBorderA = data.m_OwnerColor.a;
                m_RenderingDefaultsCaptured = true;
            }

            if (!m_MaterialDefaultsCaptured && TryResolveOutlineMaterial())
            {
                Color outer = m_OutlineMaterial!.GetColor("_OuterColor");
                Color inner = m_OutlineMaterial.GetColor("_InnerColor");
                CapturedOuterColor = outer;
                CapturedInnerColor = inner;
                CapturedOutlineA = outer.a;
                CapturedFillA = inner.a;
                m_MaterialDefaultsCaptured = true;
            }

            if (!HasCapturedVanillaDefaults && m_RenderingDefaultsCaptured && m_MaterialDefaultsCaptured)
            {
                HasCapturedVanillaDefaults = true;
            }

            if (!m_CaptureLogged && HasCapturedVanillaDefaults)
            {
                m_CaptureLogged = true;
                LogUtils.Info(() => $"{Mod.ModTag} Captured vanilla hover defaults: " +
                    $"Hovered RGBA=({CapturedHoveredColor.r:F3}, {CapturedHoveredColor.g:F3}, {CapturedHoveredColor.b:F3}, {CapturedHoveredColor.a:F3}) " +
                    $"Owner RGBA=({CapturedOwnerColor.r:F3}, {CapturedOwnerColor.g:F3}, {CapturedOwnerColor.b:F3}, {CapturedOwnerColor.a:F3}) " +
                    $"Outer RGBA=({CapturedOuterColor.r:F3}, {CapturedOuterColor.g:F3}, {CapturedOuterColor.b:F3}, {CapturedOuterColor.a:F3}) " +
                    $"Inner RGBA=({CapturedInnerColor.r:F3}, {CapturedInnerColor.g:F3}, {CapturedInnerColor.b:F3}, {CapturedInnerColor.a:F3})");
            }
        }

        // ECS singleton: hovered + owner overlay color used by several vanilla render paths.
        // Building lots clamp hovered alpha internally, but area/surface borders read owner alpha
        // directly, so AreaBorderA gives districts / extractors their own visibility control while
        // the visible halo edge still comes from the HDRP _OuterColor material alpha.
        private bool ApplyRenderingSettingsColors(float r, float g, float b, float outlineA, float areaBorderA, bool useVanillaPalette)
        {
            if (m_RenderSettingsQuery.IsEmptyIgnoreFilter)
            {
                return false;
            }

            Entity entity = m_RenderSettingsQuery.GetSingletonEntity();
            RenderingSettingsData data = EntityManager.GetComponentData<RenderingSettingsData>(entity);

            if (useVanillaPalette)
            {
                data.m_HoveredColor = CapturedHoveredColor;
                data.m_OwnerColor = CapturedOwnerColor;
            }
            else
            {
                data.m_HoveredColor = new Color(r, g, b, outlineA);
                data.m_OwnerColor = new Color(r, g, b, areaBorderA);
            }

            EntityManager.SetComponentData(entity, data);
            return true;
        }

        // HDRP material: same RGB to both inner and outer (so the color covers the halo edge AND
        // the fill overlay inside the silhouette). Two distinct alphas:
        //   _OuterColor.a = outlineA (halo edge opacity)
        //   _InnerColor.a = fillA    (fill overlay opacity inside the silhouette)
        private bool ApplyOutlineMaterialColors(float r, float g, float b, float outlineA, float fillA, bool useVanillaPalette)
        {
            if (!TryResolveOutlineMaterial())
            {
                return false;
            }

            Color outer;
            Color inner;
            if (useVanillaPalette)
            {
                outer = CapturedOuterColor;
                inner = CapturedInnerColor;
            }
            else
            {
                outer = new Color(r, g, b, outlineA);
                inner = new Color(r, g, b, fillA);
            }

            m_OutlineMaterial!.SetColor("_OuterColor", outer);
            m_OutlineMaterial.SetColor("_InnerColor", inner);
            return true;
        }

        private bool MatchesCapturedVanillaProfile(float r, float g, float b, float outlineA, float areaBorderA, float fillA)
        {
            return ApproximatelyEqual(r, CapturedHoveredColor.r)
                && ApproximatelyEqual(g, CapturedHoveredColor.g)
                && ApproximatelyEqual(b, CapturedHoveredColor.b)
                && ApproximatelyEqual(outlineA, CapturedOutlineA)
                && ApproximatelyEqual(areaBorderA, CapturedAreaBorderA)
                && ApproximatelyEqual(fillA, CapturedFillA);
        }

        private static bool ApproximatelyEqual(float a, float b)
        {
            return Mathf.Abs(a - b) < 0.0005f;
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
