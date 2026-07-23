# 商店系统（Shop System）

<p align="center">
  🌍
  中文 |
  <a href="./ShopSystem_EN.md">English</a> |
  <a href="./ShopSystem_JA.md">日本語</a>
</p>

- 返回 [说明文档](../README.md)

商店系统在道具 / 仓库之上实现买卖。商店是配置目录（商品为条目），运行时的每玩家交易进度（已购次数、刷新时间戳）由 `ShopRuntimeManager` 维护。价格不写死——取自道具的 `StringIntPair` 属性（货币 ID → 价格），支持多货币、价格倍率、周期性刷新可交易次数。

# 📜目录

- [商店系统（Shop System）](#商店系统shop-system)
- [📜目录](#目录)
- [核心概念](#核心概念)
- [商店类型](#商店类型)
- [页签结构](#页签结构)
- [商店模板（左侧列）](#商店模板左侧列)
- [商店 Inspector（右侧列）](#商店-inspector右侧列)
- [价格来源](#价格来源)
- [交易仓库](#交易仓库)
- [商品组与商品](#商品组与商品)
- [刷新计划](#刷新计划)
- [运行时 API](#运行时-api)
- [时间注入](#时间注入)
- [存档与读档](#存档与读档)
- [商店 UI](#商店-ui)

# 核心概念

| 概念 | 说明 |
|------|------|
| 商店（`Shop`） | 一个店铺：类型 + 交易仓库 + 价格来源 + 过滤标签 + 若干商品组 |
| 商品组（`ShopCommodityGroup`） | UI 中以页签分组的一组商品，持组级刷新计划 |
| 商品（`ShopCommodity`） | 关联一个道具 ID，描述交易数量、价格倍率、可交易次数、刷新覆盖 |
| 价格属性来源 | 道具上一个 `StringIntPair`（货币 ID → 价格）属性的 ID |
| 交易仓库 | 与本店交易使用的仓库列表：统计货币、购入落袋、回收来源、找零写入 |

# 商店类型

| 类型 | 行为 |
|------|------|
| **售卖（Sell）** | 玩家用货币购买商店商品 |
| **回收（Recycle）** | 玩家把背包道具出售给商店换取货币（可用「交易功能标签」限定哪些道具可回收） |
| **等价交换（Barter）** | 双方按总价值互换（**本期占位**，交易接口返回 `NotSupported`） |

三种类型都需配置「交易仓库」。

# 页签结构

在 Inventory Editor 顶部点击「**商店系统**」页签。三列布局，与仓库系统对称：

```
左列：商店模板（列表 + 编辑面板）
中列：商店列表
右列：选中商店的 Inspector（含商品组 / 商品编辑）
```

# 商店模板（左侧列）

商店模板定义可配置项的默认值 + 自定义属性字段，作为创建商店的蓝本（商店与模板共享同一套配置项绘制）。

| 字段 | 说明 |
|------|------|
| 名称 / 颜色 | 模板名 + 列表色点 |
| 商店类型 | 售卖 / 回收 / 等价交换 |
| 交易仓库 | 仓库 ID 列表（可多选） |
| 交易功能标签 | 仅回收生效：只回收含其中任一标签的道具；空 = 不限制 |
| 过滤标签 | UI 中以功能标签按钮呈现 |
| 显示「全部」页签 | 是否在 UI 页签栏显示「全部」并默认选中 |
| 数字格式 | 引用的数字格式配置名称 |
| 价格属性来源 | StringIntPair 道具属性 ID |
| 商品组 | 商品组与商品列表 |
| 属性字段列表 | 模板自定义属性字段 |

# 商店 Inspector（右侧列）

选中商店后右列显示完整配置，字段与模板一致，外加：

- **ID**：唯一标识；空或重复时高亮提示。
- **来源模板**：只读。
- **名称 / 描述**：`displayNameText` / `descriptionText`（`Text`：纯文本 fallback + 可选本地化引用；名称空时退回 `id`）。
- **商品组 / 商品**：折叠 + 搜索 + 按道具模板分组下拉 + 道具 ID 校验（引用不存在的道具会在导出校验时报错）。

# 价格来源

价格不在商品上写死，而是从命中道具的属性读取：

1. 在道具系统给道具加一个 **`StringIntPair` 数组属性**（例如 ID 为「售价」），每个元素 = `货币ID → 单价`，如 `金币 → 100`、`钻石 → 1`。
2. 在商店「价格属性来源」填该属性 ID（如「售价」）。
3. 运行时 `ShopRuntimeManager.GetUnitPrice(shop, commodity)` 读取该属性，每个货币对的价格乘以商品 `priceMultiplier` 后汇总，返回 `货币ID → 金额` 字典。

```csharp
Dictionary<string,int> unit  = ShopRuntimeManager.Instance.GetUnitPrice(shop, commodity);
Dictionary<string,int> total = ShopRuntimeManager.Instance.GetTotalPrice(shop, commodity, times);
```

无价格来源 / 道具无该属性时返回空字典（视为免费 / 无收益）。多货币即一件商品同时标多种货币价格。详见 [属性系统 - StringIntPair](AttributeSystem.md#stringintpair-与价格--货币)。

# 交易仓库

商店的 `tradeInventoryRefs` 是一组仓库 ID（有序）。它们同时承担：

- **货币统计**：玩家货币 = 这些仓库中「货币道具」的总持有量（货币就是 id 等于货币 ID 的道具）。
- **购入落袋**：售卖店买到的道具按优先级放入这些仓库。
- **回收来源**：回收店从这些仓库扣除被回收的道具。
- **找零写入**：交易产生的货币按优先级写入这些仓库。

# 商品组与商品

**商品组（`ShopCommodityGroup`）**：name（UI 页签名）、description、组级刷新计划、商品列表。

**商品（`ShopCommodity`）**：

| 字段 | 说明 |
|------|------|
| `itemId` | 关联道具 ID |
| `count` | 每次交易获得 / 回收的数量 |
| `priceMultiplier` | 价格倍率（1 = 原价；回收常用 <1，如 0.5 半价回收） |
| `tradeLimit` | 每个刷新周期内可交易次数（-1 = 无限） |
| `overrideRefresh` | 是否覆盖组级刷新计划，使用自己的 `refresh` |
| `refresh` | 商品级刷新计划（仅 `overrideRefresh` 为 true 时生效） |

# 刷新计划

刷新计划（`ShopRefreshSchedule`）描述「可交易次数」按何种周期、依据哪种时钟、在何时间点重置。组级持有一份，商品可覆盖。

| 字段 | 说明 |
|------|------|
| 刷新周期 | 不刷新 / 每日 / 每周 / 每月 |
| 时钟类型 | 游戏时间 / 本地时间 / 服务器时间 |
| 时区 ID | IANA / Windows 时区标识（空 = 时钟自身本地时区） |
| 时间点 | 小时（0-23）+ 分钟（0-59） |
| 星期几 | 每周刷新用（0 = 周日 … 6 = 周六） |
| 几号 | 每月刷新用（1-31；超出当月天数取最后一天） |

「不刷新」时 `tradeLimit` 即终身上限。

# 运行时 API

```csharp
using Ale.Inventory.Runtime;

var sm = ShopRuntimeManager.Instance;

// 查询
int owned   = sm.GetOwnedCount(shop, itemId);          // 交易仓库中持有量（也用于查货币）
int left    = sm.GetRemainingTrades(shop, commodity);  // 本周期剩余可交易次数
int maxBuy  = sm.GetMaxPurchasable(shop, commodity);   // 受 次数/货币/容量 约束的最大购买数
int maxRec  = sm.GetMaxRecyclable(shop, commodity);    // 最大可回收数

// 交易（自动按 次数 / 货币 / 容量 下调成交次数）
ShopTradeResult buy  = sm.Purchase(shopId, commodity, times);   // 售卖店：购买
ShopTradeResult sell = sm.Recycle(shopId, commodity, times);    // 回收店：回收
ShopTradeResult s2   = sm.RecycleItem(shopId, itemId, times);   // 回收店：按道具 ID 回收

// 监听交易进度变化（UI 刷新）
sm.OnShopChanged += shopId => RefreshShopUI(shopId);
```

`ShopTradeResult` 描述实际成交次数 / 价格与失败原因（如货币不足、容量已满、达次数上限、`NotSupported`）。

> `ShopRuntimeManager` 与 `CraftingRuntimeManager` 一样是轻量单例：商品目录来自已注册数据库，仅每玩家交易进度按需创建并存档。

# 时间注入

刷新所需的时钟由 `InventoryRuntimeManager` 统一提供。游戏 / 服务器时间需注册获取器，未注册时回退系统本地时间：

```csharp
InventoryRuntimeManager.Instance.RegisterTimeGetter(ShopTimeType.游戏时间,   () => GameClock.Now);
InventoryRuntimeManager.Instance.RegisterTimeGetter(ShopTimeType.服务器时间, () => NetTime.UtcNow);
```

# 存档与读档

```csharp
// 保存每玩家交易进度
List<ShopRuntimeState> save = ShopRuntimeManager.Instance.GetSaveData();

// 读取
ShopRuntimeManager.Instance.LoadSaveData(save);

// 清空全部进度（如换号 / 重开）
ShopRuntimeManager.Instance.ResetAll();
```

# 商店 UI

商店 UI 按类型拆分，共享基类 `UiwShopViewBase`（`Runtime/UI/View/Shop/`）：

| 视图 | 说明 |
|------|------|
| `UiwSellShopView` | 售卖店界面：商品组页签 + 货币栏 + 购物车式购买结算 |
| `UiwRecycleShopView` | 回收店界面：背包道具回收结算 |
| `UiwBarterShopView` | 等价交换界面（占位） |

**筛选 / 排序**：`UiwShopViewBase` 支持 `UiwFilterTabBar`（按商店「过滤标签」筛选商品）与 `UiwSortToolbar`（排序下拉 + 升降序 + 自动整理），二者接线复用列表基类的筛选 / 排序管线（见 [UI 组件指南 §7.4](UIComponentGuide.md)）。排序条件取自商店的 `sortPriorities` / `sortTiebreakers`（整理排序，`Shop` / `ShopTemplate` 均可配，从模板创建商店时复制）。

**打开与目标商店**：`Open(shopId)` 记录商店 ID 后打开；`_shopId`（Inspector「商店」）可预设默认商店——无参 `Open()` 由 `_shopId` 解析商店并打开，编辑器 `autoOpenOnStart` 亦用当前 `_shopId`，直到经 `Open(shopId)` 或 Inspector 改动。（`ShopId` 现为 `protected` 属性，backing 字段 `_shopId`。）

预制体制作与组件参数见 [UI 组件指南](UIComponentGuide.md)。
