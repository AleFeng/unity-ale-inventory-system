# Shop System

<p align="center">
  🌍
  <a href="./ShopSystem.md">中文</a> |
  English |
  <a href="./ShopSystem_JA.md">日本語</a>
</p>

- Back to [documentation](../README_EN.md)

The Shop System implements buying and selling on top of the Item / Warehouse systems. A shop is a config catalog (products are entries); the per-player trade progress at runtime (purchase counts, refresh timestamps) is maintained by `ShopRuntimeManager`. Prices are not hard-coded — they are read from an item's `StringIntPair` attribute (currency ID → price), supporting multiple currencies, price multipliers, and periodically refreshed tradeable counts.

# 📜 Table of Contents

- [Shop System](#shop-system)
- [📜 Table of Contents](#-table-of-contents)
- [Core Concepts](#core-concepts)
- [Shop Types](#shop-types)
- [Tab Structure](#tab-structure)
- [Shop Templates (Left Column)](#shop-templates-left-column)
- [Shop Inspector (Right Column)](#shop-inspector-right-column)
- [Price Sources](#price-sources)
- [Trade Warehouses](#trade-warehouses)
- [Product Groups and Products](#product-groups-and-products)
- [Refresh Schedule](#refresh-schedule)
- [Runtime API](#runtime-api)
- [Time Injection](#time-injection)
- [Save and Load](#save-and-load)
- [Shop UI](#shop-ui)

# Core Concepts

| Concept | Description |
|------|------|
| Shop (`Shop`) | A single store: type + trade warehouses + price source + filter tags + several product groups |
| Product group (`ShopCommodityGroup`) | A group of products shown as a tab in the UI, holding a group-level refresh schedule |
| Product (`ShopCommodity`) | Associated with one item ID; describes trade quantity, price multiplier, tradeable count, refresh override |
| Price attribute source | The ID of a `StringIntPair` (currency ID → price) attribute on the item |
| Trade warehouses | The warehouse list used for trading with this shop: tallies currency, receives purchases, sources buy-backs, writes change |

# Shop Types

| Type | Behavior |
|------|------|
| **Sell** | The player buys shop products with currency |
| **Recycle (buy-back)** | The player sells backpack items to the shop for currency (a "trade function tag" can limit which items may be recycled) |
| **Barter** | Both sides exchange by total value (**a placeholder this release**; the trade API returns `NotSupported`) |

All three types require "trade warehouses" to be configured.

# Tab Structure

Click the "**Shop System**" tab at the top of the Inventory Editor. A three-column layout, symmetric with the Warehouse System:

```
Left column: shop templates (list + edit panel)
Middle column: shop list
Right column: Inspector of the selected shop (includes product-group / product editing)
```

# Shop Templates (Left Column)

A shop template defines the default values of the configurable options + custom attribute fields, acting as a blueprint for creating shops (shops and templates share the same config-option drawer).

| Field | Description |
|------|------|
| Name / Color | Template name + list color dot |
| Shop type | Sell / Recycle / Barter |
| Trade warehouses | A list of warehouse IDs (multi-select) |
| Trade function tags | Effective only for Recycle: only recycle items carrying any of these tags; empty = no restriction |
| Filter tags | Presented as function-tag buttons in the UI |
| Show "All" tab | Whether to show "All" in the UI tab bar and select it by default |
| Number format | The name of the referenced number-format config |
| Price attribute source | The StringIntPair item-attribute ID |
| Product groups | The product-group and product list |
| Attribute field list | The template's custom attribute fields |

# Shop Inspector (Right Column)

After selecting a shop, the right column shows the full config, with the same fields as the template, plus:

- **ID**: unique identifier; highlighted when empty or duplicate.
- **Source template**: read-only.
- **Name / Description**: `displayNameText` / `descriptionText` (`Text`: plain-text fallback + optional localization reference; falls back to `id` when the name is empty).
- **Product groups / products**: collapsible + search + grouped-by-item-template dropdown + item-ID validation (referencing a non-existent item raises an error during export validation).

# Price Sources

The price is not hard-coded on the product but read from a matched item's attribute:

1. In the Item System, add a **`StringIntPair` array attribute** to the item (e.g. ID "sale price"), where each element = `currency ID → unit price`, e.g. `gold → 100`, `gem → 1`.
2. In the shop's "Price attribute source", enter that attribute ID (e.g. "sale price").
3. At runtime `ShopRuntimeManager.GetUnitPrice(shop, commodity)` reads that attribute, multiplies each currency pair's price by the product's `priceMultiplier`, sums them, and returns a `currency ID → amount` dictionary.

```csharp
Dictionary<string,int> unit  = ShopRuntimeManager.Instance.GetUnitPrice(shop, commodity);
Dictionary<string,int> total = ShopRuntimeManager.Instance.GetTotalPrice(shop, commodity, times);
```

When there's no price source / the item lacks that attribute, an empty dictionary is returned (treated as free / no gain). Multiple currencies means a single product is priced in several currencies at once. See [Attribute System – StringIntPair](AttributeSystem_EN.md#stringintpair-and-price--currency) for details.

# Trade Warehouses

A shop's `tradeInventoryRefs` is a set of warehouse IDs (ordered). They simultaneously handle:

- **Currency tally**: the player's currency = the total holdings of "currency items" in these warehouses (a currency is simply an item whose id equals the currency ID).
- **Purchase deposit**: items bought from a Sell shop are placed into these warehouses by priority.
- **Recycle source**: a Recycle shop deducts recycled items from these warehouses.
- **Change deposit**: currency generated by a trade is written into these warehouses by priority.

# Product Groups and Products

**Product group (`ShopCommodityGroup`)**: name (UI tab name), description, group-level refresh schedule, product list.

**Product (`ShopCommodity`)**:

| Field | Description |
|------|------|
| `itemId` | The associated item ID |
| `count` | The quantity gained / recycled per trade |
| `priceMultiplier` | Price multiplier (1 = original price; Recycle often uses <1, e.g. 0.5 for half-price buy-back) |
| `tradeLimit` | Tradeable count within each refresh cycle (-1 = unlimited) |
| `overrideRefresh` | Whether to override the group-level refresh schedule and use its own `refresh` |
| `refresh` | Product-level refresh schedule (effective only when `overrideRefresh` is true) |

# Refresh Schedule

A refresh schedule (`ShopRefreshSchedule`) describes on what cycle, by which clock, and at what time point the "tradeable count" resets. The group holds one; a product can override it.

| Field | Description |
|------|------|
| Refresh cycle | None / Daily / Weekly / Monthly |
| Clock type | Game time / Local time / Server time |
| Time zone ID | IANA / Windows time-zone identifier (empty = the clock's own local time zone) |
| Time point | Hour (0-23) + minute (0-59) |
| Day of week | For weekly refresh (0 = Sunday … 6 = Saturday) |
| Day of month | For monthly refresh (1-31; if it exceeds the month's days, the last day is used) |

When "None", `tradeLimit` is a lifetime limit.

# Runtime API

```csharp
using Ale.Inventory.Runtime;

var sm = ShopRuntimeManager.Instance;

// Queries
int owned   = sm.GetOwnedCount(shop, itemId);          // holdings in the trade warehouses (also used to check currency)
int left    = sm.GetRemainingTrades(shop, commodity);  // remaining tradeable count this cycle
int maxBuy  = sm.GetMaxPurchasable(shop, commodity);   // max buyable, constrained by count/currency/capacity
int maxRec  = sm.GetMaxRecyclable(shop, commodity);    // max recyclable

// Trade (automatically reduces the trade count by count / currency / capacity)
ShopTradeResult buy  = sm.Purchase(shopId, commodity, times);   // Sell shop: buy
ShopTradeResult sell = sm.Recycle(shopId, commodity, times);    // Recycle shop: recycle
ShopTradeResult s2   = sm.RecycleItem(shopId, itemId, times);   // Recycle shop: recycle by item ID

// Listen for trade-progress changes (UI refresh)
sm.OnShopChanged += shopId => RefreshShopUI(shopId);
```

`ShopTradeResult` describes the actual trade count / price and the failure reason (e.g. insufficient currency, capacity full, count limit reached, `NotSupported`).

> Like `CraftingRuntimeManager`, `ShopRuntimeManager` is a lightweight singleton: the product catalog comes from registered databases, and only per-player trade progress is created on demand and saved.

# Time Injection

The clock needed for refresh is provided uniformly by `InventoryRuntimeManager`. Game / server time require registering a getter; when unregistered, they fall back to system local time:

```csharp
// The enum members are Chinese identifiers in the source; keep them verbatim.
InventoryRuntimeManager.Instance.RegisterTimeGetter(ShopTimeType.GameTime,   () => GameClock.Now); // 游戏时间 = game time
InventoryRuntimeManager.Instance.RegisterTimeGetter(ShopTimeType.ServerTime, () => NetTime.UtcNow); // 服务器时间 = server time
```

# Save and Load

```csharp
// Save the per-player trade progress
List<ShopRuntimeState> save = ShopRuntimeManager.Instance.GetSaveData();

// Load
ShopRuntimeManager.Instance.LoadSaveData(save);

// Clear all progress (e.g. account switch / restart)
ShopRuntimeManager.Instance.ResetAll();
```

> **Save contract (1.6.0)**: this manager implements `IInventorySaveable<TState>` — `GetSaveData` returns a deep
> copy, `LoadSaveData` **replaces rather than merges** (entries in memory but absent from the save do not survive),
> and none of the three methods fires a change event (the caller refreshes the UI after a bulk swap). All four
> saveable managers share these semantics — see [Architecture](Architecture_EN.md#subsystem-runtime-managers).

> **Progress is keyed by stable `guid` (1.5.0)**: each trade-progress entry is recorded under
> "commodity-group `guid` + commodity `guid`". Both are assigned at creation and never change, so
> **renaming a group, or drag-reordering commodities or groups, no longer misaligns existing saves**.
> Data from 1.4.0 and earlier has no such field; opening the database in the configuration editor
> backfills it automatically (remember to **save the asset**). Entries without a `guid` fall back to
> the legacy key (`group name` + `index-in-group:itemId`), behaving exactly as in 1.4.0.
> Saves written with legacy keys are **migrated in place** the first time that commodity's progress is
> queried, so accumulated trade counts are not lost.

# Shop UI

The shop UI is split by type, sharing the base class `UiwShopViewBase` (`Runtime/UI/View/Shop/`):

| View | Description |
|------|------|
| `UiwSellShopView` | Sell-shop screen: product-group tabs + currency bar + cart-style purchase checkout |
| `UiwRecycleShopView` | Recycle-shop screen: backpack-item recycle checkout |
| `UiwBarterShopView` | Barter screen (placeholder) |

**Filter / sort**: `UiwShopViewBase` supports `UiwFilterTabBar` (filter products by the shop's "filter tags") and `UiwSortToolbar` (sort dropdown + ascending/descending + auto sort); both reuse the list base class's filter / sort pipeline (see [UI Component Guide §7.4](UIComponentGuide_EN.md)). Sort criteria come from the shop's `sortPriorities` / `sortTiebreakers` (sorting; configurable on both `Shop` / `ShopTemplate`, copied when creating a shop from a template).

**Opening and target shop**: `Open(shopId)` records the shop ID then opens; `_shopId` (Inspector "Shop") can preset a default shop — the parameterless `Open()` resolves the shop from `_shopId` and opens, and the editor's `autoOpenOnStart` also uses the current `_shopId`, until changed via `Open(shopId)` or the Inspector. (`ShopId` is now a `protected` property with backing field `_shopId`.)

For prefab authoring and component parameters, see the [UI Component Guide](UIComponentGuide_EN.md).
