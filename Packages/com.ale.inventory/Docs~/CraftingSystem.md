# 制作系统（Crafting System）

- 返回 [说明文档](../README.md)

制作系统用「蓝图（配方）」描述「消耗材料 → 产出道具」。蓝图引用道具系统的道具 ID，从制作仓库扣材料、向制作仓库放产出。蓝图是配置目录；运行时的制作动作由 `CraftingRuntimeManager` 执行，连续制作的次数 / 计时 / 进度由 UI 层驱动。

# 📜目录

- [核心概念](#核心概念)
- [页签结构](#页签结构)
- [分组标签](#分组标签)
- [蓝图模板](#蓝图模板)
- [蓝图（中间列）](#蓝图中间列)
- [蓝图 Inspector（右侧列）](#蓝图-inspector右侧列)
- [配方：产出与消耗](#配方产出与消耗)
- [制作仓库](#制作仓库)
- [属性字段显示](#属性字段显示)
- [可制作次数与连续制作次数](#可制作次数与连续制作次数)
- [运行时 API](#运行时-api)
- [制作 UI](#制作-ui)

# 核心概念

| 概念 | 说明 |
|------|------|
| 分组标签（`CraftingGroupTag`） | 对蓝图分组，便于 UI 筛选；每蓝图 1 个主分组 + 多个副分组 |
| 蓝图模板（`CraftingBlueprintTemplate`） | 自定义属性字段 + 配置默认值 + 模板级整理排序，作为创建蓝图蓝本，也用于分类（防具 / 武器 / 食品 …） |
| 蓝图（`CraftingBlueprint`） | 一条配方：产出 / 消耗道具列表 + 制作时间 / 次数 + 制作仓库 + UI 配置 |
| 制作仓库 | 有序仓库 ID 列表：按优先级作为材料来源与产出落点 |

# 页签结构

在 Inventory Editor 顶部点击「**制作系统**」页签。三列布局，与商店系统对称：

```
左列：子页签【分组标签 / 蓝图模板】+ 列表 + 编辑面板
中列：蓝图列表
右列：上下文 Inspector（分组标签 / 蓝图模板 / 蓝图）
```

左列子页签切换「分组标签」或「蓝图模板」；选中左列条目时右列显示该条目的编辑面板，选中中列蓝图时右列显示蓝图 Inspector。

# 分组标签

仅承载基础信息，**不携带自定义属性字段**。用于 UI 中对蓝图分组筛选。

| 字段 | 说明 |
|------|------|
| ID | 唯一标识（蓝图按此 ID 引用） |
| 名称 | `Text`（纯文本 fallback + 可选本地化引用；空时退回 ID） |
| 描述 | `Text`（纯文本 fallback + 可选本地化引用） |
| 颜色 | 列表色点 |

# 蓝图模板

定义自定义属性字段 + 一整套蓝图可配置项默认值（创建蓝图时复制），并承载**模板级整理排序**（该模板下所有蓝图在 UI 列表中的排序，蓝图自身不再单独配置排序）。

| 字段 | 说明 |
|------|------|
| 名称 / 颜色 | 模板名 + 列表色点 |
| 制作时间 | 制作一次需要的秒数 |
| 连续制作次数 | 见 [可制作次数与连续制作次数](#可制作次数与连续制作次数) |
| 制作仓库 | 仓库 ID 列表（有序） |
| 数字格式 | 引用的数字格式配置名称 |
| 整理列表 / 整理优先级 | 模板级排序条件（主 + 次） |
| 属性字段显示 | UI 上显示哪些属性（Label + 属性字段 ID） |
| 属性字段列表 | 模板自定义属性字段 |

> 蓝图与蓝图模板共享 `ICraftingConfig`，配置项一致、编辑器复用同一套绘制。

# 蓝图（中间列）

中列是蓝图列表（按所选左列模板过滤）。新建蓝图从模板创建，ID 自动生成；点击行选中，右列显示蓝图 Inspector，「删除蓝图」在右列顶部。

# 蓝图 Inspector（右侧列）

| 字段 | 说明 |
|------|------|
| ID | 唯一标识（重复时导出校验报错） |
| 名称 / 描述 | `Text`（纯文本 fallback + 可选本地化引用；名称空时退回 ID / 基础描述） |
| 来源模板 | 决定可配置项与自定义属性 |
| 主分组标签 | 单选，引用分组标签 ID |
| 副分组标签 | 多选 |
| 产出道具列表 | Index0 = 主产出（UI 显示），其余为副产出 |
| 消耗道具列表 | 制作一次所需材料 |
| 制作参数（制作时间 / 连续制作次数） | **蓝图级，可编辑** |
| 制作仓库 / UI 配置（数字格式 + 属性字段显示） | **模板级，蓝图条目只读展示**；镜像来源模板，仅可在「蓝图模板」中修改 |
| 属性字段值 | 来自模板定义的自定义属性值 |

> **模板级配置**：制作仓库与 UI 配置（数字格式 + 属性字段显示）只能在「蓝图模板」中配置。蓝图条目始终镜像其来源模板的这些字段（由 `CraftingBlueprint.RebuildAttributes` 同步），在蓝图 Inspector 中以只读形式展示，不可单独修改。无来源模板的蓝图则需先指定模板才能配置这些项。

# 配方：产出与消耗

产出（`outputs`）与消耗（`inputs`）都是「道具 + 数量」（`CraftingItemAmount`：`itemId` + `count`）列表。

- **产出 Index0 = 主产出**：UI 详情的图标 / 名称 / 描述取自主产出道具；其余为副产出（详情中以小图标显示，悬停弹详情）。
- **消耗列表**：制作一次所需材料；运行时按需求逐项校验持有量。
- 蓝图引用了不存在的道具时，导出校验会报错。

# 制作仓库

`craftInventoryRefs` 是有序的仓库 ID 列表（**模板级配置：在「蓝图模板」中编辑，蓝图条目只读继承**）：

- **材料来源**：扣除材料时按列表顺序（优先级）从各仓库累计扣除。
- **产出落点**：放入产出时按列表顺序分摊到各仓库的剩余容量；全部放不下时超出部分按容量丢弃（与商店交易一致）。
- **持有量统计**：可制作次数 / 持有量跨所有制作仓库汇总。

# 属性字段显示

「属性字段显示」（`CraftingAttributeDisplay`：`label` + `attrId`）控制在蓝图条目 / 详情上显示**主产出道具**的哪些属性，形如「Label 值」（如「等级 5」「价值 120」）。**模板级配置：在「蓝图模板」中编辑（可拖拽左侧句柄重排），蓝图条目只读继承。**

值由 `AttributeValue.ToDisplayString()` 按字段类型拼接（数值直接转、向量列全部分量、StringIntPair 显示 `key: value` 等），见 [属性系统 - 显示字符串](AttributeSystem.md#显示字符串-todisplaystring)。

# 可制作次数与连续制作次数

两个易混淆的概念：

| 概念 | 含义 | 计算 |
|------|------|------|
| **可制作次数（材料）** | 当前材料够做几次 | 各消耗道具 `floor(持有量 / 单次消耗)` 的最小值 |
| **连续制作次数（`maxCraftCount`）** | 单次「制作」动作允许连发的批量上限 | `1` = 仅一次；`-1` = 无限 |

- UI 详情的「可制作：N」展示的是**材料可制作次数**（`GetMaxCraftableByMaterials`）。
- 制作次数选择器（连续制作批量）的上限 = 材料可制作次数 与 `maxCraftCount` 取小（`GetMaxCraftable`）。

# 运行时 API

`CraftingRuntimeManager` 是轻量单例，无自身状态、不存档。道具数据经 `InventoryDataManager` 查询；仓库读写经 `InventoryRuntimeManager`（消耗 / 产出会触发其 `OnInventoryChanged`，UI 据此刷新）。

```csharp
using InventorySystem.Runtime;

var cm = CraftingRuntimeManager.Instance;
CraftingBlueprint bp = InventoryDataManager.Instance.GetCraftingBlueprint("craft_sword");

int owned = cm.GetOwnedAcross(bp, "iron_ingot");          // 跨制作仓库持有量
int byMat = cm.GetMaxCraftableByMaterials(bp);            // 材料可制作次数（用于展示）
int max   = cm.GetMaxCraftable(bp);                       // 选择器上限（受连续制作次数约束）
bool can  = cm.CanCraftOnce(bp);                          // 材料是否够做一次

// 执行一次制作（先校验全部消耗充足，再跨仓库扣材料、放产出）
bool ok = cm.CraftOnce(bp);          // 或 cm.CraftOnce("craft_sword")
```

连续制作 = UI 层循环调用 `CraftOnce` 并驱动计时 / 进度（见 `UiwCraftingDetail`）。

# 制作 UI

制作 UI 在 `Runtime/UI/View/Crafting/`，结构与背包对称：

| 组件 | 说明 |
|------|------|
| `UiwCraftingView` | 制作主界面：蓝图模板页签 + 名称搜索 + 分组折叠页签 + 排序整理栏 + 蓝图虚拟列表 + 蓝图详情 |
| `UiwCraftingDetail` | 蓝图详情：主 / 副产出、消耗列表、可制作次数、制作次数选择（数字计数器）、制作 / 停止 + 进度条 |
| `UiwCraftingBlueprintCell` | 蓝图列表条目 |
| `UiwCraftingInputCell` | 消耗道具行（图标 / 名称 / 需求 / 持有） |
| `UiwCraftingBlueprintList` | 蓝图虚拟滚动列表 |
| `UiwCraftingGroupFilter` | 分组折叠页签（主分组可折叠出副分组，复用 `UiwFoldTab`） |

过滤管线：模板 → 分组（主 / 副）→ 名称搜索；再按所选模板的整理设置排序。预制体制作与参数见 [UI 组件指南](UIComponentGuide.md)。
