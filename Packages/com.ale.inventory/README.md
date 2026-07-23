# 仓库系统（Inventory System）

<p align="center">
  🌍
  中文 |
  <a href="./README_EN.md">English</a> |
  <a href="./README_JA.md">日本語</a>
</p>

面向设计师的 Unity 静态数据配置工具插件。用一个 `InventoryDatabase` 资产集中配置 **道具 / 仓库 / 商店 / 制作 / 装备 / 技能** 六大子系统的静态定义数据；动态运行时状态（拥有数量、实例 ID、交易进度、制作产出、已装备道具、已学技能、存档）由对应的运行时管理器维护。配套一整套开箱即用的运行时 UI 组件（背包 / 商店 / 制作 / 装备 / 技能界面）。

- 编辑器始终且仅在 ScriptableObject 上工作；JSON / 二进制 仅作为单向导出格式。
- 全程支持 Undo / Redo。
- 文本组件、本地化、Addressable 均通过编译宏可选启用。

---

## 子系统概览

![alt text](Docs~/Images/image.png)

| 子系统 | 配置内容 | 运行时管理器 | 详细文档 |
|--------|---------|------------|---------|
| **道具系统** | 枚举类型、功能标签、道具模板、道具 + 灵活属性 | `InventoryDataManager`（查询） | [道具系统](Docs~/ItemSystem.md) |
| **仓库系统** | 仓库模板、仓库、容量/重量/标签限制、整理排序 | `InventoryRuntimeManager`（格子状态 + 存档） | [仓库系统](Docs~/WarehouseSystem.md) |
| **商店系统** | 商店模板、商店、商品组、价格来源、刷新计划 | `ShopRuntimeManager`（交易 + 进度存档） | [商店系统](Docs~/ShopSystem.md) |
| **制作系统** | 分组标签、蓝图模板、蓝图（配方）、制作仓库 | `CraftingRuntimeManager`（消耗 → 产出） | [制作系统](Docs~/CraftingSystem.md) |
| **装备系统** | 分组标签、装备组模板、装备组（槽位列表 / 装备槽 / 道具限制 / 属性加成） | `EquipmentRuntimeManager`（装备 / 卸下 + 加成 + 存档） | [装备系统](Docs~/EquipmentSystem.md) |
| **技能系统** | 分组标签、技能模板、技能（自定义属性承载 类型 / 效果 / 数值 / 位阶 等） | `SkillRuntimeManager`（已学技能状态 + 存档）+ `SkillCollector`（四来源采集） | [技能系统](Docs~/SkillSystem.md) |

### 道具系统
- **灵活属性系统**：字段类型支持 Bool / Int / Float / String / Text（纯文本 fallback + 可选本地化引用）/ Vector2~4 / VectorInt2~4 / Color / Enum / StringIntPair / EnumIntPair / Sprite / Texture / Prefab / Material / AudioClip / AnimationClip / AnimationCurve / PhysicsMaterial(2D)，每种均支持数组形态。
- **自定义枚举类型**：枚举值由系统自动分配（单调递增，永不复用）；可拖拽重排显示顺序；枚举项可携带自定义属性字段。
- **功能标签**：每个标签定义一组属性字段；给道具增删标签会自动增删对应字段；支持把标签锁定到道具模板。
- **道具模板 / 道具列表 / 道具 Inspector**：模板作为创建蓝本；列表支持模板过滤标签栏 + 搜索 + 拖拽重排；Inspector 实时重复 ID 检查、属性按来源分组、枚举子属性自动展开。

### 仓库系统
- **仓库模板 / 仓库实例**：模板定义容量、重量上限、放入/取出/操作功能标签限制、过滤标签、整理排序规则及自定义属性；实例从模板创建并可覆盖。
- **整理排序**：主排序「整理列表」+ 次排序「整理优先级」；排序字段可选 道具 ID / 标签顺序 / 任意自定义属性；属性按 `EFieldType` 采用不同比较规则（数值直接比、向量比模长、StringIntPair 比其 Int 值）。每个排序字段对应一个「整理选项」，带内置「名称」（`Text`：排序下拉显示名）与「忽略ID」（排序时跳过的条目 ID 列表，默认 0 条）。
- **运行时**：`InventoryRuntimeManager` 管理每个仓库的格子列表，提供增删/查询/整理/存档接口与 `OnInventoryChanged` 事件。

