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
import { useLocalization } from "cs2/l10n";
import { VanillaComponentResolver } from "./utils/vanilla/VanillaComponentResolver";
import infoIconSrc from "../images/AdvisorInfoViewWhite.svg";
import surfaceIconSrc from "../images/Districts02.svg";
import fillIconSrc from "../images/MainElements-Fill2.svg";
import outlineIconSrc from "../images/MainElements.svg";
import guidelinesIconSrc from "../images/GuideLines4.svg";
import resetIconSrc from "../images/Reset_Button2.svg";
import closeIconSrc from "../images/Close.svg";
import locale from "../../L10n/lang/en-US.json";
import styles from "./MochiColorPickerPanel.module.scss";

const CHANNEL = "HoverPower";
type LocaleKey = keyof typeof locale;

const outlineR$ = bindValue<number>(CHANNEL, "OutlineR", 0.502);
const outlineG$ = bindValue<number>(CHANNEL, "OutlineG", 0.869);
const outlineB$ = bindValue<number>(CHANNEL, "OutlineB", 1);
const outlineA$ = bindValue<number>(CHANNEL, "OutlineA", 0.855);
const fillA$ = bindValue<number>(CHANNEL, "FillA", 0);
const guidelineOpacity$ = bindValue<number>(CHANNEL, "GuidelineOpacityPercent", 40);
const surfaceToolAreasSuppressed$ = bindValue<boolean>(CHANNEL, "SurfaceToolAreasSuppressed", false);
const vanillaOutlineActive$ = bindValue<boolean>(CHANNEL, "VanillaOutlineActive", false);

// Gentle neutral preset for Mochi's preferred subtle highlight.
const PRESET_MOCHI_GRAY_OUTLINE: Color = { r: 140 / 255, g: 140 / 255, b: 171 / 255, a: 0.5 };
const PRESET_MOCHI_GRAY_FILL_A = 0;

// Purple-gray test preset inspired by yenyang's highlight experiments.
const PRESET_YENYANG_OUTLINE: Color = { r: 0.25, g: 0.15, b: 0.25, a: 0.5 };
const PRESET_YENYANG_FILL_A = 0;

const COLOR_EPSILON = 0.0005;

const approximatelyEqual = (left: number, right: number) => Math.abs(left - right) < COLOR_EPSILON;

const matchesPreset = (current: Color, currentFillA: number, preset: Color, presetFillA: number) =>
    approximatelyEqual(current.r, preset.r)
    && approximatelyEqual(current.g, preset.g)
    && approximatelyEqual(current.b, preset.b)
    && approximatelyEqual(current.a, preset.a)
    && approximatelyEqual(currentFillA, presetFillA);

