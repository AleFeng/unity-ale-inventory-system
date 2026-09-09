# Inventory System

<p align="center">
  🌍
  <a href="./README.md">中文</a> |
  English |
  <a href="./README_JA.md">日本語</a>
</p>

A Unity plugin providing designer-facing static-data configuration tooling. A single `InventoryDatabase` asset centralizes the static definition data of five subsystems — **Item / Warehouse / Shop / Crafting / Equipment**; the dynamic runtime state (owned counts, instance IDs, trade progress, crafted output, equipped items, save data) is maintained by the corresponding runtime managers. A full set of ready-to-use runtime UI components (inventory / shop / crafting / equipment screens) is included.

- The editor always and only works on ScriptableObjects; JSON / binary are used solely as one-way export formats.
- Full Undo / Redo support throughout.
- Text components, localization, and Addressables are all optionally enabled via compile-time macros.

---

## Subsystem Overview

![alt text](Docs~/Images/image.png)

| Subsystem | What you configure | Runtime manager | Docs |
|--------|---------|------------|---------|
| **Item System** | Enum types, function tags, item templates, items + flexible attributes | `InventoryDataManager` (queries) | [Item System](Docs~/ItemSystem_EN.md) |
| **Warehouse System** | Warehouse templates, warehouses, capacity/weight/tag limits, sorting | `InventoryRuntimeManager` (slot state + save) | [Warehouse System](Docs~/WarehouseSystem_EN.md) |
| **Shop System** | Shop templates, shops, product groups, price sources, refresh schedules | `ShopRuntimeManager` (trades + progress save) | [Shop System](Docs~/ShopSystem_EN.md) |
| **Crafting System** | Group tags, blueprint templates, blueprints (recipes), crafting warehouses | `CraftingRuntimeManager` (consume → produce) | [Crafting System](Docs~/CraftingSystem_EN.md) |
| **Equipment System** | Group tags, equipment-group templates, equipment groups (slot lists / slots / item limits / attribute bonuses) | `EquipmentRuntimeManager` (equip / unequip + bonuses + save) | [Equipment System](Docs~/EquipmentSystem_EN.md) |
| **Effect System** (`1.12.0`; effects moved out in `1.13.0`) | Effects and gameplay tags live in the toolkit **shared effect library `EffectDatabase`** (configured in the Effect Editor); this database keeps only the items' `onUseEffectRefs` (effect-id references + jump) | `InventoryRuntimeManager.UseItem` (applies to a caller-supplied target context and consumes the item); effects resolve by id through toolkit `EffectDataManager` / the global registry | See "Effect System" below |

### Item System
- **Flexible attribute system**: field types support Bool / Int / Float / String / Text (plain-text fallback + optional localization reference) / Vector2~4 / VectorInt2~4 / Color / Enum / StringIntPair / EnumIntPair / Sprite / Texture / Prefab / Material / AudioClip / AnimationClip / AnimationCurve / PhysicsMaterial(2D), each also supporting an array form.
- **Custom enum types**: enum values are auto-assigned by the system (monotonically increasing, never reused); display order can be reordered by drag; enum items can carry custom attribute fields.
- **Function tags**: each tag defines a group of attribute fields; adding/removing a tag on an item automatically adds/removes the corresponding fields; tags can be locked onto item templates.
- **Item templates / item list / item Inspector**: templates act as blueprints for creation; the list supports a template-filter tag bar + search + drag-to-reorder; the Inspector does live duplicate-ID checking, groups attributes by source, and auto-expands enum sub-attributes.
- **Effects on use** (`1.12.0`): `onUseEffectRefs` on items / templates references effect ids of the toolkit effect library in order (since `1.13.0`; "+" picks from the catalog, "Open" jumps to the Effect Editor; effect ids of other systems work too); `UseItem` applies them at runtime and consumes the item.

