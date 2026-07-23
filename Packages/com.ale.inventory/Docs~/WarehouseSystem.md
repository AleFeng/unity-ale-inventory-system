# 仓库系统（Warehouse System）

<p align="center">
  🌍
  中文 |
  <a href="./WarehouseSystem_EN.md">English</a> |
  <a href="./WarehouseSystem_JA.md">日本語</a>
</p>

- 返回 [说明文档](../README.md)

仓库系统定义「容器」：容量、重量上限、可放入 / 取出 / 操作的功能标签限制、过滤标签、整理排序规则。运行时由 `InventoryRuntimeManager` 维护每个仓库的格子列表，并提供增删 / 查询 / 整理 / 存档接口。背包、装备栏、商店货架、制作材料库都用仓库来承载。

# 📜目录

- [仓库系统（Warehouse System）](#仓库系统warehouse-system)
- [📜目录](#目录)
- [页签结构](#页签结构)
- [仓库模板（左侧列）](#仓库模板左侧列)
- [仓库列表（中间列）](#仓库列表中间列)
- [仓库 Inspector（右侧列）](#仓库-inspector右侧列)
- [整理排序](#整理排序)
- [运行时挂载](#运行时挂载)
    - [覆盖式 UI 设置（弹窗 / 幽灵图标）](#覆盖式-ui-设置弹窗--幽灵图标)
    - [编辑器测试道具填充（Play 自动填入）](#编辑器测试道具填充play-自动填入)
- [运行时 API](#运行时-api)
- [RuntimeItemSlot 结构](#runtimeitemslot-结构)
- [存档与读档](#存档与读档)
- [数据来源与加载](#数据来源与加载)
- [背包 UI](#背包-ui)

# 页签结构

在 Inventory Editor 顶部点击「**仓库系统**」页签。三列布局：

```
左列：仓库模板（列表 + 编辑面板）
中列：仓库列表（模板过滤标签 + 搜索 + 拖拽重排）
右列：选中仓库的 Inspector
```

# 仓库模板（左侧列）

仓库模板定义仓库的默认配置，从模板创建的仓库继承这些配置（可在仓库层覆盖）。

| 字段 | 说明 |
|------|------|
| 名称 | 模板唯一名称 |
| 颜色 | 仓库列表中的色点 |
| 容量上限 | 格子数上限（0 = 无限制） |
| 重量上限 | 总重量上限（0 = 无限制） |
| **放入功能标签** | 只允许携带这些标签的道具放入；空 = 不限制 |
| **取出功能标签** | 只允许携带这些标签的道具取出；空 = 不限制 |
| **操作功能标签** | 限制可对仓库内道具执行的操作；空 = 不限制 |
| **过滤标签** | 仓库 UI 中的功能标签过滤按钮（「全部」固定存在无需配置） |
| 自动整理 | 勾选后每次道具变化自动按排序规则整理。**升降序的来源**：界面接了排序整理栏（`UiwSortToolbar`）时统一取排序栏当前方向；没接排序栏时，各条排序条件用自己配置的升 / 降序（1.6.0 修复——此前无排序栏一律按降序） |
| 拖拽排序 | 勾选后允许玩家在 UI 中拖拽调整道具顺序 |
| **整理列表** | 主排序规则；每项选排序字段 + 升降序 |
| **整理优先级** | 次排序规则；主排序值相同时依次比较 |
| 属性字段列表 | 仓库的自定义属性字段（备注、分区说明等） |

# 仓库列表（中间列）

```
[ 全部 ][ 背包 ][ 商店 ][ 装备栏 ]  ← 仓库模板过滤标签栏
[ 🔍 搜索框 ][ 从模板添加 ▾ ][ 快速添加 ]
─────────────────────────────────────
≡ ● 模板名 | ID | 容量 | 重量 | 放入 | 取出 | 操作  ✕
```

| 操作 | 说明 |
|------|------|
| 模板过滤标签栏 | 按仓库模板过滤 |
| 搜索框 | 按仓库 ID / 模板名过滤 |
| 从模板添加 | 创建继承模板配置的新仓库（ID 自动 `inv_N`） |
| 快速添加 | 克隆最后一个仓库 |
| 拖拽 ≡ 句柄 | 调整仓库在数据库中的顺序 |
| 点击行 | 选中，右列显示 Inspector |

# 仓库 Inspector（右侧列）

| 字段 | 说明 |
|------|------|
| ID | 唯一标识；空或重复时高亮 |
| 来源模板 | 只读 |
| 容量上限 / 重量上限 | 覆盖模板值（0 = 无限制） |
| 放入 / 取出 / 操作 / 过滤功能标签 | 多选复选框，覆盖模板值 |
| 自动整理 / 拖拽排序 | Toggle |
| 整理列表 / 整理优先级 | 可拖拽排序的排序规则列表 |
| 属性字段值 | 来自仓库模板定义的自定义属性值 |

# 整理排序

排序由主排序「整理列表」与次排序「整理优先级」两级组成；主排序值相同时依次比较次排序，直到分出先后。

可选排序字段：

| 字段 | 含义 |
|------|------|
| 道具 ID（`__id__`） | 按道具 ID 排序 |
| 标签顺序（`__tagOrder__`） | 按道具首个功能标签在标签列表中的顺序 |
| 任意自定义属性字段 | 按该属性值排序 |

**自定义属性按类型采用不同比较规则**（见 [属性系统 - 排序比较数值](AttributeSystem.md#排序比较数值-tocomparablenumber)）：

- Int / Float / Bool / Enum → 直接比较数值；
- Vector2~4 / Color / VectorInt2~4 → 比较模长（magnitude）；
- StringIntPair → 仅比较其中的 Int 值；
- String → 特殊处理：先按长度、再按字典序。

底层实现：`InventorySortService.CompareSlots` → `CompareByField` → `GetAttrNumeric`（→ `AttributeValue.ToComparableNumber()`）。
（1.6.0 起 `InventoryRuntimeManager` 上的同名 `public static` 兼容转发已移除，请直接调用 `InventorySortService`。）

> 每个「整理选项」带两个内置字段：**名称**（`displayName`，Text：纯文本 fallback + 可选本地化引用，供排序下拉读取显示名）与**忽略ID**（`ignoreIds`，排序时跳过的条目 ID 列表，默认 0 条，可拖拽增删；语义随字段而定——按道具ID排序=道具ID、功能页签=标签名、按属性排序=属性值）。在「仓库系统 → 整理选项」子页签中编辑，运行时经 `SortOption.ResolveDisplayName` / `SortOption.EffectiveIgnoreIds` 读取。旧版把这两项存为通用「属性字段定义」值的数据，首次打开该面板时会自动迁移到内置字段。

# 运行时挂载

在场景新建空 GameObject，挂 `InventoryRuntimeManager`，把 `.asset` 拖入 `databases` 数组。启动时自动把数据库注册到 `InventoryDataManager`，并为每个已定义仓库创建空运行时状态。

```
Hierarchy
└── [InventoryManager]
      └── InventoryRuntimeManager
            databases: [GameDatabase.asset]
```

### 覆盖式 UI 设置（弹窗 / 幽灵图标）

`InventoryRuntimeManager` 的「UI 设置」区可配置**覆盖式 UI**（悬停弹窗、下拉弹窗、拖拽幽灵图标等需盖在所有 UI 之上者）：

- **根节点**（`coverUiRoot`）：覆盖式 UI 的父节点；留空则运行时自动取场景首个 Canvas。
- **强制 Layer**（`applyCoverUiLayer` + `[Layer] coverUiLayer`）：开启后，覆盖式 UI 实例化后会被递归设置到指定 Layer（如 `UI`）。适配「独立 UI 摄像机、Culling Mask 仅渲染 UI 层」的场景——这些 UI 会各自分配独立 Canvas，打断父级 Layer，需重新指定，UI 摄像机方可渲染。也可在代码中用 `SetCoverUiLayer(int)` / `SetCoverUiLayer(string)` / `DisableCoverUiLayer()` 调整，`ApplyCoverUiLayer(GameObject)` 对自建的覆盖式 UI 手动套用。

### 编辑器测试道具填充（Play 自动填入）

`InventoryRuntimeManager` 的「测试功能」区可在进入 Play 模式时（`Init()`，Awake 时机）自动向测试仓库填入道具，**仅填充数据、不打开任何界面**（界面由各视图自行打开）：

- **`autoPopulateOnStart`**（主开关）：是否自动填入。
- **`testInventoryId`**：目标仓库 ID（需与数据库中的 `Inventory.id` 一致）。
- **`testItems`**：逐条指定「道具 ID + 数量」的列表。
- **`addAllConfiguredItems` + `addAllItemCount`**：额外把所有数据库（`databases`）中配置的道具，各按 `addAllItemCount` 的数量补充到测试仓库；已在 `testItems` 中配置的道具会跳过（保留其指定数量、不重复添加），同一道具 ID 跨多个库仅添加一次。同样受主开关 `autoPopulateOnStart` 约束。

> 该区可由 Demo 向导（`Tools > Inventory System`）自动写入示例值。

# 运行时 API

```csharp
using Ale.Inventory.Runtime;

var rm = InventoryRuntimeManager.Instance;

// 添加（返回 false = 容量/重量/标签限制不满足）
bool ok = rm.TryAddItem("backpack", "potion_hp", 3);

// 查询
int  total = rm.GetTotalCount("backpack", "potion_hp");   // 跨格累计
bool has   = rm.HasItem("backpack", "potion_hp", 1);
int  free  = rm.GetFreeSpaceFor("backpack", "potion_hp"); // 还能再放多少
float w    = rm.GetTotalWeight("backpack");
float wMax = rm.GetWeightLimit("backpack");

// 取格子列表（顺序即 UI 显示顺序）。注意：返回值仅供读取——命中时是运行时状态的实时引用，
// 未命中（仓库 ID 不存在）时是全局共享的空列表；需要排序 / 过滤请先自行拷贝一份。
List<RuntimeItemSlot> slots = rm.GetSlots("backpack");

// 移除：按格子 ID（精确） / 按道具 ID（跨格累减）
rm.TryRemoveItem("backpack", slots[0].slotId, 1);
rm.TryRemoveItemById("backpack", "potion_hp", 2);

// 交换两格内容（拖拽排序）
rm.SwapSlotContents("backpack", slotA, slotB);

// 拖拽落点（同道具优先堆叠，堆不下 / 不同道具 / 空槽则交换）——UI 网格拖拽整理使用
rm.StackOrSwapSlots("backpack", srcSlot, targetSlot);

// 整理排序（按仓库定义的规则写入运行时状态）
rm.SortInventory("backpack");

// 监听仓库变化（UI 据此刷新）
rm.OnInventoryChanged += id => RefreshUI(id);
```

> 注意方法名是 `GetTotalCount`（非 `GetTotalQuantity`）。

# RuntimeItemSlot 结构

| 字段 | 类型 | 说明 |
|------|------|------|
| `slotId` | string | 格子唯一 ID（`Guid.NewGuid()`） |
| `itemId` | string | 道具 ID |
| `quantity` | int | 当前格子内数量 |

# 存档与读档

`InventoryRuntimeManager` 提供与游戏存档系统对接的接口：

```csharp
// 保存：取所有仓库状态深拷贝（可序列化）
List<RuntimeInventoryState> save = InventoryRuntimeManager.Instance.GetSaveData();
string json = JsonUtility.ToJson(new SaveWrapper { inventories = save });

// 读取：反序列化后在 Init 完成后调用
var wrapper = JsonUtility.FromJson<SaveWrapper>(json);
InventoryRuntimeManager.Instance.LoadSaveData(wrapper.inventories);

// 开新游戏：清空全部仓库并重建为初始空态
InventoryRuntimeManager.Instance.ResetAll();
```

`RuntimeInventoryState` = `inventoryId` + `slots`（有序格子列表）。

> **`LoadSaveData` 为覆盖语义（1.6.0 起）**：先清空当前内存状态、按数据库重建所有仓库的空骨架
> （固定容量仓库恢复预分配空槽），再叠加存档中的格子。因此——
> **数据库中有、存档中没有**的仓库回到初始空态（不会残留上一局的内容）；
> **存档中有、数据库中没有**的仓库 ID 仍会载入内存（查不到定义，按无限容量处理），不会被丢弃。
> 契约与其余三个运行时管理器一致，见 [架构说明 - 存档契约](Architecture.md#子系统运行时管理器)。

# 数据来源与加载

`InventoryDataManager` 支持三种数据来源（编辑器导出 → 运行时加载）：

| 来源 | 用途 |
|------|------|
| `.asset`（ScriptableObject） | 直接拖入 `InventoryRuntimeManager.databases`，开发期最简单 |
| JSON | 可读文本；对象引用以 AssetGUID 承载，适合调试 |
| 二进制 | 紧凑高效，适合正式发布 |

导出经工具栏「导出 JSON / 导出二进制」。运行时可加载到 `InventoryDataManager` 后注册。序列化与资源解析细节见 [架构说明 - 数据流](Architecture.md#数据流)。

# 背包 UI

`UiwInventoryView`（`Runtime/UI/View/Inventory/`）是背包主界面控制器，组合：多仓库页签、货币栏、虚拟滚动列表、过滤页签栏、排序整理栏。

```csharp
using Ale.Inventory.Runtime.UI;

inventoryView.Open(new[] { "backpack", "stash" });  // 打开并显示这些仓库
inventoryView.Close();
```

> `_inventoryIds` 可在 `UiwInventoryView` 的 Inspector 预设为默认要显示的仓库列表：无参 `Open()` 用该值打开，`Open(inventoryIds)` 则覆盖后打开——设定后视图始终使用该值，直到经 `Open(...)` 或 Inspector 改动。

预制体制作、各组件 Inspector 参数、虚拟列表配置见 [UI 组件指南](UIComponentGuide.md)。
