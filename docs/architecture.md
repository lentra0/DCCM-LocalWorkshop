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
| `src/CdbRowOrder.cs` | Fixes up the merged CastleDB JSON: restores original row order, drops `__STRUCTURE__` rows (3.4). |
| `src/LocalWorkshopMenu.cs` | The in-game menu (`IModMenu`): status line + one toggle per mod. |

---

## 3. Engine hooks & APIs used

### 3.1 (removed) `ModManager.onSteamInit`
Earlier versions mounted paks from a hook on `dc.tool.mod.ModManager.onSteamInit`. Game v35
(2026-06-16) no longer has `dc.tool.mod`, so the hook was removed; mounting moved to 3.2.

### 3.2 `IOnAfterLoadingAssets` — mount entry point
* **Symbol:** DCCM event interface (`ModCore.Events.Interfaces.IOnAfterLoadingAssets`).
* **Source:** broadcast by the DCCM core's `FsPak` module right after `dc.Boot.initRes`
  (`dc.Boot.init` is **HL #6118**; the core hooks `initRes` and broadcasts the event).
* **Why:** at this point the pak file system exists and it is safe to add more paks.
  `LocalWorkshopMod` implements the interface and calls `GameModBridge.ApplyEnabledDiffPaks()`,
  which mounts every enabled local pak (already-mounted paks are skipped) and merges CastleDB.

### 3.3 `FileSystem.loadPak` — mounting a pak
* **Symbol:** `dc.hxd.fmt.pak.FileSystem.loadPak(file)` · **HL #6375**.
* **Accessed via:** `ModCore.Modules.FsPak.Instance.FileSystem`.
* **Behaviour:** merges the pak's entries into the live VFS. When two paks provide the same path,
  the **last one loaded wins**.
* Related (not used by this version, but present on the same type): `loadModPak` **#6376**,
  `canLoadModPak` **#6377**, `unloadModPak` **#6378**.

### 3.4 `CDBManager.GetAlteredCDB` + `LoadJsonData` — applying data changes
* **Symbols:** DCCM core module `ModCore.Modules.CDBManager` (`CDBManager.Instance`):
  `GetAlteredCDB()` and `LoadJsonData(json)`.
* **Flow:** paks carry CastleDB changes as a `data.cdb_/<sheet>/<row>.json` tree. `GetAlteredCDB()`
  collects the diffs from every mounted pak and returns the merged CastleDB JSON (with
  `UseGameCDBManager` enabled in the core config it delegates to the game's own CDB manager);
  `LoadJsonData(...)` loads that JSON into the live game database via `dc.Data.loadFrom`.
  `GameModBridge.MergeCdb()` performs this pair and fixes up the JSON in between (see below).
* **Row order fix-up (`src/CdbRowOrder.cs`):** the core (35.14.1) merges row files in pak order
  (alphabetical) and, for each one, removes the matching row and **appends it to the end of its
  separator group**. It also merges each sheet's `__STRUCTURE__.json` as if it were a row. Some
  sheets are read by position — e.g. `difficulty`, indexed by the number of active boss cells —
  so a mod that changes all six difficulty rows would leave them as Expert, Hard, Hell, …, and
  0 BC would play as Expert. Before `LoadJsonData` the bridge therefore, for every sheet that has
  `__original_Index` rows or structure rows:
  1. splits `lines` into separator segments using the `separators` start-index array;
  2. drops structure rows (only `__`-prefixed keys, including `__columns`);
  3. puts each row with `__original_Index` back at that index **within its segment** (that is
     what the field means; `__separator_group_Name` picks the segment) — other rows fill the
     remaining slots in their current order;
  4. rebuilds `separators` from the new segment sizes, so group-derived data (e.g. `affix`
     groups) keeps the same boundaries;
  5. removes the helper keys `__original_Index`, `__separator_group_ID`, `__separator_group_Name`
     (unless the sheet declares a column with that name).

  Sheets without such rows are left untouched. If the fix-up throws, the merge is loaded as the
  core produced it and a warning is logged.

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
  └─ Scan()                         → LocalWorkshopRepository.Scan(modRoot)

dc.Boot.initRes runs → core broadcasts IOnAfterLoadingAssets (3.2)
  └─ LocalWorkshopMod.OnAfterLoadingAssets()
       ├─ Scan() (idempotent)
       └─ GameModBridge.ApplyEnabledDiffPaks()
            ├─ for each enabled mod: FileSystem.loadPak(pak)   (3.3)
            └─ MergeCdb()                                       (3.4)
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
