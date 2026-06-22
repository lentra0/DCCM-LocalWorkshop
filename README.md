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
            │   ├── settings.json
            │   └── preview.png     (optional)
            │
            └── 1234567890/         (a Steam Workshop folder)
                ├── res.pak
                └── settings.json
```

### Mod metadata — `settings.json`

Every mod folder is described by a **`settings.json`** next to the `.pak`. Mods you bring over from
the Steam Workshop **always** ship one — Steam writes it — so they work as-is; just keep the file
next to the pak. For your own hand-made paks, add a `settings.json` too.

A Steam Workshop `settings.json` looks like this:

```json
{
  "name": "100x more Cells",
  "category": "Gameplay"
}
```

LocalWorkshop understands the following fields (Workshop files only use `name` / `category`; the
rest are available if you author your own):

| field | purpose |
|---|---|
| `name` / `title` | name shown in the menu (`title` wins if both are present) |
| `description` | sub-text shown under the toggle |
| `author` | shown when there is no description |
| `category` | shown when there is no description or author |
| `enabledByDefault` | load the mod the first time it is seen, before it is toggled on |
| `pak` | load a specific `.pak` in the folder instead of auto-detecting the first one |

> The file may also be named `info.json` if you prefer. Without it, the menu falls back to the
> folder name and a generated id.

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

## Documentation

Developer documentation lives in [`docs/`](docs/):

* [`docs/architecture.md`](docs/architecture.md) — how the mod works end to end: components,
  load lifecycle, and every engine hook / API it touches.

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
