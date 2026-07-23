# Warehouse System

<p align="center">
  🌍
  <a href="./WarehouseSystem.md">中文</a> |
  English |
  <a href="./WarehouseSystem_JA.md">日本語</a>
</p>

- Back to [documentation](../README_EN.md)

The Warehouse System defines "containers": capacity, weight limit, put-in / take-out / operate function-tag restrictions, filter tags, and sorting rules. At runtime, `InventoryRuntimeManager` maintains each warehouse's slot list and provides add/remove / query / sort / save APIs. Backpacks, equipment bars, shop shelves, and crafting material stores are all backed by warehouses.

# 📜 Table of Contents

- [Warehouse System](#warehouse-system)
- [📜 Table of Contents](#-table-of-contents)
- [Tab Structure](#tab-structure)
- [Warehouse Templates (Left Column)](#warehouse-templates-left-column)
- [Warehouse List (Middle Column)](#warehouse-list-middle-column)
- [Warehouse Inspector (Right Column)](#warehouse-inspector-right-column)
- [Sorting](#sorting)
- [Runtime Setup](#runtime-setup)
    - [Cover-UI Setup (Popups / Ghost Icon)](#cover-ui-setup-popups--ghost-icon)
    - [Editor Test-Item Population (Auto-Fill on Play)](#editor-test-item-population-auto-fill-on-play)
- [Runtime API](#runtime-api)
- [RuntimeItemSlot Structure](#runtimeitemslot-structure)
- [Save and Load](#save-and-load)
- [Data Sources and Loading](#data-sources-and-loading)
- [Inventory UI](#inventory-ui)

# Tab Structure

Click the "**Warehouse System**" tab at the top of the Inventory Editor. Three-column layout:

```
Left column: warehouse templates (list + edit panel)
Middle column: warehouse list (template filter tags + search + drag-to-reorder)
Right column: Inspector of the selected warehouse
```

# Warehouse Templates (Left Column)

A warehouse template defines a warehouse's default configuration; warehouses created from it inherit these settings (which can be overridden at the warehouse level).

| Field | Description |
|------|------|
| Name | The template's unique name |
| Color | The color dot in the warehouse list |
| Capacity limit | Max number of slots (0 = unlimited) |
| Weight limit | Max total weight (0 = unlimited) |
| **Put-in function tags** | Only items carrying these tags may be put in; empty = no restriction |
| **Take-out function tags** | Only items carrying these tags may be taken out; empty = no restriction |
| **Operate function tags** | Restricts the operations allowed on items in the warehouse; empty = no restriction |
| **Filter tags** | The function-tag filter buttons in the warehouse UI ("All" always exists and needs no configuration) |
| Auto sort | When checked, automatically sorts by the sort rules on every item change. **Where the direction comes from**: if the view has a sort toolbar (`UiwSortToolbar`), its current direction applies to every rule; without a toolbar each rule uses its own ascending / descending setting (fixed in 1.6.0 — previously everything was forced to descending when there was no toolbar) |
| Drag sort | When checked, allows the player to drag-reorder items in the UI |
| **Sort list** | The primary sort rules; each entry picks a sort field + ascending/descending |
| **Sort priority** | The secondary sort rules; compared in turn when the primary sort values are equal |
| Attribute field list | The warehouse's custom attribute fields (notes, section descriptions, etc.) |

# Warehouse List (Middle Column)

```
[ All ][ Backpack ][ Shop ][ Equipment Bar ]  ← warehouse-template filter tag bar
[ 🔍 search box ][ Add from Template ▾ ][ Quick Add ]
─────────────────────────────────────
≡ ● template name | ID | capacity | weight | put-in | take-out | operate  ✕
```

| Action | Description |
|------|------|
| Template filter tag bar | Filters by warehouse template |
| Search box | Filters by warehouse ID / template name |
| Add from Template | Creates a new warehouse inheriting the template config (ID auto `inv_N`) |
| Quick Add | Clones the last warehouse |
| Drag ≡ handle | Adjusts the warehouse's order in the database |
| Click row | Selects it; the right column shows the Inspector |

# Warehouse Inspector (Right Column)

| Field | Description |
|------|------|
| ID | Unique identifier; highlighted when empty or duplicate |
| Source template | Read-only |
| Capacity limit / Weight limit | Override the template values (0 = unlimited) |
| Put-in / Take-out / Operate / Filter function tags | Multi-select checkboxes, override the template values |
| Auto sort / Drag sort | Toggle |
| Sort list / Sort priority | Drag-reorderable lists of sort rules |
| Attribute field values | Custom attribute values from the warehouse template definition |

# Sorting

Sorting consists of two levels: the primary sort "sort list" and the secondary sort "sort priority"; when primary sort values are equal, the secondary sorts are compared in turn until a tie is broken.

Available sort fields:

| Field | Meaning |
|------|------|
| Item ID (`__id__`) | Sort by item ID |
| Tag order (`__tagOrder__`) | Sort by the position of the item's first function tag in the tag list |
| Any custom attribute field | Sort by that attribute value |

**Custom attributes are compared by different rules per type** (see [Attribute System – Sort Comparison Value](AttributeSystem_EN.md#sort-comparison-value-tocomparablenumber)):

- Int / Float / Bool / Enum → compare the numeric value directly;
- Vector2~4 / Color / VectorInt2~4 → compare magnitude;
- StringIntPair → compare only its Int value;
- String → special handling: by length first, then lexicographically.

Underlying implementation: `InventorySortService.CompareSlots` → `CompareByField` → `GetAttrNumeric` (→ `AttributeValue.ToComparableNumber()`).
(As of 1.6.0 the same-named `public static` compatibility forwards on `InventoryRuntimeManager` have been removed — call `InventorySortService` directly.)

> Each "sort option" carries two built-in fields: **name** (`displayName`, Text: plain-text fallback + optional localization reference, read as the display name in the sort dropdown) and **ignore IDs** (`ignoreIds`, a list of entry IDs skipped during sorting, 0 by default, drag-add/removable; the semantics depend on the field — sort by item ID = item IDs, function tab = tag names, sort by attribute = attribute values). Edit them in the "Warehouse System → Sort Options" sub-tab; at runtime they're read via `SortOption.ResolveDisplayName` / `SortOption.EffectiveIgnoreIds`. Older versions stored these two as generic "attribute field definition" values; they are auto-migrated to the built-in fields the first time you open that panel.

# Runtime Setup

Create an empty GameObject in the scene, attach `InventoryRuntimeManager`, and drag the `.asset` into the `databases` array. On startup it automatically registers the database into `InventoryDataManager` and creates empty runtime state for every defined warehouse.

```
Hierarchy
└── [InventoryManager]
      └── InventoryRuntimeManager
            databases: [GameDatabase.asset]
```

### Cover-UI Setup (Popups / Ghost Icon)

The "UI Settings" area of `InventoryRuntimeManager` configures **cover UI** (hover popups, dropdown popups, drag ghost icons, and anything else that must sit above all other UI):

- **Root node** (`coverUiRoot`): the parent node for cover UI; if empty, at runtime it automatically uses the first Canvas in the scene.
- **Force Layer** (`applyCoverUiLayer` + `[Layer] coverUiLayer`): when enabled, cover UI is recursively set to the specified Layer (e.g. `UI`) after instantiation. This suits scenes with a "separate UI camera whose Culling Mask renders only the UI layer" — such UI gets its own Canvas, breaking the parent Layer, so it must be re-specified for the UI camera to render it. You can also adjust it in code with `SetCoverUiLayer(int)` / `SetCoverUiLayer(string)` / `DisableCoverUiLayer()`, and apply it manually to self-built cover UI with `ApplyCoverUiLayer(GameObject)`.

### Editor Test-Item Population (Auto-Fill on Play)

The "Test Features" area of `InventoryRuntimeManager` can automatically fill items into a test warehouse when entering Play mode (`Init()`, at Awake time), **filling data only, opening no UI** (screens are opened by each view itself):

- **`autoPopulateOnStart`** (master toggle): whether to auto-fill.
- **`testInventoryId`**: the target warehouse ID (must match an `Inventory.id` in the database).
- **`testItems`**: a list specifying "item ID + count" entry by entry.
- **`addAllConfiguredItems` + `addAllItemCount`**: additionally tops up the test warehouse with every item configured across all databases (`databases`), each at the `addAllItemCount` quantity; items already configured in `testItems` are skipped (their specified quantity is kept, not added again), and the same item ID across multiple databases is added only once. This is also gated by the master toggle `autoPopulateOnStart`.

> This area can be auto-filled with example values by the Demo wizard (`Tools > Inventory System`).

# Runtime API

```csharp
using Ale.Inventory.Runtime;

var rm = InventoryRuntimeManager.Instance;

// Add (returns false = capacity/weight/tag restriction not satisfied)
bool ok = rm.TryAddItem("backpack", "potion_hp", 3);

// Queries
int  total = rm.GetTotalCount("backpack", "potion_hp");   // accumulated across slots
bool has   = rm.HasItem("backpack", "potion_hp", 1);
int  free  = rm.GetFreeSpaceFor("backpack", "potion_hp"); // how much more can be added
float w    = rm.GetTotalWeight("backpack");
float wMax = rm.GetWeightLimit("backpack");

// Get the slot list (order = UI display order)
// Note: the return value is read-only by contract — on a hit it is a live reference to the runtime
// state; on a miss (unknown inventory ID) it is a globally shared empty list. Copy it before
// sorting / filtering.
List<RuntimeItemSlot> slots = rm.GetSlots("backpack");

// Remove: by slot ID (exact) / by item ID (accumulated deduction across slots)
rm.TryRemoveItem("backpack", slots[0].slotId, 1);
rm.TryRemoveItemById("backpack", "potion_hp", 2);

// Swap the contents of two slots (drag sort)
rm.SwapSlotContents("backpack", slotA, slotB);

// Drag drop point (same item stacks first; if it won't fit / different item / empty slot, swaps) — used by UI grid drag sorting
rm.StackOrSwapSlots("backpack", srcSlot, targetSlot);

// Sort (writes runtime state per the warehouse-defined rules)
rm.SortInventory("backpack");

// Listen for warehouse changes (UI refreshes accordingly)
rm.OnInventoryChanged += id => RefreshUI(id);
```

> Note the method is named `GetTotalCount` (not `GetTotalQuantity`).

# RuntimeItemSlot Structure

| Field | Type | Description |
|------|------|------|
| `slotId` | string | The slot's unique ID (`Guid.NewGuid()`) |
| `itemId` | string | The item ID |
| `quantity` | int | The current count in this slot |

# Save and Load

`InventoryRuntimeManager` provides interfaces to integrate with the game's save system:

```csharp
// Save: take a deep copy of all warehouse state (serializable)
List<RuntimeInventoryState> save = InventoryRuntimeManager.Instance.GetSaveData();
string json = JsonUtility.ToJson(new SaveWrapper { inventories = save });

// Load: after deserializing, call once Init has completed
var wrapper = JsonUtility.FromJson<SaveWrapper>(json);
InventoryRuntimeManager.Instance.LoadSaveData(wrapper.inventories);

// New game: clear every warehouse and rebuild the initial empty state
InventoryRuntimeManager.Instance.ResetAll();
```

`RuntimeInventoryState` = `inventoryId` + `slots` (ordered slot list).

> **`LoadSaveData` replaces rather than merges (since 1.6.0)**: it first clears the in-memory state and
> rebuilds an empty skeleton for every warehouse in the database (fixed-capacity warehouses get their
> pre-allocated empty slots back), then overlays the saved slots. So —
> warehouses **in the database but not in the save** return to their initial empty state (last session's
> contents do not survive); warehouse IDs **in the save but not in the database** are still loaded into
> memory (no definition found, treated as unlimited capacity) rather than discarded.
> The contract matches the other three runtime managers — see
> [Architecture – Save contract](Architecture_EN.md#subsystem-runtime-managers).

# Data Sources and Loading

`InventoryDataManager` supports three data sources (editor export → runtime load):

| Source | Use |
|------|------|
| `.asset` (ScriptableObject) | Drag directly into `InventoryRuntimeManager.databases`; simplest during development |
| JSON | Readable text; object references carried as AssetGUIDs, good for debugging |
| Binary | Compact and efficient, good for release |

Export via the toolbar's "Export JSON / Export Binary". At runtime it can be loaded into `InventoryDataManager` and registered. For serialization and asset-resolution details, see [Architecture – Data Flow](Architecture_EN.md#data-flow).

# Inventory UI

`UiwInventoryView` (`Runtime/UI/View/Inventory/`) is the backpack main-screen controller, composing: multi-warehouse tabs, a currency bar, a virtual-scroll list, a filter tab bar, and a sort bar.

```csharp
using Ale.Inventory.Runtime.UI;

inventoryView.Open(new[] { "backpack", "stash" });  // open and show these warehouses
inventoryView.Close();
```

> `_inventoryIds` can be preset in `UiwInventoryView`'s Inspector as the default warehouse list to show: the parameterless `Open()` opens with that value, while `Open(inventoryIds)` overrides it before opening — once set, the view always uses that value until changed via `Open(...)` or the Inspector.

For prefab authoring, each component's Inspector parameters, and virtual-list configuration, see the [UI Component Guide](UIComponentGuide_EN.md).