### Warehouse System
- **Warehouse templates / warehouse instances**: a template defines capacity, weight limit, put-in/take-out/operate function-tag restrictions, filter tags, sorting rules, and custom attributes; an instance is created from a template and can override it.
- **Sorting**: a primary sort ("sort list") + a secondary sort ("sort priority"); sort fields can be item ID / tag order / any custom attribute; attributes are compared by different rules depending on `EFieldType` (numbers compared directly, vectors by magnitude, StringIntPair by its Int value). Each sort field maps to a "sort option" with a built-in "name" (`Text`: the display name in the sort dropdown) and "ignore IDs" (a list of entry IDs skipped during sorting, 0 by default).
- **Runtime**: `InventoryRuntimeManager` manages each warehouse's slot list and provides add/remove/query/sort/save APIs plus an `OnInventoryChanged` event.

### Shop System
- **Shop types**: sell / buy-back / barter (barter is a placeholder).
- **Price sources**: prices are not hard-coded — they are read from an item's `StringIntPair` (currency ID → price) attribute, designated by the "price attribute source", then multiplied by the product's price multiplier; multiple currencies are supported.
- **Trade warehouses**: each shop is configured with a set of warehouses used to tally currency, receive purchases, source buy-backs, and write change.
- **Product groups and refresh**: products are grouped (tabs); each group / each product can have a refresh schedule (none / daily / weekly / monthly × game / local / server time + time point / time zone), periodically resetting the "tradeable count".

### Crafting System
- **Group tags / blueprint templates / blueprints**: group tags are used for UI grouping and filtering (each blueprint has 1 primary + multiple secondary); a template defines custom attributes + config default values + a template-level sort, acting as a blueprint for creating blueprints; a blueprint holds a recipe (output / consumed item lists).
- **Crafting warehouses**: an ordered list of warehouse IDs, used by priority as material sources and output destinations.
- **Runtime**: `CraftingRuntimeManager` computes how many times a recipe can be crafted, deducts materials across crafting warehouses, and places output; the count, timing, and progress of continuous crafting are driven by the UI layer.

### Equipment System
- **Group tags / equipment-group templates / equipment groups**: group tags are used to group the total attribute-bonus fields for display; a template carries a full set of configurable options (slot list + equipment attribute fields) + custom attribute fields, acting as a blueprint for creating equipment groups (deep-copied on creation, independently editable afterward); an equipment group defines the complete slot structure.
- **Slot lists / equipment slots / item limits**: an equipment group contains multiple slot lists, each containing multiple equipment slots; a slot list restricts equippable items by "function tag + enum constraint", and an equipment slot further narrows by "filter conditions" (attribute equality). Matching uses **all-AND**.
- **Attribute bonuses**: the "equipment attribute field list" specifies which item attributes are summed into the equipment group's total bonuses, displayed grouped by group tag.
- **Runtime**: `EquipmentRuntimeManager` maintains the equipped items per slot; equipping / unequipping / swapping cooperates with `InventoryRuntimeManager` to move items, and it provides auto slot-finding, bonus aggregation, save data, and an `OnEquipmentChanged` event.

