// File: UI/src/MochiColorPickerPanel.tsx
// Purpose: Compact in-city hover-color panel anchored under the GameTopLeft icon button.
// Layout:
//   - Short draggable title bar with info tooltip, title, close button, and backup grip
//   - Outline row: icon + vanilla ColorField swatch launcher
//   - Fill / Guidelines rows: icon-led compact sliders with percent readouts
//   - Preset row: compact slot buttons + icon reset

import React from "react";
import { Button, Tooltip } from "cs2/ui";
import { Color } from "cs2/bindings";
import { bindValue, trigger, useValue } from "cs2/api";
import { VanillaComponentResolver } from "./utils/vanilla/VanillaComponentResolver";
import infoIconSrc from "../images/AdvisorInfoViewWhite.svg";
import areaIconSrc from "../images/Districts.svg";
import fillIconSrc from "../images/MainElements-Fill2.svg";
import outlineIconSrc from "../images/MainElements.svg";
import guidelinesIconSrc from "../images/GuideLines.svg";
import resetIconSrc from "../images/Reset_Button.svg";
import saveIconSrc from "../images/Save.svg";
import styles from "./MochiColorPickerPanel.module.scss";

const CHANNEL = "HoverPower";
const closeIconSrc = "coui://uil/Standard/XClose.svg";

const outlineR$ = bindValue<number>(CHANNEL, "OutlineR", 0.502);
const outlineG$ = bindValue<number>(CHANNEL, "OutlineG", 0.869);
const outlineB$ = bindValue<number>(CHANNEL, "OutlineB", 1);
const outlineA$ = bindValue<number>(CHANNEL, "OutlineA", 0.855);
const areaBorderA$ = bindValue<number>(CHANNEL, "AreaBorderA", 0.702);
const fillA$ = bindValue<number>(CHANNEL, "FillA", 0);
const guidelineOpacity$ = bindValue<number>(CHANNEL, "GuidelineOpacityPercent", 40);

// Gentle neutral preset for Mochi's preferred subtle highlight.
const PRESET_MOCHI_GRAY_OUTLINE: Color = { r: 140 / 255, g: 140 / 255, b: 171 / 255, a: 0.5 };
const PRESET_MOCHI_GRAY_FILL_A = 0;

// Purple-gray test preset inspired by yenyang's highlight experiments.
const PRESET_YENYANG_OUTLINE: Color = { r: 0.25, g: 0.15, b: 0.25, a: 0.5 };
const PRESET_YENYANG_FILL_A = 0;

