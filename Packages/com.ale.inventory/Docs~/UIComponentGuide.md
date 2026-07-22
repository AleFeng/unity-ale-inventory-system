# UI 组件与预制体制作指南

- 返回 [说明文档](../README.md)

本文档说明 `InventorySystem.UI` 程序集中各 UI 组件的功能、Inspector 参数及预制体制作方法。覆盖背包、商店、制作、装备四套界面与可复用的通用组件。

> **命名空间**：所有 UI 脚本声明在 `InventorySystem.Runtime.UI`，引用时 `using InventorySystem.Runtime.UI;`。（asmdef 的 `rootNamespace` 字段虽写作 `UI.InventorySystem`，但仅影响新建脚本默认值，现有代码以实际声明为准。）

---

## 目录

1. [概述与程序集配置](#1-概述与程序集配置)
2. [NumberFormatConfig — 数字格式化配置](#2-numberformatconfig--数字格式化配置数据库内置)
3. [UiwInventoryTab — 仓库页签按钮](#3-uiwinventorytab--仓库页签按钮)
4. [UiwInventoryItemSimple — 简版道具格子](#4-uiwinventoryitemsimple--简版道具格子)
5. [UiwInventoryItemDetail — 完整道具格子](#5-uiwinventoryitemdetail--完整道具格子)
6. [虚拟滚动列表 — 基类 + 网格 / 顺序](#6-虚拟滚动列表--基类--网格--顺序)
7. [工具栏组件 — 货币栏 / 过滤栏 / 排序栏](#7-工具栏组件--货币栏--过滤栏--排序栏)
8. [UiwInventoryView — 背包主界面](#8-uiwinventoryview--背包主界面)
9. [完整场景搭建示例](#9-完整场景搭建示例)
10. [其他系统界面与通用组件](#10-其他系统界面与通用组件)
11. [常见问题](#11-常见问题)

---

## 1. 概述与程序集配置

### 位置与目录结构

脚本按类型分置于 `Assets/Plugins/InventorySystem/Runtime/UI/` 下的子文件夹（命名空间统一为 `InventorySystem.Runtime.UI`，不随文件夹改变）：

```
Runtime/UI/
├── Item/      单个格子（UiwInventoryItemBase / SlotBase / Cell / Simple / Detail、UiwShopItemDetail、UiwCraftingBlueprintCell、UiwCraftingInputCell、UiwEquipmentSlot、UiwEquipmentBonusEntry、UiwInventoryItemEvents）
├── ItemList/  虚拟滚动列表族：基类 UiwInventoryItemListBase + 通用 UiwInventoryGridList / UiwInventoryOrderList + 叶子（仓库 UiwInventoryItemGridList / UiwInventoryItemOrderList、制作 UiwCraftingBlueprintList、技能 UiwSkillGridList / UiwSkillOrderList）+ GridCellDragHandler + ViewportSizeWatcher（装备候选列表在 View/Equipment/、商店商品列表在 View/Shop/）
├── Tab/       页签 / 过滤（UiwInventoryTab、UiwShopGroupTab、UiwFoldTab、UiwFilterTabBar、UiwCraftingGroupFilter）
├── Tool/      通用工具类组件（UiwCurrencyBar、UiwSortToolbar、UiwItemTooltip、UiwNumberCounter）
├── View/      主界面：UiwViewBase 基类
│   ├── Inventory/  UiwInventoryView
│   ├── Shop/       UiwShopViewBase + UiwSellShopView / UiwRecycleShopView / UiwBarterShopView
│   ├── Crafting/   UiwCraftingView、UiwCraftingDetail
│   └── Equipment/  UiwEquipmentView、UiwEquipmentGroupPanel、UiwEquipmentSlotList、UiwEquipmentBonusPanel、UiwEquipmentSelectPanel、UiwEquipmentCandidateList、UiwEquipmentDragContext
├── Common/    通用小部件（UiwTextLabel）
└── InventorySystem.UI.asmdef   （位于根目录，自动覆盖全部子文件夹）
```

> 货币栏 / 排序整理栏 / 悬停弹窗 / 数字计数器（`Tool/`）与 过滤页签栏 / 折叠页签（`Tab/`）均为独立通用组件；各主界面（`UiwInventoryView`、`UiwShopViewBase`、`UiwCraftingView`，均派生自 `UiwViewBase`）以「组合」方式持有其引用，在背包 / 商店 / 制作各系统 UI 间复用。

### 程序集

`InventorySystem.UI`（`InventorySystem.UI.asmdef`）

- 引用 `InventorySystem.Runtime`（运行时数据与管理器）
- 引用 `Unity.TextMeshPro`（可通过宏开关）
- 代码命名空间：`InventorySystem.Runtime.UI`（asmdef 的 `rootNamespace` 字段虽为 `UI.InventorySystem`，但仅影响新建脚本默认值，以实际声明为准）

### TextMeshPro 宏开关

所有文本组件通过编译宏 `IS_TMP` 切换：

| 宏状态 | 文本组件类型 |
|--------|------------|
| **已定义** `IS_TMP` | `TMPro.TMP_Text` |
| **未定义**（默认） | `UnityEngine.UI.Text` |

**启用 TMP**：在 `Project Settings > Player > Scripting Define Symbols` 中添加 `IS_TMP`，并确保项目已导入 TextMeshPro 包。

> 切换宏后需重新编译，所有使用文本组件的 Prefab 中的引用字段需重新赋值为对应类型的组件。

---

## 2. NumberFormatConfig — 数字格式化配置（数据库内置）

`NumberFormatConfig` 用于将大数值格式化为本地化短字符串（如 `1500 → "1.5K"`、`10000000 → "1000万"`）。

> **重要变更**：它**不再是独立的 ScriptableObject 资产**，而是 `InventoryDatabase` 内的一组**命名配置**（`numberFormatConfigs` 列表）。在 Inventory Editor 的「仓库系统」页签经数字格式面板编辑，并由 仓库 / 仓库模板 / 商店 / 蓝图（模板）通过 `numberFormatRef`（按 name 引用）选用。

### 数据结构

```
NumberFormatConfig
├── name                配置名称（数据库内唯一，供 numberFormatRef 引用）
└── locales: List<NumberFormatLocale>
        ├── languageCode   语言代码（"zh-CN"/"en-US"…；空字符串 = 默认回退语言）
        └── rules: List<NumberFormatRule>   （按 threshold 从大到小）
                ├── threshold       触发此规则的最小数值（含）
                ├── divisor         除数（如 1000 → 缩小千倍显示）
                ├── suffix          后缀（"K"/"万"/"M"；可选本地化后缀表/键）
                └── decimalPlaces   小数位数（0 = 取整）
```

效果示例：中文 `15000 → "1万"`、`2_0000_0000 → "2.0亿"`；英文 `15000 → "15.0K"`；无规则命中时原样返回数字。

### 流程：从配置到 UI

1. 在数据库中建一个命名 `NumberFormatConfig`（如 `Default`），配置各语言规则。
2. 在 仓库 / 商店 / 蓝图（或其模板）的 `numberFormatRef` 填该 name。
3. 运行时各主界面（`UiwViewBase` 派生）按当前语言把命中的 `NumberFormatLocale` 解析出来（`ResolveNumberFormatLocale`），传给各格子组件。

各格子组件（`UiwInventoryItemBase` 派生）持有的格式字段是
`[HideInInspector] public NumberFormatLocale numberFormat;`——**由视图在运行时赋值，Inspector 中不再手动指定**。后续章节凡提到「把数字格式赋给 `numberFormat`」均指此运行时流程。

### API

```csharp
// 直接格式化（按语言）
string text = config.Format(value, langCode);
// 或先解析出某语言的 locale 再格式化
string t2 = locale.Format(value);
```

---

## 3. UiwInventoryTab — 仓库页签按钮

`UiwInventoryTab` 是一个轻量 MonoBehaviour，负责显示**仓库 ID 名称**并根据是否选中切换视觉状态。由 `UiwInventoryView` 自动实例化和管理，通常不需要手动驱动。

### 制作 Prefab

1. 新建 UI **Button** 节点（Canvas 下），命名为 `Prefab_InventoryTab`。
2. 挂载 `UiwInventoryTab` 组件。
3. 在 Button 子节点中准备文本组件（`Text` 或 `TMP_Text`），将引用赋给 `label` 字段。
4. 在 Button 内新建一个子 GameObject 作为**选中指示器**（如底部高亮条、颜色覆盖层），赋给 `selectedIndicator` 字段；`UiwInventoryView` 会在页签切换时调用 `SetActive(isSelected)` 控制其显示。

### Inspector 参数

| 参数 | 说明 |
|------|------|
| `label` | 显示仓库 ID 的文本组件（`Text` / `TMP_Text`） |
| `selectedIndicator` | 选中状态指示器 GameObject（选中时 `SetActive(true)`） |

### 公开属性与方法

| 成员 | 说明 |
|------|------|
| `string InventoryId` | 当前绑定的仓库 ID（只读） |
| `SetData(inventoryId, displayName, isSelected)` | 由 `UiwInventoryView` / `UiwCraftingView` 调用，刷新文本和选中状态 |

> **注意**：Button 的 `onClick` 事件由 `UiwInventoryView.BuildTabs()` 在运行时动态绑定，Prefab 内**无需**自行绑定事件。

---

## 4. UiwInventoryItemSimple — 简版道具格子

仅显示**图标**和**数量**，适用于货币道具栏等无需详细信息的场景。由 `UiwInventoryView` 在货币栏区域实例化。

### 制作 Prefab

1. 新建 UI 节点，命名为 `Prefab_InventoryItemSimple`。
2. 挂载 `UiwInventoryItemSimple` 组件。
3. 准备子节点：
   - **图标**：`Image` 组件 → 赋给 `iconImage`
   - **数量文字**：`Text` / `TMP_Text` → 赋给 `quantityText`
4. 数字格式无需在 Prefab 上指定：`numberFormat` 字段为 `[HideInInspector]`，由主界面运行时按 `numberFormatRef` + 当前语言赋值（见 [§2](#2-numberformatconfig--数字格式化配置数据库内置)）；未赋值时直接显示整数字符串。
5. 设置 `iconAttrId` 为道具静态数据中图标属性的 ID（默认 `"图标"`）。

### Inspector 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `iconAttrId` | `"图标"` | 图标属性 ID（`Sprite` 类型属性字段的 ID） |
| `iconImage` | — | 图标 `Image` 组件引用 |
| `quantityText` | — | 数量文本组件引用 |
| `numberFormat` | — | `NumberFormatLocale`，`[HideInInspector]`，由主界面运行时赋值 |

### 公开方法

| 方法 | 说明 |
|------|------|
| `SetItem(itemId, quantity)` | 显示指定道具和数量（自动查询静态数据获取图标） |
| `SetEmpty()` | 清空显示 |

### 扩展

若需要接入本地化数字，可继承 `UiwInventoryItemSimple` 并重写：

```csharp
protected override string GetCurrentLanguage() => LocalizationManager.CurrentLanguage;
```

---

## 5. UiwInventoryItemDetail — 完整道具格子

功能完整的道具格子组件，支持图标、名称、描述、品质背景、数量、价格、已购数量，以及**悬停高亮**和**堆叠已满动画**。由 `UiwInventoryItemOrderList` 等虚拟滚动列表统一驱动。

### 制作 Prefab

建议层级结构：

```
Prefab_InventoryItemDetail  [UiwInventoryItemDetail]
├── Background              [Image]  品质背景
├── Icon                    [Image]  道具图标
├── StackFullIcon           [Image]  堆叠已满图标（初始 alpha=0）
├── HoverBorder             [Image]  悬停高亮边框（初始 alpha=0）
├── NameText                [Text / TMP_Text]
├── DescText                [Text / TMP_Text]（可选）
├── QuantityText            [Text / TMP_Text]
├── PriceGroup              [GameObject]  （可选）
│   ├── PriceIconImage      [Image]
│   └── PriceText           [Text / TMP_Text]
└── PurchaseCountText       [Text / TMP_Text]（可选）
```

步骤：
1. 新建 UI 节点并按上方层级组织子节点。
2. 挂载 `UiwInventoryItemDetail` 组件。
3. 将各子节点引用填入 Inspector 对应字段。
4. 设置属性字段 ID（见下表），与 `InventoryDatabase` 中道具的属性字段 ID 一致。
5. `RectTransform` 的高度即条目行高（虚拟滚动列表按 Prefab 的实际尺寸**自动测量**，无需另填数值），锚点类型不限（虚拟滚动会自动覆盖位置）。

### Inspector 参数

**属性字段 ID**（与数据库中 `AttributeDefinition.id` 对应）

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `iconAttrId` | `""` | 图标属性 ID（`Sprite` 类型） |
| `nameAttrId` | `"名称"` | 名称属性 ID（`String` 类型） |
| `descAttrId` | `""` | 描述属性 ID（留空则不显示） |
| `qualityAttrId` | `"品质"` | 品质属性 ID（`Enum` 类型，整数值作为 `qualitySprites` 下标） |
| `priceAttrId` | `""` | 价格属性 ID（留空则不显示） |
| `currencyItemId` | `""` | 货币道具 ID（用于显示价格旁的货币图标） |
| `purchaseCountAttrId` | `""` | 已购数量属性 ID（留空则不显示） |

**显示控制**

> 所有显示元素 **没有独立的布尔开关**：是否显示完全取决于预制体是否挂载了对应的子组件
> （名称 `nameText`、描述 `descText`、品质背景 `qualityBackground`、数量 `countText`、
> 价格 `priceContainer` + `priceCurrencyPrefab` 等），未挂载即不显示；价格区在道具无价格数据时也会自动隐藏。

**子组件引用**

| 参数 | 说明 |
|------|------|
| `iconImage` | 道具图标 `Image` |
| `nameText` | 名称文本 |
| `descText` | 描述文本 |
| `qualityBackground` | 品质背景 `Image` |
| `qualitySprites` | 品质背景 Sprite 数组，**下标 = 枚举整数值**（品质枚举值 0 对应 `qualitySprites[0]`） |
| `quantityText` | 数量文本 |
| `priceIconImage` | 货币图标 `Image` |
| `priceText` | 价格文本 |
| `purchaseCountText` | 已购数量文本 |

**悬停高亮**

| 参数 | 默认 | 说明 |
|------|------|------|
| `hoverBorder` | — | 悬停时淡入的边框 `Image`（初始 `Color.a = 0`） |
| `hoverFadeDuration` | `0.15` | 淡入淡出时长（秒） |

**堆叠已满提示**

| 参数 | 默认 | 说明 |
|------|------|------|
| `stackFullIcon` | — | 堆叠已满时右上角显示的图标 `Image`（初始 `Color.a = 0`） |
| `stackFullFadeDuration` | `0.15` | 淡入淡出时长（秒） |

堆叠已满的判断条件：道具的 `stackLimit > 0` 且当前格子 `quantity >= stackLimit`。

**数字格式**

| 参数 | 说明 |
|------|------|
| `numberFormat` | `NumberFormatLocale`（`[HideInInspector]`），控制数量和价格显示格式；由主界面运行时赋值 |

### 公开方法

| 方法 | 说明 |
|------|------|
| `SetSlot(inventoryId, slot)` | 绑定到指定格子并刷新所有显示，由 `UiwInventoryItemOrderList` / `UiwInventoryItemGridList` 调用 |
| `SetEmpty()` | 清空所有显示，隐藏 GameObject |

### 扩展

继承此类可重写 `GetCurrentLanguage()` 接入本地化系统（同 `UiwInventoryItemSimple`）。

---

## 6. 虚拟滚动列表 — 基类 + 网格 / 顺序

所有"以列表 / 网格显示大量条目 / Item"的列表都建立在**同一套虚拟滚动引擎**之上：无论条目多少，屏幕上只维护少量格子实例（= 可见区域格数 + 两端各 `bufferCount` 个缓冲），滚动时只更新位置和数据，不动态创建 / 销毁对象。**网格与顺序列表都是虚拟滚动**。

### 三层架构

```
UiwInventoryItemListBase<TData, TCell>        ← 基类：虚拟滚动引擎（对象池 + 视口监听 + 回收/复用）+ 抽象"布局策略"
   ├─ UiwInventoryGridList<TData, TCell>       ← 通用网格布局（多列/多行，纵向 / 横向滚动，跨轴数量自动）
   │     ├─ UiwInventoryItemGridList           ← 仓库网格（RuntimeItemSlot + UiwInventoryItemCell，含拖拽整理）
   │     ├─ UiwSkillGridList                    ← 技能网格（Skill + UiwSkillEntry）
   │     └─ UiwEquipmentCandidateList          ← 装备候选（无整理拖拽，保装备拖拽）
   └─ UiwInventoryOrderList<TData, TCell>      ← 通用顺序布局（单列纵向）
         ├─ UiwInventoryItemOrderList          ← 仓库列表（RuntimeItemSlot + UiwInventoryItemDetail）
         ├─ UiwCraftingBlueprintList           ← 制作蓝图列表（+ 选中）
         ├─ UiwSkillOrderList                   ← 技能列表（Skill + UiwSkillEntry）
         └─ UiwShopCommodityList               ← 商店商品列表（次数存数据模型，见商店界面）
```

- **基类**（泛型抽象，不直接挂载）持有对象池、视口尺寸监听、回收 / 复用循环，以及公共入口 `SetItems` / `UpdateItems` / `ScrollToStart`。
- **通用层**（泛型抽象，不直接挂载）只提供布局策略：`UiwInventoryOrderList` = 单列纵向；`UiwInventoryGridList` = 二维网格，按 `scrollDirection` 分**纵向**（列数 = 视口宽 ÷ 格子宽）/ **横向**（行数 = 视口高 ÷ 格子高），跨轴数量随视口尺寸自动重算。
- **叶子层**（非泛型，挂在预制体上）闭合泛型 `<数据类型, 格子类型>`，实现"把一条数据显示到格子 / 清空格子"，并对接各系统上下文。为**新系统**加列表时：继承 `UiwInventoryGridList<T,TCell>` 或 `UiwInventoryOrderList<T,TCell>`，重写 `BindCell` / `ClearCell`（及可选 `InitCell` / `OnCellAssigned`）即可。

### 制作 Prefab

标准 Unity UGUI ScrollView 结构；**Content 上不要挂 `GridLayoutGroup` / `VerticalLayoutGroup` / `ContentSizeFitter`**——格子位置与 Content 尺寸由虚拟滚动接管：

```
Prefab_List  [叶子组件，如 UiwInventoryItemGridList]
└── ScrollRect      [ScrollRect]
    └── Viewport    [RectTransform] (Mask 组件)
        └── Content [RectTransform]  ← 无 LayoutGroup / SizeFitter
```

步骤：
1. 新建 UI **ScrollView**（`GameObject > UI > Scroll View`），删除 Content 上的任何 LayoutGroup / ContentSizeFitter。
2. 把所需叶子组件挂到根节点（如背包网格 `UiwInventoryItemGridList`、背包列表 `UiwInventoryItemOrderList`）。
3. 将 `ScrollRect` 与 `Content` 引用分别赋给组件的 `scrollRect` / `content` 字段。
4. 将条目 Prefab 赋给 `cellPrefab` 字段（网格用 `UiwInventoryItemCell`、列表用 `UiwInventoryItemDetail`；格子高 / 宽由 Prefab 的 `RectTransform` 自动测量，无需手填）。

> **Content 锚点**：顺序列表顶部左右拉伸（`anchorMin=(0,1), anchorMax=(1,1), pivot=(0.5,1)`），网格纵向滚动同上（横向滚动则改为左侧上下拉伸）。虚拟滚动据数据量自动撑开 Content 尺寸并逐格定位。

### Inspector 参数

| 参数 | 层级 | 默认 | 说明 |
|------|------|------|------|
| `cellPrefab` | 基类 | — | 条目格子 Prefab（`TCell` 组件，必填）；尺寸从其 `RectTransform` 自动测量 |
| `scrollRect` | 基类 | — | 所属 `ScrollRect`（其 viewport 用于测量可见区域、监听尺寸变化） |
| `content` | 基类 | — | Content 节点的 `RectTransform` |
| `bufferCount` | 基类 | `1` | 视口沿滚动方向两端各额外保留的缓冲格数（防快速滚动露白） |
| `spawnPerSecond` | 基类 | `30` | 每秒最多**生成 / 分配**的格子数（限速）；把实例化与绑定（含图标异步加载）分摊到多帧，避免单帧峰值卡顿。`≤ 0` = 不限速（一帧填满） |
| `scrollDirection` | 网格 | `纵向` | `纵向`（跨轴=列，按视口宽算列数）/ `横向`（跨轴=行，按视口高算行数） |
| `spacing` / `padding` | 网格 | `(6,6)` | 格子间距 / 内容起始内边距（像素） |

### 公开方法（基类）

| 方法 | 说明 |
|------|------|
| `SetItems(items)` | 设置数据并从**起点**重新显示（切换页签 / 过滤 / 排序等回到顶部 / 起始的场景） |
| `UpdateItems(items)` | 增量更新数据但**保留当前滚动位置**（内容变化不打断玩家滚动） |
| `RefreshItemsData(items)` | 增量**差异刷新**（保留滚动位置）：条目数不变时只重绑数据变化的可见格（由 `NeedsRebind` 判定），未变的不动——避免图标异步重载闪烁；仓库拖拽换位 / 堆叠即走此路径 |
| `ScrollToStart()` | 滚动到起点（纵向=顶部 / 横向=最左）并刷新可见格 |

各叶子在此之上提供领域方法（如仓库 `SetItemSlotList` / `UpdateItemSlotList` / `SetNumberFormat`、蓝图 `SetBlueprints` / `SetSelectedById`、技能 `SetSkills`）。通常由所属主界面在数据变化后调用，无需手动。

### 性能与体验（基类内建）

引擎在"只渲染可见区域"之外，还内建三项优化，均由基类统一提供、对所有叶子生效：

- **增量差异刷新**（`RefreshItemsData` + `NeedsRebind`）：仓库内容变化时**只重绑"显示内容已变"的可见格**（拖拽换位 / 就地堆叠通常仅 2 格），未变的格子不动——避免图标异步重载闪烁与无谓开销。判定依据是"格子当前显示内容"与新数据的比较（道具格用 `UiwInventoryItemSlotBase.MatchesSlot` 比 道具 ID + 数量）。仓库拖拽换位因此**不再把滚动条复位到顶部**（保留当前滚动位置）。
- **生成 / 分配限速**（`spawnPerSecond`，默认 `30` 个/秒）：把格子的实例化与绑定（含图标异步加载）**分摊到多帧**，避免单帧一次性生成 / 加载大量格子导致卡顿或资源加载堵塞。实例按需**惰性创建**到目标池上限；预算带封顶（约 0.1 秒的量），使"打开界面那一重帧"也不会爆发实例化，严格贴近设定速率。`≤ 0` = 不限速（一帧填满，旧行为）。
- **逐格浮现跟随滚动方向**：待分配的格子按**进入视口的先后**顺序出现——向末尾滚动从前往后（纵向"从上往下"）、向起点滚动从后往前（纵向"从下往上"）；初次打开 / 切页 / 整表重刷为升序（从上往下）。

> **仓库网格拖拽整理**：`UiwInventoryItemGridList` 在虚拟滚动下仍支持拖拽换位——格子的数据索引随绑定动态更新，拖到视口边缘会自动滚动，拖拽期间"钉住"源格子避免其被回收停用。仅 `dragSort=true` 的仓库启用（顺序列表不支持拖拽整理，用右键）。

---

## 7. 工具栏组件 — 货币栏 / 过滤栏 / 排序栏

货币栏（`Tool/`）、过滤页签栏（`Tab/`）、排序整理栏（`Tool/`）均为**独立通用组件**，与具体系统解耦：组件只负责「显示 + 输入事件」，数据与回调由宿主界面注入。`UiwInventoryView`、`UiwShopViewBase`、`UiwCraftingView` 等以「组合」方式持有其引用并订阅事件，从而在各系统 UI 间复用同一套工具栏。

### 7.1 UiwCurrencyBar — 货币栏

宿主提供「货币 ID 列表」与「按 ID 取持有量」的 getter，组件负责实例化货币格并刷新。

| Inspector 参数 | 说明 |
|------|------|
| `currencyContainer` | 货币格子父容器 |
| `currencyPrefab` | 货币格子 Prefab（`UiwInventoryItemSimple`） |
| `currencyItemIds` | **货币道具 ID 列表（直接在本组件上配置）**；可被带 ids 参数的 `Setup` 重载在运行时覆盖 |

| 公开方法 | 说明 |
|------|------|
| `Setup(ownedGetter, fmt)` | 使用本组件 `currencyItemIds` 建格；`ownedGetter(id)` 返回持有量；`fmt` 为数字格式 |
| `Setup(currencyIds, ownedGetter, fmt)` | 用显式 `currencyIds` 覆盖（为 null 时退回 `currencyItemIds`），供商店等动态收集货币使用 |
| `SetNumberFormat(fmt)` / `Refresh()` / `Clear()` | 更新格式 / 重新读取持有量刷新 / 清空 |

> 背包：getter = 跨已打开仓库的 `GetTotalCount` 求和；商店：getter = `ShopRuntimeManager.GetOwnedCount(shop, id)`。

### 7.2 UiwFilterTabBar — 过滤页签栏

以功能标签按钮呈现过滤项（固定首位「全部」），管理选中高亮，变化时回调宿主。

| Inspector 参数 | 默认 | 说明 |
|------|------|------|
| `filterContainer` | — | 过滤按钮父容器 |
| `filterButtonPrefab` | — | 过滤 `Button` Prefab（含 `Text`/`TMP_Text` 子节点显示标签名） |
| `allLabel` | `全部` | 「全部」按钮显示名 |
| `activeColor` / `inactiveColor` | 金 / 白 | 选中 / 未选中按钮 normalColor |

| 公开成员 | 说明 |
|------|------|
| `event OnFilterChanged(string)` | 过滤变化（参数为标签名，`null` = 全部） |
| `SetFilters(tagNames, selectAll=true)` | 重建按钮，默认选中「全部」并触发一次回调 |
| `string ActiveFilter` / `Clear()` | 当前激活过滤 / 清空 |

### 7.3 UiwSortToolbar — 排序整理栏

聚合 排序下拉 + 升降序切换 + 自动整理 三个控件，事件回调驱动。

| Inspector 参数 | 默认 | 说明 |
|------|------|------|
| `sortDropdown` | — | 排序条件下拉框 |
| `sortDirectionButton` | — | 升降序切换按钮 |
| `sortDirectionLabel` | — | 显示「升序」/「降序」的文本 |
| `autoSortButton` | — | 自动整理按钮 |
| `ascText` / `descText` | `升序` / `降序` | 升降序文本 |

> 下拉**显示名**与排序**忽略 ID** 不在本组件上配置，而是整理选项自身的内置字段（`SortOption.displayName` / `SortOption.ignoreIds`，在「仓库系统 → 整理选项」中编辑）。本组件显示名经 `SortOption.ResolveDisplayName` 自动解析，忽略 ID 由排序逻辑读 `SortOption.EffectiveIgnoreIds`。

| 公开成员 | 说明 |
|------|------|
| `event OnSortChanged(int,bool)` | 排序条件 / 方向变化（下拉下标, 是否升序） |
| `event OnAutoSort` | 自动整理按钮点击 |
| `SetOptions(displayNames)` | 填充下拉项（无项时隐藏下拉与升降序按钮） |
| `SetSortPriorities(priorities, db)` | 用排序条件（`SortPriority`）填充下拉，显示名经对应整理选项的内置 `displayName`（`SortOption.ResolveDisplayName`）解析（`SetOptions` 的便捷封装） |
| `int SortIndex` / `bool Ascending` | 当前选中下标 / 是否升序 |

### 7.4 筛选 / 排序管线（封装在列表基类）

`UiwFilterTabBar` + `UiwSortToolbar` 的接线已统一封装进虚拟滚动列表基类 `UiwInventoryListBase`，各系统视图**只需把两个组件引用挂到列表上并配置一次**，无需在每个视图里重复写筛选 / 排序代码。管线为可选、增量式：

```
源条目 → 主/次页签筛选(filterBar / secondaryFilterBar) → 额外筛选(SetExtraFilter，如搜索) → 排序(sortToolbar) → 显示
```

| 列表基类 API | 说明 |
|------|------|
| `ConfigureFilter(predicate, primaryTokens, secondaryTokens=null, showAll=true)` | 配置页签筛选谓词与页签项 |
| `SetExtraFilter(predicate, refresh=true)` | 额外筛选（搜索框 / 分组等），叠加在页签筛选之上 |
| `ConfigureSort(keySelector, db, priorities, tiebreakers, writeRuntime=null)` | 配置排序：显示排序或写运行时排序 |
| `SetSourceItems(items, preserveScroll=false)` | 设置源数据，触发筛选 → 排序 → 显示 |

- **复用范围**：商店商品列表（`UiwShopCommodityList`）、制作蓝图列表、技能列表均走此管线；`UiwShopViewBase` 直接支持 `UiwFilterTabBar` / `UiwSortToolbar`。
- **背包例外**：背包因拖拽整理（`dragSort`）与写运行时排序等耦合，保留其自有筛选 / 排序逻辑，不走本管线。

---

## 8. UiwInventoryView — 背包主界面

`UiwInventoryView` 是最顶层的背包主界面控制器，**组合**以下组件与功能：

- **多仓库页签切换**（使用 `UiwInventoryTab` Prefab）
- **货币栏**（`UiwCurrencyBar` 组件）
- **虚拟滚动列表**（网格 `UiwInventoryItemGridList` / 顺序 `UiwInventoryItemOrderList`，由视图切换显示其一）
- **过滤页签栏**（`UiwFilterTabBar` 组件）
- **排序整理栏**（`UiwSortToolbar` 组件：排序下拉 + 升降序 + 自动整理）

### 制作 Prefab（推荐层级结构）

```
Prefab_InventoryView  [UiwInventoryView]
│
├── TabContainer      [HorizontalLayoutGroup]   ← 页签容器
│
├── CurrencyContainer [HorizontalLayoutGroup + UiwCurrencyBar]   ← 货币栏组件
│
├── ToolbarRow        [HorizontalLayoutGroup]
│   ├── FilterContainer  [HorizontalLayoutGroup + UiwFilterTabBar] ← 过滤页签栏组件
│   └── SortBar          [HorizontalLayoutGroup + UiwSortToolbar]   ← 排序整理栏组件
│       ├── SortDropdown     [Dropdown]
│       ├── SortDirButton    [Button → SortDirLabel(Text/TMP_Text)]
│       └── AutoSortButton   [Button]
│
├── ItemOrderList     [UiwInventoryItemOrderList] ← 顺序（列表）虚拟滚动
└── ItemGridList      [UiwInventoryItemGridList]  ← 网格虚拟滚动（可切换显示其一）
```

> 货币栏 / 过滤栏 / 排序栏的具体子节点引用（容器、按钮、下拉、文本）连在**各自的工具栏组件**上（见上一节）；`UiwInventoryView` 只需引用这三个组件本身。

### Inspector 参数

**页签**

| 参数 | 说明 |
|------|------|
| `tabContainer` | 页签按钮的父节点（`UiwInventoryTab` 实例化在此下） |
| `tabPrefab` | `UiwInventoryTab` Prefab |

**虚拟滚动列表**

| 参数 | 说明 |
|------|------|
| `itemOrderList` | 顺序（列表）虚拟滚动列表 `UiwInventoryItemOrderList` 引用 |
| `itemGridList` | 网格虚拟滚动列表 `UiwInventoryItemGridList` 引用（与顺序列表由切换按钮显示其一） |

**货币栏**

| 参数 | 说明 |
|------|------|
| `currencyBar` | `UiwCurrencyBar` 组件引用（货币格的实例化与刷新由它负责；**货币道具 ID 在该组件上配置**） |

**过滤 / 排序工具栏**

| 参数 | 说明 |
|------|------|
| `filterBar` | `UiwFilterTabBar` 组件引用（过滤页签栏） |
| `sortToolbar` | `UiwSortToolbar` 组件引用（排序下拉 + 升降序 + 自动整理；**下拉显示名 / 忽略 ID 是整理选项自身的内置字段**，在「仓库系统 → 整理选项」中编辑） |

### 公开 API

```csharp
using InventorySystem.Runtime.UI;

// 打开背包（传入要显示的仓库 ID 数组，可选默认过滤标签）
inventoryView.Open(new[] { "backpack", "stash" }, defaultFilter: null);

// 关闭背包
inventoryView.Close();
```

**`Open` 行为**：
1. 激活 GameObject。
2. 按 `inventoryIds` 顺序实例化页签，绑定切换事件。
3. 通过 `currencyBar` 组件实例化货币道具格子（跨所有打开的仓库统计数量；仓库变化时自动刷新）。
4. 订阅 `InventoryRuntimeManager.OnInventoryChanged` 事件，仓库内容变化时自动刷新列表。
5. 默认切换到第一个页签（`SwitchTab(0)`）。

**`Close` 行为**：取消订阅事件，隐藏 GameObject。

### 排序逻辑说明

UI 的排序为**本地视图排序**（不修改运行时仓库数据），仅影响传入 `itemOrderList` / `itemGridList` 的 slot 顺序：

- 用户在下拉框选择主排序字段；
- 次排序（tiebreakers）来自当前仓库定义（`Inventory.sortTiebreakers`），自动生效；
- 升降序通过切换按钮控制。

若需要**持久化排序**（存档后仍保留），需在游戏逻辑层调用 `InventoryRuntimeManager.SortInventory` 写入运行时状态。

---

## 9. 完整场景搭建示例

### 步骤一：配置数字格式

在 Inventory Editor「仓库系统」页签的数字格式面板中新建一个命名 `NumberFormatConfig`（如 `Default`）并配置规则；在要使用的 仓库 / 商店 / 蓝图（或其模板）上把 `numberFormatRef` 填为该 name。运行时主界面会按当前语言解析并下发给各格子组件（见 [§2](#2-numberformatconfig--数字格式化配置数据库内置)）。

### 步骤二：制作 Prefab

按以下顺序制作（后面的 Prefab 依赖前面的）：

| 顺序 | Prefab 名称 | 组件 |
|------|------------|------|
| 1 | `Prefab_InventoryTab` | `UiwInventoryTab` + `Button` |
| 2 | `Prefab_ItemSimple` | `UiwInventoryItemSimple` |
| 3 | `Prefab_ItemDetail` | `UiwInventoryItemDetail` |
| 4 | `Prefab_ItemOrderList` / `Prefab_ItemGridList` | `UiwInventoryItemOrderList` / `UiwInventoryItemGridList`（引用条目 Prefab；Content 不挂 LayoutGroup） |
| 5 | `Prefab_FilterButton` | `Button`（含文本子节点） |
| 6 | `Prefab_InventoryView` | `UiwInventoryView`（引用上述所有 Prefab） |

### 步骤三：场景挂载

```
Hierarchy
├── [InventoryManager]
│     └── InventoryRuntimeManager
│           databases: [GameDatabase.asset]
│
└── Canvas
      └── InventoryViewRoot          ← 预先隐藏（SetActive false）
            └── Prefab_InventoryView
                  tabPrefab:        Prefab_InventoryTab
                  tabContainer:     TabContainer
                  itemList:         （指向同一层级的 Prefab_ItemList 实例）
                  currencyBar:      CurrencyContainer 上的 UiwCurrencyBar
                  filterBar:        FilterContainer 上的 UiwFilterTabBar
                  sortToolbar:      SortBar 上的 UiwSortToolbar

  其中各工具栏组件自身连好子节点引用与配置：
    UiwCurrencyBar  → currencyContainer / currencyPrefab(Prefab_ItemSimple) / currencyItemIds:["gold_coin"]
    UiwFilterTabBar → filterContainer / filterButtonPrefab(Prefab_FilterButton)
    UiwSortToolbar  → sortDropdown / sortDirectionButton / sortDirectionLabel / autoSortButton
```

### 步骤四：打开背包

```csharp
using InventorySystem.Runtime.UI;
using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    [SerializeField] private UiwInventoryView inventoryView;

    // 按 B 键打开/关闭背包
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

## 10. 其他系统界面与通用组件

第 2–9 节聚焦背包 UI。商店、制作界面与其它通用组件结构对称、复用同一批工具栏组件，下面给出速查；行为细节见各子系统文档。

### 10.1 通用基类与新增工具组件

| 组件 | 子目录 | 说明 |
|------|------|------|
| `UiwViewBase` | `View/` | 视图基类：标题、打开 / 关闭切换（`Close` / `ToggleOpenClose`）、按语言解析数字格式。无参 `Open()` 为**模板方法**（含公共步骤 `SetActive(true)`），子类覆写实现各自打开逻辑；带参 `Open(...)` 重载缓存参数后调无参 `Open()`。背包 / 装备 / 商店视图把目标 ID（`inventoryIds` / `groupId` / `shopId`）暴露到 Inspector，可预设默认 |
| `UiwItemTooltip` | `Tool/` | 全局唯一道具悬停弹窗：复用 `UiwInventoryItemDetail` 显示内容，跟随鼠标并限制在屏幕内 |
| `UiwNumberCounter` | `Tool/` | 数字计数器：+/- 步进 + 长按连发（可选输入框），商店次数 / 制作次数复用；事件 `OnValueChanged`，方法 `Configure / SetRange / SetValue / SetInteractable` |
| `UiwFoldTab` | `Tab/` | 通用折叠页签：可点击按钮 + 左侧图标 + 右侧文本，作普通页签或可折叠分组标题 |

### 10.2 商店界面

`Runtime/UI/View/Shop/`：`UiwShopViewBase` + 按类型分化的 `UiwSellShopView`（售卖）、`UiwRecycleShopView`（回收）、`UiwBarterShopView`（占位）。

- 复用 `UiwCurrencyBar`（货币栏）、`UiwFilterTabBar`（过滤）、`UiwShopGroupTab`（商品组页签）。
- 商品格子用 `UiwShopItemDetail`（品质背景 + 图标 + 名称 / 单价 + 剩余可交易次数 + 数量选择）。
- 商品列表用虚拟滚动 `UiwShopCommodityList`（挂在 ScrollRect 根，接 `cellPrefab` / `scrollRect` / `content`；Content 不挂 LayoutGroup）。**选中的交易次数存于数据模型 `ShopCommodityEntry.times`（不在格子上）**：虚拟滚动只保留可见格，故次数必须存数据模型，翻页 / 滚动离屏后不丢失；购物车总价与结算按**全部商品数据**（`UiwShopViewBase.Entries`）汇总，而非可见格。
- 行为（价格、交易、刷新）见 [商店系统](ShopSystem.md)。

### 10.3 制作界面

`Runtime/UI/View/Crafting/`：

| 组件 | 说明 |
|------|------|
| `UiwCraftingView` | 主界面：蓝图模板页签 + 名称搜索 + 分组折叠页签（`UiwCraftingGroupFilter`）+ 排序整理栏 + 蓝图虚拟列表 + 详情 |
| `UiwCraftingDetail` | 蓝图详情：主 / 副产出、消耗列表、可制作次数、制作次数选择（`UiwNumberCounter`）、制作 / 停止 + 进度条 |
| `UiwCraftingBlueprintCell` | 蓝图列表条目（主产出图标 + 名称 + 属性显示行） |
| `UiwCraftingInputCell` | 消耗道具行（图标 / 名称 / 需求 / 持有） |
| `UiwCraftingBlueprintList` | 蓝图虚拟滚动列表（继承 `UiwInventoryOrderList`，额外支持选中高亮 + 选中事件） |
| `UiwCraftingGroupFilter` | 分组折叠页签（主分组折叠出副分组，复用 `UiwFoldTab`） |

行为（配方、制作仓库、可制作次数）见 [制作系统](CraftingSystem.md)。

### 10.4 装备界面

`Runtime/UI/View/Equipment/` + `Runtime/UI/Item/`：

| 组件 | 说明 |
|------|------|
| `UiwEquipmentView` | 装备主界面：装备组面板 + 属性加成面板 + 装备选择面板；`Open(groupId)`（仓库取自装备组「装备仓库」）；右键装备槽卸下、左键打开选择面板；订阅 `OnEquipmentChanged` 刷新 |
| `UiwEquipmentGroupPanel` | 装备组面板：显示装备组名称 + 全部槽位列表。`displayMode` 两种布局：`Auto`（自动实例化各槽位列表）/ `Manual`（手动模式：层级中自行摆放槽位列表物体，用 `manualSlotLists` 按槽位列表 ID 逐一绑定，自由排版）。自定义 Inspector 按模式只显示对应字段。可配 `groupId` + `bindOnStart` 独立使用 |
| `UiwEquipmentSlotList` | 槽位列表：名称 + 全部装备槽。`displayMode` 两种布局：`Auto`（HorizontalLayoutGroup 下实例化各装备槽）/ `Manual`（手动模式：层级中自行摆放装备槽物体，用 `manualSlots` 按槽位 ID 逐一绑定）。自定义 Inspector 按模式只显示对应字段 |
| `UiwEquipmentSlot` | 装备槽（继承 `UiwInventoryItemSlotBase`）：显示已装备道具；左 / 右键事件；拖拽源（拖出交换）+ 放置目标（装备 / 交换）+ 绿/红有效性叠加（`selectedIndicator` / `validityOverlay` 可选） |
| `UiwEquipmentBonusPanel` / `UiwEquipmentBonusEntry` | 属性加成面板：按分组标签分组显示总属性加成 |
| `UiwEquipmentSelectPanel` | 装备选择面板：切换栏（左右 + 名称 + N/M）+ 当前槽位列表 + 可装备道具列表 + 退出（按钮 / 空白处右键） |
| `UiwEquipmentCandidateList` | 可装备道具列表（虚拟滚动网格，继承 `UiwInventoryGridList`）：跨装备组「装备仓库」按当前槽位列表限制筛选（每格记录来源仓库）；**右键快速装备 / 左键拖拽到装备槽装备**。候选格子复用 `UiwInventoryItemCell` + `GridCellDragHandler`（不接整理拖拽，故 handler 驱动装备拖拽） |
| `GridCellDragHandler` | 道具格子拖拽中转组件（挂在 `UiwInventoryItemCell` 或其子物体上）：**已接入网格列表**（背包）时转发给 `UiwInventoryItemGridList`，结束拖拽时按落点决定（装备槽→装备，道具格子→换位）；**未接入网格列表**（候选列表）时驱动「拖到装备槽装备」（经 `UiwEquipmentDragContext`）。右键快速装备不由本组件处理（`UiwInventoryItemCell` 广播 → `UiwEquipmentView` 订阅）|
| `UiwEquipmentDragContext` | 装备拖拽全局上下文（载荷 + 跟随光标幽灵 + 来源图标复位）；非 MonoBehaviour 静态类 |
| `UiwInventoryItemEvents` | 通用静态事件总线：背包 / 明细道具格子右键广播 (仓库ID, 道具ID)，装备界面打开时由 `UiwEquipmentView` 订阅自动装备（与装备概念解耦，兼容网格格子与顺序 / 明细行） |

> 预制体提示：装备槽预制体加一张默认禁用的「有效性叠加图」（接 `validityOverlay`）方显示绿/红；选择面板根节点需有 raycast 图形才能「右键空白退出」；拖拽需场景内有 EventSystem；背包网格格子拖拽装备需该背包 `dragSort=true`（顺序列表不支持拖拽，用右键）。

行为（装备 / 卸下 / 交换、道具限制、属性加成、存档）见 [装备系统](EquipmentSystem.md)。

### 10.5 一键生成全部预制体（Demo 向导）

打开 **欢迎窗口**（`Tools > Inventory System > Welcome Window`）→ 展开「测试工具-预制体生成」：

- 「生成全部（数据库 + 全部 Prefab）」一键生成示例数据库 + 全部 UI 预制体 + 背包 / 商店 / 制作 / 装备面板 + 管理器（装备面板自动打开「角色装备」、并挂背包右键装备桥接）；
- 列表中可逐项生成单个预制体（生成依赖型预制体时会询问是否一并生成子预制体，已存在资产覆盖前确认）。

> 欢迎窗口的「插件支持」区还可一键开关 `IS_TMP` / `IS_LOCALIZATION` / `IS_ADDRESSABLE` 三个宏，并配置向导生成 Prefab 时使用的默认 TMP 字体。

---

## 11. 常见问题

**Q：打开背包后列表为空？**  
A：检查 `InventoryRuntimeManager` 是否已在场景中且 `databases` 已赋值；确认 `InventoryRuntimeManager.Awake` 在 `Open` 之前执行（脚本执行顺序）。

**Q：图标不显示？**  
A：确认 `iconAttrId` 与数据库中道具的图标属性字段 ID 完全一致（区分大小写）；确认属性字段类型为 `Sprite`，且 Sprite 已在 Inspector 中赋值。

**Q：品质背景显示错误/空白？**  
A：`qualitySprites` 数组的**下标**必须与枚举整数值对应；若品质枚举值为 0–6，数组至少需要 7 个元素（可以部分留空）。

**Q：排序下拉框不出现选项？**  
A：仓库定义（`Inventory`）中的 `sortPriorities` 列表为空；在仓库 Inspector 中添加至少一条排序规则。

**Q：`IS_TMP` 宏切换后 Prefab 中文本引用丢失？**  
A：宏切换导致字段类型变化，需在 Prefab Inspector 中手动将 `label`、`nameText` 等字段重新拖入 `TMP_Text` 组件。建议确定文本方案后固定宏，不频繁切换。

**Q：虚拟列表滚动时出现空白格？**  
A：增大 `bufferCount`（建议 1–2）；或确认 Content 未挂 `LayoutGroup` / `ContentSizeFitter`（虚拟滚动手动定位，行高按 Prefab 的 `RectTransform` 自动测量，无需另填）。快速滚动时短暂露白也可能是 `spawnPerSecond` 限速所致（见下条）。

**Q：打开 / 切页 / 快速滚动时格子"逐格浮现"而非瞬间铺满？**  
A：这是 `spawnPerSecond`（默认 `30`）限速生效——把格子的实例化与绑定（含图标异步加载）分摊到多帧，避免单帧一次性生成 / 加载大量格子导致卡顿或资源加载堵塞。想更快铺满可调大该值；填 `0`（或负数）则不限速、一帧填满（旧行为）。逐格浮现的顺序**跟随滚动方向**（按进入视口的先后）：向末尾滚动从前往后（如纵向"从上往下"）、向起点滚动从后往前（如纵向"从下往上"）。注意：数量增减导致的整表重刷也会以此速度逐格浮现（拖拽换位 / 堆叠等"只重绑变化格"的差异刷新不受限速影响、即时完成）。

**Q：如何响应用户点击某个道具格子？**  
A：在 `UiwInventoryItemDetail` 组件上添加 `Button` 组件并绑定事件，或在 `SetSlot` 中通过 `EventSystem` 方案实现。也可继承 `UiwInventoryItemDetail`，在子类中覆写 `OnPointerEnter`/`OnPointerExit` 或添加 `IPointerClickHandler`。