### 商店系统
- **商店类型**：售卖 / 回收 / 等价交换（等价交换为占位）。
- **价格来源**：价格不写死，取自道具的 `StringIntPair`（货币 ID → 价格）属性，由「价格属性来源」指定，再乘以商品价格倍率；支持多货币。
- **交易仓库**：每个商店配置一组仓库，用于统计货币、购入落袋、回收来源、找零写入。
- **商品组与刷新**：商品分组（页签）；每组 / 每商品可配刷新计划（不刷新 / 每日 / 每周 / 每月 × 游戏 / 本地 / 服务器时间 + 时间点 / 时区），周期性重置「可交易次数」。

### 制作系统
- **分组标签 / 蓝图模板 / 蓝图**：分组标签用于 UI 分组筛选（每蓝图 1 主 + 多副）；模板定义自定义属性 + 配置默认值 + 模板级整理排序，作为创建蓝图蓝本；蓝图持有配方（产出 / 消耗道具列表）。
- **制作仓库**：有序的仓库 ID 列表，按优先级作为材料来源与产出落点。
- **运行时**：`CraftingRuntimeManager` 计算可制作次数、跨制作仓库扣材料 / 放产出；连续制作的次数、计时、进度由 UI 层驱动。

### 装备系统
- **分组标签 / 装备组模板 / 装备组**：分组标签用于对总属性加成字段分组显示；模板承载一整套可配置项（槽位列表 + 装备属性字段）+ 自定义属性字段，作为创建装备组蓝本（创建时深拷贝、此后独立可编辑）；装备组定义完整槽位结构。
- **槽位列表 / 装备槽 / 道具限制**：装备组含多个槽位列表，每个含多个装备槽；槽位列表以「功能标签 + 枚举约束」限制可装备道具，装备槽再以「过滤条件」（属性等值）进一步收窄。判定采用**全部 AND**。
- **属性加成**：「装备属性字段列表」指定哪些道具属性汇总为装备组总加成，按分组标签分组显示。
- **运行时**：`EquipmentRuntimeManager` 维护各槽已装备道具，装备 / 卸下 / 交换与 `InventoryRuntimeManager` 协作搬运道具，提供自动找槽、加成汇总、存档与 `OnEquipmentChanged` 事件。

### 技能系统
- **分组标签 / 技能模板 / 技能**：分组标签用于运行时 UI 的分组页签筛选（每技能 1 主 + 多副）；模板定义自定义属性字段（schema）+ 一套「技能默认信息」（名称 / 描述 / 图标 / 分组标签），作为创建技能蓝本——「从模板添加」时把默认信息复制进新技能，此后独立可编辑；技能是配置条目，携带 ID / 名称 / 描述 / 图标 + 自定义属性值（技能的类型 / 效果 / 数值 / 位阶等，由使用方在自定义属性字段中自行约定 attrId 承载）。
- **道具 ↔ 技能关联**：技能主要赋予给装备类道具，也可赋予其他道具。道具在其一个「技能引用属性字段」（String，可数组 = 一个道具多技能）中存放技能 ID；运行时按该 attrId 解析。
- **位阶（Enum）驱动显示**：技能可配一个 Enum 类型的「位阶」属性字段；其枚举项（在道具系统「枚举类型」中定义）携带「名称 / 背景框(Sprite)」等自定义属性——技能条目按位阶显示对应「背景框」，Tooltip 显示位阶「名称」（复用道具品质背景的解析链）。相关 attrId 均在 UI 组件上可配。
- **运行时**：`SkillRuntimeManager`（轻量单例）以 **角色 ID → 已学技能 ID 列表** 维护多角色的已学技能，提供 `Learn / Forget / HasLearned / GetLearnedSkills / GetSaveData / LoadSaveData` 接口与 `OnLearnedChanged` 事件；`SkillCollector` 按来源采集要显示的技能集合（去重、保序）。
- **运行时 UI**：`UiwSkillView` = 标题 + 搜索栏 + 主 / 副分组页签（两个 AND 筛选条件，各含「全部」、横向可滚动）+ 网格 / 顺序双显示模式列表 + 悬停详情弹窗（`UiwSkillTooltip`，预制体配到 `InventoryRuntimeManager` 由其全局实例化）；技能来源可切换（见下），`UiwSkillView` 的自定义 Inspector 会按来源只显示对应的 ID 字段。
- **四种技能来源**（`ESkillSource`）：
  - **InventoryDatabase**：数据库全部技能（技能书 / 图鉴）。
  - **Equipment**：某装备组所有装备槽已装备道具引用的技能（配装备组 ID）。
  - **Inventory**：某仓库所有道具引用的技能（配仓库 ID）。
  - **Character**：某角色当前已学会的技能（配角色 ID，读 `SkillRuntimeManager`）。