export const MochiColorPickerPanel = () => {
    const boundOutline: Color = {
        r: useValue(outlineR$),
        g: useValue(outlineG$),
        b: useValue(outlineB$),
        a: useValue(outlineA$),
    };
    const boundAreaBorderA = useValue(areaBorderA$);
    const boundFillA = useValue(fillA$);
    const boundGuideline = useValue(guidelineOpacity$);

    const [outline, setOutline] = React.useState<Color>(boundOutline);
    const [areaBorderA, setAreaBorderA] = React.useState<number>(boundAreaBorderA);
    const [fillA, setFillA] = React.useState<number>(boundFillA);
    const [guidelineOpacity, setGuidelineOpacity] = React.useState<number>(boundGuideline);
    const [panelOffset, setPanelOffset] = React.useState({ x: 0, y: 0 });
    const [panelDragging, setPanelDragging] = React.useState(false);
    const panelDragRef = React.useRef<{
        pointerX: number;
        pointerY: number;
        originX: number;
        originY: number;
    } | null>(null);

    React.useEffect(() => {
        setOutline(boundOutline);
    }, [boundOutline.r, boundOutline.g, boundOutline.b, boundOutline.a]);

    React.useEffect(() => {
        setAreaBorderA(boundAreaBorderA);
    }, [boundAreaBorderA]);

    React.useEffect(() => {
        setFillA(boundFillA);
    }, [boundFillA]);

    React.useEffect(() => {
        setGuidelineOpacity(boundGuideline);
    }, [boundGuideline]);

    React.useEffect(() => {
        if (!panelDragging) {
            return;
        }

        const handleMouseMove = (event: MouseEvent) => {
            const dragState = panelDragRef.current;
            if (dragState == null) {
                return;
            }

            setPanelOffset({
                x: dragState.originX + (event.clientX - dragState.pointerX),
                y: dragState.originY + (event.clientY - dragState.pointerY),
            });
        };

        const handleMouseUp = () => {
            panelDragRef.current = null;
            setPanelDragging(false);
        };

        window.addEventListener("mousemove", handleMouseMove);
        window.addEventListener("mouseup", handleMouseUp);

        return () => {
            window.removeEventListener("mousemove", handleMouseMove);
            window.removeEventListener("mouseup", handleMouseUp);
        };
    }, [panelDragging]);

    const handleOutlineChange = (value: Color) => {
        setOutline(value);
        trigger(CHANNEL, "SetOutlineColor", value.r, value.g, value.b, value.a);
    };

    const handleFillAChange = (sliderValue: number) => {
        const value = Math.max(0, Math.min(1, sliderValue));
        setFillA(value);
        trigger(CHANNEL, "SetFillAlpha", value);
    };

    const handleAreaBorderAChange = (sliderValue: number) => {
        const value = Math.max(0, Math.min(1, sliderValue));
        setAreaBorderA(value);
        trigger(CHANNEL, "SetAreaAlpha", value);
    };

    const handleGuidelineChange = (percent: number) => {
        const value = Math.max(0, Math.min(100, Math.round(percent / 5) * 5));
        setGuidelineOpacity(value);
        trigger(CHANNEL, "SetGuidelineOpacity", value);
    };

    const applyPreset = (outlineColor: Color, areaAlpha: number, fillAlpha: number) => {
        setOutline(outlineColor);
        setAreaBorderA(areaAlpha);
        setFillA(fillAlpha);
        trigger(CHANNEL, "SetOutlineColor", outlineColor.r, outlineColor.g, outlineColor.b, outlineColor.a);
        trigger(CHANNEL, "SetAreaAlpha", areaAlpha);
        trigger(CHANNEL, "SetFillAlpha", fillAlpha);
    };

    const handleSet1 = () => applyPreset(PRESET_MOCHI_GRAY_OUTLINE, PRESET_MOCHI_GRAY_OUTLINE.a, PRESET_MOCHI_GRAY_FILL_A);
    const handleSet2 = () => applyPreset(PRESET_YENYANG_OUTLINE, PRESET_YENYANG_OUTLINE.a, PRESET_YENYANG_FILL_A);
    const handleReset = () => trigger(CHANNEL, "ResetToVanilla");
    const handleClosePanel = () => trigger(CHANNEL, "SetPanelOpen", false);
    const handleResetOutline = () => trigger(CHANNEL, "ResetOutlineToVanilla");
    const handleResetArea = () => trigger(CHANNEL, "ResetAreaToVanilla");
    const handleResetFill = () => handleFillAChange(0);
    const handleResetGuidelines = () => handleGuidelineChange(40);

    const handlePanelDragStart = (event: React.MouseEvent<HTMLDivElement>) => {
        event.preventDefault();
        event.stopPropagation();
        panelDragRef.current = {
            pointerX: event.clientX,
            pointerY: event.clientY,
            originX: panelOffset.x,
            originY: panelOffset.y,
        };
        setPanelDragging(true);
    };

    const resolver = VanillaComponentResolver.instance;
    const ColorField = resolver.ColorField;
    const Slider = resolver.Slider;
    const focusDisabled = resolver.FOCUS_DISABLED;
    const numberFieldClass = resolver.mouseToolOptionsTheme["number-field"];
    const colorFieldTheme = resolver.colorFieldTheme;
    const roundHighlightButtonTheme = resolver.roundHighlightButtonTheme;
    const outlineFieldClass = `${colorFieldTheme["colorField"] ?? ""} ${styles.outlineField}`;
    const closeButtonClass = `${roundHighlightButtonTheme["button"] ?? ""} ${styles.closeButton}`;

    return (
        <div
            className={styles.panelAnchor}
            style={{ transform: `translate(${panelOffset.x}px, ${panelOffset.y}px)` }}
        >
            <div className={`panel_YqS menu_O_M ${styles.panelFrame}`}>
                <div className={`content_XD5 content_AD7 child-opacity-transition_nkS content_Hzl ${styles.panelContent}`}>
                    <div className={styles.titleBar}>
                        <Tooltip tooltip="Mochi's color picker.">
                            <div className={styles.infoButton}>
                                <img src={infoIconSrc} className={styles.infoIcon} alt="" />
                            </div>
                        </Tooltip>

                        <Tooltip tooltip="Draggable">
                            <div
                                className={`${styles.titleDragHandle} ${panelDragging ? styles.titleDragHandleActive : ""}`}
                                onMouseDown={handlePanelDragStart}
                            >
                                <span className={styles.titleText}>Mochi&apos;s Blue Buster</span>
                            </div>
                        </Tooltip>

                        <Tooltip tooltip="Close this panel. You can also toggle it with the GTL icon or the H hotkey.">
                            <Button
                                className={closeButtonClass}
                                variant="icon"
                                onClick={handleClosePanel}
                                focusKey={focusDisabled}
                                aria-label="Close panel"
                            >
                                <img src={closeIconSrc} className={styles.closeIcon} alt="" />
                            </Button>
                        </Tooltip>
                    </div>

                    <div className={styles.body}>
                        <div className={styles.controlRow}>
                            <Tooltip tooltip="Reset outline color and alpha to the game default. The same chosen color also feeds the Area row below.">
                                <Button className={styles.controlIconButton} onClick={handleResetOutline} focusKey={focusDisabled}>
                                    <img src={outlineIconSrc} className={styles.controlIcon} alt="" />
                                </Button>
                            </Tooltip>
                            <div className={styles.controlBody}>
                                <Tooltip tooltip="Click this swatch to open the vanilla color picker. The chosen color is shared by Outline and Area borders.">
                                    <div className={styles.outlineFieldShell}>
                                        <ColorField
                                            focusKey={focusDisabled}
                                            className={outlineFieldClass}
                                            value={outline}
                                            alpha={true}
                                            popupDirection="right"
                                            hideHint={true}
                                            hexInput={true}
                                            colorWheel={true}
                                            onChange={handleOutlineChange}
                                        />
                                    </div>
                                </Tooltip>
                            </div>
                        </div>

                        <div className={styles.controlRow}>
                            <Tooltip tooltip="Reset area-border opacity to the game default. Use this for districts, specialized industry, and similar bordered areas.">
                                <Button className={styles.controlIconButton} onClick={handleResetArea} focusKey={focusDisabled}>
                                    <img src={areaIconSrc} className={styles.controlIcon} alt="" />
                                </Button>
                            </Tooltip>
                            <Tooltip tooltip="Area border opacity for districts, specialized industry, and similar owner borders. If those lines get too faint, raise this slider or use Reset.">
                                <div className={styles.controlBody}>
                                    <div className={styles.sliderRow}>
                                        <Slider
                                            focusKey={focusDisabled}
                                            className={styles.slider}
                                            value={areaBorderA}
                                            start={0}
                                            end={1}
                                            gamepadStep={0.01}
                                            onChange={handleAreaBorderAChange}
                                        />
                                        <div className={`${styles.valueField} ${numberFieldClass}`}>
                                            {`${Math.round(areaBorderA * 100)}%`}
                                        </div>
                                    </div>
                                </div>
                            </Tooltip>
                        </div>

                        <div className={styles.controlRow}>
                            <Tooltip tooltip="Reset fill to the game default. Vanilla fill is 0% for normal building hover.">
                                <Button className={styles.controlIconButton} onClick={handleResetFill} focusKey={focusDisabled}>
                                    <img src={fillIconSrc} className={styles.controlIcon} alt="" />
                                </Button>
                            </Tooltip>
                            <Tooltip tooltip="Opacity of the fill inside the hovered outline. 0% = no inner fill, 100% = fully visible fill.">
                                <div className={styles.controlBody}>
                                    <div className={styles.sliderRow}>
                                        <Slider
                                            focusKey={focusDisabled}
                                            className={styles.slider}
                                            value={fillA}
                                            start={0}
                                            end={1}
                                            gamepadStep={0.01}
                                            onChange={handleFillAChange}
                                        />
                                        <div className={`${styles.valueField} ${numberFieldClass}`}>
                                            {`${Math.round(fillA * 100)}%`}
                                        </div>
                                    </div>
                                </div>
                            </Tooltip>
                        </div>

                        <div className={styles.controlRow}>
                            <Tooltip tooltip="Reset guidelines to the default HoverPower level. Both this panel and Options stay in sync.">
                                <Button className={styles.controlIconButton} onClick={handleResetGuidelines} focusKey={focusDisabled}>
                                    <img src={guidelinesIconSrc} className={styles.controlIcon} alt="" />
                                </Button>
                            </Tooltip>
                            <Tooltip tooltip="Guidelines opacity. Below 15% is not advised because the guides may become too hard to see.">
                                <div className={styles.controlBody}>
                                    <div className={styles.sliderRow}>
                                        <Slider
                                            focusKey={focusDisabled}
                                            className={styles.slider}
                                            value={guidelineOpacity}
                                            start={0}
                                            end={100}
                                            gamepadStep={5}
                                            onChange={handleGuidelineChange}
                                        />
                                        <div className={`${styles.valueField} ${numberFieldClass}`}>
                                            {`${guidelineOpacity}%`}
                                        </div>
                                    </div>
                                </div>
                            </Tooltip>
                        </div>
                    </div>

                    <div className={styles.actions}>
                        <Tooltip tooltip="Preset 1: Mochi gray-purple preset.">
                            <Button className={`${styles.actionButton} ${styles.presetButton}`} onClick={handleSet1} focusKey={focusDisabled}>
                                <span className={styles.slotBadge}>1</span>
                                <img src={saveIconSrc} className={styles.actionIcon} alt="" />
                            </Button>
                        </Tooltip>
                        <Tooltip tooltip="Preset 2: yenyang purple-gray.">
                            <Button className={`${styles.actionButton} ${styles.presetButton}`} onClick={handleSet2} focusKey={focusDisabled}>
                                <span className={styles.slotBadge}>2</span>
                                <img src={saveIconSrc} className={styles.actionIcon} alt="" />
                            </Button>
                        </Tooltip>
                        <Tooltip tooltip="Reset outline, area border, and fill back to the captured game defaults.">
                            <Button className={`${styles.actionButton} ${styles.resetButton}`} onClick={handleReset} focusKey={focusDisabled}>
                                <img src={resetIconSrc} className={styles.actionIcon} alt="" />
                            </Button>
                        </Tooltip>
                    </div>

                    <Tooltip tooltip="Draggable">
                        <div
                            className={`${styles.dragGrip} ${panelDragging ? styles.dragGripActive : ""}`}
                            onMouseDown={handlePanelDragStart}
                        >
                            <span className={styles.dragGripDot}></span>
                            <span className={styles.dragGripDot}></span>
                            <span className={styles.dragGripDot}></span>
                            <span className={styles.dragGripDot}></span>
                        </div>
                    </Tooltip>
                </div>
            </div>
        </div>
    );
};
