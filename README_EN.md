<p align="center">
  <img alt="Ale Inventory System" src="./Packages/com.ale.inventory/Docs~/Images/InventorySystem_Logo_L.png" width="280">
</p>

<p align="center">
  <img alt="GitHub Release" src="https://img.shields.io/github/v/release/AleFeng/unity-ale-inventory-system?color=blue">
  <img alt="GitHub Downloads (all assets, all releases)" src="https://img.shields.io/github/downloads/AleFeng/unity-ale-inventory-system/total?color=green">
  <img alt="Unity Version" src="https://img.shields.io/badge/Unity-2022.3%2B-black?logo=unity">
  <img alt="Unity Version" src="https://img.shields.io/badge/Unity-6000.3%2B-black?logo=unity">
  <img alt="GitHub Repo License" src="https://img.shields.io/badge/license-MIT-blueviolet">
  <img alt="GitHub Repo Issues" src="https://img.shields.io/github/issues/AleFeng/unity-ale-inventory-system?color=yellow">
</p>

<p align="center">
  🌍
  <a href="./README.md">中文</a> |
  English |
  <a href="./README_JA.md">日本語</a>
</p>

<p align="center">
  📥
  <a href="#-installation">Installation</a> |
  <a href="#-quick-start">Quick Start</a> |
  <a href="Packages/com.ale.inventory/README.md">Full Docs</a>
</p>

# Ale Inventory System
Ale Inventory System is a **data-driven inventory plugin** for `Unity` that unifies six subsystems — **Item / Warehouse / Shop / Crafting / Equipment / Skill** — into a single toolchain.  
A single `InventoryDatabase` asset centralizes the **static definition data** of all six subsystems (enum types, function tags, item templates, warehouses, shops, blueprints, equipment groups, skills, and more), backed by a full set of **ready-to-use runtime UI** (inventory / shop / crafting / equipment / skill screens) and their respective **runtime managers** (owned counts, trade progress, crafted output, equipped items, learned skills, and save data are all maintained by the managers).  
It is **designed for designers**: the editor works exclusively on ScriptableObjects with full Undo / Redo, while `JSON` / binary serve only as **one-way export** formats. Text components (TextMeshPro), localization (Unity Localization), and asset loading (Addressables) are all **optional via compile-time macros**, so the package itself pulls in no hard dependencies.

![screenshot](./Packages/com.ale.inventory/Docs~/Images/image.png)

