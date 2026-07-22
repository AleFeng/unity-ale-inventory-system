# 架构说明

- 返回 [说明文档](../README.md)

## 设计目标

编辑器始终且仅在 ScriptableObject 上工作；JSON / 二进制仅作为单向导出格式（供运行时 / 打包消费）。因此编辑器侧无需考虑序列化往返兼容，Undo/Redo 也只作用于 SO。

---

## 核心数据模型

### 灵活属性系统

采用 **标签联合（tagged-union / 变体）+ 数组组合**，而非 `[SerializeReference]`。

`AttributeValue`（`Runtime/Data/AttributeValue.cs`）携带：

- `EFieldType _type`：当前类型（共 22 种，见下方）；
- `bool _isArray`：标量 / 数组；
- `string _enumTypeRef`：枚举类型名称（仅 Enum）；
- 五个按类型分类的后备列表：`List<int>` / `List<float>` / `List<string>` / `List<Object>` / `List<AnimationCurve>`。

**后备列表约定**：

| 类型组 | 后备列表 | 步长 |
|--------|---------|------|
| Bool / Enum | `ints` | 1 |
| Int | `ints` | 1 |
| VectorInt2 / 3 / 4 | `ints` | 2 / 3 / 4 |
| Float | `floats` | 1 |
| Vector2 / 3 / 4 | `floats` | 2 / 3 / 4 |
| Color | `floats` | 4 |
| String | `strings` | 1 |
| Text | `strings` | 3（纯文本 + 表引用 + 条目键） |
| Sprite / Prefab / Texture / Material / AudioClip / AnimationClip / PhysicsMaterial / PhysicsMaterial2D | `objRefs` | 1 |
| AnimationCurve | `curves` | 1 |

标量存元素 `[0]`，数组存 `[0..n]`（步长 > 1 时压平存储）。

