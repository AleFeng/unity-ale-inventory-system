# 装备系统（Equipment System）

<p align="center">
  🌍
  中文 |
  <a href="./EquipmentSystem_EN.md">English</a> |
  <a href="./EquipmentSystem_JA.md">日本語</a>
</p>

- 返回 [说明文档](../README.md)

装备系统让玩家把道具装备到「装备组」的槽位上以获得属性加成。装备组定义一整套槽位结构（多个**槽位列表**，每个含多个**装备槽**），并以「道具限制」（功能标签 / 枚举约束）与「槽位过滤条件」约束每个槽位可装入的道具；「装备属性字段列表」指定哪些道具属性汇总为装备组的总属性加成。装备组是配置目录；运行时的装备 / 卸下 / 交换与属性加成汇总由 `EquipmentRuntimeManager` 执行。

# 📜目录

- [装备系统（Equipment System）](#装备系统equipment-system)
- [📜目录](#目录)
- [核心概念](#核心概念)
- [页签结构](#页签结构)
- [分组标签](#分组标签)
- [装备组模板](#装备组模板)
- [装备组（中间列）](#装备组中间列)
- [装备组 Inspector（右侧列）](#装备组-inspector右侧列)
- [装备仓库](#装备仓库)
- [槽位列表与道具限制](#槽位列表与道具限制)
- [装备槽与过滤条件](#装备槽与过滤条件)
- [装备属性字段列表](#装备属性字段列表)
- [限制判定规则](#限制判定规则)
- [运行时 API](#运行时-api)
- [装备 UI](#装备-ui)
  - [交互](#交互)

# 核心概念

| 概念 | 说明 |
|------|------|
| 分组标签（`EquipmentGroupTag`） | 对**装备属性字段条目**分组，便于在 UI 上分组显示总属性加成（如 物品等级 / 主属性 / 副属性）。仅承载基础信息，不携带属性字段 |
| 装备组模板（`EquipmentGroupTemplate`） | 创建装备组的蓝本，承载一整套可配置项（槽位列表 + 装备属性字段）+ 自定义属性字段定义，也用于分类筛选 |
| 装备组（`EquipmentGroup`） | 一整套装备的槽位结构：多个槽位列表（每个含装备槽 + 道具限制）+ 装备属性字段列表 + 自定义属性值 |
| 槽位列表（`EquipmentSlotList`） | 一组装备槽 + 统一的「道具限制」（功能标签 + 枚举约束） |
| 装备槽（`EquipmentSlot`） | 可装备一个道具的槽位，含「槽位过滤条件」进一步收窄 |

# 页签结构

在 Inventory Editor 顶部点击「**装备系统**」页签。三列布局，与制作 / 商店系统对称：

```
左列：子页签【分组标签 / 装备组模板】+ 列表 + 编辑面板
中列：装备组列表
右列：上下文 Inspector（分组标签 / 装备组模板 / 装备组）
```

左列子页签切换「分组标签」或「装备组模板」；选中左列条目时右列显示该条目的编辑面板，选中中列装备组时右列显示装备组 Inspector。

# 分组标签

仅承载基础信息，**不携带属性字段**。用于 UI 中对装备组的「装备属性字段」条目分组显示。

| 字段 | 说明 |
|------|------|
| ID | 唯一标识（装备属性字段条目按此 ID 引用） |
| 名称 | `Text`（纯文本 fallback + 可选本地化引用；空时退回 ID） |
| 描述 | `Text`（纯文本 fallback + 可选本地化引用） |
| 颜色 | 列表色点 |

# 装备组模板

**承载一整套装备组可配置项的默认值**（与装备组一致：槽位列表 + 装备属性字段列表），外加自定义属性字段定义（schema），并用于分类筛选。

| 字段 | 说明 |
|------|------|
| 名称 / 颜色 | 模板名 + 列表色点 |
| 装备仓库 | 见 [装备仓库](#装备仓库) |
| 槽位列表 | 见 [槽位列表与道具限制](#槽位列表与道具限制) |
| 装备属性字段列表 | 见 [装备属性字段列表](#装备属性字段列表) |
| 整理排序 | 排序条件 + 整理优先级，见下 |
| 自定义属性字段 | 模板定义的属性字段（装备组据此协调其属性值） |

> 装备组与模板共享 `IEquipmentConfig`（装备仓库 + 槽位列表 + 装备属性字段 + 整理排序），编辑器复用同一套绘制（`EquipmentConfigDrawer`）。
>
> **从模板创建装备组时，会深拷贝模板的全部可配置项**（装备仓库 + 槽位列表 + 装备属性字段 + 整理排序）作为初始数据；此后装备组**独立可编辑**（与制作系统「模板级只读」不同）。
>
> **整理排序**：模板与装备组均可配「整理排序」（排序条件 + 整理优先级），用于**装备选择面板底部「可装备道具候选列表」**（`UiwEquipmentCandidateList`）的显示排序——候选列表有排序栏时玩家可选并升降序，否则以首条为默认排序。条件从模板复制到装备组后可独立编辑（候选列表读装备组自身的配置）。

# 装备组（中间列）

中列是装备组列表（按所选左列模板过滤）。「从模板添加」从模板创建（深拷贝其配置 + 协调自定义属性默认值），ID 自动生成；「快速添加」克隆末项。点击行选中，右列显示装备组 Inspector，「删除装备组」在右列顶部。

# 装备组 Inspector（右侧列）

| 字段 | 说明 |
|------|------|
| ID | 唯一标识（重复时导出校验报错） |
| 名称 / 描述 | `Text`（纯文本 fallback + 可选本地化引用；名称空时退回 ID / 基础描述） |
| 来源模板 | 只读，决定自定义属性字段 |
| 装备仓库 | 装备系统 / 装备 UI 可交互的仓库，见 [装备仓库](#装备仓库) |
| 槽位列表 | 嵌套编辑（道具限制 + 装备槽 + 槽过滤条件），见下 |
| 装备属性字段列表 | 总属性加成字段（属性字段 ID + 分组标签），见下 |
| 自定义属性值 | 来自模板定义的自定义属性值 |

# 装备仓库

「装备仓库」（`equipmentInventoryRefs`，字符串仓库 ID 列表）指定装备系统 / 装备 UI 能**直接交互**的玩家仓库，是装备界面**唯一的仓库来源**（装备 UI 无需再传入仓库 ID）。**顺序即优先级**：Inspector 中每条前有序号，左侧拖拽句柄可调整顺序（与商店「交易仓库」、制作「制作仓库」共用 `InventoryRefListDrawer`）。

> **模板 / 装备组两级 + 运行时回退**：模板与装备组均可配置装备仓库；从模板创建装备组时会深拷贝模板的装备仓库。运行时取「**有效装备仓库**」——装备组自身列表非空则用之，为空时**自动回退到来源模板的装备仓库**（`EquipmentRuntimeManager.GetEquipmentInventories`）。因此只在模板层配置、或装备组是在新增此字段之前创建的（自身列表为空），运行时仍能正常工作。

- **候选道具来源**：装备选择面板底部的可装备道具列表**跨全部装备仓库汇总**符合限制的道具；装备时从该道具所在仓库取出。
- **卸下装备**：从列表 **Index0 起逐个尝试**，放入第一个「放得下」（自由空间 ≥ 1）的仓库；都放不下则**不卸下**（返回 false，避免道具丢失）。
- **背包桥接**：背包右键自动装备时，仅处理来自装备仓库中仓库的道具（未配置装备仓库则不限制来源）。

# 槽位列表与道具限制

一个装备组通常含多个槽位列表（例如「武器」「防具」「饰品」）。每个槽位列表条目默认折叠，仅显示 ID + 「详细配置」折叠栏；展开后可编辑：

- **名称 / 描述**。
- **道具限制 → 功能标签**：从道具系统已定义的功能标签中选择，限制本列表可装备的道具；可添加多个（左侧拖拽句柄可重排）。
- **道具限制 → 枚举约束**：先选「枚举类型」，再多选「枚举值」；可添加多个（左侧拖拽句柄可重排）。空枚举值集合表示「任意值」。
- **装备槽**：本列表的所有装备槽（见下）。

槽位列表条目左侧有拖拽句柄，可整体重排槽位列表顺序。

# 装备槽与过滤条件

每个槽位列表含多个装备槽。装备槽配置：

| 字段 | 说明 |
|------|------|
| ID | 槽位稳定标识（运行时按它定位已装备道具、存档） |
| 名称 | UI 显示名（可选；空槽时可作占位） |
| 槽位过滤条件 | 在槽位列表限制之上进一步收窄本槽可装入的道具 |

**槽位过滤条件**（`EquipmentSlotFilter`）= 属性字段 ID + 期望值：道具该属性等于期望值才可装入（如 武器主类型 = 近战单手、武器次类型 = 剑）。期望值用通用属性编辑器按字段类型编辑；同一槽位的多条过滤条件需**全部满足**。借此可为不同职业 / 角色类型配置不同的装备限制。

> 装备槽**不存放运行时已装备的道具**——已装备数据由 `EquipmentRuntimeManager` 按槽位 ID 维护。

# 装备属性字段列表

「装备属性字段列表」（`EquipmentAttributeDisplay`）指定道具上哪些属性字段汇总为装备组的**总属性加成**（如 物品等级 / 攻击力 / 防御力 / 生命值）。每条：

| 字段 | 说明 |
|------|------|
| 属性字段 ID | 关联道具系统的属性字段（可输入或从下拉选择已定义字段） |
| 分组标签 | 引用分组标签 ID，用于 UI 分组显示（如 主属性 / 副属性） |
| 显示名 | 可选覆盖（空时回退属性字段 ID） |

每条左侧有拖拽句柄，可重排。运行时按此列表对全部已装备道具求和（见 [运行时 API](#运行时-api)）。

# 限制判定规则

判定某道具能否装入某槽位，采用**全部 AND**：

1. **槽位列表的功能标签**：道具需**具备全部**所列功能标签。
2. **槽位列表的枚举约束**：道具需**满足每一条**枚举约束——存在引用该枚举类型的属性且其值在允许集合内；允许集合为空表示「任意值」（仍需道具具备该枚举类型的属性）。
3. **槽位的过滤条件**：道具对应属性需**等于每一条**过滤条件的期望值。

三者全部满足，道具方可装入该槽位。

# 运行时 API

`EquipmentRuntimeManager` 是轻量单例（首次访问自动创建，仿 `ShopRuntimeManager`），按 `装备组 ID → (槽位 ID → 已装备道具 ID)` 维护运行时状态。装备组目录经 `InventoryDataManager` 查询；仓库读写一律经 `InventoryRuntimeManager`（装备 / 卸下会触发其 `OnInventoryChanged`，背包 UI 据此刷新）。

```csharp
using InventorySystem.Runtime;

var eq = EquipmentRuntimeManager.Instance;

// 查询
string equipped = eq.GetEquipped("equip_player", "slot_weapon"); // 该槽已装备道具，空槽返回 null
bool occupied   = eq.IsSlotOccupied("equip_player", "slot_weapon");

// 限制匹配
var group    = InventoryDataManager.Instance.GetEquipmentGroup("equip_player");
var slotList = group.slotLists[0];
bool canList = eq.ItemMatchesSlotList(slotList, "sword_01");                 // 满足槽位列表限制？
bool canSlot = eq.ItemMatchesSlot(slotList, slotList.slots[0], "sword_01");  // 进一步满足该槽过滤？

// 自动找槽 + 自动装备（从背包取出 1 个，放入第一个可装入的空槽）
if (eq.TryFindEquipSlot("equip_player", "sword_01", out var slId, out var slotId)) { /* ... */ }
bool autoOk = eq.TryAutoEquip("equip_player", "sword_01", "背包");         // 仅填空槽
// 快速装备（可替换）：① 首选槽 preferredSlotId（占用则替换）→ ② 空槽 → ③ 第一个满足限制的已占用槽（Index0 起，卸下原道具放回来源仓库）
// UI 右键快速装备走此路径；装备选择面板打开时会把当前选中槽作为首选槽传入
bool quickOk = eq.TryAutoEquipOrReplace("equip_player", "sword_01", "背包", preferredSlotId: "slot_weapon");

// 指定槽位装备 / 卸下 / 交换（与仓库协作搬运；装备占用槽时换下旧道具回来源仓库）
bool eOk = eq.Equip("equip_player", "weapon_list", "slot_weapon", "sword_01", "背包");
bool uOk = eq.Unequip("equip_player", "slot_weapon", "背包");      // 卸下到指定仓库（放不下则失败、不卸下）
// 卸下到装备组配置的「装备仓库」：从 Index0 起找第一个放得下的；未配置时回退到 fallback（此处 背包）
bool ucOk = eq.UnequipToConfigured("equip_player", "slot_weapon", "背包");
string inv = eq.FindEquipmentInventoryFor("equip_player", "sword_01"); // 该道具在装备仓库中第一个放得下的仓库 ID（无则 null）
bool sOk = eq.SwapSlots("equip_player", "slot_weapon", "slot_offhand"); // 同组槽↔槽交换（双方需各自满足目标限制）

// 总属性加成（按「装备属性字段列表」跨全部已装备道具汇总，携带分组标签供 UI 分组）
foreach (var b in eq.GetTotalBonuses("equip_player"))
    Debug.Log($"{b.Label}({b.GroupTag}) = {b.Total}");   // EnumIntPair 时按枚举 Key 拆分为多条，b.EnumValue 为枚举值

// 事件 + 存档
eq.OnEquipmentChanged += groupId => { /* 刷新装备 UI */ };
var save = eq.GetSaveData();          // 交由游戏层 SaveManager 序列化
eq.LoadSaveData(save);                // 读档恢复
eq.ResetAll();                        // 清空（如开始新游戏）
```

- **装备**：`Equip` 先校验限制，从来源仓库取出 1 个道具；槽位已占用则把旧道具放回来源仓库（先取后放，净占用不变；放不回则回滚，不丢失道具）。未提供来源仓库时仅设置槽位（旧道具直接替换，调用方自负）。
- **卸下**：`Unequip` 把道具放回指定目标仓库；放不下则返回 false、不卸下。UI 右键卸下走 `UnequipToConfigured(groupId, slotId)`——从装备组「装备仓库」列表 Index0 起找第一个放得下的仓库；都放不下（或未配置且无回退仓库）则不卸下，不会丢弃道具。
- **属性加成**：`GetTotalBonuses` 对每条装备属性字段，跨全部已装备道具汇总，**记录方式随源属性 `AttributeValue.Type` 而不同**：
  - **`EnumIntPair`**：按枚举 Key 拆分——每个枚举 Key 单独成一条 `EquipmentBonus`，其整数值累加进 `Total`；`EnumTypeRef`/`EnumValue` 记录来源枚举，显示名经装备属性字段的「显示名来源（枚举字段）」（`EquipmentAttributeDisplay.enumLabelAttrId`）从对应枚举项的 String / Text 属性解析，未配置则回退枚举项名称。典型用于「角色属性值加成」（如 力量 +13、敏捷 +5）。**无法解析到实际枚举项的 Key（枚举类型缺失 / 枚举项被删除）不显示。**
  - **`StringIntPair`**：按字符串 Key 拆分累加，显示名即该字符串 Key。**空字符串 Key 不显示。**
  - **其它数组类型**：按元素索引拆分——每个索引位置一条，各道具同索引位置累加。
  - **标量类型**：汇总为一条，按 `AttributeValue.ToComparableNumber()` 求和（数值直接取、向量取模长），见 [属性系统 - 排序比较数值](AttributeSystem.md)。

# 装备 UI

装备 UI 在 `Runtime/UI/`（程序集 `InventorySystem.UI`）：

| 组件 | 说明 |
|------|------|
| `UiwEquipmentView` | 装备主界面：整合装备组面板 + 属性加成面板 + 装备选择面板。`Open(groupId)`（仓库取自装备组「装备仓库」，无需传入）；右键装备槽卸下到装备仓库；左键装备槽打开选择面板。`_groupId` 可在 Inspector 预设（默认「角色装备」）：无参 `Open()` 用该值，`Open(groupId)` 覆盖，编辑器 `autoOpenOnStart` 亦用当前 `_groupId` |
| `UiwEquipmentGroupPanel` | 装备组面板：显示装备组名称 + 展示全部槽位列表。**布局方式**（`displayMode`）：`Auto` 自动按配置实例化各槽位列表；`Manual` 手动模式——用户在层级中自行摆放槽位列表物体，用 `manualSlotLists`（槽位列表 ID → 槽位列表）逐一绑定，实现自由排版（可在 Inspector 配 `groupId` + `bindOnStart` 独立使用） |
| `UiwEquipmentSlotList` | 槽位列表：显示名称 + 展示全部装备槽。**布局方式**（`displayMode`）：`Auto` 在 HorizontalLayoutGroup 下实例化各装备槽；`Manual` 手动模式——用户在层级中自行摆放装备槽物体，用 `manualSlots`（槽位 ID → 装备槽）逐一绑定 |
| `UiwEquipmentSlot` | 装备槽：继承 `UiwInventoryItemSlotBase`；显示已装备道具；左键 / 右键事件；拖拽源（拖出交换）+ 放置目标（装备 / 交换）+ 绿/红有效性叠加 |
| `UiwEquipmentBonusPanel` / `UiwEquipmentBonusEntry` | 属性加成面板：按分组标签分组显示总属性加成；无任何加成时显示一条空状态提示（`emptyText`，支持本地化表/条目引用 `emptyTextLocalized*`） |
| `UiwEquipmentSelectPanel` | 装备选择面板：顶部槽位组切换栏（左右 + N/M）+ 中间当前槽位列表 + 底部可装备道具列表 + 退出（按钮 / 空白处右键）|
| `UiwEquipmentCandidateList` | 可装备道具列表（**虚拟滚动网格**，继承 `UiwInventoryGridList`）：跨装备组「装备仓库」按当前槽位列表限制筛选候选道具（每格记录其来源仓库）；**右键快速装备 / 左键拖拽到装备槽装备**。候选格子复用仓库格子 `UiwInventoryItemCell` + `GridCellDragHandler`（与背包格子同源；不接整理拖拽，故 handler 驱动装备拖拽）。预制体需 ScrollRect + Viewport + Content（Content 不挂 GridLayoutGroup） |
| `GridCellDragHandler` | 道具格子拖拽中转组件（挂在 `UiwInventoryItemCell` 或其子物体上，位于 `Runtime/UI/ItemList/`）：**已接入网格列表**（背包）时转发给 `UiwInventoryItemGridList`，结束拖拽时按落点决定（装备槽→装备，道具格子→换位）；**未接入网格列表**（候选列表）时驱动「拖到装备槽装备」（经 `UiwEquipmentDragContext`，带幽灵 + 绿/红有效性）。右键快速装备不由本组件处理——由 `UiwInventoryItemCell` 广播「道具右键」、`UiwEquipmentView` 订阅统一处理，使格子在任何容器中交互一致 |
| `UiwEquipmentDragContext` | 装备拖拽全局上下文（载荷 + 跟随光标的幽灵 + 来源图标复位） |

## 交互

- **选择面板装备**：左键装备槽 → 打开选择面板 → 切换槽位组 → 底部候选道具**右键快速装备**（`UiwEquipmentView` 订阅「道具右键」事件，装入优先级：**① 选择面板当前选中的装备槽**（占用则替换）→ ② 第一个可装入的空槽 → ③ 第一个满足限制的已占用槽（Index0 起，卸下原道具放回来源仓库））或**左键拖拽到装备槽**装备；左键单击候选道具不触发装备。候选道具与背包道具**交互一致**（右键快速装备 / 左键拖拽到装备槽装备）。
- **卸下**：右键装备槽 → 卸下。放入装备组「装备仓库」列表中 Index0 起第一个放得下的仓库；都放不下则不卸下（不丢弃道具）。
- **装备槽之间交换**：拖拽一个装备槽到另一个装备槽（同组），双方各自满足目标限制时交换。
- **装备槽拖到背包格子**（需该背包 `dragSort = true`）：拖拽装备槽到背包某格——落到**空格** → 卸下并精确放入该格；落到**有道具的格** → 若该道具可装入源装备槽则**交换位置**（该格道具装入装备槽、原装备道具落回该格），不可装入则取消、不改动。
- **拖拽有效性**：拖拽悬停在装备槽上时，按可否装入显示绿（可）/ 红（不可）。
- **与背包互通**：背包界面**右键**道具 → 快速装备（`UiwEquipmentView` 打开时自订阅「道具右键」事件，无需额外接线；无空槽时替换第一个满足限制的已占用槽）；背包**网格**格子可**拖拽**到装备槽装备（需该背包 `dragSort = true`；顺序列表不可拖，用右键）。

预制体制作与通用组件见 [UI 组件指南](UIComponentGuide.md)。

> 提示：装备槽预制体加一张默认禁用的「有效性叠加图」（接 `validityOverlay`）才会显示绿/红提示；选择面板根节点需有 raycast 图形才能「空白处右键退出」；拖拽需场景内有 EventSystem。
