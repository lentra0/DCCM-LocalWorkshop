# LocalWorkshop — Architecture & Internals

This document explains how LocalWorkshop works under the hood, the DCCM/engine hooks and APIs it
relies on, and the load lifecycle. It is aimed at developers reading or extending the mod.

> Engine symbols are given as `namespace.Type.method` together with their Hashlink function index
> (`HL #`) so they can be cross-referenced against the generated `GameProxy` / HookDocs.

---

## 1. Overview

LocalWorkshop is a DCCM mod that loads other mods' `.pak` files from a folder on disk and exposes
them as toggleable entries in the in-game mod menu. A loaded pak's resources are mounted into the
game's virtual file system, and any CastleDB changes it carries (`data.cdb_/…`) are merged into the
live game data.

It does **not** create fake Steam UGC items or touch the vanilla Workshop list; it loads paks
directly through the engine's file system and re-merges CastleDB.

---

## 2. Components

| File | Responsibility |
|---|---|
| `src/LocalWorkshopMod.cs` | Entry point (`ModBase`). Wires everything up; scans the folder; provides the menu. |
| `src/LocalWorkshopRepository.cs` | Scans the mod root, builds a `LocalWorkshopItem` per subfolder (id, pak, preview, metadata). |
| `src/LocalWorkshopItem.cs` | `LocalWorkshopItem` (resolved mod) and `LocalWorkshopInfo` (parsed `settings.json`). |
| `src/LocalWorkshopConfig.cs` | Loads/saves enabled mod ids to `coremod/config/localworkshop.json`. |
| `src/GameModBridge.cs` | Mounts enabled paks into the game and merges CastleDB. The only component that talks to the engine's file system. |
| `src/LocalWorkshopMenu.cs` | The in-game menu (`IModMenu`): status line + one toggle per mod. |

---

## 3. Engine hooks & APIs used

### 3.1 `ModManager.onSteamInit` — mount entry point
* **Symbol:** `dc.tool.mod.ModManager.onSteamInit(self)` · **HL #6357** · hooked via `Hook_ModManager.onSteamInit`.
* **Why:** this fires after the game's own mod/Steam initialization, at a point where the pak file
  system exists and it is safe to add more paks. `GameModBridge.Install()` subscribes to it, calls
  the original, then mounts every enabled local pak.

