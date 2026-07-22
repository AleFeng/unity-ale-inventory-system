# Crafting System

- Back to [documentation](../README_EN.md)

The Crafting System uses "blueprints (recipes)" to describe "consume materials → produce items". A blueprint references item IDs from the Item System, deducts materials from crafting warehouses, and places output into crafting warehouses. Blueprints are a config catalog; the crafting action at runtime is executed by `CraftingRuntimeManager`, while the count / timing / progress of continuous crafting is driven by the UI layer.

# 📜 Table of Contents

- [Core Concepts](#core-concepts)
- [Tab Structure](#tab-structure)
- [Group Tags](#group-tags)
- [Blueprint Templates](#blueprint-templates)
- [Blueprints (Middle Column)](#blueprints-middle-column)
- [Blueprint Inspector (Right Column)](#blueprint-inspector-right-column)
- [Recipe: Outputs and Inputs](#recipe-outputs-and-inputs)
- [Crafting Warehouses](#crafting-warehouses)
- [Attribute Field Display](#attribute-field-display)
- [Craftable Count and Continuous Craft Count](#craftable-count-and-continuous-craft-count)
- [Runtime API](#runtime-api)
- [Crafting UI](#crafting-ui)

# Core Concepts

| Concept | Description |
|------|------|
| Group tag (`CraftingGroupTag`) | Groups blueprints for easy UI filtering; each blueprint has 1 primary group + multiple secondary groups |
| Blueprint template (`CraftingBlueprintTemplate`) | Custom attribute fields + config default values + a template-level sort, acting as a blueprint for creating blueprints, and also used for categorization (armor / weapon / food …) |
| Blueprint (`CraftingBlueprint`) | A single recipe: output / input item lists + craft time / count + crafting warehouses + UI config |
| Crafting warehouses | An ordered list of warehouse IDs: used by priority as material sources and output destinations |

# Tab Structure

Click the "**Crafting System**" tab at the top of the Inventory Editor. A three-column layout, symmetric with the Shop System:

```
Left column: sub-tabs [Group Tags / Blueprint Templates] + list + edit panel
Middle column: blueprint list
Right column: context Inspector (group tag / blueprint template / blueprint)
```

The left column's sub-tabs switch between "Group Tags" and "Blueprint Templates"; selecting a left-column entry shows that entry's edit panel in the right column, while selecting a middle-column blueprint shows the blueprint Inspector.

# Group Tags

Carry only basic info, and **do not carry custom attribute fields**. Used to group and filter blueprints in the UI.

| Field | Description |
|------|------|
| ID | Unique identifier (blueprints reference it by this ID) |
| Name | `Text` (plain-text fallback + optional localization reference; falls back to ID when empty) |
| Description | `Text` (plain-text fallback + optional localization reference) |
| Color | List color dot |

# Blueprint Templates

Defines custom attribute fields + a full set of blueprint config-option default values (copied when creating a blueprint), and carries the **template-level sort** (the order in the UI list of all blueprints under this template; the blueprint itself no longer configures sorting separately).

| Field | Description |
|------|------|
| Name / Color | Template name + list color dot |
| Craft time | The seconds needed to craft once |
| Continuous craft count | See [Craftable Count and Continuous Craft Count](#craftable-count-and-continuous-craft-count) |
| Crafting warehouses | A list of warehouse IDs (ordered) |
| Number format | The name of the referenced number-format config |
| Sort list / Sort priority | Template-level sort criteria (primary + secondary) |
| Attribute field display | Which attributes to show in the UI (Label + attribute field ID) |
| Attribute field list | The template's custom attribute fields |

> A blueprint and a blueprint template share `ICraftingConfig`; the config options are identical, and the editor reuses the same drawer.

# Blueprints (Middle Column)

The middle column is the blueprint list (filtered by the selected left-column template). A new blueprint is created from a template with an auto-generated ID; click a row to select, the right column shows the blueprint Inspector, and "Delete Blueprint" is at the top of the right column.

# Blueprint Inspector (Right Column)

| Field | Description |
|------|------|
| ID | Unique identifier (a duplicate raises an error during export validation) |
| Name / Description | `Text` (plain-text fallback + optional localization reference; falls back to ID / base description when the name is empty) |
| Source template | Determines the config options and custom attributes |
| Primary group tag | Single-select, references a group-tag ID |
| Secondary group tags | Multi-select |
| Output item list | Index0 = primary output (shown in the UI), the rest are secondary outputs |
| Input item list | Materials needed to craft once |
| Craft parameters (craft time / continuous craft count) | **Blueprint-level, editable** |
| Crafting warehouses / UI config (number format + attribute field display) | **Template-level, read-only display on the blueprint entry**; mirrors the source template, editable only in "Blueprint Templates" |
| Attribute field values | Custom attribute values from the template definition |

> **Template-level config**: crafting warehouses and UI config (number format + attribute field display) can only be configured in "Blueprint Templates". A blueprint entry always mirrors these fields from its source template (synced by `CraftingBlueprint.RebuildAttributes`), shown read-only in the blueprint Inspector and not individually editable. A blueprint with no source template must first be assigned a template before these items can be configured.

# Recipe: Outputs and Inputs

Both outputs (`outputs`) and inputs (`inputs`) are lists of "item + quantity" (`CraftingItemAmount`: `itemId` + `count`).

- **Output Index0 = primary output**: the UI detail's icon / name / description come from the primary output item; the rest are secondary outputs (shown as small icons in the detail, with details on hover).
- **Input list**: the materials needed to craft once; at runtime the holdings are validated item by item against the requirement.
- When a blueprint references a non-existent item, export validation raises an error.

# Crafting Warehouses

`craftInventoryRefs` is an ordered list of warehouse IDs (**template-level config: edited in "Blueprint Templates", read-only inherited by blueprint entries**):

- **Material sources**: when deducting materials, they're accumulated and deducted from each warehouse in list order (priority).
- **Output destinations**: when placing output, it's distributed to each warehouse's remaining capacity in list order; if it can't all fit, the excess is discarded per capacity (consistent with shop trades).
- **Holdings tally**: the craftable count / holdings are aggregated across all crafting warehouses.

# Attribute Field Display

"Attribute field display" (`CraftingAttributeDisplay`: `label` + `attrId`) controls which attributes of the **primary output item** are shown on the blueprint entry / detail, in the form "Label value" (e.g. "Level 5", "Value 120"). **Template-level config: edited in "Blueprint Templates" (drag the left handle to reorder), read-only inherited by blueprint entries.**

The value is assembled by `AttributeValue.ToDisplayString()` per field type (numbers converted directly, vectors listing all components, StringIntPair shown as `key: value`, etc.); see [Attribute System – Display String](AttributeSystem_EN.md#display-string-todisplaystring).

# Craftable Count and Continuous Craft Count

Two easily confused concepts:

| Concept | Meaning | Computation |
|------|------|------|
| **Craftable count (materials)** | How many times current materials allow crafting | The minimum of each input item's `floor(holdings / per-craft consumption)` |
| **Continuous craft count (`maxCraftCount`)** | The batch upper limit a single "Craft" action may fire | `1` = only once; `-1` = unlimited |

- The UI detail's "Craftable: N" shows the **materials craftable count** (`GetMaxCraftableByMaterials`).
- The craft-count selector's (continuous-craft batch) upper limit = the min of the materials craftable count and `maxCraftCount` (`GetMaxCraftable`).

# Runtime API

`CraftingRuntimeManager` is a lightweight singleton with no state of its own and no save data. Item data is queried via `InventoryDataManager`; warehouse reads/writes go through `InventoryRuntimeManager` (consumption / output triggers its `OnInventoryChanged`, and the UI refreshes accordingly).

```csharp
using Ale.Inventory.Runtime;

var cm = CraftingRuntimeManager.Instance;
CraftingBlueprint bp = InventoryDataManager.Instance.GetCraftingBlueprint("craft_sword");

int owned = cm.GetOwnedAcross(bp, "iron_ingot");          // holdings across crafting warehouses
int byMat = cm.GetMaxCraftableByMaterials(bp);            // materials craftable count (for display)
int max   = cm.GetMaxCraftable(bp);                       // selector upper limit (constrained by continuous craft count)
bool can  = cm.CanCraftOnce(bp);                          // whether materials are enough to craft once

// Execute one craft (validates all consumption is sufficient first, then deducts materials across warehouses and places output)
bool ok = cm.CraftOnce(bp);          // or cm.CraftOnce("craft_sword")
```

Continuous crafting = the UI layer calls `CraftOnce` in a loop and drives the timing / progress (see `UiwCraftingDetail`).

# Crafting UI

The crafting UI is under `Runtime/UI/View/Crafting/`, with a structure symmetric to the backpack:

| Component | Description |
|------|------|
| `UiwCraftingView` | Crafting main screen: blueprint-template tabs + name search + collapsible group tabs + sort bar + blueprint virtual list + blueprint detail |
| `UiwCraftingDetail` | Blueprint detail: primary / secondary outputs, input list, craftable count, craft-count selector (number counter), craft / stop + progress bar |
| `UiwCraftingBlueprintCell` | Blueprint list entry |
| `UiwCraftingInputCell` | Input item row (icon / name / requirement / holdings) |
| `UiwCraftingBlueprintList` | Blueprint virtual-scroll list |
| `UiwCraftingGroupFilter` | Collapsible group tabs (a primary group can unfold its secondary groups, reusing `UiwFoldTab`) |

Filter pipeline: template → group (primary / secondary) → name search; then sorted by the selected template's sort settings. For prefab authoring and parameters, see the [UI Component Guide](UIComponentGuide_EN.md).
