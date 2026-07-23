# Skill System

<p align="center">
  🌍
  <a href="./SkillSystem.md">中文</a> |
  English |
  <a href="./SkillSystem_JA.md">日本語</a>
</p>

- Back to [documentation](../README_EN.md)

The Skill System configures the skills the player can use (attack / heal / buff / debuff / support, etc.). A skill is an independent config entry carrying fixed info like ID / name / description / icon, while a skill's **type / effect / values / tier**, etc., are carried by attrIds the consumer agrees on in "custom attribute fields" for other systems to read. Skills are primarily **granted to equipment-type items** (a weapon's attack skill, armor's defense skill, etc.), and can also be granted to other items (a consumable's use skill, magic scrolls / skill books, etc.). Skills are a config catalog; the "learned skills" state at runtime is maintained by `SkillRuntimeManager`, and the display set is collected by `SkillCollector` by source.

# 📜 Table of Contents

- [Skill System](#skill-system)
- [📜 Table of Contents](#-table-of-contents)
- [Core Concepts](#core-concepts)
- [Tab Structure](#tab-structure)
- [Group Tags](#group-tags)
- [Skill Templates](#skill-templates)
- [Skills (Middle Column)](#skills-middle-column)
- [Skill Inspector (Right Column)](#skill-inspector-right-column)
- [Item ↔ Skill Association](#item--skill-association)
- [Tier (Enum)-Driven Display](#tier-enum-driven-display)
- [Skill Sources](#skill-sources)
- [Runtime API](#runtime-api)
- [Skill UI](#skill-ui)
  - [Display Modes and Filtering](#display-modes-and-filtering)
  - [Custom Attribute Fields (Tooltip)](#custom-attribute-fields-tooltip)
  - [Custom Inspector](#custom-inspector)
  - [One-Click Prefab Generation](#one-click-prefab-generation)

# Core Concepts

| Concept | Description |
|------|------|
| Group tag (`SkillGroupTag`) | Groups **skills** for easy filtering by grouping tabs in the runtime UI (e.g. attack / heal / buff / debuff / support). Carries only basic info, no attribute fields |
| Skill template (`SkillTemplate`) | A blueprint for creating skills: defines custom attribute fields (schema) + a set of "skill default info" (name / description / icon / group tags), also used for category filtering |
| Skill (`Skill`) | A skill config entry: fixed info (ID / name / description / icon) + custom attribute values from the template (carrying type / effect / values / tier, etc.) |
| Item skill reference | An item stores a skill ID in one of its "skill reference attribute fields" (String, array-capable), **granting** the skill to that item |
| Tier (Enum attribute) | An Enum-typed custom attribute on the skill; its enum items carry attributes like "name / background frame", driving the entry background frame and the Tooltip's tier-name display |

> `Skill` inherits `AttributeOwner` (same lineage as `Item` / `EnumItem`), so you can use `GetEntry` / `GetAttributeValue<T>` to read custom attributes by attrId (String / Text / numeric / enum, etc.).

# Tab Structure

Click the "**Skill System**" tab at the top of the Inventory Editor. A three-column layout, symmetric with the Crafting / Equipment systems:

```
Left column: sub-tabs [Group Tags / Skill Templates] + list + edit panel
Middle column: skill list
Right column: context Inspector (group tag / skill template / skill)
```

The left column's sub-tabs switch between "Group Tags" and "Skill Templates"; selecting a left-column entry shows that entry's edit panel in the right column, while selecting a middle-column skill shows the skill Inspector.

# Group Tags

Carry only basic info, and **do not carry attribute fields**. Used to filter skills by grouping tabs in the runtime UI.

| Field | Description |
|------|------|
| ID | Unique identifier (skills reference a group tag by this ID) |
| Name | `Text` (plain-text fallback + optional localization reference; falls back to ID when empty) |
| Description | `Text` (plain-text fallback + optional localization reference) |
| Color | List color dot |

> The runtime grouping tabs use a group tag's **display name** as the filter token, so each group tag's display name should be distinct.

# Skill Templates

**Defines custom attribute fields (schema)** and carries a set of "skill default info", acting as a blueprint for creating skills and also used for category filtering.

| Field | Description |
|------|------|
| Name / Color | Template name (the `templateRef` reference key of a skill) + list color dot |
| Skill default info | Name / localized name / description / localized description / icon / primary group tag / secondary group tags |
| Custom attribute fields | The attribute field schema defined by the template (skills reconcile their attribute values accordingly; each field's `defaultValue` is the initial value of the skill attribute) |

> A skill and a template share `ISkillConfig` (name / description / icon / localization / group tags), and the editor reuses the same drawer (`SkillConfigDrawer`).
>
> **When creating a skill from a template, the template's "skill default info"** (name / description / icon / group tags) is copied as the initial values, and the custom attribute values are initialized per the schema's `defaultValue`; afterward the skill is **independently editable** (no longer linked to the template).

# Skills (Middle Column)

The middle column is the skill list (filtered by the selected left-column template + a top search bar filtering by ID / name). "**Add from Template**" creates one from a template (copying the default info + initializing attribute values per the schema), with an auto-generated ID; "**Quick Add**" clones the last entry. Click a row to select, the right column shows the skill Inspector, and "Delete Skill" is at the top of the right column. Each row has a drag handle on the left to reorder skills; a duplicate ID highlights the row in red, and the bottom status bar shows "⚠ Duplicate skill ID".

# Skill Inspector (Right Column)

| Field | Description |
|------|------|
| ID | Unique identifier (a duplicate raises an error during export validation; items reference the skill by this ID) |
| Name / Description | `Text` (plain-text fallback + optional localization reference; falls back to ID when the name is empty; the description is shown in the detail popup) |
| Icon | `Sprite` (shown on the skill entry / popup) |
| Source template | Read-only, determines the custom attribute fields |
| Primary group tag / Secondary group tags | Single-select primary group + multi-select secondary groups (reference group-tag IDs) |
| Custom attribute values | Custom attribute values from the template definition (the skill's type / effect / values / tier, etc., are carried here) |

> A skill's type / effect / values, etc., have **no fixed fields** — they are entirely up to the custom attribute fields you define in the template (e.g. a `skill type` Enum, an `effect` String, a `damage` Int, a `位阶` (tier) Enum), whose values you fill on the skill and which other systems / UI read by attrId.

# Item ↔ Skill Association

A skill is granted to an item via **one of the item's custom attribute fields**:

- In the item template (or a function tag), define a **String-typed** attribute field (e.g. `技能` / "skill"), and check the **array** form to let **one item carry multiple skills**.
- Fill in the skill ID(s) on the item (multiple for an array).
- The runtime UI (`Equipment` / `Inventory` sources) configures that field's attrId on the component (`skillRefAttrId`, default `技能`), and during collection reads all non-empty strings of that item attribute, resolving each into a skill via `InventoryDataManager.GetSkill`; unresolvable IDs are skipped.

> The collection entry point `SkillCollector` only recognizes `EFieldType.String` (scalar or array) skill reference fields; a skill referenced by multiple items / slots is shown only once (deduplicated by reference, order-preserving).

# Tier (Enum)-Driven Display

A skill can be configured with an **Enum-typed** "tier" attribute field (attrId default `位阶`). Its enum items (defined in the **Item System's "Enum Types"**, where `EnumItem` is also an `AttributeOwner`) can carry custom attribute fields such as **name**, **description**, **background frame (Sprite)**. The UI renders from these (**mirroring the item UI's quality background**, with an identical resolution chain):

```
skill.GetEntry(rankAttrId).value → enum value + enum type reference
  → InventoryDataManager.GetEnumType(ref).GetItemByValue(enumValue)     // the enum item EnumItem
  → enumItem.GetAttributeValue<Sprite>(backgroundAttrId)   // the skill entry's "tier background frame"
  → enumItem.GetAttributeValue<string>(nameAttrId)         // the Tooltip's "tier name" (works for both String / Text)
```

- **Skill entry** (`UiwSkillEntry`): shows the entry background frame from the tier enum item's "background frame" Sprite. Configure `rankAttrId` (default `位阶`) + `rankBackgroundAttrId` (default `背景框`) on the component.
- **Skill Tooltip** (`UiwSkillTooltip`): shows the tier enum item's "name". Configure `rankAttrId` + `rankNameAttrId` (default `名称`) on the component.
- This is all resolved at the UI layer, requiring no new data API; when there's no tier data / the enum item can't be resolved, the related display is auto-hidden (null-safe).

# Skill Sources

The runtime skill UI's skill set can come from four sources (`ESkillSource`, switched on `UiwSkillView`; its custom Inspector **shows only the corresponding ID field** by source):

| Source | What's collected | Config needed |
|------|---------|--------|
| `InventoryDatabase` | All skills in the database (skill book / codex) | — |
| `Equipment` | Skills referenced by the equipped items of every slot in an equipment group | Equipment-group ID + skill reference attribute `skillRefAttrId` |
| `Inventory` | Skills referenced by every item in a warehouse | Warehouse ID + skill reference attribute `skillRefAttrId` |
| `Character` | The skills a character has currently learned | Character ID (reads `SkillRuntimeManager`) |

Collection goes uniformly through `SkillCollector.Collect(source, configId, skillRefAttrId)`, with the result deduplicated and order-preserving.

# Runtime API

`SkillRuntimeManager` is a lightweight singleton (auto-created on first access, mirroring `EquipmentRuntimeManager`), maintaining multiple characters' learned skills as **character ID → list of learned skill IDs** (preserving learning order, deduplicated), which can be saved. Skill definitions are queried via `InventoryDataManager`; `SkillCollector` is a stateless source-collection tool.

```csharp
using System.Collections.Generic;
using Ale.Inventory.Runtime;

// ── Collect the skill set to display (four sources; deduplicated, order-preserving) ──
var all      = SkillCollector.Collect(ESkillSource.InventoryDatabase, null, null);
var equipped = SkillCollector.Collect(ESkillSource.Equipment, "equip_player", "技能"); // skills of an equipment group's equipped items
var invSk    = SkillCollector.Collect(ESkillSource.Inventory, "backpack", "技能");     // skills of a warehouse's items
var learned  = SkillCollector.Collect(ESkillSource.Character, "hero_01", null);        // a character's learned skills

// ── A character's learned skills (multi-character, saveable) ──
var sk = SkillRuntimeManager.Instance;
sk.Learn("hero_01", "skill_fireball");                       // learn (ignored if already learned), returns whether it changed
bool has = sk.HasLearned("hero_01", "skill_fireball");
sk.Forget("hero_01", "skill_fireball");                      // forget
IReadOnlyList<string> ids = sk.GetLearnedSkillIds("hero_01"); // learned skill IDs (read-only)
List<Skill> skills        = sk.GetLearnedSkills("hero_01");   // resolved into skill objects
sk.ClearLearned("hero_01");                                  // clear one character

// Events + save
sk.OnLearnedChanged += characterId => { /* refresh skill UI */ };
var save = sk.GetSaveData();     // List<RuntimeLearnedSkillState>, serialized by the game-layer SaveManager
sk.LoadSaveData(save);           // restore on load
sk.ResetAll();                   // clear all characters (e.g. starting a new game)

// ── Query a skill definition + read custom attributes ──
Skill skill    = InventoryDataManager.Instance.GetSkill("skill_fireball");
string effect  = skill.GetAttributeValue<string>("effect");   // String / Text
int    damage  = skill.GetAttributeValue<int>("damage");      // numeric type
```

> **Save contract (1.6.0)**: this manager implements `IInventorySaveable<TState>` — `GetSaveData` returns a deep
> copy, `LoadSaveData` **replaces rather than merges** (entries in memory but absent from the save do not survive),
> and none of the three methods fires a change event (the caller refreshes the UI after a bulk swap). All four
> saveable managers share these semantics — see [Architecture](Architecture_EN.md#subsystem-runtime-managers).

- **`SkillRuntimeManager`**: maintains only the mutable "learned skills" state; everything else (name / attributes, etc.) is read from the skill definition. `Learn` / `Forget` / `ClearLearned` trigger `OnLearnedChanged(characterId)` on change for UI refresh; the save unit is `RuntimeLearnedSkillState` (character ID + skill ID list).
- **`SkillCollector`**: a static tool with no runtime state. `Equipment` iterates the equipped items of each slot in the equipment group, `Inventory` iterates each item in the warehouse, reading the item's `skillRefAttrId` (String / array) to resolve skill IDs; `Character` reads `SkillRuntimeManager.GetLearnedSkills`; `InventoryDatabase` takes all skills.

# Skill UI

The skill UI is under `Runtime/UI/` (assembly `Ale.Inventory.UI`):

| Component | Description |
|------|------|
| `UiwSkillView` | Skill main screen (inherits `UiwViewBase`): title + search bar + **primary / secondary grouping tabs** (each with "All", each reusing a horizontally scrollable `UiwFilterTabBar`) + grid / ordered dual-display-mode toggle + hover detail popup. `Open()` opens with the serialized source config; `Open(source, configId)` switches source and opens. Subscribes to `OnEquipmentChanged` / `OnInventoryChanged` / `OnLearnedChanged` by source for auto-refresh (`InventoryDatabase` is static data, not subscribed) |
| `UiwSkillGridList` / `UiwSkillOrderList` | Skill list (virtual-scroll): inherit the generic `UiwInventoryGridList` / `UiwInventoryOrderList` respectively; `SetSkills` binds skills to `UiwSkillEntry` and pools them for reuse, the grid auto-computes column count by viewport width, the ordered list is single-column, both render only the visible region. The view holds one instance of each, showing one via the toggle button |
| `UiwSkillEntry` | Skill entry (shared by grid / ordered): icon + name + **tier background frame** + optional description / custom attribute field rows; hover pops the detail via `UiwSkillTooltip` |
| `UiwSkillTooltip` | Skill hover popup (implements `ISkillTooltip`): icon + name + **tier name** + description + custom fields configured on the component (`customFieldKeys`, Array-capable). Reuses `UiwItemTooltip`'s fade / cursor positioning / queue. **The prefab is configured on `InventoryRuntimeManager`'s `skillTooltipPrefab` and instantiated once globally by the manager** (reusing `tooltipParent`), invoked via `ShowSkillTooltip` / `HideSkillTooltip` |
| `UiwSkillText` / `SkillRankUtil` | Shared resolution helpers: text resolution for name / description / custom fields (localization first); tier enum-item resolution. Shared by the entry and the Tooltip |

## Display Modes and Filtering

- **Display modes**: the view's toggle button switches between the **grid list** (`UiwSkillGridList`) and the **ordered list** (`UiwSkillOrderList`) (the two instances are stacked, one active; both are virtual-scrolling inside a `ScrollRect`, rendering only the visible region). Without a toggle button, whichever list is configured is used automatically.
- **Primary / secondary grouping tabs** (two AND filter conditions): the **primary tab** filters by a skill's primary group tag, the **secondary tab** by a skill's secondary group tags, and only skills that **satisfy both** are shown (e.g. primary "Warrior", secondary "Attack" → shows the warrior's attack skills). Each tab bar can have a leading "All" (`showAllTab`; selecting "All" means that condition doesn't filter).
  - **Tabs are generated only from tags actually used by skills**: the primary / secondary tabs take the primary / secondary group tags actually configured on the current source's skills (in database group-tag order, deduplicated), avoiding empty tag tabs with no skills.
  - **Horizontally scrollable**: each tab bar is a horizontal `ScrollRect` (`Clamped`) — it doesn't scroll when the total tag width fits, and can be dragged / scrolled horizontally when it exceeds the screen range.
- **Search**: filters by skill name / ID. **When the "All" tab is enabled, typing a search switches both the primary / secondary grouping tabs to "All"** before filtering; when "All" is not enabled, it searches within the currently selected primary / secondary grouping tabs.

## Custom Attribute Fields (Tooltip)

Besides the fixed fields (name / description / icon) and the tier name, the Tooltip can also show a skill's **custom attribute fields**: configure `customFieldKeys` on the component (`string[]`, multiple keys allowed), each non-empty value producing a row; `String` takes the string, `Text` takes the localized text (falling back to plain text if unavailable), other types take the generic display string (`AttributeValue.ToDisplayString()`). The skill entry (detail rows) supports the same.

## Custom Inspector

`UiwSkillView` has a custom Inspector (`UiwSkillViewEditor`): by the current `source`, it **shows only the ID fields that source needs to configure** (`Equipment`→equipment-group ID + `skillRefAttrId`; `Inventory`→warehouse ID + `skillRefAttrId`; `Character`→character ID; `InventoryDatabase`→none), hiding irrelevant fields.

## One-Click Prefab Generation

In the **Welcome Window**'s "Test Tools – Prefab Generation", the "Skill System" category can generate skill prefabs individually or all at once: `PF_UiwSkillCell` (grid entry) / `PF_UiwSkillDetail` (list entry) / `PF_UiwSkillGridList` / `PF_UiwSkillOrderList` / `PF_UiwSkillTooltip` / `PF_UiwSkillView`; the `InventoryManager` prefab instantiates the skill main screen and configures the skill Tooltip prefab on the manager. For prefab authoring and common components, see the [UI Component Guide](UIComponentGuide_EN.md).

> Tip: a skill-entry prefab needs a "tier background frame" `Image` disabled by default (wired to `rankBackground`) to show the background frame by tier; the skill Tooltip must be configured on `InventoryRuntimeManager.skillTooltipPrefab` to pop up on hover; the `Equipment` / `Inventory` sources need the skill reference attribute configured on items by `skillRefAttrId`, and the `Character` source needs the character to have learned skills via `SkillRuntimeManager.Learn` first.
