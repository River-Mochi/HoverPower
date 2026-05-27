// File: UI/src/ModIconButton.tsx
// Purpose: Floating GameTopLeft launcher button for the Hover Power in-city panel.
//
// Rendering pattern — IDENTICAL to EasyZoning's ez-zone-tool-button.tsx and CityWatchdog's
// EntryButton.tsx: <Button variant="floating" src={iconPath} selected={isOpen} onSelect={...}>.
// The SVG is passed as the `src` prop so its own colors render straight (single color today,
// multi-color tomorrow if the SVG is swapped) and the vanilla floating-button styling owns the
// hover + selected (light-blue overlay when active) visuals automatically.
//
// Panel-open state lives in C# (Settings/Mod -> HoverPowerUISystem.s_PanelOpen) so the H hotkey
// and this button stay in sync — if the GTL render fails for any reason, pressing H still works
// (the panel reads PanelOpen too).

import React from "react";
import { Button, Tooltip } from "cs2/ui";
import { bindValue, trigger, useValue } from "cs2/api";
import { MochiColorPickerPanel } from "./MochiColorPickerPanel";
import styles from "./ModIconButton.module.scss";

// Webpack emits the file to coui://ui-mods/images/. The SVG content itself controls how the
// icon looks — when the file changes (e.g. add fill colors), no code change here is needed.
import ModIconPath from "../images/OutlineColorsActive.svg";

const CHANNEL = "HoverPower";
const panelOpen$ = bindValue<boolean>(CHANNEL, "PanelOpen", false);

export default () => {
    const isOpen = useValue(panelOpen$);

    return (
        <div className={styles.anchor}>
            <Tooltip tooltip="Hover Power">
                <Button
                    variant="floating"
                    src={ModIconPath}
                    selected={isOpen}
                    onSelect={() => trigger(CHANNEL, "SetPanelOpen", !isOpen)}
                />
            </Tooltip>

            {isOpen && <MochiColorPickerPanel />}
        </div>
    );
};