### 运行时与序列化
- **`InventoryDataManager`**（数据查询单例）：注册数据库、按 ID 查询道具 / 仓库 / 商店 / 蓝图 / 枚举类型等；支持从 `.asset`、JSON、二进制三种来源加载。查询走惰性构建的字典索引（O(1)），注册 / 注销数据库后自动失效重建。
- **`InventoryRuntimeManager`**（MonoBehaviour 单例）：仓库格子状态、整理排序、存档、时间注入入口、覆盖式 UI 根节点 / Layer 配置（弹窗 / 悬停弹窗 / 拖拽幽灵图标等实例化后重新套用指定 Layer），并把数据库注册到 `InventoryDataManager`；含编辑器测试道具填充（`autoPopulateOnStart` / `testInventoryId` / `testItems`，`Init` 时机填入、仅数据不开 UI）与一键「添加所有配置表道具」（`addAllConfiguredItems` + `addAllItemCount`）。
- **`ShopRuntimeManager` / `CraftingRuntimeManager` / `EquipmentRuntimeManager` / `SkillRuntimeManager`**（轻量单例）：交易 / 制作 / 装备 / 技能逻辑（装备已装备状态、技能已学状态均可存档，商店有交易进度存档）；技能展示集合另由 `SkillCollector` 按四种来源采集。
- **导出**：`InventoryDtoMapper` → JSON / 二进制，**覆盖数据库全部 20 个列表**（六大子系统的配置数据无一遗漏，格式版本 v6）；对象引用以 AssetGUID 承载；可选 Addressable 异步加载。v5 及更早导出的 `.bytes` 仍可导入。
- **存档契约**：仓库 / 装备 / 商店 / 技能四个管理器统一实现 `IInventorySaveable<TState>`——`GetSaveData` 返回深拷贝、`LoadSaveData` 为**覆盖而非合并**、三者都不触发变更事件；非泛型的 `IInventorySaveable` 只含 `ResetAll`，供「开新游戏」一次遍历重置。

### UI 组件
位于 `Runtime/UI/`，程序集 `Ale.Inventory.UI`，命名空间 `Ale.Inventory.Runtime.UI`。提供背包 / 商店 / 制作 / 装备 / 技能主界面与可复用的货币栏、过滤栏、排序栏、悬停弹窗、数字计数器、折叠页签等通用组件。各主界面均派生自 `UiwViewBase`：无参 `Open()` 为基类模板方法（激活面板），子类覆写实现各自打开逻辑；背包 / 装备 / 商店视图把目标 ID（`inventoryIds` / `groupId` / `shopId`）暴露到 Inspector，可预设默认值。

- **统一虚拟滚动列表**：所有"显示大量条目 / Item"的列表都建立在同一套虚拟滚动引擎之上（基类 `UiwInventoryItemListBase<TData,TCell>` → 通用 `UiwInventoryGridList` / `UiwInventoryOrderList` → 各系统叶子）。**网格与顺序列表都是虚拟滚动**：只渲染可见区域 + 缓冲、滚动循环复用；网格支持纵向 / 横向两种滚动、跨轴数量按视口自动计算；仓库网格在虚拟滚动下仍支持拖拽整理换位。为新系统加列表只需继承通用网格 / 顺序层、重写"绑定 / 清空格子"。
- **列表性能与体验**（引擎内建）：
  - **增量差异刷新**——内容变化时只重绑数据变化的可见格（拖拽换位 / 堆叠通常仅 2 格），图标不闪烁、滚动位置保留。
  - **生成 / 分配限速**（`spawnPerSecond`，默认 30 个/秒）——把实例化与绑定分摊到多帧，避免单帧峰值卡顿或资源加载堵塞（含预算封顶防"打开界面那一帧"爆发）。
  - **逐格浮现跟随滚动方向**——格子按进入视口的先后出现（下滚从上往下、上滚从下往上）。
