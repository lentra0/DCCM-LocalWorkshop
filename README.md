# DCCM LocalWorkshop

Load local `.pak` mods in **Dead Cells** without Steam Workshop.

Created using the **Dead Cells Core Modding (DCCM)** framework.

## Features

* Load `.pak` mods from a local folder
* Works with non-Steam versions of the game (GOG, DRM-free, etc.)
* Enable or disable mods directly from the in-game menu
* Supports CastleDB (`data.cdb`) modifications
* Compatible with existing Workshop mod folders

---

## Requirements

* Dead Cells
* DCCM (Dead Cells Core Modding)

Built against DCCM core version **35.9.27**.

---

## Installation

Download the latest release and extract it into:

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

Place each mod in its own folder inside `LocalWorkshop`.

Example structure:

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
            │   └── preview.png
            │
            └── CustomWeapons/
                ├── res.pak
                └── settings.json
```

---

## Using the In-Game Menu

Open:

```text
Options → Core Modding → Local Workshop
```

The menu displays:

* Number of loaded mods
* Number of enabled mods
* Individual toggle for each detected mod

Changes are saved to:

```text
coremod/config/localworkshop.json
```

---

## Building from Source

Requirements:

* .NET 10 SDK
* DCCM Modding Kit (MDK)

```bash
git clone https://github.com/lentra0/DCCM-LocalWorkshop.git
cd DCCM-LocalWorkshop
dotnet build -c Release
```

Build output:

```text
bin/Release/net10.0/output/LocalWorkshop/
```

Copy the generated folder into:

```text
coremod/mods/
```

---

## Links

* DCCM Core: https://github.com/dead-cells-core-modding/core

## License

MIT License
