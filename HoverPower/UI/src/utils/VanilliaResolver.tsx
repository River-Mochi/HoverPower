import { BalloonDirection, Color, FocusKey, Theme, UniqueFocusKey } from "cs2/bindings";
import { ModuleRegistry } from "cs2/modding";
import { CSSProperties, HTMLAttributes, ReactNode } from "react";

// Props for vanilla ColorField. Shape mirrors the in-game component at
// game-ui/common/input/color-picker/color-field/color-field.tsx
export type PropsColorField = {
    focusKey?: FocusKey | null;
    disabled?: boolean;
    value: Color;
    className?: string;
    selectAction?: string;
    alpha?: boolean;
    popupDirection?: BalloonDirection;
    hideHint?: boolean;
    colorWheel?: boolean;
    hexInput?: boolean;
    onChange?: (value: Color) => void;
    onClick?: () => void;
    onMouseEnter?: () => void;
    onMouseLeave?: () => void;
    onClosePicker?: () => void;
};

export type PropsSlider = {
    focusKey?: FocusKey | null;
    value: number;
    start: number;
    end: number;
    gamepadStep?: number;
    disabled?: boolean;
    vertical?: boolean;
    sounds?: boolean;
    thumb?: any;
    theme?: Theme;
    className?: string;
    style?: CSSProperties;
    children?: ReactNode;
    noFill?: boolean;
    valueTransformer?: (value: number, start: number, end: number) => number;
    onChange?: (value: number) => void;
    onDragStart?: () => void;
    onDragEnd?: () => void;
    onMouseOver?: () => void;
    onMouseLeave?: () => void;
};

type PropsToolButton = {
    focusKey?: UniqueFocusKey | null
    src?: string
    selected?: boolean
    multiSelect?: boolean
    disabled?: boolean
    tooltip?: string | JSX.Element | null
    selectSound?: any
    uiTag?: string
    className?: string
    children?: string | JSX.Element | JSX.Element[]
    onSelect?: (x: any) => any,
} & HTMLAttributes<any>
type PropsStepToolButton = {
    focusKey?: UniqueFocusKey | null
    selectedValue: number
    values: number[]
    tooltip?: string | null
    uiTag?: string
    onSelect?: (x: any) => any,
} & HTMLAttributes<any>
type PropsSection = {
    title?: string | null
    uiTag?: string
    children: string | JSX.Element | JSX.Element[]
}

const registryIndex = {
    Section: ["game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", "Section"],
    ToolButton: ["game-ui/game/components/tool-options/tool-button/tool-button.tsx", "ToolButton"],
    toolButtonTheme: ["game-ui/game/components/tool-options/tool-button/tool-button.module.scss", "classes"],
    StepToolButton: ["game-ui/game/components/tool-options/tool-button/tool-button.tsx", "StepToolButton"],
    mouseToolOptionsTheme: ["game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.module.scss", "classes"],
    FOCUS_DISABLED: ["game-ui/common/focus/focus-key.ts", "FOCUS_DISABLED"],
    ColorField: ["game-ui/common/input/color-picker/color-field/color-field.tsx", "ColorField"],
    Slider: ["game-ui/common/input/slider/slider.tsx", "Slider"],
}

export class VanillaResolver {
    public static get instance(): VanillaResolver { return this._instance!! }
    private static _instance?: VanillaResolver

    public static setRegistry(in_registry: ModuleRegistry) { this._instance = new VanillaResolver(in_registry); }
    private registryData: ModuleRegistry;

    constructor(in_registry: ModuleRegistry) {
        this.registryData = in_registry;
    }

    private cachedData: Partial<Record<keyof typeof registryIndex, any>> = {}

    private updateCache(entry: keyof typeof registryIndex) {
        const entryData = registryIndex[entry];
        return this.cachedData[entry] = this.registryData.registry.get(entryData[0])!![entryData[1]]
    }

    public get Section(): (props: PropsSection) => JSX.Element { return this.cachedData["Section"] ?? this.updateCache("Section") }
    public get ToolButton(): (props: PropsToolButton) => JSX.Element { return this.cachedData["ToolButton"] ?? this.updateCache("ToolButton") }
    public get toolButtonTheme(): Theme | any { return this.cachedData["toolButtonTheme"] ?? this.updateCache("toolButtonTheme") }
    public get mouseToolOptionsTheme(): Theme | any { return this.cachedData["mouseToolOptionsTheme"] ?? this.updateCache("mouseToolOptionsTheme") }
    public get FOCUS_DISABLED(): UniqueFocusKey { return this.cachedData["FOCUS_DISABLED"] ?? this.updateCache("FOCUS_DISABLED") }
    public get ColorField(): (props: PropsColorField) => JSX.Element { return this.cachedData["ColorField"] ?? this.updateCache("ColorField") }
    public get Slider(): (props: PropsSlider) => JSX.Element { return this.cachedData["Slider"] ?? this.updateCache("Slider") }
}