### Effect System (`1.12.0`; moved to the toolkit shared effect library in `1.13.0`)
- **Where effects live**: since `1.13.0` effect entries (display name / description / icon + template-driven custom attributes + toolkit `EffectDefinition` — GAS-style: duration policy / period / stacking / tags / application condition / chance / modifiers / phased executions / cue tags) and gameplay tags are stored in the toolkit shared effect library **`EffectDatabase`**, configured in one place in the **Effect Editor** (`Tools > Ale Toolkit > Effect System > Effect Editor`) and referenced by id from every upper system (Inventory / Chronicle …); at runtime `EffectDataManager.Instance.Register(effectDatabase)` (or an asset under `Resources`, auto-registered on startup) joins the global effect registry. The inventory database **no longer** holds effects / tags — data from 1.12.0 assets lands in hidden legacy fields, see "Migration" below.
- **References**: `onUseEffectRefs` on items / templates; the inspector uses the toolkit `EditorEffectRefListDrawer`: "+" picks from the catalog of every effect library in the project (grouped by database), drag to reorder, "Open" jumps to the Effect Editor and locates the effect, unknown ids are only marked (non-blocking), ids can be typed freely.
- **Using items**: `InventoryRuntimeManager.UseItem(inventoryId, itemId, targetContext)` / `UseItemInSlot(inventoryId, slotId, targetContext)` — the target context (toolkit `IEffectContext`, subject = effect target) is built by the game layer (e.g. `ChronicleEffectContext.Create(characterId)` of the character system); definitions resolve through the context's definition source first, then the global `EffectDefinitionRegistry.Default` (the toolkit effect library is registered as a source through `EffectDataManager`; this database no longer holds definitions, and effects of other systems are referenced by id the same way); the item is consumed only when at least one effect applies; returns `ItemUseResult` (`Used / Blocked / NoEffects / NotOwned / UnknownItem / NoContext`, consumed flag, per-effect results) and raises `OnItemUsed(ItemUseEvent)`. **This package knows nothing about any domain system**: attributes / traits etc. are landed by the other system through the toolkit effect contracts.
- **Validation / migration**: this database no longer validates effects / tags (the effect library validates itself in the Effect Editor); item effect references are not checked for dangling (the editor marks them "not found"). **Migration (1.12.0 → 1.13.0)**: `Tools > Ale Toolkit > Inventory System > Migrate Effects to Effect Database` (the asset inspector offers the same entry when it detects legacy data) — `InventoryLegacyEffects.MigrateInto(sourceInventoryDb, targetEffectDb)` moves entries one by one (definition deep-copied, display name taken from `displayName`, no template) and clears the legacy fields; id conflicts are skipped and reported, never overwritten (re-run after resolving).

### Runtime and Serialization
- **`InventoryDataManager`** (data-query singleton): registers databases, queries items / warehouses / shops / blueprints / enum types / effects, etc. by ID; since `1.13.0` it no longer registers as a toolkit effect-definition source nor merges gameplay tags (effects resolve through toolkit `EffectDataManager` / the global registry); supports loading from `.asset`, JSON, and binary sources. Lookups go through a lazily built dictionary index (O(1)), invalidated and rebuilt when databases are registered / unregistered.
- **`InventoryRuntimeManager`** (MonoBehaviour singleton): warehouse slot state, sorting, save data, the time-injection entry point, the cover-UI root node / Layer config (popups / hover popups / drag ghost icons, etc., are re-assigned the specified Layer after instantiation), and registers databases into `InventoryDataManager`; includes editor test-item population (`autoPopulateOnStart` / `testInventoryId` / `testItems`, filled at `Init` time, data-only, no UI opened) and a one-click "add all configured items" (`addAllConfiguredItems` + `addAllItemCount`).
- **`ShopRuntimeManager` / `CraftingRuntimeManager` / `EquipmentRuntimeManager`** (lightweight singletons): trade / craft / equip logic (equipped state can be saved, and shops have trade-progress save data).
- **Export**: `InventoryDtoMapper` → JSON / binary, **covering all 17 database lists** (nothing from the five subsystems is left out; format version v9: effects / gameplay tags moved to the toolkit effect library and are no longer written (exported separately by `EffectConfigSerializer`), the item / template blocks keep `onUseEffectRefs`; v8 files load their effects / tags into the legacy fields for migration); object references are carried as AssetGUIDs; optional async loading via Addressables. `.bytes` exported by v5 ~ v7 still imports.
- **Save contract**: the inventory / equipment / shop managers all implement `IInventorySaveable<TState>` — `GetSaveData` returns a deep copy, `LoadSaveData` **replaces rather than merges**, and none of them fires a change event; the non-generic `IInventorySaveable` carries only `ResetAll`, so "new game" can reset every system in one loop.

### UI Components
Located under `Runtime/UI/`, assembly `Ale.Inventory.Runtime.UI`, namespace `Ale.Inventory.Runtime.UI`. Provides the inventory / shop / crafting / equipment main screens plus reusable common components such as a currency bar, filter bar, sort bar, hover popup, number counter, and collapsible tabs. Each main screen derives from `UiwViewBase`: the parameterless `Open()` is a base-class template method (activates the panel), which subclasses override to implement their own open logic; the inventory / equipment / shop views expose their target IDs (`inventoryIds` / `groupId` / `shopId`) to the Inspector so defaults can be preset.

