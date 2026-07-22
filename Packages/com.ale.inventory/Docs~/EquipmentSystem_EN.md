# Equipment System

<p align="center">
  🌍
  <a href="./EquipmentSystem.md">中文</a> |
  English |
  <a href="./EquipmentSystem_JA.md">日本語</a>
</p>

- Back to [documentation](../README_EN.md)

The Equipment System lets the player equip items into the slots of an "equipment group" to gain attribute bonuses. An equipment group defines a full slot structure (multiple **slot lists**, each containing multiple **equipment slots**), and constrains which items each slot may hold via "item limits" (function tag / enum constraints) and "slot filter conditions"; the "equipment attribute field list" specifies which item attributes are summed into the equipment group's total attribute bonuses. Equipment groups are a config catalog; the equip / unequip / swap actions and bonus aggregation at runtime are executed by `EquipmentRuntimeManager`.

# 📜 Table of Contents

- [Equipment System](#equipment-system)
- [📜 Table of Contents](#-table-of-contents)
- [Core Concepts](#core-concepts)
- [Tab Structure](#tab-structure)
- [Group Tags](#group-tags)
- [Equipment-Group Templates](#equipment-group-templates)
- [Equipment Groups (Middle Column)](#equipment-groups-middle-column)
- [Equipment-Group Inspector (Right Column)](#equipment-group-inspector-right-column)
- [Equipment Warehouses](#equipment-warehouses)
- [Slot Lists and Item Limits](#slot-lists-and-item-limits)
- [Equipment Slots and Filter Conditions](#equipment-slots-and-filter-conditions)
- [Equipment Attribute Field List](#equipment-attribute-field-list)
- [Limit Matching Rules](#limit-matching-rules)
- [Runtime API](#runtime-api)
- [Equipment UI](#equipment-ui)
  - [Interaction](#interaction)

# Core Concepts

| Concept | Description |
|------|------|
| Group tag (`EquipmentGroupTag`) | Groups the **equipment attribute field entries** for easy grouped display of total attribute bonuses in the UI (e.g. item level / primary attributes / secondary attributes). Carries only basic info, no attribute fields |
| Equipment-group template (`EquipmentGroupTemplate`) | A blueprint for creating equipment groups, carrying a full set of configurable options (slot lists + equipment attribute fields) + custom attribute field definitions, also used for category filtering |
| Equipment group (`EquipmentGroup`) | The complete slot structure of a set of equipment: multiple slot lists (each with equipment slots + item limits) + an equipment attribute field list + custom attribute values |
| Slot list (`EquipmentSlotList`) | A group of equipment slots + a unified "item limit" (function tags + enum constraints) |
| Equipment slot (`EquipmentSlot`) | A slot that can equip one item, with a "slot filter condition" to narrow it further |

# Tab Structure

Click the "**Equipment System**" tab at the top of the Inventory Editor. A three-column layout, symmetric with the Crafting / Shop systems:

```
Left column: sub-tabs [Group Tags / Equipment-Group Templates] + list + edit panel
Middle column: equipment-group list
Right column: context Inspector (group tag / equipment-group template / equipment group)
```

The left column's sub-tabs switch between "Group Tags" and "Equipment-Group Templates"; selecting a left-column entry shows that entry's edit panel in the right column, while selecting a middle-column equipment group shows the equipment-group Inspector.

# Group Tags

Carry only basic info, and **do not carry attribute fields**. Used to group the "equipment attribute field" entries of an equipment group for display in the UI.

| Field | Description |
|------|------|
| ID | Unique identifier (equipment attribute field entries reference it by this ID) |
| Name | `Text` (plain-text fallback + optional localization reference; falls back to ID when empty) |
| Description | `Text` (plain-text fallback + optional localization reference) |
| Color | List color dot |

# Equipment-Group Templates

**Carries the default values of a full set of equipment-group configurable options** (identical to an equipment group: slot lists + equipment attribute field list), plus custom attribute field definitions (schema), and is used for category filtering.

| Field | Description |
|------|------|
| Name / Color | Template name + list color dot |
| Equipment warehouses | See [Equipment Warehouses](#equipment-warehouses) |
| Slot lists | See [Slot Lists and Item Limits](#slot-lists-and-item-limits) |
| Equipment attribute field list | See [Equipment Attribute Field List](#equipment-attribute-field-list) |
| Sorting | Sort criteria + sort priority, see below |
| Custom attribute fields | The attribute fields defined by the template (equipment groups reconcile their attribute values accordingly) |

> An equipment group and a template share `IEquipmentConfig` (equipment warehouses + slot lists + equipment attribute fields + sorting), and the editor reuses the same drawer (`EquipmentConfigDrawer`).
>
> **When creating an equipment group from a template, all of the template's configurable options are deep-copied** (equipment warehouses + slot lists + equipment attribute fields + sorting) as initial data; afterward the equipment group is **independently editable** (unlike the Crafting System's "template-level read-only").
>
> **Sorting**: both the template and the equipment group can configure "sorting" (sort criteria + sort priority), used for the display order of the **"equippable item candidate list" at the bottom of the equipment selection panel** (`UiwEquipmentCandidateList`) — when the candidate list has a sort bar, the player can pick and toggle ascending/descending; otherwise the first entry is the default sort. Criteria copied from the template into the equipment group can then be edited independently (the candidate list reads the equipment group's own config).

# Equipment Groups (Middle Column)

The middle column is the equipment-group list (filtered by the selected left-column template). "Add from Template" creates one from a template (deep-copying its config + reconciling custom attribute default values), with an auto-generated ID; "Quick Add" clones the last entry. Click a row to select, the right column shows the equipment-group Inspector, and "Delete Equipment Group" is at the top of the right column.

# Equipment-Group Inspector (Right Column)

| Field | Description |
|------|------|
| ID | Unique identifier (a duplicate raises an error during export validation) |
| Name / Description | `Text` (plain-text fallback + optional localization reference; falls back to ID / base description when the name is empty) |
| Source template | Read-only, determines the custom attribute fields |
| Equipment warehouses | The warehouses the equipment system / equipment UI can interact with, see [Equipment Warehouses](#equipment-warehouses) |
| Slot lists | Nested editing (item limits + equipment slots + slot filter conditions), see below |
| Equipment attribute field list | The total attribute-bonus fields (attribute field ID + group tag), see below |
| Custom attribute values | Custom attribute values from the template definition |

# Equipment Warehouses

"Equipment warehouses" (`equipmentInventoryRefs`, a list of string warehouse IDs) specifies the player warehouses the equipment system / equipment UI can **directly interact with**, and is the **sole warehouse source** for the equipment screen (the equipment UI doesn't need to pass in a warehouse ID). **Order is priority**: each entry is numbered in the Inspector, and the left drag handle adjusts order (sharing `InventoryRefListDrawer` with the shop's "trade warehouses" and crafting's "crafting warehouses").

> **Two levels (template / equipment group) + runtime fallback**: both the template and the equipment group can configure equipment warehouses; creating an equipment group from a template deep-copies the template's equipment warehouses. At runtime it takes the "**effective equipment warehouses**" — using the equipment group's own list if non-empty, and **automatically falling back to the source template's equipment warehouses** when empty (`EquipmentRuntimeManager.GetEquipmentInventories`). So configuring only at the template level, or an equipment group created before this field was added (its own list empty), still works at runtime.

- **Candidate item source**: the equippable item list at the bottom of the equipment selection panel **aggregates matching items across all equipment warehouses**; on equip, the item is taken from the warehouse it resides in.
- **Unequip**: **tries each warehouse from Index0**, placing it into the first one that "has room" (free space ≥ 1); if none has room, it **does not unequip** (returns false, avoiding item loss).
- **Backpack bridging**: when the backpack's right-click auto-equip is used, it only handles items from warehouses in the equipment warehouses (when no equipment warehouses are configured, the source is unrestricted).

# Slot Lists and Item Limits

An equipment group usually contains multiple slot lists (e.g. "Weapon", "Armor", "Accessory"). Each slot list entry is collapsed by default, showing only the ID + a "detailed config" foldout; expand it to edit:

- **Name / Description**.
- **Item limit → function tags**: choose from the function tags defined in the Item System to restrict the items this list can equip; multiple can be added (the left drag handle reorders them).
- **Item limit → enum constraint**: first pick an "enum type", then multi-select "enum values"; multiple can be added (the left drag handle reorders them). An empty enum-value set means "any value".
- **Equipment slots**: all equipment slots of this list (see below).

Each slot list entry has a drag handle on the left to reorder slot lists as a whole.

# Equipment Slots and Filter Conditions

Each slot list contains multiple equipment slots. Equipment slot config:

| Field | Description |
|------|------|
| ID | The slot's stable identifier (at runtime the equipped item is located and saved by it) |
| Name | The UI display name (optional; can serve as a placeholder for an empty slot) |
| Slot filter condition | Narrows the items this slot can hold further, on top of the slot list's limits |

**Slot filter condition** (`EquipmentSlotFilter`) = attribute field ID + expected value: the item can only be equipped if its attribute equals the expected value (e.g. weapon primary type = melee one-handed, weapon secondary type = sword). The expected value is edited by the generic attribute editor per field type; multiple filter conditions on the same slot must **all be satisfied**. This lets you configure different equipment limits for different classes / character types.

> An equipment slot **does not store the runtime-equipped item** — the equipped data is maintained by `EquipmentRuntimeManager` keyed by slot ID.

# Equipment Attribute Field List

The "equipment attribute field list" (`EquipmentAttributeDisplay`) specifies which attribute fields on items are summed into the equipment group's **total attribute bonuses** (e.g. item level / attack / defense / HP). Each entry:

| Field | Description |
|------|------|
| Attribute field ID | Associates with an Item System attribute field (type it in or pick a defined field from the dropdown) |
| Group tag | References a group-tag ID, for grouped display in the UI (e.g. primary / secondary attributes) |
| Display name | Optional override (falls back to the attribute field ID when empty) |

Each entry has a drag handle on the left to reorder. At runtime this list sums across all equipped items (see [Runtime API](#runtime-api)).

# Limit Matching Rules

To decide whether an item can be equipped into a slot, **all-AND** is used:

1. **The slot list's function tags**: the item must **have all** the listed function tags.
2. **The slot list's enum constraints**: the item must **satisfy each** enum constraint — there exists an attribute referencing that enum type whose value is in the allowed set; an empty allowed set means "any value" (the item must still have the attribute of that enum type).
3. **The slot's filter conditions**: the item's corresponding attribute must **equal each** filter condition's expected value.

Only when all three are satisfied can the item be equipped into that slot.

# Runtime API

`EquipmentRuntimeManager` is a lightweight singleton (auto-created on first access, mirroring `ShopRuntimeManager`), maintaining runtime state as `equipment-group ID → (slot ID → equipped item ID)`. The equipment-group catalog is queried via `InventoryDataManager`; warehouse reads/writes always go through `InventoryRuntimeManager` (equip / unequip triggers its `OnInventoryChanged`, and the backpack UI refreshes accordingly).

```csharp
using Ale.Inventory.Runtime;

var eq = EquipmentRuntimeManager.Instance;

// Queries
string equipped = eq.GetEquipped("equip_player", "slot_weapon"); // the item equipped in that slot, null for an empty slot
bool occupied   = eq.IsSlotOccupied("equip_player", "slot_weapon");

// Limit matching
var group    = InventoryDataManager.Instance.GetEquipmentGroup("equip_player");
var slotList = group.slotLists[0];
bool canList = eq.ItemMatchesSlotList(slotList, "sword_01");                 // satisfies the slot list's limits?
bool canSlot = eq.ItemMatchesSlot(slotList, slotList.slots[0], "sword_01");  // further satisfies that slot's filter?

// Auto-find slot + auto-equip (take 1 from the backpack, place into the first equippable empty slot)
if (eq.TryFindEquipSlot("equip_player", "sword_01", out var slId, out var slotId)) { /* ... */ }
bool autoOk = eq.TryAutoEquip("equip_player", "sword_01", "backpack");     // fill an empty slot only
// Quick equip (replaceable): ① preferred slot preferredSlotId (replace if occupied) → ② empty slot → ③ the first occupied slot that satisfies the limits (from Index0, unequipping the original item back to its source warehouse)
// The UI's right-click quick-equip takes this path; when the equipment selection panel is open, the currently selected slot is passed in as the preferred slot
bool quickOk = eq.TryAutoEquipOrReplace("equip_player", "sword_01", "backpack", preferredSlotId: "slot_weapon");

// Equip / unequip / swap at a specific slot (cooperates with the warehouse to move items; when equipping into an occupied slot, the old item is returned to its source warehouse)
bool eOk = eq.Equip("equip_player", "weapon_list", "slot_weapon", "sword_01", "backpack");
bool uOk = eq.Unequip("equip_player", "slot_weapon", "backpack");      // unequip to a specified warehouse (fails and does not unequip if there's no room)
// Unequip to the equipment group's configured "equipment warehouses": from Index0, find the first with room; falls back to fallback (here backpack) when unconfigured
bool ucOk = eq.UnequipToConfigured("equip_player", "slot_weapon", "backpack");
string inv = eq.FindEquipmentInventoryFor("equip_player", "sword_01"); // the first equipment-warehouse ID with room for this item (null if none)
bool sOk = eq.SwapSlots("equip_player", "slot_weapon", "slot_offhand"); // slot↔slot swap within the same group (both must satisfy the target's limits)

// Total attribute bonuses (summed across all equipped items per the "equipment attribute field list", carrying the group tag for UI grouping)
foreach (var b in eq.GetTotalBonuses("equip_player"))
    Debug.Log($"{b.Label}({b.GroupTag}) = {b.Total}");   // for EnumIntPair, split into multiple entries by enum Key, with b.EnumValue being the enum value

// Events + save
eq.OnEquipmentChanged += groupId => { /* refresh equipment UI */ };
var save = eq.GetSaveData();          // serialized by the game-layer SaveManager
eq.LoadSaveData(save);                // restore on load
eq.ResetAll();                        // clear (e.g. starting a new game)
```

- **Equip**: `Equip` first validates the limits and takes 1 item from the source warehouse; if the slot is already occupied, it returns the old item to the source warehouse (take before place, net occupancy unchanged; rolls back if it can't be returned, so no item is lost). When no source warehouse is provided, it only sets the slot (the old item is replaced directly, at the caller's own risk).
- **Unequip**: `Unequip` returns the item to the specified target warehouse; if there's no room, it returns false and does not unequip. The UI's right-click unequip uses `UnequipToConfigured(groupId, slotId)` — from Index0 of the equipment group's "equipment warehouses" list, it finds the first warehouse with room; if none has room (or unconfigured with no fallback warehouse), it does not unequip and will not discard the item.
- **Attribute bonuses**: `GetTotalBonuses`, for each equipment attribute field, aggregates across all equipped items, **recording differently depending on the source attribute's `AttributeValue.Type`**:
  - **`EnumIntPair`**: split by enum Key — each enum Key becomes its own `EquipmentBonus`, whose integer value accumulates into `Total`; `EnumTypeRef`/`EnumValue` record the source enum, and the display name is resolved from the corresponding enum item's String / Text attribute via the equipment attribute field's "display name source (enum field)" (`EquipmentAttributeDisplay.enumLabelAttrId`), falling back to the enum item's name when unconfigured. Typical for "character attribute-value bonuses" (e.g. Strength +13, Agility +5). **Keys that cannot be resolved to an actual enum item (enum type missing / enum item deleted) are not shown.**
  - **`StringIntPair`**: split and accumulated by string Key, with the display name being that string Key. **Empty string Keys are not shown.**
  - **Other array types**: split by element index — one entry per index position, accumulating across items at the same index.
  - **Scalar types**: aggregated into one entry, summed by `AttributeValue.ToComparableNumber()` (numbers taken directly, vectors by magnitude); see [Attribute System – Sort Comparison Value](AttributeSystem_EN.md).

# Equipment UI

The equipment UI is under `Runtime/UI/` (assembly `Ale.Inventory.UI`):

| Component | Description |
|------|------|
| `UiwEquipmentView` | Equipment main screen: integrates the equipment-group panel + bonus panel + selection panel. `Open(groupId)` (warehouses come from the equipment group's "equipment warehouses", no need to pass them); right-click an equipment slot to unequip to the equipment warehouses; left-click a slot to open the selection panel. `_groupId` can be preset in the Inspector (default "Character Equipment"): the parameterless `Open()` uses that value, `Open(groupId)` overrides it, and the editor's `autoOpenOnStart` also uses the current `_groupId` |
| `UiwEquipmentGroupPanel` | Equipment-group panel: shows the equipment-group name + all slot lists. **Layout mode** (`displayMode`): `Auto` instantiates each slot list automatically per config; `Manual` — the user places the slot-list objects in the hierarchy themselves and binds them one by one via `manualSlotLists` (slot-list ID → slot list) for free layout (can be used standalone by configuring `groupId` + `bindOnStart` in the Inspector) |
| `UiwEquipmentSlotList` | Slot list: shows the name + all equipment slots. **Layout mode** (`displayMode`): `Auto` instantiates each equipment slot under a HorizontalLayoutGroup; `Manual` — the user places the equipment-slot objects in the hierarchy themselves and binds them one by one via `manualSlots` (slot ID → equipment slot) |
| `UiwEquipmentSlot` | Equipment slot: inherits `UiwInventoryItemSlotBase`; shows the equipped item; left/right-click events; drag source (drag out to swap) + drop target (equip / swap) + green/red validity overlay |
| `UiwEquipmentBonusPanel` / `UiwEquipmentBonusEntry` | Bonus panel: shows the total attribute bonuses grouped by group tag; shows an empty-state hint when there are no bonuses (`emptyText`, supporting localization table/entry references `emptyTextLocalized*`) |
| `UiwEquipmentSelectPanel` | Equipment selection panel: a top slot-group switch bar (left/right + N/M) + the current slot list in the middle + the equippable item list at the bottom + exit (button / right-click on blank space) |
| `UiwEquipmentCandidateList` | Equippable item list (**virtual-scroll grid**, inherits `UiwInventoryGridList`): filters candidate items across the equipment group's "equipment warehouses" by the current slot list's limits (each cell records its source warehouse); **right-click to quick-equip / left-drag to an equipment slot to equip**. Candidate cells reuse the warehouse cell `UiwInventoryItemCell` + `GridCellDragHandler` (same source as backpack cells; it doesn't take sort dragging, so the handler drives equip dragging). The prefab needs ScrollRect + Viewport + Content (Content has no GridLayoutGroup) |
| `GridCellDragHandler` | The item-cell drag relay component (attached to `UiwInventoryItemCell` or its child, located in `Runtime/UI/ItemList/`): when **connected to a grid list** (backpack), it forwards to `UiwInventoryItemGridList`, and at drag end decides by the drop point (equipment slot→equip, item cell→reorder); when **not connected to a grid list** (candidate list), it drives "drag to an equipment slot to equip" (via `UiwEquipmentDragContext`, with a ghost + green/red validity). Right-click quick-equip is not handled by this component — `UiwInventoryItemCell` broadcasts "item right-click" and `UiwEquipmentView` subscribes and handles it uniformly, so the cell interacts consistently in any container |
| `UiwEquipmentDragContext` | The global equipment-drag context (payload + a cursor-following ghost + source icon reset) |

## Interaction

- **Equip via the selection panel**: left-click an equipment slot → open the selection panel → switch slot groups → **right-click a candidate item at the bottom to quick-equip** (`UiwEquipmentView` subscribes to the "item right-click" event, with equip priority: **① the equipment slot currently selected in the selection panel** (replace if occupied) → ② the first equippable empty slot → ③ the first occupied slot that satisfies the limits (from Index0, unequipping the original item back to its source warehouse)) or **left-drag onto an equipment slot** to equip; a single left-click on a candidate item does not trigger equipping. Candidate items and backpack items **interact consistently** (right-click to quick-equip / left-drag to an equipment slot to equip).
- **Unequip**: right-click an equipment slot → unequip. Placed into the first warehouse with room from Index0 of the equipment group's "equipment warehouses" list; if none has room, it does not unequip (no item is discarded).
- **Swap between equipment slots**: drag one equipment slot onto another (same group); they swap when both satisfy the target's limits.
- **Drag an equipment slot onto a backpack cell** (requires that backpack's `dragSort = true`): drag an equipment slot to a backpack cell — landing on an **empty cell** → unequip and place exactly into that cell; landing on a **cell with an item** → if that item can be equipped into the source equipment slot, **swap positions** (that cell's item goes into the equipment slot, the originally equipped item lands back in that cell), otherwise cancel with no change.
- **Drag validity**: while dragging over an equipment slot, it shows green (can) / red (cannot) by whether it can be equipped.
- **Interop with the backpack**: **right-click** an item in the backpack screen → quick-equip (`UiwEquipmentView` auto-subscribes to the "item right-click" event when open, no extra wiring; when there's no empty slot it replaces the first occupied slot that satisfies the limits); a backpack **grid** cell can be **dragged** to an equipment slot to equip (requires that backpack's `dragSort = true`; ordered lists can't be dragged, use right-click).

For prefab authoring and common components, see the [UI Component Guide](UIComponentGuide_EN.md).

> Tip: an equipment-slot prefab needs a "validity overlay image" that is disabled by default (wired to `validityOverlay`) to show the green/red hint; the selection panel's root node needs a raycast graphic to support "right-click on blank space to exit"; dragging requires an EventSystem in the scene.