对象类字段另有一个与 `objRefs` 平行的字符串地址列表 `_objAddresses`（承载 Addressable 地址 / AssetReference 授权 GUID）：启用 `IS_ADDRESSABLE` 时编辑器以原生 AssetReference 选择器授权（仅存 GUID、objRefs 槽置空，加载数据库不再一并载入资源）；运行时经 `InventoryAssets` 门面「优先实时引用、否则地址异步加载」，宿主销毁自动卸载。详见 [属性系统 - 资源字段的加载](AttributeSystem.md#资源字段的加载addressables)。

**为什么不用 `[SerializeReference]`**：标签联合在 SO 中原生序列化、原生支持 Undo/Redo，无需多态 PropertyDrawer；`[SerializeReference]` 有托管引用/Undo 损坏隐患。代价（每个值几个空列表）对配置期数据可忽略。

### 枚举的稳定引用

`EnumType`（`Runtime/Data/EnumType.cs`）维护单调递增 `nextValue`；`AddItem` 时分配并自增、删除不回收。属性值存的是**枚举值（int）**而非索引，重排枚举项不会破坏已有引用。

枚举项（`EnumItem`）本身可携带一组 `AttributeDefinition` 自定义属性字段，用于描述枚举项的附加数据（如各职业的基础属性加成）。

### 道具与仓库

```
InventoryDatabase (ScriptableObject)
├── List<EnumType>                  枚举类型定义
├── List<FunctionTag>               功能标签定义（含属性字段）
├── List<ItemTemplate>              道具模板（含属性字段 + 锁定的功能标签引用）
├── List<Item>                      道具条目（含来源模板引用 + 自选标签引用 + 属性值）
├── List<InventoryTemplate>         仓库模板（含配置参数 + 属性字段）
├── List<Inventory>                 仓库条目（含来源模板引用 + 配置参数覆盖 + 属性值）
├── List<AttributeDefinition>       整理选项可选额外属性字段（schema；名称/忽略ID 已内置到 SortOption，此项默认为空）
├── List<SortOption>                整理选项（由 RebuildSortOptions 自动生成；含内置 displayName(Text) + ignoreIds）
├── List<NumberFormatConfig>        数字格式配置（按名引用）
├── List<ShopTemplate> / List<Shop> 商店模板 / 商店（含商品组、商品、刷新计划）
├── 制作：List<CraftingGroupTag> / List<CraftingBlueprintTemplate> / List<CraftingBlueprint>
└── 装备：List<EquipmentGroupTag> / List<EquipmentGroupTemplate> / List<EquipmentGroup>
                                   （装备组含 槽位列表 → 装备槽 + 道具限制 + 装备属性字段）
```

`Item.RebuildAttributes(db)` 按优先级（模板自有 → 模板锁定标签 → 道具自选标签）收集期望字段，增删/重排 `values` 列表，使之与来源定义保持同步（幂等，不影响 Undo 历史）。

`Inventory` / `Shop` / `CraftingBlueprint` / `EquipmentGroup` 的 `RebuildAttributes(db)` 与道具类似，分别仅从各自的模板（仓库模板 / 商店模板 / 蓝图模板 / 装备组模板）收集属性字段。`SortOption` 则由 `InventoryDatabase.RebuildSortOptions` 从所有仓库模板的排序字段自动同步。

> 装备组与装备组模板共享 `IEquipmentConfig`（装备仓库 + 槽位列表 + 装备属性字段），模板承载全部可配置项；从模板创建装备组时深拷贝这些配置，此后装备组独立可编辑（与制作系统「模板级只读」相反）。装备仓库列表指定装备系统 / UI 可交互的仓库，卸下时从 Index0 起找第一个放得下的仓库。

---

## 数据流

```
InventoryDatabase (SO)  ──编辑──▶  仍是 SO
        │
        ├─ 导出 ─▶ InventoryDtoMapper.ToDto ─▶ JsonUtility / BinaryWriter ─▶ .json / .bytes
        │                                   （Sprite 等引用经 EditorAssetGuidResolver 转 GUID）
        │
        └─ 运行时加载 ◀─ InventoryJson/BinarySerializer.Import ◀─ .json / .bytes
                       （NullAssetRefResolver：对象引用保持为空；
                         Addressable 模式：AddressableAssetRefResolver 按地址异步加载）
```

DTO 层（`Runtime/Serialization/InventoryDtoModels.cs`）是与数据模型一一镜像的扁平结构，唯一区别是对象引用以 GUID 字符串承载。

---

## 编辑器结构

```
InventoryEditorWindow          主窗口 + IInventoryEditorContext 实现（顶部系统页签 + 导出按钮）
├── ItemSystemTab              道具系统页签
│   ├── EnumTypePanel          枚举类型列表 + 编辑面板
│   ├── FunctionTagPanel       功能标签列表 + 编辑面板
│   ├── ItemTemplatePanel      道具模板列表 + 编辑面板
│   ├── ItemListPanel          道具列表（模板过滤标签 + 搜索 + 拖拽重排）
│   └── ItemInspectorPanel     道具 Inspector（分组属性 + 功能标签）
├── InventorySystemTab         仓库系统页签
│   ├── InventoryTemplatePanel 仓库模板列表 + 编辑面板
│   ├── InventoryListPanel     仓库列表 + InventoryInspectorPanel
│   └── （另有 SortOptionPanel / NumberFormatConfigPanel）
├── ShopSystemTab             商店系统页签
│   ├── ShopTemplatePanel      商店模板列表 + 编辑面板
│   ├── ShopListPanel          商店列表 + ShopInspectorPanel（商品组 / 商品）
├── CraftingSystemTab         制作系统页签
│   ├── CraftingGroupTagPanel  分组标签列表 + 编辑面板
│   ├── CraftingTemplatePanel  蓝图模板列表 + 编辑面板
│   └── CraftingListPanel      蓝图列表 + CraftingInspectorPanel
└── EquipmentSystemTab        装备系统页签
    ├── EquipmentGroupTagPanel 分组标签列表 + 编辑面板
    ├── EquipmentTemplatePanel 装备组模板列表 + 编辑面板（名称/颜色 + 共享配置 + 自定义属性字段）
    └── EquipmentListPanel     装备组列表 + EquipmentInspectorPanel（嵌套槽位列表 / 装备槽 / 道具限制 / 属性字段）
```

「技能系统」页签当前为占位（DrawStub），后续阶段实现。

> 装备系统的「槽位列表 + 装备属性字段」由 `Editor/Common/EquipmentConfigDrawer` 统一绘制（装备组 Inspector 与装备组模板 Inspector 复用），嵌套子列表的拖拽重排用按路径键的 `Dictionary<string, EditorReorderableDrag>` 隔离。

### 通用绘制器

| 类 | 职责 |
|----|------|
| `AttributeFieldDrawer` | 按 `EFieldType` 绘制单个 `AttributeValue`（GUILayout 路径 + Rect 路径双实现） |
| `AttributeDefinitionDrawer` | 绘制 `AttributeDefinition` 的完整编辑面板（Rect-based，供 ReorderableList 使用） |
| `AttributeDefinitionListDrawer` | 带拖拽重排的属性字段定义列表（内部用 `ReorderableList`，drawElementCallback 全 Rect-based） |

**Rect-based 原则**：`ReorderableList.drawElementCallback` 内严禁调用 `GUILayout.BeginArea / GUI.BeginGroup`，否则 Layout 与 Repaint 的 GUILayout 槽位数不一致时会抛出「Getting control X's position...」异常。所有 `DrawRect` 方法只使用 `EditorGUI.*`。

### 修改统一流程

所有编辑操作遵循：`ctx.RecordUndo(描述) → 修改数据 → ctx.MarkDirty()`。

重复 ID 检测由 `DuplicateIdChecker` 在 Layout 阶段重算并缓存（`HashSet<string>`），开销极低（每次 Layout 事件 O(n)）。

---

## 运行时架构

```
InventoryRuntimeManager (MonoBehaviour 单例)
├── InventoryDatabase[]    ─注册→  InventoryDataManager（静态定义查询）
└── Dictionary<inventoryId, RuntimeInventoryState>  ─维护→  各仓库运行时格子列表
```

`InventoryRuntimeManager` 职责：
- 初始化时将数据库注册到 `InventoryDataManager`；
- 为每个已定义仓库创建空 `RuntimeInventoryState`（格子列表）；
- 提供 `TryAddItem / TryRemoveItem / TryRemoveItemById / SortInventory` 运行时操作；
- 提供 `GetSaveData / LoadSaveData` 接口与游戏存档系统对接；
- 发布 `OnInventoryChanged(inventoryId)` 事件，供 UI 层订阅刷新。

`InventoryDataManager` 职责（纯数据查询，无状态）：
- 注册 `InventoryDatabase`（支持多个，合并查询）；
- 按 ID 查询 `Item / Inventory / EnumType / FunctionTag / ItemTemplate / InventoryTemplate`；
- 支持从 `.asset`、JSON 文本、二进制字节三种来源注册。

### 子系统运行时管理器

商店与制作的运行时逻辑由两个**轻量单例**（`InventorySystemSingleton<T>`，首次访问自动创建，非 MonoBehaviour）承担，二者本身不持有目录数据（目录来自已注册数据库），仓库读写一律经 `InventoryRuntimeManager`：

- `ShopRuntimeManager`：价格解析（多货币）、跨交易仓库的货币 / 持有量统计、按刷新计划重置可交易次数、购买 / 回收（自动按 次数 / 货币 / 容量 下调成交）、**每玩家交易进度存档**；事件 `OnShopChanged`。
- `CraftingRuntimeManager`：可制作次数计算、跨制作仓库扣材料 / 放产出（执行一次制作）；**无自身状态、不存档**，连续制作由 UI 层循环驱动。
- `EquipmentRuntimeManager`：按 `装备组 ID → (槽位 ID → 已装备道具 ID)` 维护已装备状态；装备 / 卸下 / 交换与 `InventoryRuntimeManager` 协作搬运道具（放不回旧道具时回滚），限制匹配 **全部 AND**，自动找槽，按「装备属性字段列表」跨已装备道具求和得总加成；**有已装备状态存档**（`GetSaveData` / `LoadSaveData`）；事件 `OnEquipmentChanged`。

商店刷新所需的三种时钟（游戏 / 本地 / 服务器时间）由 `InventoryRuntimeManager.RegisterTimeGetter` 注册，未注册回退系统本地时间。

### 程序集划分

| asmdef | 内容 |
|--------|------|
| `InventorySystem.Runtime` | 数据模型、管理器、序列化（运行时核心） |
| `InventorySystem.UI` | 运行时 UI 组件；引用 Runtime 与 TextMeshPro |
| `InventorySystem.Editor` | 编辑器窗口与面板 |
| `InventorySystem.Addressables.Runtime` / `.Editor` | Addressable 资源加载支持 |
| `InventorySystem.UI.Localization` | TMP 文本 / 字体本地化事件 |

---

## 扩展指南

新增子系统（技能等）时（商店 / 制作 / 装备已按此模式实现，可作参考）：

1. 在 `InventoryDatabase` 增加对应数据列表（+ getter / `CloneFrom` / `Validate`）；
2. 在 `Editor/` 下新建子目录，实现三列面板（复用 `AttributeDefinitionListDrawer` 和 `AttributeFieldDrawer`）；
3. 在 `InventoryEditorWindow` 注册新页签（+ 重复 ID 扫描 / `RebuildAllAttributes`）；
4. 在 `InventoryDataManager` 添加对应查询方法；运行时逻辑用轻量单例（`InventorySystemSingleton<T>`）承担；
5. （可选）在 `InventoryDtoModels.cs` 增加 DTO 镜像——商店 / 制作 / 装备均未做，因运行时直接消费已注册的 `InventoryDatabase`（ScriptableObject），DTO 导出目前仅覆盖道具系统数据。

属性系统（`AttributeValue / AttributeDefinition / EFieldType`）、枚举类型、功能标签、DTO 序列化框架均可直接复用。