- **Unified virtual-scroll list**: every list that "displays a large number of entries / items" is built on the same virtual-scroll engine (base `UiwVirtualListBase<TData,TCell>` → generic `UiwVirtualGridList` / `UiwVirtualOrderList` → each system's leaf). **Both grid and ordered lists are virtual-scrolling**: only the visible region + buffer is rendered, with scroll-loop reuse; the grid supports vertical / horizontal scrolling, with cross-axis counts computed automatically from the viewport; the warehouse grid still supports drag-to-reorder under virtual scrolling. Adding a list for a new system only requires inheriting the generic grid / ordered layer and overriding "bind / clear cell".
- **List performance and experience** (built into the engine):
  - **Incremental diff refresh** — on content change, only the visible cells whose data changed are rebound (drag-swap / stacking is usually just 2 cells); icons don't flicker and scroll position is preserved.
  - **Spawn / assignment rate limiting** (`spawnPerSecond`, default 30/sec) — amortizes instantiation and binding across multiple frames to avoid single-frame spikes or asset-loading congestion (with a budget cap to prevent an "opening-frame" burst).
  - **Per-cell staggered reveal following scroll direction** — cells appear in the order they enter the viewport (top-down when scrolling down, bottom-up when scrolling up).
  - **Assign / recycle fade in-out** — a cell fades its whole root `CanvasGroup` in when assigned (scrolled in) and out before being cleared / returned when recycled (scrolled out); item cells additionally fade their icon / quality-background in per-image once the sprite (possibly loaded asynchronously via Addressables) is ready. Driven generically by the toolkit `UiwListFadeCell` + `UiwVirtualListBase` default hooks (via the `IUiwRecycleFadeCell` / `IUiwDiffCell` interfaces), so every system's list has it.
- **Item right-click menu** (`1.14.0`): right-clicking any item cell (grid / detail row / equipment candidate) opens a
  cursor-anchored menu — **View** (a closable item-detail popup reusing `UiwInventoryItemDetail`) / **Use** (shown only when
  the item has a non-empty `onUseEffectRefs`; consumes 1 via `UseItemInSlot`) / **Discard** (a slider over `[1, stack count]`,
  then `TryRemoveItem` on that exact slot). Upper systems contribute entries through
  `UiwInventoryItemEvents.CollectingItemMenu` — the equipment view injects "Equip", folding the former "right-click to
  quick-equip" into the menu (the `ItemRightClicked` event is kept and now raised by that entry, so external subscribers are
  unaffected). The menu `UiwContextMenu` and the modal-popup base `UiwModalPopupBase` are generic parts of toolkit `1.13.0`.