- **可复用构件**：页签条 `UiwTabStrip`、子项实例池 `UiwWidgetPool`、悬停弹窗基类 `UiwTooltipBase` /
  `UiwHoverTooltipSource`、图标槽位 `SpriteSlot`、数值 / 价格格式化 `UIFormat`——扩展 UI 时优先复用，
  见 [UI 组件指南 - 可复用构件](Docs~/UIComponentGuide.md#106-可复用构件扩展-ui-时优先复用)。
- 详见 [UI 组件指南](Docs~/UIComponentGuide.md)。

---

## 详细文档

- [属性系统](Docs~/AttributeSystem.md) — 字段类型参考、`AttributeValue` 取值 / 显示 / 排序比较
- [UI 组件指南](Docs~/UIComponentGuide.md) — UI 组件、预制体制作、宏开关、Demo 向导
- [架构说明](Docs~/Architecture.md) — 设计目标、数据流、编辑器与运行时架构、扩展指南

---

## 欢迎窗口（Welcome Window）

![alt text](Docs~/Images/image-1.png)

插件的统一入口面板，集中了「创建数据 / 打开编辑器 / 查看文档 / 生成示例 / 插件宏开关」等常用操作。每次 Unity 会话首次会自动弹出一次，也可随时手动打开：

```
Tools > Inventory System > Welcome Window
```

窗口自上而下分为四个区域：

### 快捷操作

| 按钮 | 说明 |
|------|------|
| 创建新数据文件 | 新建 `InventoryDatabase` 资产（若配置了下方「数据模板」则从模板深拷贝） |
| 打开 Inventory Editor | 打开配置编辑器主窗口 |
| 打开 Addressable工具窗口 | （启用 `IS_ADDRESSABLE` 时）资源引用 Object ↔ AssetReference(GUID) 批量互转 |
| 打开 本地化工具窗口 | （启用 `IS_LOCALIZATION` 时）生成 / 关联多语言表、为所有 Text 字段一键生成中文 Key |
| 查看文档 | 用系统默认程序打开本 README |

展开「**测试工具-预制体生成**」折叠栏：

- **生成全部（数据库 + 全部 Prefab）**：一键生成完整可运行示例（数据库 + 全部 UI 预制体 + 背包 / 商店 / 制作面板 + 管理器）。
- 下方列表可**逐项生成**单个预制体；生成依赖型预制体时会询问是否一并生成子预制体，已存在资产覆盖前会确认。

### 数据模板

指定一个 `InventoryDatabase` 作为模板后，「创建新数据文件」会从该模板深拷贝全部数据（枚举 / 标签 / 模板 / 道具…）；留空则新建为默认空数据。面板会显示模板包含的枚举类型 / 功能标签 / 道具模板 / 道具数量。

### 插件支持（编译宏开关）

逐项开关三个可选宏，并实时检测对应 Package 是否已安装（未安装时勾选会弹确认对话框）：

| 开关 | 宏 | 作用 |
|------|----|------|
| TextMeshPro | `IS_TMP` | 开启后 UI 文本组件使用 `TMP_Text`，否则用 `UnityEngine.UI.Text` |
| Unity Localization | `IS_LOCALIZATION` | 开启后 `Text` 字段可挂本地化引用（表 + 条目）；配合「本地化工具窗口」一键建表 / 生成中文 Key，支持多语言 |
| Unity Addressable | `IS_ADDRESSABLE` | 开启后运行时资源经 Addressable 按需异步加载、引用计数自动卸载；导出时自动登记被引用资源 |

- **TextMeshPro** 开关下可设「默认字体」：向导生成 Prefab 时应用于所有 TMP 文本节点（留空用 TMP 默认字体）。
- **Unity Localization** 开关下可设「本地化字体」：向导生成 Prefab 时赋给 `InventoryTmpFontEvent`（需同时启用 `IS_TMP`）。
- 切换宏后需等待 Unity 重新编译生效。

### 启动时自动显示

窗口底部的「启动时自动显示」开关控制每次 Unity 会话是否自动弹出本窗口。

---

## 依赖

- Unity 2022.3+（`package.json` 声明的最低版本；本插件基于 `Unity 6000.3` 开发与维护）
- TextMeshPro（可选，`IS_TMP` 宏）
- Unity Localization（可选，`IS_LOCALIZATION` 宏）
- Unity Addressables（可选，`IS_ADDRESSABLE` 宏）

> 三个宏均可在 **欢迎窗口**（`Tools > Inventory System > Welcome Window`）的「插件支持」区一键开关，并检测对应包是否已安装。

---

## 快速开始

### 1. 创建数据文件

```
Project 面板右键 > Create > Inventory System > Inventory Database
```

（或在欢迎窗口点击「创建新数据文件」；可在欢迎窗口配置「数据模板」，新建时从模板深拷贝。）

### 2. 打开编辑器

- 选中 `.asset`，在 Inspector 顶部点击「在 Inventory Editor 中编辑」；或
- 菜单 `Tools > Inventory System > Inventory Editor`。

编辑器为顶部系统页签 + 三列布局（左：定义配置 / 中：条目列表 / 右：详细 Inspector）。中间条目列表统一为「列名表头 + 值」两行布局，支持模板过滤 / 搜索、拖拽重排，以及选中后用 ↑ / ↓ 方向键逐行切换选中（越界自动滚动）。

### 3. 配置数据

依次在「道具系统 / 仓库系统 / 商店系统 / 制作系统 / 装备系统」页签中配置。各页签的详细操作见对应子系统文档。

### 4. 导出

工具栏「导出 JSON」或「导出二进制」（存在非空重复 ID 时按钮禁用；空白 ID 条目导出时自动跳过）。

### 5. 运行时挂载

在场景中新建 GameObject，添加 `InventoryRuntimeManager` 组件，将 `.asset` 拖入 `databases` 数组。游戏启动时自动注册数据库并初始化各仓库空状态。

```csharp
using Ale.Inventory.Runtime;

// 查询静态数据
Item item = InventoryDataManager.Instance.GetItem("sword_01");

// 运行时操作仓库
InventoryRuntimeManager.Instance.TryAddItem("backpack", "sword_01", 1);
bool has = InventoryRuntimeManager.Instance.HasItem("backpack", "sword_01");

// 存档 / 读档（LoadSaveData 为覆盖语义：存档中没有的仓库回到初始空态）
var saveData = InventoryRuntimeManager.Instance.GetSaveData();
InventoryRuntimeManager.Instance.LoadSaveData(saveData);

// 开新游戏：清空全部运行时状态（固定容量仓库恢复预分配空槽）
InventoryRuntimeManager.Instance.ResetAll();
```

### 6. 一键 Demo

在 **欢迎窗口** 的「测试工具-预制体生成 → 生成全部」一键生成完整可运行示例（数据库 + 全部 UI 预制体 + 背包 / 商店 / 制作面板 + 管理器）。详见 [欢迎窗口](#欢迎窗口welcome-window) 与 [UI 组件指南](Docs~/UIComponentGuide.md)。

---

## 目录结构

```
InventorySystem/
├── Runtime/
│   ├── Data/           数据模型（Item / Inventory / Shop / Crafting* / AttributeValue 等）
│   ├── Manager/        InventoryDataManager / InventoryRuntimeManager / ShopRuntimeManager / CraftingRuntimeManager / EquipmentRuntimeManager / SkillRuntimeManager / SkillCollector
│   ├── Serialization/  DTO 定义 + 映射 / JSON / 二进制序列化（映射与二进制块按系统分部）
│   ├── Assets/         资源加载抽象（直接加载）
│   ├── Addressables/   Addressable 资源加载支持
│   ├── Localization/   TMP 文本 / 字体本地化事件
│   └── UI/             运行时 UI 组件（Item / ItemList / Tab / Tool / View / Common）
├── Editor/
│   ├── ItemSystem/     道具系统面板
│   ├── InventorySystem/仓库系统面板
│   ├── ShopSystem/     商店系统面板
│   ├── CraftingSystem/ 制作系统面板
│   ├── EquipmentSystem/装备系统面板
│   ├── SkillSystem/    技能系统面板 + UiwSkillView 自定义 Inspector
│   ├── Common/         通用属性 / 配置绘制器 + 工具窗口基类
│   ├── Addressables/   Addressable 资源引用迁移工具窗口
│   ├── Localization/   本地化工具窗口（建表 / 生成中文 Key）
│   ├── Create/         数据文件创建菜单
│   └── DemoWizard/     一键生成测试数据与预制体
├── Resources/Data/     示例数据文件
└── Docs~/              详细文档（本文件夹）
```
