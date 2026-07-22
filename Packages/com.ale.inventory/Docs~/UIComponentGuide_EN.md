# UI Component and Prefab Authoring Guide

<p align="center">
  🌍
  <a href="./UIComponentGuide.md">中文</a> |
  English |
  <a href="./UIComponentGuide_JA.md">日本語</a>
</p>

- Back to [documentation](../README_EN.md)

This document explains the functionality, Inspector parameters, and prefab authoring of each UI component in the `Ale.Inventory.UI` assembly. It covers the inventory, shop, crafting, and equipment screens plus reusable common components.

> **Namespace**: all UI scripts are declared in `Ale.Inventory.Runtime.UI`; reference them with `using Ale.Inventory.Runtime.UI;`. (The asmdef's `rootNamespace` matches this namespace.)

---

## Table of Contents

1. [Overview and Assembly Setup](#1-overview-and-assembly-setup)
2. [NumberFormatConfig — Number Formatting Config](#2-numberformatconfig--number-formatting-config-built-into-the-database)
3. [UiwInventoryTab — Inventory Tab Button](#3-uiwinventorytab--inventory-tab-button)
4. [UiwInventoryItemSimple — Simple Item Cell](#4-uiwinventoryitemsimple--simple-item-cell)
5. [UiwInventoryItemDetail — Full Item Cell](#5-uiwinventoryitemdetail--full-item-cell)
6. [Virtual-Scroll List — Base + Grid / Ordered](#6-virtual-scroll-list--base--grid--ordered)
7. [Toolbar Components — Currency Bar / Filter Bar / Sort Bar](#7-toolbar-components--currency-bar--filter-bar--sort-bar)
8. [UiwInventoryView — Inventory Main Screen](#8-uiwinventoryview--inventory-main-screen)
9. [Full Scene Setup Example](#9-full-scene-setup-example)
10. [Other System Screens and Common Components](#10-other-system-screens-and-common-components)
11. [FAQ](#11-faq)

---

## 1. Overview and Assembly Setup

### Location and Directory Structure

Scripts are placed by type into subfolders under `Runtime/UI/` (the namespace is uniformly `Ale.Inventory.Runtime.UI`, unaffected by folder):

```
Runtime/UI/
├── Item/      Single cells (UiwInventoryItemBase / SlotBase / Cell / Simple / Detail, UiwShopItemDetail, UiwCraftingBlueprintCell, UiwCraftingInputCell, UiwEquipmentSlot, UiwEquipmentBonusEntry, UiwInventoryItemEvents)
├── ItemList/  Virtual-scroll list family: base UiwInventoryItemListBase + generic UiwInventoryGridList / UiwInventoryOrderList + leaves (warehouse UiwInventoryItemGridList / UiwInventoryItemOrderList, crafting UiwCraftingBlueprintList, skill UiwSkillGridList / UiwSkillOrderList) + GridCellDragHandler + ViewportSizeWatcher (the equipment candidate list is in View/Equipment/, the shop product list in View/Shop/)
├── Tab/       Tabs / filters (UiwInventoryTab, UiwShopGroupTab, UiwFoldTab, UiwFilterTabBar, UiwCraftingGroupFilter)
├── Tool/      Common utility components (UiwCurrencyBar, UiwSortToolbar, UiwItemTooltip, UiwNumberCounter)
├── View/      Main screens: the UiwViewBase base class
│   ├── Inventory/  UiwInventoryView
│   ├── Shop/       UiwShopViewBase + UiwSellShopView / UiwRecycleShopView / UiwBarterShopView
│   ├── Crafting/   UiwCraftingView, UiwCraftingDetail
│   └── Equipment/  UiwEquipmentView, UiwEquipmentGroupPanel, UiwEquipmentSlotList, UiwEquipmentBonusPanel, UiwEquipmentSelectPanel, UiwEquipmentCandidateList, UiwEquipmentDragContext
├── Common/    Common widgets (UiwTextLabel)
└── Ale.Inventory.UI.asmdef   (at the root, automatically covers all subfolders)
```

> The currency bar / sort bar / hover popup / number counter (`Tool/`) and the filter tab bar / fold tab (`Tab/`) are all independent, generic components; each main screen (`UiwInventoryView`, `UiwShopViewBase`, `UiwCraftingView`, all derived from `UiwViewBase`) holds references to them by "composition", reusing them across the inventory / shop / crafting system UIs.

### Assembly

`Ale.Inventory.UI` (`Ale.Inventory.UI.asmdef`)

- References `Ale.Inventory.Runtime` (runtime data and managers)
- References `Unity.TextMeshPro` (toggleable via macro)
- Code namespace: `Ale.Inventory.Runtime.UI`

### TextMeshPro Macro

All text components switch via the compile macro `IS_TMP`:

| Macro state | Text component type |
|--------|------------|
| **Defined** `IS_TMP` | `TMPro.TMP_Text` |
| **Undefined** (default) | `UnityEngine.UI.Text` |

**Enable TMP**: add `IS_TMP` in `Project Settings > Player > Scripting Define Symbols`, and make sure the TextMeshPro package is imported.

> After switching the macro, a recompile is needed, and the reference fields in all prefabs using text components must be reassigned to the corresponding component type.

---

## 2. NumberFormatConfig — Number Formatting Config (Built into the Database)

`NumberFormatConfig` formats large numbers into localized short strings (e.g. `1500 → "1.5K"`, `10000000 → "1000万"`).

> **Important change**: it is **no longer a standalone ScriptableObject asset**, but a set of **named configs** inside `InventoryDatabase` (the `numberFormatConfigs` list). Edit it via the number-format panel on the Inventory Editor's "Warehouse System" tab, and select it from warehouse / warehouse template / shop / blueprint (template) via `numberFormatRef` (referenced by name).

### Data Structure

```
NumberFormatConfig
├── name                Config name (unique in the database, referenced by numberFormatRef)
└── locales: List<NumberFormatLocale>
        ├── languageCode   Language code ("zh-CN"/"en-US"…; empty string = default fallback language)
        └── rules: List<NumberFormatRule>   (by threshold, largest first)
                ├── threshold       The minimum value (inclusive) that triggers this rule
                ├── divisor         Divisor (e.g. 1000 → shrink by a thousand for display)
                ├── suffix          Suffix ("K"/"万"/"M"; optional localized suffix table/key)
                └── decimalPlaces   Decimal places (0 = round to integer)
```

Example effects: Chinese `15000 → "1万"`, `2_0000_0000 → "2.0亿"`; English `15000 → "15.0K"`; when no rule matches, the number is returned as-is.

### Flow: From Config to UI

1. Create a named `NumberFormatConfig` (e.g. `Default`) in the database and configure the rules per language.
2. Fill that name into the `numberFormatRef` of the warehouse / shop / blueprint (or their templates).
3. At runtime, each main screen (`UiwViewBase`-derived) resolves the matching `NumberFormatLocale` by the current language (`ResolveNumberFormatLocale`) and passes it to each cell component.

The format field held by each cell component (`UiwInventoryItemBase`-derived) is
`[HideInInspector] public NumberFormatLocale numberFormat;` — **assigned by the view at runtime, no longer specified manually in the Inspector**. Wherever later sections mention "assign the number format to `numberFormat`", they refer to this runtime flow.

### API

```csharp
// Format directly (by language)
string text = config.Format(value, langCode);
// Or resolve a language's locale first, then format
string t2 = locale.Format(value);
```

---

## 3. UiwInventoryTab — Inventory Tab Button

`UiwInventoryTab` is a lightweight MonoBehaviour responsible for showing the **warehouse ID name** and switching its visual state by whether it's selected. It's instantiated and managed automatically by `UiwInventoryView`, usually requiring no manual driving.

### Making the Prefab

1. Create a UI **Button** node (under a Canvas), named `Prefab_InventoryTab`.
2. Attach the `UiwInventoryTab` component.
3. Prepare a text component (`Text` or `TMP_Text`) in the Button's children and assign the reference to the `label` field.
4. Create a child GameObject inside the Button as the **selection indicator** (e.g. a bottom highlight bar, a color overlay), assign it to the `selectedIndicator` field; `UiwInventoryView` calls `SetActive(isSelected)` on it when switching tabs.

### Inspector Parameters

| Parameter | Description |
|------|------|
| `label` | The text component showing the warehouse ID (`Text` / `TMP_Text`) |
| `selectedIndicator` | The selection-state indicator GameObject (`SetActive(true)` when selected) |

### Public Properties and Methods

| Member | Description |
|------|------|
| `string InventoryId` | The currently bound warehouse ID (read-only) |
| `SetData(inventoryId, displayName, isSelected)` | Called by `UiwInventoryView` / `UiwCraftingView` to refresh the text and selection state |

> **Note**: the Button's `onClick` event is bound dynamically at runtime by `UiwInventoryView.BuildTabs()`; you **don't need** to bind the event yourself in the Prefab.

---

## 4. UiwInventoryItemSimple — Simple Item Cell

Shows only the **icon** and **count**, suited for scenarios like currency item bars that need no detailed info. Instantiated by `UiwInventoryView` in the currency-bar area.

### Making the Prefab

1. Create a UI node, named `Prefab_InventoryItemSimple`.
2. Attach the `UiwInventoryItemSimple` component.
3. Prepare child nodes:
   - **Icon**: an `Image` component → assign to `iconImage`
   - **Count text**: `Text` / `TMP_Text` → assign to `quantityText`
4. The number format needn't be specified on the Prefab: the `numberFormat` field is `[HideInInspector]`, assigned by the main screen at runtime per `numberFormatRef` + the current language (see [§2](#2-numberformatconfig--number-formatting-config-built-into-the-database)); when unassigned, the integer string is shown directly.
5. Set `iconAttrId` to the ID of the icon attribute in the item's static data (default `"图标"` / "icon").

### Inspector Parameters

| Parameter | Default | Description |
|------|--------|------|
| `iconAttrId` | `"图标"` (icon) | The icon attribute ID (the ID of a `Sprite`-typed attribute field) |
| `iconImage` | — | The icon `Image` component reference |
| `quantityText` | — | The count text component reference |
| `numberFormat` | — | `NumberFormatLocale`, `[HideInInspector]`, assigned by the main screen at runtime |

### Public Methods

| Method | Description |
|------|------|
| `SetItem(itemId, quantity)` | Shows the given item and count (auto-queries static data for the icon) |
| `SetEmpty()` | Clears the display |

### Extension

To integrate localized numbers, inherit `UiwInventoryItemSimple` and override:

```csharp
protected override string GetCurrentLanguage() => LocalizationManager.CurrentLanguage;
```

---

## 5. UiwInventoryItemDetail — Full Item Cell

A fully featured item cell component supporting icon, name, description, quality background, count, price, purchased count, plus **hover highlight** and **stack-full animation**. Driven uniformly by virtual-scroll lists such as `UiwInventoryItemOrderList`.

### Making the Prefab

Suggested hierarchy:

```
Prefab_InventoryItemDetail  [UiwInventoryItemDetail]
├── Background              [Image]  quality background
├── Icon                    [Image]  item icon
├── StackFullIcon           [Image]  stack-full icon (initial alpha=0)
├── HoverBorder             [Image]  hover highlight border (initial alpha=0)
├── NameText                [Text / TMP_Text]
├── DescText                [Text / TMP_Text] (optional)
├── QuantityText            [Text / TMP_Text]
├── PriceGroup              [GameObject]  (optional)
│   ├── PriceIconImage      [Image]
│   └── PriceText           [Text / TMP_Text]
└── PurchaseCountText       [Text / TMP_Text] (optional)
```

Steps:
1. Create a UI node and organize the child nodes per the hierarchy above.
2. Attach the `UiwInventoryItemDetail` component.
3. Fill each child node reference into the corresponding Inspector field.
4. Set the attribute field IDs (see the table below) to match the item attribute field IDs in `InventoryDatabase`.
5. The `RectTransform` height is the entry row height (virtual-scroll lists **measure it automatically** from the Prefab's actual size, no need to enter a value), and the anchor type is unrestricted (virtual scrolling auto-overrides position).

### Inspector Parameters

**Attribute field IDs** (corresponding to `AttributeDefinition.id` in the database)

| Parameter | Default | Description |
|------|--------|------|
| `iconAttrId` | `""` | The icon attribute ID (`Sprite` type) |
| `nameAttrId` | `"名称"` (name) | The name attribute ID (`String` type) |
| `descAttrId` | `""` | The description attribute ID (empty = not shown) |
| `qualityAttrId` | `"品质"` (quality) | The quality attribute ID (`Enum` type, the integer value as a `qualitySprites` index) |
| `priceAttrId` | `""` | The price attribute ID (empty = not shown) |
| `currencyItemId` | `""` | The currency item ID (used to show the currency icon next to the price) |
| `purchaseCountAttrId` | `""` | The purchased-count attribute ID (empty = not shown) |

**Display control**

> No display element has an **independent boolean toggle**: whether it shows depends entirely on whether the prefab has the corresponding child component attached
> (name `nameText`, description `descText`, quality background `qualityBackground`, count `countText`,
> price `priceContainer` + `priceCurrencyPrefab`, etc.); if not attached, it doesn't show; the price area is also auto-hidden when the item has no price data.

**Child component references**

| Parameter | Description |
|------|------|
| `iconImage` | The item icon `Image` |
| `nameText` | The name text |
| `descText` | The description text |
| `qualityBackground` | The quality background `Image` |
| `qualitySprites` | The quality background Sprite array, **index = enum integer value** (quality enum value 0 corresponds to `qualitySprites[0]`) |
| `quantityText` | The count text |
| `priceIconImage` | The currency icon `Image` |
| `priceText` | The price text |
| `purchaseCountText` | The purchased-count text |

**Hover highlight**

| Parameter | Default | Description |
|------|------|------|
| `hoverBorder` | — | The border `Image` that fades in on hover (initial `Color.a = 0`) |
| `hoverFadeDuration` | `0.15` | Fade in/out duration (seconds) |

**Stack-full hint**

| Parameter | Default | Description |
|------|------|------|
| `stackFullIcon` | — | The icon `Image` shown in the top-right when the stack is full (initial `Color.a = 0`) |
| `stackFullFadeDuration` | `0.15` | Fade in/out duration (seconds) |

Stack-full condition: the item's `stackLimit > 0` and the current cell's `quantity >= stackLimit`.

**Number format**

| Parameter | Description |
|------|------|
| `numberFormat` | `NumberFormatLocale` (`[HideInInspector]`), controls the count and price display format; assigned by the main screen at runtime |

### Public Methods

| Method | Description |
|------|------|
| `SetSlot(inventoryId, slot)` | Binds to the given slot and refreshes all displays; called by `UiwInventoryItemOrderList` / `UiwInventoryItemGridList` |
| `SetEmpty()` | Clears all displays, hides the GameObject |

### Extension

Inherit this class to override `GetCurrentLanguage()` to integrate the localization system (same as `UiwInventoryItemSimple`).

---

## 6. Virtual-Scroll List — Base + Grid / Ordered

Every list that "displays a large number of entries / items in a list / grid" is built on the **same virtual-scroll engine**: no matter how many entries there are, the screen keeps only a small number of cell instances (= the number of visible cells + `bufferCount` buffer cells on each end), and scrolling only updates positions and data, without dynamically creating / destroying objects. **Both grid and ordered lists are virtual-scrolling.**

### Three-Layer Architecture

```
UiwInventoryItemListBase<TData, TCell>        ← Base: the virtual-scroll engine (object pool + viewport monitoring + recycle/reuse) + an abstract "layout strategy"
   ├─ UiwInventoryGridList<TData, TCell>       ← Generic grid layout (multi-column/row, vertical / horizontal scroll, auto cross-axis count)
   │     ├─ UiwInventoryItemGridList           ← Warehouse grid (RuntimeItemSlot + UiwInventoryItemCell, with drag sorting)
   │     ├─ UiwSkillGridList                    ← Skill grid (Skill + UiwSkillEntry)
   │     └─ UiwEquipmentCandidateList          ← Equipment candidates (no sort dragging, keeps equip dragging)
   └─ UiwInventoryOrderList<TData, TCell>      ← Generic ordered layout (single-column vertical)
         ├─ UiwInventoryItemOrderList          ← Warehouse list (RuntimeItemSlot + UiwInventoryItemDetail)
         ├─ UiwCraftingBlueprintList           ← Crafting blueprint list (+ selection)
         ├─ UiwSkillOrderList                   ← Skill list (Skill + UiwSkillEntry)
         └─ UiwShopCommodityList               ← Shop product list (count stored in the data model, see the shop screen)
```

- The **base** (generic abstract, not attached directly) holds the object pool, viewport-size monitoring, the recycle / reuse loop, and the common entry points `SetItems` / `UpdateItems` / `ScrollToStart`.
- The **generic layer** (generic abstract, not attached directly) provides only the layout strategy: `UiwInventoryOrderList` = single-column vertical; `UiwInventoryGridList` = a 2D grid, split by `scrollDirection` into **vertical** (columns = viewport width ÷ cell width) / **horizontal** (rows = viewport height ÷ cell height), with the cross-axis count auto-recomputed as the viewport size changes.
- The **leaf layer** (non-generic, attached to prefabs) closes the generics `<data type, cell type>`, implements "display one datum into a cell / clear a cell", and integrates with each system's context. To add a list for a **new system**: inherit `UiwInventoryGridList<T,TCell>` or `UiwInventoryOrderList<T,TCell>` and override `BindCell` / `ClearCell` (and optionally `InitCell` / `OnCellAssigned`).

### Making the Prefab

A standard Unity UGUI ScrollView structure; **do not attach `GridLayoutGroup` / `VerticalLayoutGroup` / `ContentSizeFitter` on Content** — cell positions and Content size are taken over by virtual scrolling:

```
Prefab_List  [leaf component, e.g. UiwInventoryItemGridList]
└── ScrollRect      [ScrollRect]
    └── Viewport    [RectTransform] (Mask component)
        └── Content [RectTransform]  ← no LayoutGroup / SizeFitter
```

Steps:
1. Create a UI **ScrollView** (`GameObject > UI > Scroll View`), and delete any LayoutGroup / ContentSizeFitter on Content.
2. Attach the needed leaf component to the root (e.g. warehouse grid `UiwInventoryItemGridList`, warehouse list `UiwInventoryItemOrderList`).
3. Assign the `ScrollRect` and `Content` references to the component's `scrollRect` / `content` fields respectively.
4. Assign the entry Prefab to the `cellPrefab` field (grids use `UiwInventoryItemCell`, lists use `UiwInventoryItemDetail`; cell height / width is auto-measured from the Prefab's `RectTransform`, no manual entry needed).

> **Content anchor**: an ordered list stretches at the top left-to-right (`anchorMin=(0,1), anchorMax=(1,1), pivot=(0.5,1)`), and a vertically scrolling grid the same (a horizontally scrolling one instead stretches top-to-bottom on the left). Virtual scrolling auto-expands the Content size by data volume and positions each cell.

### Inspector Parameters

| Parameter | Layer | Default | Description |
|------|------|------|------|
| `cellPrefab` | Base | — | The entry cell Prefab (the `TCell` component, required); size auto-measured from its `RectTransform` |
| `scrollRect` | Base | — | The owning `ScrollRect` (its viewport is used to measure the visible region and monitor size changes) |
| `content` | Base | — | The Content node's `RectTransform` |
| `bufferCount` | Base | `1` | The number of extra buffer cells kept at each end along the scroll direction (prevents blank flashes during fast scrolling) |
| `spawnPerSecond` | Base | `30` | The max number of cells to **spawn / assign** per second (rate limit); amortizes instantiation and binding (including async icon loading) across frames to avoid single-frame spikes. `≤ 0` = no limit (fill in one frame) |
| `scrollDirection` | Grid | `Vertical` | `Vertical` (cross-axis = columns, columns by viewport width) / `Horizontal` (cross-axis = rows, rows by viewport height) |
| `spacing` / `padding` | Grid | `(6,6)` | Cell spacing / content start padding (pixels) |

### Public Methods (Base)

| Method | Description |
|------|------|
| `SetItems(items)` | Sets the data and redisplays from the **start** (scenarios that return to the top / start, like switching tabs / filter / sort) |
| `UpdateItems(items)` | Incrementally updates the data but **preserves the current scroll position** (content changes don't interrupt the player's scrolling) |
| `RefreshItemsData(items)` | Incremental **diff refresh** (preserves scroll position): when the entry count is unchanged, only rebinds visible cells whose data changed (decided by `NeedsRebind`), leaving unchanged ones untouched — avoiding icon async-reload flicker; warehouse drag-swap / stacking take this path |
| `ScrollToStart()` | Scrolls to the start (vertical = top / horizontal = far left) and refreshes the visible cells |

Each leaf provides domain methods on top of this (e.g. warehouse `SetItemSlotList` / `UpdateItemSlotList` / `SetNumberFormat`, blueprint `SetBlueprints` / `SetSelectedById`, skill `SetSkills`). Usually called by the owning main screen after data changes, requiring no manual work.

### Performance and Experience (Built into the Base)

Beyond "render only the visible region", the engine has three built-in optimizations, all provided uniformly by the base and effective for all leaves:

- **Incremental diff refresh** (`RefreshItemsData` + `NeedsRebind`): when warehouse content changes, **only rebinds visible cells whose "displayed content changed"** (drag-swap / in-place stacking is usually just 2 cells), leaving unchanged cells untouched — avoiding icon async-reload flicker and needless overhead. The decision is based on comparing "the cell's current displayed content" with the new data (item cells use `UiwInventoryItemSlotBase.MatchesSlot` to compare item ID + count). Warehouse drag-swap therefore **no longer resets the scrollbar to the top** (preserving the current scroll position).
- **Spawn / assignment rate limiting** (`spawnPerSecond`, default `30`/sec): **amortizes** cell instantiation and binding (including async icon loading) **across frames**, avoiding single-frame spawn / loading spikes that cause stutter or asset-loading congestion. Instances are **lazily created** on demand up to the target pool limit; the budget has a cap (about 0.1 second's worth), so even "the opening frame" doesn't burst-instantiate, closely following the set rate. `≤ 0` = no limit (fill in one frame, old behavior).
- **Per-cell fade-in following scroll direction**: pending cells appear in the order they **enter the viewport** — scrolling toward the end goes front-to-back (vertical "top-down"), scrolling toward the start goes back-to-front (vertical "bottom-up"); the first open / page switch / full refresh is ascending (top-down).

> **Warehouse grid drag sorting**: `UiwInventoryItemGridList` still supports drag-swap under virtual scrolling — a cell's data index updates dynamically with binding, dragging to the viewport edge auto-scrolls, and the source cell is "pinned" during dragging to avoid being recycled/disabled. Enabled only for warehouses with `dragSort=true` (ordered lists don't support drag sorting, use right-click).

---

## 7. Toolbar Components — Currency Bar / Filter Bar / Sort Bar

The currency bar (`Tool/`), filter tab bar (`Tab/`), and sort bar (`Tool/`) are all **independent generic components**, decoupled from any specific system: the component handles only "display + input events", while data and callbacks are injected by the host screen. `UiwInventoryView`, `UiwShopViewBase`, `UiwCraftingView`, etc. hold references to them by "composition" and subscribe to events, thereby reusing the same toolbar across the system UIs.

### 7.1 UiwCurrencyBar — Currency Bar

The host provides a "currency ID list" and a "get holdings by ID" getter, and the component instantiates currency cells and refreshes.

| Inspector Parameter | Description |
|------|------|
| `currencyContainer` | The parent container for currency cells |
| `currencyPrefab` | The currency cell Prefab (`UiwInventoryItemSimple`) |
| `currencyItemIds` | **The currency item ID list (configured directly on this component)**; can be overridden at runtime by the `Setup` overload with an ids parameter |

| Public Method | Description |
|------|------|
| `Setup(ownedGetter, fmt)` | Builds cells using this component's `currencyItemIds`; `ownedGetter(id)` returns the holdings; `fmt` is the number format |
| `Setup(currencyIds, ownedGetter, fmt)` | Overrides with explicit `currencyIds` (falls back to `currencyItemIds` when null), used by shops, etc., that collect currency dynamically |
| `SetNumberFormat(fmt)` / `Refresh()` / `Clear()` | Update format / re-read holdings and refresh / clear |

> Backpack: getter = the sum of `GetTotalCount` across the opened warehouses; shop: getter = `ShopRuntimeManager.GetOwnedCount(shop, id)`.

### 7.2 UiwFilterTabBar — Filter Tab Bar

Presents filter items as function-tag buttons (with a fixed leading "All"), manages selection highlighting, and calls back the host on change.

| Inspector Parameter | Default | Description |
|------|------|------|
| `filterContainer` | — | The parent container for filter buttons |
| `filterButtonPrefab` | — | The filter `Button` Prefab (with a `Text`/`TMP_Text` child showing the tag name) |
| `allLabel` | `全部` (All) | The "All" button display name |
| `activeColor` / `inactiveColor` | gold / white | The normalColor of selected / unselected buttons |

| Public Member | Description |
|------|------|
| `event OnFilterChanged(string)` | Filter change (the argument is the tag name, `null` = All) |
| `SetFilters(tagNames, selectAll=true)` | Rebuilds buttons, selecting "All" by default and triggering one callback |
| `string ActiveFilter` / `Clear()` | The currently active filter / clear |

### 7.3 UiwSortToolbar — Sort Bar

Aggregates three controls — sort dropdown + ascending/descending toggle + auto sort — driven by event callbacks.

| Inspector Parameter | Default | Description |
|------|------|------|
| `sortDropdown` | — | The sort-criteria dropdown |
| `sortDirectionButton` | — | The ascending/descending toggle button |
| `sortDirectionLabel` | — | The text showing "Ascending" / "Descending" |
| `autoSortButton` | — | The auto-sort button |
| `ascText` / `descText` | `升序` (Ascending) / `降序` (Descending) | Ascending/descending text |

> The dropdown **display name** and the sort **ignore IDs** are not configured on this component, but are built-in fields of the sort option itself (`SortOption.displayName` / `SortOption.ignoreIds`, edited in "Warehouse System → Sort Options"). This component's display name is auto-resolved via `SortOption.ResolveDisplayName`, and the ignore IDs are read by the sort logic via `SortOption.EffectiveIgnoreIds`.

| Public Member | Description |
|------|------|
| `event OnSortChanged(int,bool)` | Sort-criteria / direction change (dropdown index, is ascending) |
| `event OnAutoSort` | Auto-sort button clicked |
| `SetOptions(displayNames)` | Fills the dropdown items (hides the dropdown and toggle button when there are no items) |
| `SetSortPriorities(priorities, db)` | Fills the dropdown with sort criteria (`SortPriority`), display names resolved via the corresponding sort option's built-in `displayName` (`SortOption.ResolveDisplayName`) (a convenience wrapper over `SetOptions`) |
| `int SortIndex` / `bool Ascending` | The current selected index / is ascending |

### 7.4 Filter / Sort Pipeline (Encapsulated in the List Base)

The wiring of `UiwFilterTabBar` + `UiwSortToolbar` is uniformly encapsulated into the virtual-scroll list base `UiwInventoryListBase`; each system view **only needs to attach the two component references to the list and configure once**, without repeating filter / sort code in every view. The pipeline is optional and incremental:

```
source entries → primary/secondary tab filter (filterBar / secondaryFilterBar) → extra filter (SetExtraFilter, e.g. search) → sort (sortToolbar) → display
```

| List Base API | Description |
|------|------|
| `ConfigureFilter(predicate, primaryTokens, secondaryTokens=null, showAll=true)` | Configure the tab filter predicate and tab items |
| `SetExtraFilter(predicate, refresh=true)` | Extra filter (search box / grouping, etc.), layered on top of the tab filter |
| `ConfigureSort(keySelector, db, priorities, tiebreakers, writeRuntime=null)` | Configure sorting: display sort or write runtime sort |
| `SetSourceItems(items, preserveScroll=false)` | Set source data, triggering filter → sort → display |

- **Reuse scope**: the shop product list (`UiwShopCommodityList`), crafting blueprint list, and skill list all use this pipeline; `UiwShopViewBase` directly supports `UiwFilterTabBar` / `UiwSortToolbar`.
- **Backpack exception**: because the backpack couples drag sorting (`dragSort`) with writing runtime sort, etc., it keeps its own filter / sort logic and does not use this pipeline.

---

## 8. UiwInventoryView — Inventory Main Screen

`UiwInventoryView` is the top-level backpack main-screen controller, **composing** the following components and features:

- **Multi-warehouse tab switching** (using the `UiwInventoryTab` Prefab)
- **Currency bar** (the `UiwCurrencyBar` component)
- **Virtual-scroll list** (grid `UiwInventoryItemGridList` / ordered `UiwInventoryItemOrderList`, the view showing one of them)
- **Filter tab bar** (the `UiwFilterTabBar` component)
- **Sort bar** (the `UiwSortToolbar` component: sort dropdown + ascending/descending + auto sort)

### Making the Prefab (Suggested Hierarchy)

```
Prefab_InventoryView  [UiwInventoryView]
│
├── TabContainer      [HorizontalLayoutGroup]   ← tab container
│
├── CurrencyContainer [HorizontalLayoutGroup + UiwCurrencyBar]   ← currency bar component
│
├── ToolbarRow        [HorizontalLayoutGroup]
│   ├── FilterContainer  [HorizontalLayoutGroup + UiwFilterTabBar] ← filter tab bar component
│   └── SortBar          [HorizontalLayoutGroup + UiwSortToolbar]   ← sort bar component
│       ├── SortDropdown     [Dropdown]
│       ├── SortDirButton    [Button → SortDirLabel(Text/TMP_Text)]
│       └── AutoSortButton   [Button]
│
├── ItemOrderList     [UiwInventoryItemOrderList] ← ordered (list) virtual scroll
└── ItemGridList      [UiwInventoryItemGridList]  ← grid virtual scroll (one shown at a time)
```

> The specific child node references of the currency bar / filter bar / sort bar (containers, buttons, dropdowns, text) are wired on **their respective toolbar components** (see the previous section); `UiwInventoryView` only needs to reference these three components themselves.

### Inspector Parameters

**Tabs**

| Parameter | Description |
|------|------|
| `tabContainer` | The parent node of tab buttons (`UiwInventoryTab` is instantiated under it) |
| `tabPrefab` | The `UiwInventoryTab` Prefab |

**Virtual-scroll lists**

| Parameter | Description |
|------|------|
| `itemOrderList` | The ordered (list) virtual-scroll list `UiwInventoryItemOrderList` reference |
| `itemGridList` | The grid virtual-scroll list `UiwInventoryItemGridList` reference (one of it and the ordered list shown by the toggle button) |

**Currency bar**

| Parameter | Description |
|------|------|
| `currencyBar` | The `UiwCurrencyBar` component reference (it handles instantiating and refreshing currency cells; **the currency item IDs are configured on that component**) |

**Filter / sort toolbar**

| Parameter | Description |
|------|------|
| `filterBar` | The `UiwFilterTabBar` component reference (filter tab bar) |
| `sortToolbar` | The `UiwSortToolbar` component reference (sort dropdown + ascending/descending + auto sort; **the dropdown display name / ignore IDs are the sort option's own built-in fields**, edited in "Warehouse System → Sort Options") |

### Public API

```csharp
using Ale.Inventory.Runtime.UI;

// Open the backpack (pass the warehouse ID array to show, with an optional default filter tag)
inventoryView.Open(new[] { "backpack", "stash" }, defaultFilter: null);

// Close the backpack
inventoryView.Close();
```

**`Open` behavior**:
1. Activates the GameObject.
2. Instantiates tabs in `inventoryIds` order, binding the switch events.
3. Instantiates currency item cells via the `currencyBar` component (counting across all opened warehouses; auto-refreshing on warehouse changes).
4. Subscribes to the `InventoryRuntimeManager.OnInventoryChanged` event, auto-refreshing the list on warehouse content changes.
5. Switches to the first tab by default (`SwitchTab(0)`).

**`Close` behavior**: unsubscribes from events, hides the GameObject.

### Sorting Logic Notes

The UI's sort is a **local view sort** (it doesn't modify runtime warehouse data), affecting only the slot order passed into `itemOrderList` / `itemGridList`:

- The user picks the primary sort field in the dropdown;
- The secondary sort (tiebreakers) comes from the current warehouse definition (`Inventory.sortTiebreakers`) and takes effect automatically;
- Ascending/descending is controlled by the toggle button.

For **persistent sorting** (preserved after saving), call `InventoryRuntimeManager.SortInventory` in the game logic layer to write to the runtime state.

---

## 9. Full Scene Setup Example

### Step 1: Configure the Number Format

In the number-format panel of the Inventory Editor's "Warehouse System" tab, create a named `NumberFormatConfig` (e.g. `Default`) and configure the rules; on the warehouse / shop / blueprint (or their template) you want to use it, set `numberFormatRef` to that name. At runtime the main screen resolves it by the current language and passes it down to each cell component (see [§2](#2-numberformatconfig--number-formatting-config-built-into-the-database)).

### Step 2: Make the Prefabs

Make them in the following order (later Prefabs depend on earlier ones):

| Order | Prefab name | Component |
|------|------------|------|
| 1 | `Prefab_InventoryTab` | `UiwInventoryTab` + `Button` |
| 2 | `Prefab_ItemSimple` | `UiwInventoryItemSimple` |
| 3 | `Prefab_ItemDetail` | `UiwInventoryItemDetail` |
| 4 | `Prefab_ItemOrderList` / `Prefab_ItemGridList` | `UiwInventoryItemOrderList` / `UiwInventoryItemGridList` (reference the entry Prefab; Content has no LayoutGroup) |
| 5 | `Prefab_FilterButton` | `Button` (with a text child) |
| 6 | `Prefab_InventoryView` | `UiwInventoryView` (references all the Prefabs above) |

### Step 3: Scene Setup

```
Hierarchy
├── [InventoryManager]
│     └── InventoryRuntimeManager
│           databases: [GameDatabase.asset]
│
└── Canvas
      └── InventoryViewRoot          ← hidden beforehand (SetActive false)
            └── Prefab_InventoryView
                  tabPrefab:        Prefab_InventoryTab
                  tabContainer:     TabContainer
                  itemList:         (points to the Prefab_ItemList instance at the same level)
                  currencyBar:      the UiwCurrencyBar on CurrencyContainer
                  filterBar:        the UiwFilterTabBar on FilterContainer
                  sortToolbar:      the UiwSortToolbar on SortBar

  Each toolbar component itself has its child node references and config wired:
    UiwCurrencyBar  → currencyContainer / currencyPrefab(Prefab_ItemSimple) / currencyItemIds:["gold_coin"]
    UiwFilterTabBar → filterContainer / filterButtonPrefab(Prefab_FilterButton)
    UiwSortToolbar  → sortDropdown / sortDirectionButton / sortDirectionLabel / autoSortButton
```

### Step 4: Open the Backpack

```csharp
using Ale.Inventory.Runtime.UI;
using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    [SerializeField] private UiwInventoryView inventoryView;

    // Open/close the backpack with the B key
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (inventoryView.gameObject.activeSelf)
                inventoryView.Close();
            else
                inventoryView.Open(new[] { "backpack" });
        }
    }
}
```

---

## 10. Other System Screens and Common Components

Sections 2–9 focus on the backpack UI. The shop and crafting screens and other common components are structurally symmetric and reuse the same batch of toolbar components; a quick reference follows; behavioral details are in each subsystem's docs.

### 10.1 Common Base Class and Additional Tool Components

| Component | Subfolder | Description |
|------|------|------|
| `UiwViewBase` | `View/` | The view base class: title, open / close toggling (`Close` / `ToggleOpenClose`), resolving the number format by language. The parameterless `Open()` is a **template method** (with the common step `SetActive(true)`), which subclasses override to implement their own open logic; the parameterized `Open(...)` overload caches the parameters then calls the parameterless `Open()`. The inventory / equipment / shop views expose their target IDs (`inventoryIds` / `groupId` / `shopId`) to the Inspector, allowing preset defaults |
| `UiwItemTooltip` | `Tool/` | The globally unique item hover popup: reuses `UiwInventoryItemDetail` to show content, following the mouse and clamped to the screen |
| `UiwNumberCounter` | `Tool/` | Number counter: +/- stepping + long-press repeat (optional input box), reused by shop count / craft count; event `OnValueChanged`, methods `Configure / SetRange / SetValue / SetInteractable` |
| `UiwFoldTab` | `Tab/` | Generic fold tab: a clickable button + a left icon + right text, used as a plain tab or a collapsible group title |

### 10.2 Shop Screen

`Runtime/UI/View/Shop/`: `UiwShopViewBase` + the type-specialized `UiwSellShopView` (sell), `UiwRecycleShopView` (recycle), `UiwBarterShopView` (placeholder).

- Reuses `UiwCurrencyBar` (currency bar), `UiwFilterTabBar` (filter), `UiwShopGroupTab` (product-group tabs).
- Product cells use `UiwShopItemDetail` (quality background + icon + name / unit price + remaining tradeable count + quantity selector).
- The product list uses the virtual-scroll `UiwShopCommodityList` (attached to the ScrollRect root, wiring `cellPrefab` / `scrollRect` / `content`; Content has no LayoutGroup). **The selected trade count is stored in the data model `ShopCommodityEntry.times` (not on the cell)**: virtual scrolling keeps only visible cells, so the count must be stored in the data model to survive paging / scrolling off-screen; the cart total and checkout are aggregated over **all product data** (`UiwShopViewBase.Entries`), not the visible cells.
- Behavior (price, trade, refresh) — see [Shop System](ShopSystem_EN.md).

### 10.3 Crafting Screen

`Runtime/UI/View/Crafting/`:

| Component | Description |
|------|------|
| `UiwCraftingView` | Main screen: blueprint-template tabs + name search + collapsible group tabs (`UiwCraftingGroupFilter`) + sort bar + blueprint virtual list + detail |
| `UiwCraftingDetail` | Blueprint detail: primary / secondary outputs, input list, craftable count, craft-count selector (`UiwNumberCounter`), craft / stop + progress bar |
| `UiwCraftingBlueprintCell` | Blueprint list entry (primary-output icon + name + attribute display rows) |
| `UiwCraftingInputCell` | Input item row (icon / name / requirement / holdings) |
| `UiwCraftingBlueprintList` | Blueprint virtual-scroll list (inherits `UiwInventoryOrderList`, additionally supporting selection highlight + selection event) |
| `UiwCraftingGroupFilter` | Collapsible group tabs (a primary group unfolds its secondary groups, reusing `UiwFoldTab`) |

Behavior (recipes, crafting warehouses, craftable count) — see [Crafting System](CraftingSystem_EN.md).

### 10.4 Equipment Screen

`Runtime/UI/View/Equipment/` + `Runtime/UI/Item/`:

| Component | Description |
|------|------|
| `UiwEquipmentView` | Equipment main screen: equipment-group panel + bonus panel + selection panel; `Open(groupId)` (warehouses come from the equipment group's "equipment warehouses"); right-click a slot to unequip, left-click to open the selection panel; subscribes to `OnEquipmentChanged` to refresh |
| `UiwEquipmentGroupPanel` | Equipment-group panel: shows the equipment-group name + all slot lists. Two `displayMode` layouts: `Auto` (auto-instantiate each slot list) / `Manual` (manual mode: place slot-list objects in the hierarchy yourself, binding them one by one by slot-list ID via `manualSlotLists`, for free layout). The custom Inspector shows only the fields corresponding to the mode. Can be used standalone by configuring `groupId` + `bindOnStart` |
| `UiwEquipmentSlotList` | Slot list: name + all equipment slots. Two `displayMode` layouts: `Auto` (instantiate each equipment slot under a HorizontalLayoutGroup) / `Manual` (manual mode: place equipment-slot objects in the hierarchy yourself, binding them one by one by slot ID via `manualSlots`). The custom Inspector shows only the fields corresponding to the mode |
| `UiwEquipmentSlot` | Equipment slot (inherits `UiwInventoryItemSlotBase`): shows the equipped item; left / right-click events; drag source (drag out to swap) + drop target (equip / swap) + green/red validity overlay (`selectedIndicator` / `validityOverlay` optional) |
| `UiwEquipmentBonusPanel` / `UiwEquipmentBonusEntry` | Bonus panel: shows total attribute bonuses grouped by group tag |
| `UiwEquipmentSelectPanel` | Equipment selection panel: a switch bar (left/right + name + N/M) + the current slot list + the equippable item list + exit (button / right-click on blank space) |
| `UiwEquipmentCandidateList` | Equippable item list (virtual-scroll grid, inherits `UiwInventoryGridList`): filters across the equipment group's "equipment warehouses" by the current slot list's limits (each cell records its source warehouse); **right-click to quick-equip / left-drag to an equipment slot to equip**. Candidate cells reuse `UiwInventoryItemCell` + `GridCellDragHandler` (doesn't take sort dragging, so the handler drives equip dragging) |
| `GridCellDragHandler` | The item-cell drag relay component (attached to `UiwInventoryItemCell` or its child): when **connected to a grid list** (backpack), forwards to `UiwInventoryItemGridList`, and at drag end decides by the drop point (equipment slot→equip, item cell→reorder); when **not connected to a grid list** (candidate list), drives "drag to an equipment slot to equip" (via `UiwEquipmentDragContext`). Right-click quick-equip is not handled by this component (`UiwInventoryItemCell` broadcasts → `UiwEquipmentView` subscribes) |
| `UiwEquipmentDragContext` | The global equipment-drag context (payload + cursor-following ghost + source icon reset); a non-MonoBehaviour static class |
| `UiwInventoryItemEvents` | A generic static event bus: backpack / detail item cells broadcast a right-click of (warehouse ID, item ID), which `UiwEquipmentView` subscribes to for auto-equip when the equipment screen is open (decoupled from the equipment concept, compatible with grid cells and ordered / detail rows) |

> Prefab tip: an equipment-slot prefab needs a "validity overlay image" disabled by default (wired to `validityOverlay`) to show green/red; the selection panel's root node needs a raycast graphic to support "right-click on blank space to exit"; dragging requires an EventSystem in the scene; backpack grid-cell drag-equip requires that backpack's `dragSort=true` (ordered lists don't support dragging, use right-click).

Behavior (equip / unequip / swap, item limits, attribute bonuses, save) — see [Equipment System](EquipmentSystem_EN.md).

### 10.5 One-Click Generate All Prefabs (Demo Wizard)

Open the **Welcome Window** (`Tools > Inventory System > Welcome Window`) → expand "Test Tools – Prefab Generation":

- "Generate All (Database + All Prefabs)" generates the sample database + all UI prefabs + inventory / shop / crafting / equipment panels + managers in one click (the equipment panel auto-opens "Character Equipment" and attaches the backpack right-click equip bridge);
- The list lets you generate individual prefabs (when generating a dependent prefab it asks whether to generate child prefabs as well, and confirms before overwriting an existing asset).

> The Welcome Window's "Plugin Support" area can also toggle the three macros `IS_TMP` / `IS_LOCALIZATION` / `IS_ADDRESSABLE` in one click, and configure the default TMP font used by the wizard when generating prefabs.

---

## 11. FAQ

**Q: The list is empty after opening the backpack?**  
A: Check that `InventoryRuntimeManager` is in the scene and `databases` is assigned; make sure `InventoryRuntimeManager.Awake` runs before `Open` (script execution order).

**Q: Icons don't show?**  
A: Make sure `iconAttrId` exactly matches the item's icon attribute field ID in the database (case-sensitive); make sure the attribute field type is `Sprite` and the Sprite is assigned in the Inspector.

**Q: The quality background is wrong/blank?**  
A: The `qualitySprites` array's **index** must correspond to the enum integer value; if quality enum values are 0–6, the array needs at least 7 elements (some may be left empty).

**Q: The sort dropdown has no options?**  
A: The `sortPriorities` list in the warehouse definition (`Inventory`) is empty; add at least one sort rule in the warehouse Inspector.

**Q: Text references are lost in prefabs after switching the `IS_TMP` macro?**  
A: Switching the macro changes the field type, so you need to manually re-drag the `TMP_Text` component into fields like `label`, `nameText` in the Prefab Inspector. It's recommended to fix the macro once you've decided on a text approach, and not switch frequently.

**Q: Blank cells appear while scrolling the virtual list?**  
A: Increase `bufferCount` (1–2 recommended); or make sure Content has no `LayoutGroup` / `ContentSizeFitter` (virtual scrolling positions manually, and row height is auto-measured from the Prefab's `RectTransform`, no need to enter it). A brief blank during fast scrolling may also be due to the `spawnPerSecond` rate limit (see the next item).

**Q: Cells "fade in one by one" rather than filling instantly when opening / switching pages / fast scrolling?**  
A: This is the `spawnPerSecond` (default `30`) rate limit taking effect — amortizing cell instantiation and binding (including async icon loading) across frames to avoid single-frame spawn / loading spikes that cause stutter or asset-loading congestion. To fill faster, increase the value; setting `0` (or a negative) means no limit, filling in one frame (old behavior). The fade-in order **follows the scroll direction** (by the order of entering the viewport): scrolling toward the end goes front-to-back (e.g. vertical "top-down"), scrolling toward the start goes back-to-front (e.g. vertical "bottom-up"). Note: a full refresh caused by a count change also fades in cell by cell at this rate (diff refreshes that "only rebind changed cells" like drag-swap / stacking are not rate-limited and complete instantly).

**Q: How do I respond to a user clicking an item cell?**  
A: Add a `Button` component on the `UiwInventoryItemDetail` component and bind the event, or implement it via an `EventSystem` approach in `SetSlot`. You can also inherit `UiwInventoryItemDetail` and override `OnPointerEnter`/`OnPointerExit` or add `IPointerClickHandler` in the subclass.
