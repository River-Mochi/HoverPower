// File: UI/src/MochiColorPickerPanel.tsx
// Purpose: In-city panel anchored under the GameTopLeft icon button.
// Layout (post-redesign):
//   - Outline section: vanilla ColorField (RGB + alpha + hex input + wheel popup)
//   - Fill section:    single MochiColorSlider for alpha only (Fill RGB sliders dropped — they
//                      didn't visibly do anything because we route Outline RGB to all surfaces)
//   - Reset row:       rectangular vanilla Button that snaps everything back to game cyan defaults
//
// State sync model:
//   useValue() on each bound channel reads live from C# (Settings.OutlineR etc.).
//   We keep a local React copy so we can update the panel instantly on Reset without waiting
//   for the next binding tick.

import React from "react";
import { Button, Tooltip } from "cs2/ui";
import { Color } from "cs2/bindings";
import { bindValue, trigger, useValue } from "cs2/api";
import { VanillaResolver } from "./utils/VanilliaResolver";
import { MochiColorSlider } from "./utils/MochiColorSlider";

const CHANNEL = "HighlightsOpacity";

const outlineR$ = bindValue<number>(CHANNEL, "OutlineR", 0.502);
const outlineG$ = bindValue<number>(CHANNEL, "OutlineG", 0.869);
const outlineB$ = bindValue<number>(CHANNEL, "OutlineB", 1);
const outlineA$ = bindValue<number>(CHANNEL, "OutlineA", 0.855);
const fillA$ = bindValue<number>(CHANNEL, "FillA", 0);

// Vanilla game defaults — keep in sync with Settings/Setting.cs SetDefaults().
const VANILLA_OUTLINE: Color = { r: 0.502, g: 0.869, b: 1, a: 0.855 };
const VANILLA_FILL_A = 0;

export const MochiColorPickerPanel = () => {
    // Read live values from C# settings via bindings.
    const boundOutline: Color = {
        r: useValue(outlineR$),
        g: useValue(outlineG$),
        b: useValue(outlineB$),
        a: useValue(outlineA$),
    };
    const boundFillA = useValue(fillA$);

    // Local mirrors so Reset can snap state without waiting on the binding round-trip.
    const [outline, setOutline] = React.useState<Color>(boundOutline);
    const [fillA, setFillA] = React.useState<number>(boundFillA);

    const handleOutlineChange = (c: Color) => {
        setOutline(c);
        trigger(CHANNEL, "SetOutlineColor", c.r, c.g, c.b, c.a);
    };

    const handleFillAChange = (sliderPercent: number) => {
        const a = Math.max(0, Math.min(1, sliderPercent / 100));
        setFillA(a);
        trigger(CHANNEL, "SetFillAlpha", a);
    };

    const handleReset = () => {
        setOutline(VANILLA_OUTLINE);
        setFillA(VANILLA_FILL_A);
        trigger(CHANNEL, "SetOutlineColor", VANILLA_OUTLINE.r, VANILLA_OUTLINE.g, VANILLA_OUTLINE.b, VANILLA_OUTLINE.a);
        trigger(CHANNEL, "SetFillAlpha", VANILLA_FILL_A);
    };

    const ColorField = VanillaResolver.instance.ColorField;
    const Section = VanillaResolver.instance.Section;

    return (
        <div style={{
            position: "absolute",
            top: "100%",
            left: 0,
            marginTop: "6rem",
            zIndex: 10000,
        }}>
            <div className="panel_YqS menu_O_M">
                <div className=" content_XD5 content_AD7 child-opacity-transition_nkS content_Hzl">
                    <div style={{ padding: "8rem 8rem 4rem 8rem", display: "flex", flexDirection: "column", gap: "8rem" }}>
                        <Section title="Outline">
                            <ColorField
                                value={outline}
                                alpha={true}
                                hexInput={true}
                                colorWheel={true}
                                onChange={handleOutlineChange}
                            />
                        </Section>

                        <Section title="Fill">
                            <MochiColorSlider
                                className="sliderAlpha"
                                min={0}
                                max={100}
                                step={1}
                                value={fillA * 100}
                                onChange={handleFillAChange}
                                formatValue={(v) => v.toFixed(0)}
                            />
                        </Section>
                    </div>

                    <div style={{
                        display: "flex",
                        justifyContent: "flex-end",
                        padding: "8rem",
                        borderTop: "1rem solid rgba(255, 255, 255, 0.15)",
                    }}>
                        <Tooltip tooltip="Reset colors to game defaults, cyan blue.">
                            <Button variant="menu" onSelect={handleReset}>
                                Reset
                            </Button>
                        </Tooltip>
                    </div>
                </div>
            </div>
        </div>
    );
};
