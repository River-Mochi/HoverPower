import { ModRegistrar } from "cs2/modding";
import { VanillaComponentResolver } from "./utils/vanilla/VanillaComponentResolver";

import ModIconButton from "./ModIconButton";

const register: ModRegistrar = (moduleRegistry) => {
    VanillaComponentResolver.setRegistry(moduleRegistry);

    moduleRegistry.append(
        "GameTopLeft",
        ModIconButton
    );
};

export default register;