## 📜 Table of Contents
- [Ale Inventory System](#ale-inventory-system)
  - [📜 Table of Contents](#-table-of-contents)
  - [Introduction](#introduction)
    - [Features](#features)
    - [The Six Subsystems](#the-six-subsystems)
  - [💻 Requirements](#-requirements)
  - [📦 Installation](#-installation)
    - [Install via UPM (Recommended)](#install-via-upm-recommended)
    - [Import the Demo Sample (Optional)](#import-the-demo-sample-optional)
    - [Other Methods](#other-methods)
  - [🚀 Quick Start](#-quick-start)
    - [1. Create a Data File](#1-create-a-data-file)
    - [2. Open the Editor and Configure](#2-open-the-editor-and-configure)
    - [3. Export (Optional)](#3-export-optional)
    - [4. Runtime Setup](#4-runtime-setup)
    - [5. One-Click Demo](#5-one-click-demo)
  - [🖥️ Welcome Window](#️-welcome-window)
  - [🧩 Optional Feature Macros](#-optional-feature-macros)
  - [📖 Documentation](#-documentation)
  - [📁 Directory Structure](#-directory-structure)
  - [📋 Roadmap](#-roadmap)
  - [📄 License](#-license)

## Introduction
Almost every game needs an "items + inventory + shop + crafting + equipment + skills" data layer, yet these systems are usually scattered, tightly coupled, and expensive to reinvent each time. Ale Inventory System pulls them together under **one data asset** and **one editor**:

1. **Centralized configuration** — a single `InventoryDatabase` holds every static definition across all six subsystems. The editor uses a "top system tabs + three-column layout (definitions / entry list / detail Inspector)" and supports template filtering, search, drag-to-reorder, keyboard navigation, and live duplicate-ID checking.
2. **Flexible attributes** — the fields on items and every config entry are carried by a **flexible attribute system** (Bool / Int / Float / String / Text / Vector / Color / Enum / Sprite / Prefab / AudioClip / AnimationCurve… each also supports an array form). Fields can be added or removed in groups by function tag, letting you extend the data schema without touching code.
3. **Runtime out of the box** — each subsystem ships with a lightweight runtime manager and virtual-scrolling UI components; querying, adding/removing, sorting, trading, crafting, equipping, learning skills, and save/load all have ready-made APIs.
4. **Zero hard dependencies** — TextMeshPro / Localization / Addressables are all optional via compile-time macros; the plugin works fine with them turned off.

![screenshot](./Packages/com.ale.inventory/Docs~/Images/image-1.png)

### Features
| Feature | Description |
| --- | --- |
| Single-asset configuration | One `InventoryDatabase` centralizes all static data for the six subsystems; the editor works only on ScriptableObjects, with full Undo / Redo. |
| Flexible attribute system | 20+ field types (each with an array form): Bool / Int / Float / String / **Text** (plain-text fallback + optional localization reference) / Vector2~4 / VectorInt / Color / Enum / StringIntPair / EnumIntPair / Sprite / Texture / Prefab / Material / AudioClip / AnimationClip / AnimationCurve / PhysicsMaterial. |
| Custom enums + function tags | Enum values are auto-assigned by the system, never reused, and can be reordered by drag; a function tag defines a group of attribute fields, and adding/removing a tag adds/removes the item's corresponding fields — tags can be locked onto templates. |
| Six subsystems, unified | Item / Warehouse / Shop / Crafting / Equipment / Skill share the same data and attribute system, and entries cross-reference each other (e.g. skills attached to equippable items, shop prices sourced from item attributes). |
| Unified virtual-scroll UI | Both grid and ordered lists are virtual-scrolling (object pooling + only visible cells rendered); incremental diff refresh, spawn rate limiting (`spawnPerSecond`), and per-cell fade-in keep huge lists smooth. |
| Runtime managers | `InventoryDataManager` (queries) plus dedicated runtime managers for Warehouse / Shop / Crafting / Equipment / Skill — equipment & skill state and shop progress can all be saved. |
| One-way export | `InventoryDtoMapper` → JSON / binary, **covering every piece of database config** (all 20 lists across the six subsystems); object references are carried as AssetGUIDs and can be loaded asynchronously via Addressables. |
| Three optional macros | TextMeshPro (`IS_TMP`) / Unity Localization (`IS_LOCALIZATION`) / Unity Addressables (`IS_ADDRESSABLE`), each toggled from the Welcome Window (which also detects whether the corresponding package is installed); the package itself has zero hard dependencies. |
| Localization tooling | One click to generate / link localization tables for an `InventoryDatabase`, then walk every `Text` field in the database to auto-generate keys and write them back to entries (progress bar + log + cancel). |
| Welcome Window wizard | A single entry point: create data, open the editor / tool windows, toggle macros, and "generate a complete runnable sample in one click" (database + all UI prefabs + managers). |

### The Six Subsystems
| Subsystem | What you configure | Runtime manager |
| --- | --- | --- |
| **Item** | Enum types, function tags, item templates, items + flexible attributes | `InventoryDataManager` (queries) |
| **Warehouse** | Warehouse templates, warehouses, capacity / weight / tag limits, sorting | `InventoryRuntimeManager` (slot state + save) |
| **Shop** | Shop templates, shops, product groups, price sources, refresh schedules | `ShopRuntimeManager` (trades + progress save) |
| **Crafting** | Group tags, blueprint templates, blueprints (recipes), crafting warehouses | `CraftingRuntimeManager` (consume → produce) |
| **Equipment** | Group tags, equipment-group templates, equipment groups (slot lists / slots / item limits / attribute bonuses) | `EquipmentRuntimeManager` (equip / unequip + bonuses + save) |
| **Skill** | Group tags, skill templates, skills (type / effect / values / tier carried by custom attributes) | `SkillRuntimeManager` (learned state + save) + `SkillCollector` (four-source collection) |

> See the [full documentation](#-documentation) for each subsystem's complete configuration and runtime details.

## 💻 Requirements
- `Unity 2022.3` or newer (the minimum declared in `package.json`; this repository is developed and maintained on `Unity 6000.3`).
- The core plugin is pure C# and **introduces no hard dependencies** — TextMeshPro / Unity Localization / Unity Addressables are all **optional** via compile-time macros (see [Optional Feature Macros](#-optional-feature-macros)).
- With `IS_TMP` disabled, UI text components fall back to `UnityEngine.UI.Text` and the plugin works as usual.

## 📦 Installation
### Install via UPM (Recommended)
`Window > Package Manager` → the `+` in the top-left → `Install package from git URL...` → paste:

```
https://github.com/AleFeng/unity-ale-inventory-system.git?path=/Packages/com.ale.inventory
```

This installs the latest commit on `main`. **To pin a version, append `#<tag>` to the very end of the URL** (it must come after `?path=`):

```
https://github.com/AleFeng/unity-ale-inventory-system.git?path=/Packages/com.ale.inventory#1.6.0
```

See [Releases](https://github.com/AleFeng/unity-ale-inventory-system/releases) for available tags.

### Import the Demo Sample (Optional)
After installing, select the package in Package Manager → `Samples` → import **Inventory System Demo** (the `InventoryDatabase` asset + manager prefab + a UI sample scene) and press Play right away. Alternatively, use the "one-click generate" wizard in the [Welcome Window](#️-welcome-window) to build a full sample on the spot.

### Other Methods
You can also download the repository and copy the entire `Packages/com.ale.inventory` folder into your project's **`Packages/` directory** (not `Assets/`) — Unity will recognize it as a local package automatically.

Once installed, a **`Tools → Inventory System`** menu appears, and the **Welcome Window** pops up automatically the first time you open the project in a Unity session.

## 🚀 Quick Start
Below is the shortest path to get going; **complete subsystem configuration and API notes are in the [full documentation](#-documentation)**.

### 1. Create a Data File
```
Right-click in the Project panel > Create > Inventory System > Inventory Database
```
(Or click "Create New Data File" in the Welcome Window; you can set a "Data Template" there first to deep-copy all data from it when creating.)

### 2. Open the Editor and Configure
- Select the `.asset` and click "Edit in Inventory Editor" at the top of the Inspector; or use the menu `Tools > Inventory System > Inventory Editor`.
- The editor uses **top system tabs + a three-column layout** (left: definitions / middle: entry list / right: detail Inspector). Configure each of the "Item / Warehouse / Shop / Crafting / Equipment / Skill" tabs in turn. The middle entry list supports template filtering, search, drag-to-reorder, and ↑ / ↓ keyboard navigation.

### 3. Export (Optional)
Use the toolbar's "Export JSON" or "Export Binary" (the buttons are disabled while any non-empty duplicate ID exists; entries with a blank ID are skipped on export). The editor always works on ScriptableObjects, with export being a one-way format.

> As of **1.6.0 (format v6)** the export covers **all** of the database's config data — all 20 lists across the six subsystems. Earlier versions exported only the four Item System lists and silently dropped the rest.

### 4. Runtime Setup
Create a GameObject in your scene, add the `InventoryRuntimeManager` component, and drag the `.asset` into the `databases` array. On game start the database is registered automatically and each warehouse is initialized to an empty state.

```csharp
using Ale.Inventory.Runtime;

// Query static data
Item item = InventoryDataManager.Instance.GetItem("sword_01");

// Manipulate a warehouse at runtime
InventoryRuntimeManager.Instance.TryAddItem("backpack", "sword_01", 1);
bool has = InventoryRuntimeManager.Instance.HasItem("backpack", "sword_01");

// Save / load (LoadSaveData replaces: inventories absent from the save return to their initial empty state)
var saveData = InventoryRuntimeManager.Instance.GetSaveData();
InventoryRuntimeManager.Instance.LoadSaveData(saveData);

// New game: clear all runtime state
InventoryRuntimeManager.Instance.ResetAll();
```

### 5. One-Click Demo
In the **Welcome Window**, expand "Test Tools – Prefab Generation → Generate All" to produce a complete runnable sample in one click (database + all UI prefabs + inventory / shop / crafting / equipment / skill screens + managers).

## 🖥️ Welcome Window
The plugin's unified entry panel, gathering common actions such as "create data / open editors / view docs / generate samples / toggle feature macros". It pops up automatically the first time each Unity session, and can be opened manually at any time:

```
Tools > Inventory System > Welcome Window
```

![screenshot](./Packages/com.ale.inventory/Docs~/Images/image-1.png)

Top to bottom, the window is divided into four areas: **Quick Actions** (create data / open the various editor and tool windows / generate sample prefabs in one click), **Data Template** (pick an `InventoryDatabase` as the blueprint for new files), **Plugin Support** (one-click toggles for the three optional macros, below), and **Show on Startup** (whether to auto-open the window each session).

## 🧩 Optional Feature Macros
All three macros can be toggled from the "Plugin Support" area of the **Welcome Window**, which also detects in real time whether the corresponding package is installed (checking a macro whose package is missing pops up a confirmation dialog):

| Toggle | Macro | Effect |
| --- | --- | --- |
| TextMeshPro | `IS_TMP` | When on, UI text components use `TMP_Text`; otherwise `UnityEngine.UI.Text`. A "default font" can be configured and applied to wizard-generated prefabs. |
| Unity Localization | `IS_LOCALIZATION` | When on, `Text` fields can carry a localization reference (table + entry); combined with the "Localization Tool Window" for one-click table creation / key generation, enabling multi-language support. |
| Unity Addressables | `IS_ADDRESSABLE` | When on, runtime assets are loaded asynchronously on demand via Addressables with reference-counted auto-unloading; referenced assets are registered automatically on export. |

> After toggling a macro, wait for Unity to recompile for it to take effect.

## 📖 Documentation
This README is an overview and quick start. The **complete usage guide** — each subsystem's configuration details, runtime APIs, flexible-attribute reference, UI components, and architecture notes — lives in the in-package docs:

👉 **[Packages/com.ale.inventory/README.md](Packages/com.ale.inventory/README.md)**

Subsystem and reference docs (under `Packages/com.ale.inventory/Docs~/`):

- [Item System](Packages/com.ale.inventory/Docs~/ItemSystem_EN.md) — enum types / function tags / item templates / items / flexible attributes
- [Warehouse System](Packages/com.ale.inventory/Docs~/WarehouseSystem_EN.md) — warehouse templates / warehouses / sorting / runtime API / save data
- [Shop System](Packages/com.ale.inventory/Docs~/ShopSystem_EN.md) — shop types / price sources / product groups / refresh schedules / trade API
- [Crafting System](Packages/com.ale.inventory/Docs~/CraftingSystem_EN.md) — group tags / blueprint templates / blueprint recipes / crafting warehouses / crafting API
- [Equipment System](Packages/com.ale.inventory/Docs~/EquipmentSystem_EN.md) — group tags / equipment-group templates / slot lists / slots / item limits / attribute bonuses / equip API
- [Skill System](Packages/com.ale.inventory/Docs~/SkillSystem_EN.md) — group tags / skill templates / skills / item skill references / tier enums / four sources / learned-skill API
- [Attribute System](Packages/com.ale.inventory/Docs~/AttributeSystem_EN.md) — field-type reference, `AttributeValue` retrieval / display / sort comparison
- [UI Component Guide](Packages/com.ale.inventory/Docs~/UIComponentGuide_EN.md) — UI components, prefab authoring, feature macros, demo wizard
- [Architecture](Packages/com.ale.inventory/Docs~/Architecture_EN.md) — design goals, data flow, editor & runtime architecture, extension guide

## 📁 Directory Structure
```
Packages/com.ale.inventory/          ← package root
├── package.json  CHANGELOG.md  LICENSE.md  README.md   ← detailed usage docs
├── Runtime/
│   ├── Data/            Data models (Item / Inventory / Shop / Crafting* / AttributeValue, etc.)
│   ├── Manager/         DataManager / Warehouse / Shop / Crafting / Equipment / Skill runtime managers + SkillCollector
│   ├── Serialization/   DTO + JSON / binary serialization
│   ├── Assets/          Asset-loading abstraction (direct loading)
│   ├── Addressables/    Addressables asset-loading support
│   ├── Localization/    TMP text / font localization events
│   └── UI/              Runtime UI components (Item / ItemList / Tab / Tool / View / Common)
├── Editor/
│   ├── ItemSystem/ InventorySystem/ ShopSystem/ CraftingSystem/ EquipmentSystem/ SkillSystem/   ← the six system panels
│   ├── Common/         Shared attribute / config drawers + tool-window base class
│   ├── Addressables/   Addressables asset-reference migration tool window
│   ├── Localization/   Localization tool window (table creation / key generation)
│   ├── Create/         Data-file creation menu
│   └── DemoWizard/     One-click generation of test data and prefabs
├── Docs~/              Detailed documentation
└── Samples~/Demo/      Demo sample (database + manager prefab + UI sample scene)
```

## 📋 Roadmap
- A complete implementation of the shop "barter/exchange" type (currently a placeholder).
- More sample scenes and runtime use cases.

## 📄 License
This project is open-source under the [MIT License](LICENSE) and free for commercial and non-commercial use.
