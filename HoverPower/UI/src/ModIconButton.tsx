// File: UI/src/ModIconButton.tsx
// Purpose: Floating GameTopLeft launcher button for the Hover Power in-city panel.
//
// Rendering pattern — matches the OTHER documented branch of CityWatchdog's EntryButton comment:
//   "For a white/tintable icons use Monochrome/tinted path:
//      <Icon tinted={true} src={ModIconPath} /> inside the Button body"
//
// Why tinted (not Button src={...}): our SVG is a FILLED solid-white path (the icon area is
// the fill, the detail lines are negative-space cutouts). Rendered as an <img> via Button.src
// it shows as a near-square white blob. Tinted Icon uses the SVG as a CSS mask instead — the
// vanilla theme paints the mask shape in the correct color (white-on-dark normally, vanilla
// blue overlay when selected={true}) — same visual GTL as CWD/EasyZoning GTL.
//
// Panel-open state is the shared C# binding PanelOpen so the H hotkey + button click stay in sync.

import React from "react";
import { Button, Icon, Tooltip } from "cs2/ui";
import { bindValue, trigger, useValue } from "cs2/api";
import { MochiColorPickerPanel } from "./MochiColorPickerPanel";
import styles from "./ModIconButton.module.scss";

// Webpack emits this to coui://ui-mods/images/. We use the Active variant as the base icon
// (per user instruction). OutlineColors.svg stays in the images/ folder unused — harmless.
import ModIconPath from "../images/OutlineColors.svg";

const CHANNEL = "HoverPower";
const panelOpen$ = bindValue<boolean>(CHANNEL, "PanelOpen", false);

export default () => {
    const isOpen = useValue(panelOpen$);

    return (
        <div className={styles.anchor}>
            <Tooltip tooltip="Hover Power">
                <Button
                    variant="floating"
                    selected={isOpen}
                    onSelect={() => trigger(CHANNEL, "SetPanelOpen", !isOpen)}
                >
                    <Icon tinted={true} src={ModIconPath} />
                </Button>
            </Tooltip>

            {isOpen && <MochiColorPickerPanel />}
        </div>
    );
};
