# Item System

<p align="center">
  🌍
  <a href="./ItemSystem.md">中文</a> |
  English |
  <a href="./ItemSystem_JA.md">日本語</a>
</p>

- Back to [documentation](../README_EN.md)

The Item System is the foundation of the whole plugin: it defines every item in the game and its attributes. The Warehouse / Shop / Crafting systems all reference item IDs from the Item System. This document explains how to configure enum types, function tags, item templates, and items on the "Item System" tab of the Inventory Editor, and how to query item data at runtime.

# 📜 Table of Contents

- [Item System](#item-system)
- [📜 Table of Contents](#-table-of-contents)
- [Open the Editor](#open-the-editor)
- [Tab Structure](#tab-structure)
- [Enum Types](#enum-types)
- [Function Tags](#function-tags)
- [Item Templates](#item-templates)
- [Item List (Middle Column)](#item-list-middle-column)
- [Item Inspector (Right Column)](#item-inspector-right-column)
- [Warehouse Attributes: Weight and Stacking](#warehouse-attributes-weight-and-stacking)
- [Runtime Queries](#runtime-queries)
- [Item-Related UI Components](#item-related-ui-components)
- [FAQ](#faq)

# Open the Editor

1. **Create a data file**: `Right-click in the Project panel > Create > Inventory System > Inventory Database` (or "Create New Data File" in the Welcome Window).
2. **Open the editor**: select the `.asset` and click "Edit in Inventory Editor" in the Inspector, or use the menu `Tools > Inventory System > Inventory Editor`.
3. Click the "**Item System**" tab at the top.

The editor uses a three-column layout: **left column** (definitions) | **middle column** (item list) | **right column** (item Inspector).

# Tab Structure

The left column of the Item System tab has three sub-tabs at the top: **Enum Types**, **Function Tags**, **Item Templates**. All three are "definitions" that items reference and reuse.

```
Left column: Enum Types / Function Tags / Item Templates (sub-tabs + list + edit panel)
Middle column: item list (template filter tags + search + drag-to-reorder)
Right column: Inspector of the selected item
```

# Enum Types

Used to define fields that need a limited set of options, e.g. quality, class, body part, elemental attribute.

- **New**: the "New Enum" button at the bottom of the left column.
- **Edit** (click an enum name; the edit panel appears in the right column):

| Field | Description |
|------|------|
| Name | The enum type's unique name; attribute fields reference it by this name |
| Color | The color dot marker in the list |
| Enum item list | Each item has a display name + a read-only enum value (integer); the **drag ≡ handle** adjusts display order (without changing stored values); ✕ deletes |
| Enum item attribute fields | Each enum item can carry an additional group of custom attribute fields (e.g. an "Equipment" enum item carrying attribute bonuses); configured like function tags |
| "Add Enum Item" | The system auto-assigns a non-repeating enum value, **never reused** after deletion |

> Enum values are assigned by the system and read-only, unaffected by display order. See [Attribute System – Enum Types and Stable References](AttributeSystem_EN.md#enum-types-and-stable-references) for details.

# Function Tags

Attaches a group of capability markers to an item, and at the same time provides the definitions for the attribute fields that marker carries. For example, an "Equipment" tag carries fields like "body part" and "attack".

- **New**: the "New Function Tag" button.
- **Edit**:

| Field | Description |
|------|------|
| Name | The tag's unique name; items reference it by this name |
| Description | Optional description (editor-only) |
| Attribute field list | The attribute field definitions this tag carries; drag to reorder; ✕ deletes |
| "Add Field" | Adds an attribute field (see [Attribute Field Type Reference](AttributeSystem_EN.md#attribute-field-type-reference) for types) |

**Attribute field config options**: ID (unique key, non-ASCII text supported), type, is-array, enum type (Enum only), default value.

# Item Templates

An item template defines a group of **base attribute fields** that every item created from it will carry.

- **New**: the "New Item Template" button.
- **Edit**:

| Field | Description |
|------|------|
| Name | The template's unique name |
| Color | The color dot for this template's items in the item list |
| Function tags | Function tags can be **locked** onto the template (items of this template automatically carry these tags and fields, which cannot be unchecked at the item level) |
| Attribute field list | The template's own attribute fields |

> **Design tip**: put common fields (name, description, icon, quality) in template attributes; put category-specific fields (attack, heal amount) in function-tag attributes, then lock the corresponding tag onto the template.

# Item List (Middle Column)

```
[ All ][ Weapon ][ Armor ][ Consumable ]   ← template filter tag bar (single-select)
[ 🔍 search box ][ Add from Template ▾ ][ Quick Add ]
─────────────────────────────────────
≡ ● template name | ID | attr1 | attr2 …  ✕
```

| Action | Description |
|------|------|
| Template filter tag bar | "All" shows every item; clicking a template name shows only that template's items |
| Search box | Fuzzy filter by item ID / attribute string value / enum display name / tag name |
| Add from Template | Pick a template to generate a new item (ID auto `item_N`) |
| Quick Add | Clones the last item in the list |
| Drag ≡ handle | Adjusts item order within the current filter result |
| Click row / ✕ | Select / delete |

**Duplicate-ID detection**: rows with an empty or duplicate ID are highlighted in red, a warning shows in the bottom status bar, and export buttons are disabled while non-empty duplicates exist.

# Item Inspector (Right Column)

After selecting an item, the right column shows the full editing UI:

- **ID**: text field; the background turns red when duplicate or blank.
- **Source template**: read-only, cannot be changed after creation.
- **Function tags**:
  - Template-locked tags: gray, read-only, marked "locked by template".
  - Item's own tags: a Toggle list; checking automatically adds that tag's fields, unchecking removes them.
- **Attributes (grouped display, collapsible)**:

| Group | Source |
|----|------|
| Template: XXX | The template's own attribute fields |
| Tag name (locked by template) | Attribute fields of template-locked tags |
| Tag name | Attribute fields of the item's own tags |
| Other (source deleted) | Historical fields whose source template / tag has been deleted (cleared on the next Rebuild) |

When same-named fields come from multiple sources, a conflict warning is shown, and priority (template-own → template-locked tag → own tag) is first-come-first-served. After selecting a value for an enum field, that enum item's sub-attributes are expanded read-only beneath it.

# Warehouse Attributes: Weight and Stacking

The item Inspector also has two fields used by the Warehouse System:

| Field | Description |
|------|------|
| Weight | Float; 0 = weightless (does not count toward a warehouse's weight limit) |
| Stack limit | Integer; 0 = unlimited stacking, 1 = not stackable, >1 = a specific limit |

# Runtime Queries

Item data is a static definition, queried at runtime via `InventoryDataManager`:

```csharp
using Ale.Inventory.Runtime;

// Get an item by ID
Item item = InventoryDataManager.Instance.GetItem("sword_01");

// Get an attribute value (strongly typed + fallback)
int   atk  = item.GetAttributeValue<int>("attack", 0);
string nm  = item.GetAttributeValue<string>("name");

// Get an enum display name
EnumType quality = InventoryDataManager.Instance.GetEnumType("quality");
string qName = quality?.GetItemByValue(item.GetAttributeValue<int>("quality"))?.name;

// Check a tag
bool isEquip = InventoryDataManager.Instance.ItemHasTag("sword_01", "equipment");
```

For the full attribute-read API, display strings, and sort conversion, see [Attribute System](AttributeSystem_EN.md).

# Item-Related UI Components

The cell components under `Runtime/UI/Item/` automatically pull icon / name / quality, etc. from item static data:

| Component | Description |
|------|------|
| `UiwInventoryItemSimple` | Simple cell: icon + count only (currency bars, etc.) |
| `UiwInventoryItemDetail` | Full cell: icon / name / description / quality background / count / price / hover highlight / stack-full animation |
| `UiwShopItemDetail` | Shop cell: quality background + icon + name / unit price + remaining tradeable count + quantity selector |
| `UiwCraftingBlueprintCell` | Blueprint entry: primary-output icon + blueprint name + primary-output item attributes shown per blueprint config |

See the [UI Component Guide](UIComponentGuide_EN.md) for each component's Inspector parameters and prefab authoring.

# FAQ

**Q: If I change an enum item's display order, will existing items' enum values change?**
A: No. An enum attribute stores an immutable integer enum value, independent of display order.

**Q: Is a function tag's field data lost after I delete the tag?**
A: After calling `RebuildAttributes`, fields whose source has been deleted are removed; before removal they're visible in the Inspector's "Other (source deleted)" group.

**Q: The Inspector shows "enum type not found"?**
A: Check whether the enum type name was renamed; re-select the enum type in the attribute field definition to fix it.

**Q: The export button is grayed out?**
A: There are items / warehouses / shops / blueprints with non-empty duplicate IDs; fix the red-highlighted entries first (blank IDs are skipped automatically on export and do not block).