export const MochiColorPickerPanel = () => {
    const boundOutline: Color = {
        r: useValue(outlineR$),
        g: useValue(outlineG$),
        b: useValue(outlineB$),
        a: useValue(outlineA$),
    };
    const boundFillA = useValue(fillA$);
    const boundGuideline = useValue(guidelineOpacity$);
    const surfaceToolAreasSuppressed = useValue(surfaceToolAreasSuppressed$);
    const vanillaOutlineActive = useValue(vanillaOutlineActive$);
    const { translate } = useLocalization();
    const text = React.useMemo(() => {
        const l = (key: LocaleKey) => translate(key, locale[key]) ?? locale[key];
        return {
            ariaClosePanel: l("HoverPower.UI.Aria.ClosePanel"),
            title: l("HoverPower.UI.Title"),
            tooltipClose: l("HoverPower.UI.Tooltip.Close"),
            tooltipDraggable: l("HoverPower.UI.Tooltip.Draggable"),
            tooltipFillOpacity: l("HoverPower.UI.Tooltip.FillOpacity"),
            tooltipGuidelinesOpacity: l("HoverPower.UI.Tooltip.GuidelinesOpacity"),
            tooltipInfo: l("HoverPower.UI.Tooltip.Info"),
            tooltipOutlineSwatch: l("HoverPower.UI.Tooltip.OutlineSwatch"),
            tooltipPreset1: l("HoverPower.UI.Tooltip.Preset1"),
            tooltipPreset2: l("HoverPower.UI.Tooltip.Preset2"),
            tooltipReset: l("HoverPower.UI.Tooltip.Reset"),
            tooltipResetFill: l("HoverPower.UI.Tooltip.ResetFill"),
            tooltipResetGuidelines: l("HoverPower.UI.Tooltip.ResetGuidelines"),
            tooltipResetOutline: l("HoverPower.UI.Tooltip.ResetOutline"),
            tooltipSurfaceToggle: l("HoverPower.UI.Tooltip.SurfaceToggle"),
        };
    }, [translate]);

    const [outline, setOutline] = React.useState<Color>(boundOutline);
    const [fillA, setFillA] = React.useState<number>(boundFillA);
    const [guidelineOpacity, setGuidelineOpacity] = React.useState<number>(boundGuideline);
    const [panelOffset, setPanelOffset] = React.useState({ x: 0, y: 0 });
    const [panelDragging, setPanelDragging] = React.useState(false);
    const [colorPickerDirection, setColorPickerDirection] = React.useState<"up" | "down">("down");
    const outlineSwatchRef = React.useRef<HTMLDivElement | null>(null);
    const panelAnchorRef = React.useRef<HTMLDivElement | null>(null);
    const panelElementRef = React.useRef<HTMLDivElement | null>(null);
    const panelDragFrameRef = React.useRef<number | null>(null);
    const panelDragPendingOffsetRef = React.useRef(panelOffset);
    const panelDragRef = React.useRef<{
        pointerX: number;
        pointerY: number;
        originX: number;
        originY: number;
        originLeft: number;
        originTop: number;
        originWidth: number;
        originHeight: number;
    } | null>(null);

    React.useEffect(() => {
        setOutline(boundOutline);
    }, [boundOutline.r, boundOutline.g, boundOutline.b, boundOutline.a]);

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

            const deltaX = event.clientX - dragState.pointerX;
            const deltaY = event.clientY - dragState.pointerY;
            let nextX = dragState.originX + deltaX;
            let nextY = dragState.originY + deltaY;

            // Keep the panel inside the viewport, especially on the left where the
            // vanilla ColorField popup anchors to the swatch and can otherwise start off-screen.
            const nextLeft = dragState.originLeft + deltaX;
            const nextTop = dragState.originTop + deltaY;
            const nextRight = nextLeft + dragState.originWidth;
            const nextBottom = nextTop + dragState.originHeight;
            if (nextLeft < 0) {
                nextX -= nextLeft;
            }
            if (nextTop < 0) {
                nextY -= nextTop;
            }
            if (nextRight > window.innerWidth) {
                nextX -= nextRight - window.innerWidth;
            }
            if (nextBottom > window.innerHeight) {
                nextY -= nextBottom - window.innerHeight;
            }

            panelDragPendingOffsetRef.current = { x: nextX, y: nextY };
            if (panelDragFrameRef.current == null) {
                panelDragFrameRef.current = window.requestAnimationFrame(() => {
                    panelDragFrameRef.current = null;
                    const anchor = panelAnchorRef.current;
                    if (anchor != null) {
                        const pending = panelDragPendingOffsetRef.current;
                        anchor.style.transform = `translate(${pending.x}px, ${pending.y}px)`;
                    }
                });
            }
        };

        const handleMouseUp = () => {
            if (panelDragFrameRef.current != null) {
                window.cancelAnimationFrame(panelDragFrameRef.current);
                panelDragFrameRef.current = null;
            }
            panelDragRef.current = null;
            setPanelDragging(false);
            setPanelOffset(panelDragPendingOffsetRef.current);
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

    const handleGuidelineChange = (percent: number) => {
        const value = Math.max(0, Math.min(100, Math.round(percent / 5) * 5));
        setGuidelineOpacity(value);
        trigger(CHANNEL, "SetGuidelineOpacity", value);
    };

    const applyPreset = (outlineColor: Color, fillAlpha: number) => {
        setOutline(outlineColor);
        setFillA(fillAlpha);
        trigger(CHANNEL, "SetOutlineColor", outlineColor.r, outlineColor.g, outlineColor.b, outlineColor.a);
        trigger(CHANNEL, "SetFillAlpha", fillAlpha);
    };

    const handleSet1 = () => applyPreset(PRESET_MOCHI_GRAY_OUTLINE, PRESET_MOCHI_GRAY_FILL_A);
    const handleSet2 = () => applyPreset(PRESET_YENYANG_OUTLINE, PRESET_YENYANG_FILL_A);
    const handleReset = () => trigger(CHANNEL, "ResetToVanilla");
    const handleClosePanel = () => trigger(CHANNEL, "SetPanelOpen", false);
    const handleResetOutline = () => trigger(CHANNEL, "ResetOutlineToVanilla");
    const handleResetFill = () => handleFillAChange(0);
    const handleResetGuidelines = () => handleGuidelineChange(40);
    const handleToggleSurfaceToolAreas = () => trigger(CHANNEL, "ToggleSurfaceToolAreas");
    const preset1Active = matchesPreset(outline, fillA, PRESET_MOCHI_GRAY_OUTLINE, PRESET_MOCHI_GRAY_FILL_A);
    const preset2Active = matchesPreset(outline, fillA, PRESET_YENYANG_OUTLINE, PRESET_YENYANG_FILL_A);

    const updateColorPickerDirection = React.useCallback(() => {
        const swatch = outlineSwatchRef.current;
        if (swatch == null) {
            return;
        }

        const rect = swatch.getBoundingClientRect();
        const swatchMiddleY = rect.top + rect.height / 2;
        setColorPickerDirection(swatchMiddleY < window.innerHeight / 2 ? "down" : "up");
    }, []);

    const handlePanelDragStart = (event: React.MouseEvent<HTMLDivElement>) => {
        event.preventDefault();
        event.stopPropagation();
        const rect = panelElementRef.current?.getBoundingClientRect();
        panelDragPendingOffsetRef.current = panelOffset;
        panelDragRef.current = {
            pointerX: event.clientX,
            pointerY: event.clientY,
            originX: panelOffset.x,
            originY: panelOffset.y,
            originLeft: rect?.left ?? 0,
            originTop: rect?.top ?? 0,
            originWidth: rect?.width ?? 0,
            originHeight: rect?.height ?? 0,
        };
        setPanelDragging(true);
    };

    const resolver = VanillaComponentResolver.instance;
    const ColorField = resolver.ColorField;
    const Slider = resolver.Slider;
    const focusDisabled = resolver.FOCUS_DISABLED;
    const numberFieldClass = resolver.mouseToolOptionsTheme["number-field"];
    const roundHighlightButtonTheme = resolver.roundHighlightButtonTheme;
    const outlineFieldClass = styles.outlineField;
    const closeButtonClass = `${roundHighlightButtonTheme["button"] ?? ""} ${styles.closeButton}`;

    return (
        <div
            ref={panelAnchorRef}
            className={styles.panelAnchor}
            style={{ transform: `translate(${panelOffset.x}px, ${panelOffset.y}px)` }}
        >
            <div ref={panelElementRef} className={`panel_YqS menu_O_M ${styles.panelFrame}`}>
                <div className={`content_XD5 content_AD7 child-opacity-transition_nkS content_Hzl ${styles.panelContent}`}>
                    <div className={styles.titleBar}>
                        <Tooltip tooltip={text.tooltipInfo}>
                            <div className={styles.infoButton}>
                                <img src={infoIconSrc} className={styles.infoIcon} alt="" />
                            </div>
                        </Tooltip>

                        <Tooltip tooltip={text.tooltipDraggable}>
                            <div
                                className={`${styles.titleDragHandle} ${panelDragging ? styles.titleDragHandleActive : ""}`}
                                onMouseDown={handlePanelDragStart}
                            >
                                <span className={styles.titleText}>{text.title}</span>
                            </div>
                        </Tooltip>

                        <Tooltip tooltip={text.tooltipClose}>
                            <Button
                                className={closeButtonClass}
                                variant="icon"
                                onClick={handleClosePanel}
                                focusKey={focusDisabled}
                                aria-label={text.ariaClosePanel}
                            >
                                <img src={closeIconSrc} className={styles.closeIcon} alt="" />
                            </Button>
                        </Tooltip>
                    </div>

                    <div className={styles.body}>
                        <div className={`${styles.controlRow} ${styles.outlineRow}`}>
                            <Tooltip tooltip={text.tooltipResetOutline}>
                                <button type="button" className={styles.controlIconButton} onClick={handleResetOutline}>
                                    <img src={outlineIconSrc} className={styles.controlIcon} alt="" />
                                </button>
                            </Tooltip>
                            <div className={`${styles.controlBody} ${styles.outlineControlBody}`}>
                                <Tooltip tooltip={text.tooltipOutlineSwatch}>
                                    <div
                                        ref={outlineSwatchRef}
                                        className={styles.outlineFieldShell}
                                        onMouseEnter={updateColorPickerDirection}
                                        onMouseDown={updateColorPickerDirection}
                                    >
                                        <ColorField
                                            focusKey={focusDisabled}
                                            className={outlineFieldClass}
                                            value={outline}
                                            alpha={true}
                                            popupDirection={colorPickerDirection}
                                            hideHint={true}
                                            hexInput={true}
                                            colorWheel={true}
                                            onChange={handleOutlineChange}
                                            onOpenPicker={updateColorPickerDirection}
                                        />
                                    </div>
                                </Tooltip>
                            </div>
                        </div>

                        <div className={styles.controlRow}>
                            <Tooltip tooltip={text.tooltipResetFill}>
                                <button type="button" className={styles.controlIconButton} onClick={handleResetFill}>
                                    <img src={fillIconSrc} className={styles.controlIcon} alt="" />
                                </button>
                            </Tooltip>
                            <Tooltip tooltip={text.tooltipFillOpacity}>
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
                            <Tooltip tooltip={text.tooltipResetGuidelines}>
                                <button type="button" className={styles.controlIconButton} onClick={handleResetGuidelines}>
                                    <img src={guidelinesIconSrc} className={`${styles.controlIcon} ${styles.guidelinesIcon}`} alt="" />
                                </button>
                            </Tooltip>
                            <Tooltip tooltip={text.tooltipGuidelinesOpacity}>
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
                        <div className={styles.surfaceActions}>
                            <Tooltip tooltip={text.tooltipSurfaceToggle}>
                                <button
                                    type="button"
                                    className={`${styles.actionButton} ${styles.surfaceButton} ${surfaceToolAreasSuppressed ? styles.surfaceButtonActive : ""}`}
                                    onClick={handleToggleSurfaceToolAreas}
                                >
                                    <img src={surfaceIconSrc} className={styles.actionIcon} alt="" />
                                </button>
                            </Tooltip>
                        </div>
                        <div className={styles.presetActions}>
                            <Tooltip tooltip={text.tooltipPreset1}>
                                <button
                                    type="button"
                                    className={`${styles.actionButton} ${styles.presetButton} ${preset1Active ? styles.presetButtonActive : ""}`}
                                    onClick={handleSet1}
                                >
                                    <span className={styles.presetGlyph}>➀</span>
                                </button>
                            </Tooltip>
                            <Tooltip tooltip={text.tooltipPreset2}>
                                <button
                                    type="button"
                                    className={`${styles.actionButton} ${styles.presetButton} ${preset2Active ? styles.presetButtonActive : ""}`}
                                    onClick={handleSet2}
                                >
                                    <span className={styles.presetGlyph}>➁</span>
                                </button>
                            </Tooltip>
                            <Tooltip tooltip={text.tooltipReset}>
                                <button
                                    type="button"
                                    className={`${styles.actionButton} ${styles.resetButton} ${vanillaOutlineActive ? styles.resetButtonActive : ""}`}
                                    onClick={handleReset}
                                >
                                    <img src={resetIconSrc} className={styles.actionIcon} alt="" />
                                </button>
                            </Tooltip>
                        </div>
                    </div>

                    <Tooltip tooltip={text.tooltipDraggable}>
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
