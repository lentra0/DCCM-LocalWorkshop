# DCCM LocalWorkshop

Load local `.pak` mods in **Dead Cells** without going through the Steam Workshop.

LocalWorkshop is a mod for the [**Dead Cells Core Modding (DCCM)**](https://github.com/dead-cells-core-modding/core)
framework. It scans a folder on your disk, lists every mod it finds in a dedicated
in-game menu, and lets you enable or disable each one individually — including mods
you already downloaded from the Workshop.

---

## Features

* **Local `.pak` loading** — drop a mod folder on disk and it shows up in-game, no Steam required.
* **Works on any platform/store** — Steam, GOG, DRM-free; nothing depends on Steam Workshop being available.
* **In-game toggles** — enable/disable each mod from `Options → Core Modding → Local Workshop`.
* **CastleDB merging** — `.pak` files that alter `data.cdb` are merged into the game's database automatically.
* **Workshop-compatible layout** — folders named with a numeric Steam Workshop ID are picked up as-is, so you can point it at an existing Workshop directory.
* **Per-mod metadata** — optional `info.json` / `settings.json` gives each mod a title, description, author and more.

---

## Requirements

* Dead Cells
* [DCCM (Dead Cells Core Modding)](https://github.com/dead-cells-core-modding/core)

Built against DCCM core version **35.9.27**.

---

## Installation

1. Make sure DCCM is installed.
2. Download the latest release archive from the [Releases page](https://github.com/lentra0/DCCM-LocalWorkshop/releases).
3. Extract it into your game's `coremod/mods/` folder so the layout looks like this:

```text
Dead Cells/
└── coremod/
    └── mods/
        └── LocalWorkshop/
            ├── LocalWorkshop.dll
            └── modinfo.json
```

---

## Adding Mods

Each mod lives in its own subfolder inside `LocalWorkshop/`. LocalWorkshop picks up the
**first `.pak`** it finds in the folder (or the one named in metadata — see below).

```text
Dead Cells/
└── coremod/
    └── mods/
        └── LocalWorkshop/
            ├── LocalWorkshop.dll
            ├── modinfo.json
            │
            ├── BetterDrops/
            │   ├── res.pak
            │   ├── info.json       (optional)
            │   └── preview.png     (optional)
            │
            └── 1234567890/         (e.g. a Steam Workshop folder)
                └── res.pak
```

### Mod metadata (optional)

Add an `info.json` (or `settings.json`) next to the `.pak` to control how the mod is
displayed and loaded. Every field is optional:

```jsonc
{
  "title": "Better Drops",          // name shown in the menu
  "description": "Tweaks item drop rates",
  "author": "SomeModder",
  "category": "Gameplay",
  "enabledByDefault": false,         // load this mod even before it's toggled on
  "pak": "res.pak"                   // load a specific .pak instead of auto-detecting
}
```

If no metadata is present, the menu falls back to the folder name / a generated id.

### How mod ids work

* A folder named with **digits** (e.g. `1234567890`) is treated as that Steam Workshop id.
* Other names get a stable id derived from the folder name, so toggles persist across restarts.

A `preview.png` / `.jpg` / `.gif` in the folder is detected if present.

---

## Using the In-Game Menu

Open:

```text
Options → Core Modding → Local Workshop
```

The menu shows:

* how many mods are loaded and how many are enabled,
* an individual on/off toggle for each detected mod.

> **Note:** enabling a mod takes effect immediately. **Disabling** a mod only fully
> unloads it after a **game restart**.

Your choices are saved to:

```text
coremod/config/localworkshop.json
```

---

## Building from Source

Requirements:

* .NET 10 SDK
* [DCCM Modding Kit (MDK)](https://github.com/dead-cells-core-modding/core) — pulled in via NuGet (`DeadCellsCoreModding.MDK`)

```bash
git clone https://github.com/lentra0/DCCM-LocalWorkshop.git
cd DCCM-LocalWorkshop
dotnet build -c Release
```

Build output:

```text
bin/Release/net10.0/output/LocalWorkshop/
```

Copy that folder into your game's `coremod/mods/`.

---

## Links

* DCCM Core: https://github.com/dead-cells-core-modding/core

---

## Thanks

Huge thanks to the [**Dead Cells Core Modding (DCCM)**](https://github.com/dead-cells-core-modding/core)
project and its contributors. LocalWorkshop is built entirely on top of the DCCM
framework and Modding Kit — none of this would be possible without their work.

---

## License

Released under the [MIT License](LICENSE).