- **Reusable building blocks**: the tab strip `UiwTabStrip`, child-item pool `UiwWidgetPool`, hover-tooltip bases
  `UiwTooltipBase` / `UiwHoverTooltipSource`, icon slot `SpriteSlot` and number / price formatter `UIFormat` — prefer
  reusing these when extending the UI; see
  [UI Component Guide – Reusable Building Blocks](Docs~/UIComponentGuide_EN.md#106-reusable-building-blocks-prefer-these-when-extending-the-ui).
- See the [UI Component Guide](Docs~/UIComponentGuide_EN.md) for details.

---

## Documentation

- [Attribute System](Docs~/AttributeSystem_EN.md) — field-type reference, `AttributeValue` retrieval / display / sort comparison
- [UI Component Guide](Docs~/UIComponentGuide_EN.md) — UI components, prefab authoring, feature macros, demo wizard
- [Architecture](Docs~/Architecture_EN.md) — design goals, data flow, editor & runtime architecture, extension guide

---

## Welcome Window

![alt text](Docs~/Images/image-1.png)

The plugin's unified entry panel, gathering common inventory-domain actions such as "create data / open editors / view docs / generate samples". It pops up automatically the first time each Unity session, and can be opened manually at any time:

```
Tools > Ale Toolkit > Inventory System > Welcome Window
```

> **Editor UI language, enum translation, and the optional feature macros (`ATK_TMP` / `ATK_LOCALIZATION` / `ATK_ADDRESSABLE`) are all project-level global settings; since 1.10.0 they live in the [Ale Toolkit Welcome Window](../com.ale.toolkit) (`Tools > Ale Toolkit > Welcome`).** This window offers an "Open Ale Toolkit Settings (Language / Macros)" button at the top for quick access.

Top to bottom: a **header** (title / version) + the "Open Ale Toolkit Settings" button + inventory-domain areas.

### Editor Language & Macros (global → Ale Toolkit)

UI language switching (中 / English / 日本語), the "Enum Values" translation toggle, and the three optional feature-macro toggles are all configured in the **Ale Toolkit Welcome Window** (`Tools > Ale Toolkit > Welcome`) — click the "Open Ale Toolkit Settings" button in this window to jump there. The language choice is persisted via `EditorPrefs` and kept across sessions; switching it refreshes the `Inventory Editor` (all five subsystem panels and config drawers). This affects **editor UI text only**, and is unrelated to runtime content localization (`ATK_LOCALIZATION` / Unity Localization). See the [Ale Toolkit docs](../com.ale.toolkit).

### Quick Actions

| Button | Description |
|------|------|
| Create New Data File | Creates a new `InventoryDatabase` asset (deep-copied from a "Data Template" if one is configured below) |
| Open Inventory Editor | Opens the main configuration editor window |
| Open Localization Tool Window | (When `ATK_LOCALIZATION` is enabled) Opens the toolkit's generic localization window to generate / link tables and keys for this library |
| View Documentation | Opens this README with the system default application |

Expand the "**Test Tools – Prefab Generation**" foldout:

- **Generate All (Database + All Prefabs)**: one click to generate a complete runnable sample (database + all UI prefabs + inventory / shop / crafting screens + managers).
- The list below lets you **generate individual prefabs**; when generating a dependent prefab it asks whether to generate child prefabs as well, and confirms before overwriting an existing asset.

### Data Template

Once you designate an `InventoryDatabase` as a template, "Create New Data File" deep-copies all its data (enums / tags / templates / items…); leaving it empty creates default empty data. The panel shows the number of enum types / function tags / item templates / items the template contains.

> Since 1.10.0 this template choice is stored in the project-level `ProjectSettings/AleInventorySettings.asset` (a `ScriptableSingleton`, committed with the repo, referenced by GUID, shared across the team; a setting saved by an older version in EditorPrefs is migrated in automatically the first time you open the window). Per-user preferences such as "auto-show on startup" remain in EditorPrefs.

### Wizard Fonts (when `ATK_TMP` is enabled)

Font settings used by the "Test Tools – Prefab Generation" wizard (inventory-domain config, hence kept in this window):

- **Default font**: applied to all TMP text nodes when the wizard generates prefabs (leave empty to use the TMP default font).
- **Localization font** (when `ATK_LOCALIZATION` is also enabled): assigned to `LocalizedFontEvent` when the wizard generates prefabs.

> The three optional feature-macro toggles themselves have moved to the Ale Toolkit Welcome Window — see "Editor Language & Macros" above. After toggling a macro, wait for Unity to recompile for it to take effect.

### Show on Startup

The "Show on Startup" toggle at the bottom of the window controls whether this window auto-opens each Unity session.

---

## Dependencies

> ⚠️ **Since 1.8.0 this plugin depends on [`com.ale.toolkit`](../com.ale.toolkit).** Unity's Package Manager does not support git-URL entries in `package.json` `dependencies`, so `dependencies` is left empty — you **must install `com.ale.toolkit` first, then this plugin**, otherwise you will get many "type not found" compile errors.

- **`com.ale.toolkit` (required, install first; 1.13.0 or newer since `1.14.0` — context menu `UiwContextMenu` / modal-popup base `UiwModalPopupBase`)** — the shared foundation this plugin builds on (attribute system, virtual-scroll lists, the three-column editor framework, trilingual editor UI, sorting engine, tag system, `Ale.Effect` effect system, `Ale.GameplayTags` hierarchical tags, `Ale.Condition` condition system, etc.).
- Unity 2022.3+ (the minimum declared in `package.json`; this plugin is developed and maintained on `Unity 6000.3`)
- TextMeshPro (optional, `ATK_TMP` macro)
- Unity Localization (optional, `ATK_LOCALIZATION` macro)
- Unity Addressables (optional, `ATK_ADDRESSABLE` macro)

> All three macros are toggled with one click in the "Plugin Support (Defines)" area of the **Ale Toolkit Welcome Window** (`Tools > Ale Toolkit > Welcome`), which also detects whether the corresponding package is installed; the inventory Welcome Window provides a jump button.

---

## Quick Start

### 1. Create a Data File

```
Right-click in the Project panel > Create > Inventory System > Inventory Database
```

(Or click "Create New Data File" in the Welcome Window; you can configure a "Data Template" there to deep-copy from when creating.)

### 2. Open the Editor

- Select the `.asset` and click "Edit in Inventory Editor" at the top of the Inspector; or
- Menu `Tools > Ale Toolkit > Inventory System > Inventory Editor`.

The editor is top system tabs + a three-column layout (left: definitions / middle: entry list / right: detail Inspector). The middle entry list uses a "column header + value" two-row layout, supporting template filtering / search, drag-to-reorder, and ↑ / ↓ keyboard navigation to switch selection after selecting (auto-scrolling when out of view).

### 3. Configure Data

Configure each of the "Item System / Warehouse System / Shop System / Crafting System / Equipment System" tabs in turn. See the corresponding subsystem docs for detailed operations. Effects / gameplay tags are configured in the toolkit **Effect Editor** (`Tools > Ale Toolkit > Effect System > Effect Editor`); the effect references in the item inspector jump there with "Open".

### 4. Export

Toolbar "Export JSON" or "Export Binary" (the buttons are disabled while a non-empty duplicate ID exists; entries with a blank ID are skipped on export).

### 5. Runtime Setup

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

// New game: clear all runtime state (fixed-capacity inventories get their pre-allocated empty slots back)
InventoryRuntimeManager.Instance.ResetAll();

// Use an item (1.12.0): apply its effects to a game-layer target context (toolkit IEffectContext); consumes 1 on success
var result = InventoryRuntimeManager.Instance.UseItem("backpack", "regen_draught", targetContext);
if (result.IsUsed) Debug.Log($"{result.AppliedCount} effect(s) applied, consumed={result.Consumed}");
```

### 6. One-Click Demo

In the **Welcome Window**, "Test Tools – Prefab Generation → Generate All" produces a complete runnable sample in one click (database + all UI prefabs + inventory / shop / crafting screens + managers). See [Welcome Window](#welcome-window) and the [UI Component Guide](Docs~/UIComponentGuide_EN.md).

---

## Directory Structure

```
InventorySystem/
├── Runtime/
│   ├── Data/           Data models (Item / Inventory / Shop / Crafting* / AttributeValue, etc.)
│   ├── Manager/        InventoryDataManager / InventoryRuntimeManager (incl. Use partial) / ShopRuntimeManager / CraftingRuntimeManager / EquipmentRuntimeManager
│   ├── Serialization/  DTO definitions + mapping / JSON / binary (mapping and binary blocks split per system)
│   ├── Assets/         Asset-loading abstraction (direct loading)
│   ├── Addressables/   Addressables asset-loading support
│   ├── Localization/   TMP text / font localization events
│   └── UI/             Runtime UI components (Item / ItemList / Tab / Tool / View / Common)
├── Editor/
│   ├── ItemSystem/     Item System panel
│   ├── InventorySystem/Warehouse System panel
│   ├── ShopSystem/     Shop System panel
│   ├── CraftingSystem/ Crafting System panel
│   ├── EquipmentSystem/Equipment System panel
│   ├── Migration/      legacy effect migration window (effects stored in a 1.12.0 database → toolkit effect library)
│   ├── Common/         Shared attribute / config drawers + tool-window base class
│   ├── Addressables/   Addressables asset-reference migration tool window
│   ├── Localization/   Localization tool window (table creation / key generation)
│   ├── Create/         Data-file creation menu
│   └── DemoWizard/     One-click generation of test data and prefabs
├── Resources/Data/     Sample data files
└── Docs~/              Detailed docs (this folder)
```
