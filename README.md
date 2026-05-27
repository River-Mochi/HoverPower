# Magic Highlights [MMH]

Recolor selection outlines and hover highlights in Cities: Skylines II.

- Pick custom **inner** (fill) and **outer** (outline) colors for the hovered/selected object highlight
- Independent **alpha** sliders for inner and outer
- Settings persist between sessions
- No Harmony patches — uses the game's own rendering ECS components and HDRP custom pass

---

## How it works

Two render surfaces are affected:

1. **Selection outline halo** (HDRP `OutlinesWorldUIPass` fullscreen pass)
   The full RGBA you choose flows through to the shader. Both inner and outer alpha sliders are honored.
2. **Lot fill pattern** (translucent tile fill under a hovered/placing building)
   The RGB you choose is applied via `Game.Prefabs.RenderingSettingsData.m_HoveredColor` / `m_OwnerColor`.
   Note: the game's `BuildingLotRenderJob` force-overrides this surface's alpha to 0.25 — that's intentional from
   Colossal/IFS and not something this mod fights.

So the inner/outer alpha sliders visibly change the outline halo, while the RGB changes also tint the lot pattern.



## License

MIT — see [LICENSE](LICENSE).
