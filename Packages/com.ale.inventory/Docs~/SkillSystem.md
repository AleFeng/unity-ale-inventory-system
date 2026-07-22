# 技能系统（Skill System）

- 返回 [说明文档](../README.md)

技能系统用于配置玩家可使用的技能（攻击 / 治疗 / 增益 / 弱化 / 辅助等）。技能是独立的配置条目，携带 ID / 名称 / 描述 / 图标等固定信息，技能的**类型 / 效果 / 数值 / 位阶**等则由使用方在「自定义属性字段」中自行约定 attrId 承载，供其它系统读取使用。技能主要**赋予给装备类道具**（武器的攻击技能、防具的防御技能等），也可赋予其它道具（消耗品的使用技能、魔法卷轴 / 技能书等）。技能是配置目录；运行时的「已学技能」状态由 `SkillRuntimeManager` 维护，展示集合由 `SkillCollector` 按来源采集。

# 📜目录

- [核心概念](#核心概念)
- [页签结构](#页签结构)
- [分组标签](#分组标签)
- [技能模板](#技能模板)
- [技能（中间列）](#技能中间列)
- [技能 Inspector（右侧列）](#技能-inspector右侧列)
- [道具 ↔ 技能关联](#道具--技能关联)
- [位阶（Enum）驱动显示](#位阶enum驱动显示)
- [技能来源](#技能来源)
- [运行时 API](#运行时-api)
- [技能 UI](#技能-ui)

# 核心概念

| 概念 | 说明 |
|------|------|
| 分组标签（`SkillGroupTag`） | 对**技能**分组，便于在运行时 UI 上以分组页签筛选（如 攻击 / 治疗 / 强化 / 弱化 / 辅助）。仅承载基础信息，不携带属性字段 |
| 技能模板（`SkillTemplate`） | 创建技能的蓝本：定义自定义属性字段（schema）+ 一套「技能默认信息」（名称 / 描述 / 图标 / 分组标签），也用于分类筛选 |
| 技能（`Skill`） | 技能配置条目：固定信息（ID / 名称 / 描述 / 图标）+ 来自模板的自定义属性值（承载类型 / 效果 / 数值 / 位阶等） |
| 道具技能引用 | 道具在其一个「技能引用属性字段」（String，可数组）中存放技能 ID，把技能**赋予**给该道具 |
| 位阶（Enum 属性） | 技能上一个 Enum 类型的自定义属性；其枚举项携带「名称 / 背景框」等属性，驱动条目背景框与 Tooltip 位阶名显示 |

> `Skill` 继承 `AttributeOwner`（与 `Item` / `EnumItem` 同源），因此可用 `GetEntry` / `GetAttributeValue<T>` 按 attrId 读取自定义属性（String / Text / 数值 / 枚举等）。

# 页签结构

在 Inventory Editor 顶部点击「**技能系统**」页签。三列布局，与制作 / 装备系统对称：

```
左列：子页签【分组标签 / 技能模板】+ 列表 + 编辑面板
中列：技能列表
右列：上下文 Inspector（分组标签 / 技能模板 / 技能）
```

左列子页签切换「分组标签」或「技能模板」；选中左列条目时右列显示该条目的编辑面板，选中中列技能时右列显示技能 Inspector。

# 分组标签

仅承载基础信息，**不携带属性字段**。用于运行时 UI 中以分组页签对技能筛选。

| 字段 | 说明 |
|------|------|
| ID | 唯一标识（技能按此 ID 引用分组标签） |
| 名称 | `Text`（纯文本 fallback + 可选本地化引用；空时退回 ID） |
| 描述 | `Text`（纯文本 fallback + 可选本地化引用） |
| 颜色 | 列表色点 |

> 运行时分组页签以分组标签的**显示名**作为过滤 token，因此各分组标签的显示名应互不相同。

# 技能模板

**定义自定义属性字段（schema）**，并承载一套「技能默认信息」，作为创建技能的蓝本，同时用于分类筛选。

| 字段 | 说明 |
|------|------|
| 名称 / 颜色 | 模板名（技能的 `templateRef` 引用键）+ 列表色点 |
| 技能默认信息 | 名称 / 本地化名 / 描述 / 本地化描述 / 图标 / 主分组标签 / 副分组标签 |
| 自定义属性字段 | 模板定义的属性字段 schema（技能据此协调其属性值；各字段的 `defaultValue` 即技能属性的初始值） |

> 技能与模板共享 `ISkillConfig`（名称 / 描述 / 图标 / 本地化 / 分组标签），编辑器复用同一套绘制（`SkillConfigDrawer`）。
>
> **从模板创建技能时，会复制模板的「技能默认信息」**（名称 / 描述 / 图标 / 分组标签）作为初始值，并按属性 schema 的 `defaultValue` 初始化自定义属性值；此后技能**独立可编辑**（与模板不再联动）。

# 技能（中间列）

中列是技能列表（按所选左列模板过滤 + 顶部搜索栏按 ID / 名称过滤）。「**从模板添加**」从模板创建（复制默认信息 + 按 schema 初始化属性值），ID 自动生成；「**快速添加**」克隆末项。点击行选中，右列显示技能 Inspector，「删除技能」在右列顶部。每行左侧有拖拽句柄，可重排技能顺序；ID 重复时行内红色高亮，底部状态栏提示「⚠ 技能重复 ID」。

# 技能 Inspector（右侧列）

| 字段 | 说明 |
|------|------|
| ID | 唯一标识（重复时导出校验报错；道具通过此 ID 引用技能） |
| 名称 / 描述 | `Text`（纯文本 fallback + 可选本地化引用；名称空时退回 ID；描述详情弹窗显示） |
| 图标 | `Sprite`（技能条目 / 弹窗显示） |
| 来源模板 | 只读，决定自定义属性字段 |
| 主分组标签 / 副分组标签 | 主分组单选 + 副分组多选（引用分组标签 ID） |
| 自定义属性值 | 来自模板定义的自定义属性值（技能的类型 / 效果 / 数值 / 位阶等承载于此） |

> 技能的类型 / 效果 / 数值等**没有固定字段**——完全由你在模板中定义自定义属性字段（如 `技能类型` Enum、`效果` String、`伤害` Int、`位阶` Enum 等），在技能上填值，再由其它系统 / UI 按 attrId 读取。

# 道具 ↔ 技能关联

技能通过**道具的一个自定义属性字段**赋予给道具：

- 在道具模板（或功能标签）中定义一个 **String 类型**的属性字段（例如 `技能`），勾选**数组**形态即可让**一个道具携带多个技能**。
- 在道具上填入技能 ID（数组则填多个）。
- 运行时 UI（`Equipment` / `Inventory` 来源）在组件上配置该字段的 attrId（`skillRefAttrId`，默认 `技能`），采集时读取道具该属性的全部非空字符串，逐个经 `InventoryDataManager.GetSkill` 解析为技能；解析不到的 ID 跳过。

> 采集入口 `SkillCollector` 仅识别 `EFieldType.String`（标量或数组）的技能引用字段；同一技能被多个道具 / 槽位引用时只显示一次（按引用去重、保序）。

# 位阶（Enum）驱动显示

技能可配一个 **Enum 类型**的「位阶」属性字段（attrId 默认 `位阶`）。其枚举项（在**道具系统「枚举类型」**中定义，`EnumItem` 同为 `AttributeOwner`）可携带自定义属性字段，如 **名称**、**描述**、**背景框（Sprite）**。UI 据此渲染（**参考道具 UI 的品质背景**，解析链完全一致）：

```
skill.GetEntry(位阶attrId).value → 枚举值 + 枚举类型引用
  → InventoryDataManager.GetEnumType(引用).GetItemByValue(枚举值)     // 枚举项 EnumItem
  → enumItem.GetAttributeValue<Sprite>(背景框attrId)   // 技能条目「位阶背景框」
  → enumItem.GetAttributeValue<string>(名称attrId)     // Tooltip「位阶名称」（String / Text 通吃）
```

- **技能条目**（`UiwSkillEntry`）：按位阶枚举项的「背景框」Sprite 显示条目背景框。组件上配 `rankAttrId`（默认 `位阶`）+ `rankBackgroundAttrId`（默认 `背景框`）。
- **技能 Tooltip**（`UiwSkillTooltip`）：显示位阶枚举项的「名称」。组件上配 `rankAttrId` + `rankNameAttrId`（默认 `名称`）。
- 全程为 UI 层解析，无需新增数据 API；无位阶数据 / 解析不到枚举项时相关显示自动隐藏（null 安全）。

# 技能来源

运行时技能 UI 的技能集合可来自四种来源（`ESkillSource`，在 `UiwSkillView` 上切换；其自定义 Inspector 会按来源**只显示对应的 ID 字段**）：

| 来源 | 采集内容 | 需配置 |
|------|---------|--------|
| `InventoryDatabase` | 数据库全部技能（技能书 / 图鉴） | — |
| `Equipment` | 某装备组所有装备槽已装备道具引用的技能 | 装备组 ID + 技能引用属性 `skillRefAttrId` |
| `Inventory` | 某仓库所有道具引用的技能 | 仓库 ID + 技能引用属性 `skillRefAttrId` |
| `Character` | 某角色当前已学会的技能 | 角色 ID（读 `SkillRuntimeManager`） |

采集统一经 `SkillCollector.Collect(source, configId, skillRefAttrId)`，结果去重、保序。

# 运行时 API

`SkillRuntimeManager` 是轻量单例（首次访问自动创建，仿 `EquipmentRuntimeManager`），以 **角色 ID → 已学技能 ID 列表**（保持学习顺序、去重）维护多角色的已学技能，可存档。技能定义经 `InventoryDataManager` 查询；`SkillCollector` 为无状态的来源采集工具。

```csharp
using System.Collections.Generic;
using InventorySystem.Runtime;

// ── 采集要显示的技能集合（四种来源；去重、保序）──
var all      = SkillCollector.Collect(ESkillSource.InventoryDatabase, null, null);
var equipped = SkillCollector.Collect(ESkillSource.Equipment, "equip_player", "技能"); // 装备组已装备道具的技能
var invSk    = SkillCollector.Collect(ESkillSource.Inventory, "背包", "技能");         // 仓库道具的技能
var learned  = SkillCollector.Collect(ESkillSource.Character, "hero_01", null);       // 角色已学技能

// ── 角色已学技能（多角色，可存档）──
var sk = SkillRuntimeManager.Instance;
sk.Learn("hero_01", "skill_fireball");                       // 学会（已学则忽略），返回是否变化
bool has = sk.HasLearned("hero_01", "skill_fireball");
sk.Forget("hero_01", "skill_fireball");                      // 遗忘
IReadOnlyList<string> ids = sk.GetLearnedSkillIds("hero_01"); // 已学技能 ID（只读）
List<Skill> skills        = sk.GetLearnedSkills("hero_01");   // 解析为技能对象
sk.ClearLearned("hero_01");                                  // 清空某角色

// 事件 + 存档
sk.OnLearnedChanged += characterId => { /* 刷新技能 UI */ };
var save = sk.GetSaveData();     // List<RuntimeLearnedSkillState>，交由游戏层 SaveManager 序列化
sk.LoadSaveData(save);           // 读档恢复
sk.ResetAll();                   // 清空全部角色（如开始新游戏）

// ── 查询技能定义 + 读自定义属性 ──
Skill skill    = InventoryDataManager.Instance.GetSkill("skill_fireball");
string effect  = skill.GetAttributeValue<string>("效果");   // String / Text
int    damage  = skill.GetAttributeValue<int>("伤害");      // 数值类型
```

- **`SkillRuntimeManager`**：仅维护「已学技能」这一可变状态，其余（名称 / 属性等）从技能定义读取。`Learn` / `Forget` / `ClearLearned` 发生变化时触发 `OnLearnedChanged(characterId)` 供 UI 刷新；存档单元为 `RuntimeLearnedSkillState`（角色 ID + 技能 ID 列表）。
- **`SkillCollector`**：静态工具，无运行时状态。`Equipment` 遍历装备组每个槽位的已装备道具、`Inventory` 遍历仓库每个道具，读道具 `skillRefAttrId`（String / 数组）解析技能 ID；`Character` 读 `SkillRuntimeManager.GetLearnedSkills`；`InventoryDatabase` 取全部技能。

# 技能 UI

技能 UI 在 `Runtime/UI/`（程序集 `InventorySystem.UI`）：

| 组件 | 说明 |
|------|------|
| `UiwSkillView` | 技能主界面（继承 `UiwViewBase`）：标题 + 搜索栏 + **主 / 副分组页签**（各含「全部」、各复用一个横向可滚动 `UiwFilterTabBar`）+ 网格 / 顺序双显示模式切换 + 悬停详情弹窗。`Open()` 用序列化的来源配置打开；`Open(source, configId)` 切换来源打开。按来源订阅 `OnEquipmentChanged` / `OnInventoryChanged` / `OnLearnedChanged` 自动刷新（`InventoryDatabase` 为静态数据不订阅） |
| `UiwSkillGridList` / `UiwSkillOrderList` | 技能列表（虚拟滚动）：分别继承通用 `UiwInventoryGridList` / `UiwInventoryOrderList`；`SetSkills` 把技能绑定到 `UiwSkillEntry` 并池化复用，网格按视口宽自动列数、顺序为单列，均只渲染可见区域。视图各持一实例，由切换按钮显示其一 |
| `UiwSkillEntry` | 技能条目（网格 / 顺序共用）：图标 + 名称 + **位阶背景框** + 可选描述 / 自定义属性字段行；悬停经 `UiwSkillTooltip` 弹出详情 |
| `UiwSkillTooltip` | 技能悬停弹窗（实现 `ISkillTooltip`）：图标 + 名称 + **位阶名称** + 描述 + 组件上配置的自定义字段（`customFieldKeys`，Array 可多个）。复用 `UiwItemTooltip` 的淡入淡出 / 光标定位 / 队列。**预制体配到 `InventoryRuntimeManager` 的 `skillTooltipPrefab`，由管理器全局实例化一次**（父节点复用 `tooltipParent`），经 `ShowSkillTooltip` / `HideSkillTooltip` 调用 |
| `UiwSkillText` / `SkillRankUtil` | 共享解析辅助：名称 / 描述 / 自定义字段的文本解析（本地化优先）；位阶枚举项解析。供条目与 Tooltip 共用 |

## 显示模式与筛选

- **显示模式**：视图切换按钮在**网格列表**（`UiwSkillGridList`）与**顺序列表**（`UiwSkillOrderList`）间切换（两实例叠放，激活其一；两者均为 `ScrollRect` 内虚拟滚动，只渲染可见区域）。无切换按钮时自动采用已配置的那个列表。
- **主 / 副分组页签**（两个 AND 筛选条件）：**主分组页签**按技能的主分组标签筛选、**副分组页签**按技能的副分组标签筛选，**两者都满足**的技能才显示（例如主选「战士」、副选「攻击」→ 显示战士的攻击技能）。各页签栏可选首位「全部」（`showAllTab`，选「全部」则该条件不过滤）。
  - **页签只按技能实际用到的标签生成**：主 / 副页签分别取当前来源技能实际配置到的主 / 副分组标签（按数据库分组标签顺序、去重），避免出现无技能的空标签页签。
  - **横向可滚动**：每个页签栏是一个横向 `ScrollRect`（`Clamped`）——标签总宽未超出时不滚动，超出界面范围时可横向拖动 / 滚动。
- **搜索**：按技能名称 / ID 过滤。**启用「全部」页签时，输入搜索即把主 / 副分组页签都切到「全部」**再过滤；未启用「全部」时则在当前主 / 副分组页签选中范围内搜索。

## 自定义属性字段（Tooltip）

Tooltip 除固定字段（名称 / 描述 / 图标）与位阶名外，还支持显示技能的**自定义属性字段**：在组件上配置 `customFieldKeys`（`string[]`，可多个 Key），每个非空值生成一行；`String` 取字符串、`Text` 取本地化文本（取不到回退纯文本）、其它类型取通用显示串（`AttributeValue.ToDisplayString()`）。技能条目（详情行）同样支持。

## 自定义 Inspector

`UiwSkillView` 有自定义 Inspector（`UiwSkillViewEditor`）：按当前 `source` **只显示该来源需要配置的 ID 字段**（`Equipment`→装备组 ID + `skillRefAttrId`；`Inventory`→仓库 ID + `skillRefAttrId`；`Character`→角色 ID；`InventoryDatabase`→无），隐藏无关字段。

## 一键生成预制体

在 **欢迎窗口** 的「测试工具-预制体生成」中，「技能系统」分类可逐项或整体生成技能预制体：`PF_UiwSkillCell`（网格条目）/ `PF_UiwSkillDetail`（列表条目）/ `PF_UiwSkillGridList` / `PF_UiwSkillOrderList` / `PF_UiwSkillTooltip` / `PF_UiwSkillView`；`InventoryManager` 预制体会实例化技能主界面并把技能 Tooltip 预制体配到管理器。预制体制作与通用组件见 [UI 组件指南](UIComponentGuide.md)。

> 提示：技能条目预制体加一张默认禁用的「位阶背景框」`Image`（接 `rankBackground`）才会按位阶显示背景框；技能 Tooltip 需配到 `InventoryRuntimeManager.skillTooltipPrefab` 才会在悬停时弹出；`Equipment` / `Inventory` 来源需在道具上按 `skillRefAttrId` 配好技能引用属性，`Character` 来源需先经 `SkillRuntimeManager.Learn` 让角色学会技能。
