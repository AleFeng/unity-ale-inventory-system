# Attribute System

- Back to [documentation](../README_EN.md)

The flexible attribute system is the shared data foundation of the four subsystems — Item / Warehouse / Shop / Crafting. This document explains attribute field types, `AttributeValue` storage and retrieval, display strings, and sort comparison rules. Each subsystem's "custom attribute fields", "attribute field display", and "sorting" are built on top of this.

# 📜 Table of Contents

- [Attribute System](#attribute-system)
- [📜 Table of Contents](#-table-of-contents)
- [Concepts](#concepts)
- [Attribute Field Type Reference](#attribute-field-type-reference)
- [Enum Types and Stable References](#enum-types-and-stable-references)
- [Reading Attribute Values (Runtime API)](#reading-attribute-values-runtime-api)
- [Asset Field Loading (Addressables)](#asset-field-loading-addressables)
- [Localization (Fixed Text Fields and Tooling)](#localization-fixed-text-fields-and-tooling)
- [Display String (ToDisplayString)](#display-string-todisplaystring)
- [Sort Comparison Value (ToComparableNumber)](#sort-comparison-value-tocomparablenumber)
- [StringIntPair and Price / Currency](#stringintpair-and-price--currency)
- [EnumIntPair and Equipment Attribute Bonuses](#enumintpair-and-equipment-attribute-bonuses)

# Concepts

The attribute system is composed of three types:

| Type | Role | Description |
|------|------|------|
| `AttributeDefinition` | **Definition (schema)** | An attribute field's definition: `id` (key), `type` (`EFieldType`), `isArray` (is-array), `enumTypeRef` (enum type name), default value. Configured on item templates / function tags / warehouse templates / shop templates / blueprint templates / enum items. |
| `AttributeValue` | **Value (tagged-union)** | Stores one or a group of values per `type`. Uses "tagged union + array" storage, natively supporting SO serialization and Undo/Redo. |
| `AttributeEntry` | **Key-value pair** | `id` + `AttributeValue`, forming a concrete entry (the `values` list of items / warehouses / shops / blueprints). |

Which attribute fields an item carries is jointly determined by its "source template's own fields + template-locked tag fields + item's own tag fields"; `RebuildAttributes` is responsible for syncing the actual `values` list per the definitions (adding missing ones, removing orphans, preserving existing values). Warehouses / shops / blueprints work the same way (collecting only from their respective templates).

# Attribute Field Type Reference

All the following types can be set to an **array form** (checking "array" allows storing multiple values, with dynamic add/remove support).

| Type | Storage | Editor control |
|------|------|-----------|
| Bool | integer 0/1 | Toggle |
| Int | integer | IntField |
| Float | float | FloatField |
| String | string | TextField |
| **Text** | when `IS_LOCALIZATION` is enabled, includes a localization reference: table + entry (the string field serves as fallback) | localization entry selector + text box |
| Vector2 / 3 / 4 | 2 / 3 / 4 floats | multi-column FloatField |
| VectorInt2 / 3 / 4 | 2 / 3 / 4 integers | multi-column IntField |
| Color | 4 floats (RGBA) | ColorField |
| **Enum** | integer (enum value) | Popup dropdown (an enum type must also be selected) |
| **StringIntPair** | string + integer | TextField + IntField (e.g. currency ID → price) |
| **EnumIntPair** | enum + integer (integer backing list, step 2) | Popup dropdown + IntField (an enum type must also be selected; e.g. character attribute type → bonus value) |
| Sprite | UnityEngine.Object | square preview |
| Prefab / Texture / Material / AudioClip / AnimationClip / PhysicsMaterial / PhysicsMaterial2D | UnityEngine.Object | ObjectField |
| AnimationCurve | AnimationCurve | CurveField |

> **Underlying storage**: all values are flattened by type into five backing lists — `List<int>` / `List<float>` / `List<string>` / `List<Object>` / `List<AnimationCurve>` (scalars in `[0]`, vectors flattened by step, arrays laid out in order). The Addressable address / authorized GUID of object-type fields is stored separately in an address list parallel to `List<Object>` (see [Asset Field Loading](#asset-field-loading-addressables)). See [Architecture](Architecture_EN.md#core-data-model) for details.

# Enum Types and Stable References

- An enum type (`EnumType`) maintains a monotonically increasing `nextValue`: it's assigned and incremented when adding an enum item, and **not reclaimed on deletion**.
- An attribute value stores the **enum value (int)**, not the display index, so **reordering enum items' display order does not break existing references**.
- An enum item (`EnumItem`) can itself carry a group of custom attribute fields (e.g. extra data per quality / class); after selecting an enum value in the item Inspector, its sub-attributes are expanded read-only.

# Reading Attribute Values (Runtime API)

Items / warehouses / shops / blueprints all inherit or implement attribute access (items via the `AttributeOwner` base class):

```csharp
using Ale.Inventory.Runtime;

Item item = InventoryDataManager.Instance.GetItem("sword_01");

// Get the entry (with AttributeValue), returns null if not found
AttributeEntry entry = item.GetEntry("attack");

// Get a strongly typed value (with fallback); T matches the field type (int/float/string/Vector3/Color/...)
int   atk   = item.GetAttributeValue<int>("attack", 0);
string desc = item.GetAttributeValue<string>("description");

// Get the raw AttributeValue (when you need type checks / arrays / multiple values)
AttributeValue av = item.GetAttributeValue("price");
```

`AttributeValue` provides type-specific read-only accessors: `GetInt` / `GetFloat` / `GetString` / `GetVector2~4` / `GetColor` / `GetStringIntPair` / `GetObject` / `GetAnimationCurve`, etc. (all by index, out-of-bounds safe). `Type` / `IsArray` / `Count` describe its shape.

# Asset Field Loading (Addressables)

The loading of object-type fields (Sprite / Prefab / Texture / Material / AudioClip / AnimationClip / PhysicsMaterial / PhysicsMaterial2D) goes uniformly through the `InventoryAssets` facade, decoupled from whether Addressables is enabled:

```csharp
using Ale.Inventory.Runtime;

// Bind an item attribute's asset to the UI; the handle is auto-released when the host GameObject is destroyed
InventoryAssets.Bind<Sprite>(item, "icon", image.gameObject, s => { image.sprite = s; });
// Or pass an AttributeValue directly (can specify an array element index)
InventoryAssets.Bind<Sprite>(attrValue, owner, s => image.sprite = s, index);
```

- **`IS_ADDRESSABLE` disabled (direct mode)**: attribute fields directly hold Unity asset references (`objRefs`), and assets are loaded into memory along with the config data; the facade returns that live reference synchronously (the editor control is an `ObjectField`).
- **`IS_ADDRESSABLE` enabled (authorized mode)**: object fields in the editor switch to the native **AssetReference** searchable selector, the config stores only the GUID (no hard reference, so loading the database no longer loads assets into memory too); at runtime it **loads asynchronously** on demand via Addressables, reference-counted by address, and **auto-unloads** when the host is destroyed. On export, referenced assets are automatically registered into the `InventorySystem` Addressable group.

> The two storage forms have different on-disk formats and can't be shared automatically via same-named fields. After switching macros, use the menu **Tools/Inventory System/Addressables** to convert all asset fields of a database between "Object reference ↔ AssetReference(GUID)" in one click.
>
> Underlying: the authorized GUID / runtime address is stored in `AttributeValue` in parallel with the live reference (address list vs `objRefs`); the facade prefers the live reference, falling back to async address loading if absent. The core assembly has zero dependency on Addressables; the native selector is injected via the constrained Addressable editor assembly (the same injection pattern as `InventoryExportResolver`).
>
> The config classes' **fixed asset fields** (named fields like `Skill.icon`, `SkillTemplate.icon`, `FunctionTag.backgroundSprite`) use the same mechanism: each has a parallel `xxxAddress` plain-string field, drawn in the editor via `InventoryAssetRefField` (direct `ObjectField` / authorized AssetReference selector), and fetched asynchronously at runtime likewise via `InventoryAssets.Bind(liveRef, address, owner, set)`.

# Localization (Fixed Text Fields and Tooling)

The whole library's localization display text is carried uniformly by `EFieldType.Text`: `AttributeValue` stores it flattened in three slots — "plain-text fallback + table reference + entry key" (native serialization / Undo / export friendly), and at runtime `AttributeValue.ResolveText()` **prefers localization, falling back to plain text when unavailable**. This includes both Text-typed custom attribute values in the attribute system, and each config class's **fixed Text fields**:

| Config class | Fixed Text fields |
|--------|---------------|
| `Skill` / `SkillTemplate` / `CraftingBlueprint` | `displayText` (name), `descriptionText` (description) |
| `Shop` / `Inventory` / `EquipmentGroup` / `FunctionTag` | `displayNameText` (name), `descriptionText` (description) |
| `GroupTag` (skill / crafting / equipment group tags) | `displayName`, `description` |
| `NumberFormatRule` | `suffixText` (number suffix) |
| `SortOption` | `displayName` (sort dropdown display name) |

The editor draws Text uniformly via `AttributeFieldDrawer` (plain text box + native searchable table / entry selector); at runtime it's read via `ResolveText()`.

## Localization Tool Window

`Tools > Inventory System > Localization > Localization Tool Window` (`IS_LOCALIZATION` only; also has an entry button in the Welcome Window). Integrates Unity Localization for one `InventoryDatabase` in one place:

1. **Generate / link localization tables**: generates a String Table collection per the current Locale (table name `{prefix}_{database name}`, prefix / output folder configurable and remembered), and records its `SharedTableData` GUID onto the database (1:1, field `InventoryDatabase.LocalizationTableCollectionGuid`). "Link localization table" also lets you manually create a String Table Collection and drag it in; the "Edit" button opens that table's Table Editor.
2. **Generate localization keys**: iterates over **all** Text fields in the library, generating a unique **Chinese key** frame by frame (`ItemSystem-{category}-{instance id}-{field}[-{element}]`, e.g. `ItemSystem-item entry-{item id}-name`, `ItemSystem-enum type-{enum name}-{enum item name}-{attr id}`), writing back the field's table / entry reference and creating a Key→Value entry in the table. Only fields with plain-text content are processed; duplicate keys append `#n` for deduplication.
3. **Two checkboxes**:
   - **Overwrite existing localization keys**: pops a confirmation before running when checked; fields that already have a key switch to the auto-generated key (unchanged if the naming is identical to the existing one). Unchecked skips already-configured fields.
   - **Fill in the String text from Text**: fills the source Text's plain-text value as the initial value into that key's empty entries across **all language tables** when checked (does not overwrite existing translations).

> Chinese keys work perfectly: Unity Localization supports Unicode keys, resolved by key at runtime, with no material performance difference between Chinese and English. This tool shares the base class `InventoryToolWindowBase` (frame-by-frame stepping + progress bar + selectable log) with `InventoryAddressableToolWindow` (asset-reference migration).

# Display String (ToDisplayString)

Assembles an attribute value into a readable string, using different rules per `EFieldType`, for direct UI display (e.g. the "attribute field display" of a crafting blueprint entry):

```csharp
string text = item.GetEntry("attack")?.value?.ToDisplayString();
```

| Type | Display form |
|------|---------|
| Int / Float | direct to string (Float keeps up to 2 decimal places) |
| Bool | `是` / `否` (Yes / No) |
| String | verbatim |
| Enum | enum item display name |
| Vector2 / 3 / 4 | `(x, y[, z[, w]])` |
| VectorInt2 / 3 / 4 | `(x, y[, z[, w]])` |
| Color | `RGBA(r, g, b, a)` |
| **StringIntPair** | `key: value` (e.g. `gold: 120`) |
| **EnumIntPair** | `enum name: value` (e.g. `Strength: 10`; the key resolves to the enum item's display name) |
| Text | plain text; falls back to the localization entry key / table reference when empty |
| AnimationCurve | `曲线(N 关键帧)` (Curve(N keyframes)) |
| Object reference | the asset object's `name` |

In array form, each element is assembled separately and joined by a separator (default `、`). The read process is non-destructive and does not modify the underlying data.

# Sort Comparison Value (ToComparableNumber)

When sorting, a custom attribute field is converted to a `double` for comparison, per `EFieldType`:

| Type | Comparison basis |
|------|---------|
| Int / Bool / Enum | the value itself |
| Float | the value itself |
| Vector2 / 3 / 4 | **magnitude** |
| Color | the magnitude as a Vector4 |
| VectorInt2 / 3 / 4 | magnitude |
| **StringIntPair** | only its **Int value** |
| **EnumIntPair** | only its **Int value** |
| String / object reference / curve / localization | no comparable value → `0` |

> `String`-typed fields are handled specially by the comparator when sorting (by length first, then lexicographically), not via numeric conversion. See [Warehouse System – Sorting](WarehouseSystem_EN.md#sorting) for details.

Underlying entry point: `InventoryRuntimeManager.GetAttrNumeric` → `AttributeValue.ToComparableNumber()`; vector-component reads are non-destructive (they don't grow the underlying list).

# StringIntPair and Price / Currency

`StringIntPair` (a string + integer pair, usually an array) is the carrier for "price / currency":

- The shop's "price attribute source" points to a `StringIntPair` array attribute on the item, where each element = `currency ID → unit price`, supporting pricing a single product in several currencies.
- At runtime `ShopRuntimeManager.GetUnitPrice` reads it and multiplies by the product's price multiplier. See [Shop System – Price Sources](ShopSystem_EN.md#price-sources) for details.
- When sorting, `StringIntPair` compares only the Int value (price); when displaying, it's assembled as `currency: price`.

# EnumIntPair and Equipment Attribute Bonuses

`EnumIntPair` (an enum + integer pair, usually an array) is the carrier for "character attribute bonuses", structurally symmetric to `StringIntPair`, differing in that the key is an **enum** rather than an arbitrary string:

- Each element = `character attribute type (enum key) → bonus value (integer)`, supporting a single piece of equipment giving separate bonuses to several character attributes.
- You must select the corresponding **enum type** for the field (e.g. a "character attribute type" enum, same as an `Enum` field); the editor records it in Popup + IntField pairs, flattened in the integer backing list (step 2: enum value + integer value), with the enum type recorded via `EnumTypeRef`.
- It stores the **enum value (int)**, not the display index, so reordering / renaming enum items doesn't break existing references.
- When displaying, it's assembled as `enum name: value` (e.g. `Strength: 10`); when sorting, only its integer value is compared.
- At runtime, `GetEnumIntPair(index)` reads `(enumValue, value)`. For the full configuration and settlement of equipment bonuses, see [Equipment System](EquipmentSystem_EN.md).