### 3.2 `IOnAfterLoadingAssets` — CastleDB re-merge
* **Symbol:** DCCM event interface (`ModCore.Events.Interfaces.IOnAfterLoadingAssets`).
* **Source:** broadcast by the DCCM core's `FsPak` module right after `dc.Boot.initRes`
  (`dc.Boot.init` is **HL #6118**; the core hooks `initRes` and broadcasts the event).
* **Why:** whenever the engine (re)loads assets, the merged CastleDB must be re-applied or local
  data changes would be lost. `LocalWorkshopMod` implements the interface and calls
  `GameModBridge.ReMergeIfMounted()`.

### 3.3 `FileSystem.loadPak` — mounting a pak
* **Symbol:** `dc.hxd.fmt.pak.FileSystem.loadPak(file)` · **HL #6375**.
* **Accessed via:** `ModCore.Modules.FsPak.Instance.FileSystem`.
* **Behaviour:** merges the pak's entries into the live VFS. When two paks provide the same path,
  the **last one loaded wins**.
* Related (not used by this version, but present on the same type): `loadModPak` **#6376**,
  `canLoadModPak` **#6377**, `unloadModPak` **#6378**.

### 3.4 `CDBManager.getAlteredCDB` + `Data.loadJson` — applying data changes
* **Symbols:** `dc.tool.mod.CDBManager.getAlteredCDB()` · **HL #31804** (via `CDBManager.Class.instance`),
  and `dc.Data.loadJson(json, ref)`.
* **Flow:** paks carry CastleDB changes as a `data.cdb_/<sheet>/<row>.json` tree. The engine's
  `CDBManager` collects the diffs from every mounted pak; `getAlteredCDB()` returns the merged
  CastleDB JSON; `Data.loadJson(...)` loads that JSON into the live game database. `GameModBridge.MergeCdb()`
  performs this pair.

### 3.5 Menu API
* **Symbols:** `ModCore.Menu.IModMenuProvider` / `IModMenu`; `dc.ui.Options` widgets
  `createScroller`, `scrollerFlow`, `addSeparator`, `addToggleWidget`.
* `LocalWorkshopMod` implements `IModMenuProvider`; `LocalWorkshopMenu` builds the section under
  *Options → Core Modding → Local Workshop*.

---

## 4. Load lifecycle

```
DCCM core boots
  └─ FsPak loads the base game res.pak, sets up the VFS

LocalWorkshopMod.Initialize()
  ├─ GameModBridge.Install()        → subscribe to ModManager.onSteamInit (3.1)
  └─ Scan()                         → LocalWorkshopRepository.Scan(modRoot)

ModManager.onSteamInit fires
  └─ GameModBridge.ApplyEnabledDiffPaks()
       ├─ for each enabled mod: FileSystem.loadPak(pak)   (3.3)
       └─ MergeCdb()                                       (3.4)

dc.Boot.initRes runs → core broadcasts IOnAfterLoadingAssets
  └─ LocalWorkshopMod.OnAfterLoadingAssets()
       ├─ Scan() (idempotent)
       └─ GameModBridge.ReMergeIfMounted()  → MergeCdb()  (3.4)
```

---

## 5. The mod repository (scanning)

`LocalWorkshopRepository.Scan(root)` walks every immediate subfolder of the LocalWorkshop mod
folder and produces one `LocalWorkshopItem` each:

* **pak resolution** — the `pak` field of `settings.json` if set and present, otherwise the first
  `*.pak` in the folder (alphabetical). A folder with no pak is skipped.
* **metadata** — `settings.json` (or `info.json`) is parsed into `LocalWorkshopInfo`. JSON is read
  case-insensitively, allowing comments and trailing commas. Missing/invalid metadata falls back to
  defaults.
* **id derivation** (`ParseFolderId`):
  1. a fully numeric folder name → that number (a Steam Workshop published-file id);
  2. otherwise leading digits, if any;
  3. otherwise a stable FNV-1a hash of the folder name.
  This keeps a mod's id (and therefore its enabled/disabled state) stable across restarts.
* **preview** — the first of `preview.png/.jpg/.jpeg/.gif`, if present.

---

## 6. Configuration

* **File:** `coremod/config/localworkshop.json`.
* **Format:** a JSON array of enabled mod ids, e.g. `[1850789726, 7421233019283746]`.
* A mod counts as enabled when its id is in that array **or** its `settings.json` sets
  `enabledByDefault: true`.

---

## 7. Known limitations (this version)

* **Disabling needs a restart.** Paks are mounted with `loadPak`, which the engine cannot cleanly
  unmount mid-session, so turning a mod off only fully takes effect on the next launch.
* **No load-order / conflict control.** When two enabled paks provide the same file, the engine's
  "last loaded wins" rule decides, and the load order is not user-controllable here.

---

## 8. Appendix — building a compatible data mod

Local mods are ordinary Dead Cells `.pak` files. A data mod stores its CastleDB changes as a
`data.cdb_/<sheet>/<row>.json` tree inside the pak. The DCCM tool can produce such a pak by diffing
an edited `data.cdb` against the base game database:

```bash
# produces a res.pak containing only the changed rows under data.cdb_/
DCCMTool cdb diff -t <base>/data.cdb -i <edited>/data.cdb -o res.pak
```

Drop the resulting `res.pak` (plus a `settings.json`) into a folder under
`coremod/mods/LocalWorkshop/` and it will be picked up on the next scan.
