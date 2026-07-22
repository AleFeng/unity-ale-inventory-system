# 属性系统（Attribute System）

- 返回 [说明文档](../README.md)

灵活属性系统是道具 / 仓库 / 商店 / 制作四大子系统共用的数据基础。本文档说明属性字段的类型、`AttributeValue` 的存储与取值、显示字符串与排序比较规则。各子系统的「自定义属性字段」「属性字段显示」「整理排序」均建立在此之上。

# 📜目录

- [属性系统（Attribute System）](#属性系统attribute-system)
- [📜目录](#目录)
- [概念](#概念)
- [属性字段类型参考](#属性字段类型参考)
- [枚举类型与稳定引用](#枚举类型与稳定引用)
- [读取属性值（运行时 API）](#读取属性值运行时-api)
- [资源字段的加载（Addressables）](#资源字段的加载addressables)
- [本地化（固定 Text 字段与工具）](#本地化固定-text-字段与工具)
- [显示字符串 ToDisplayString](#显示字符串-todisplaystring)
- [排序比较数值 ToComparableNumber](#排序比较数值-tocomparablenumber)
- [StringIntPair 与价格 / 货币](#stringintpair-与价格--货币)
- [EnumIntPair 与装备属性加成](#enumintpair-与装备属性加成)

# 概念

属性系统由三个类型组成：

| 类型 | 角色 | 说明 |
|------|------|------|
| `AttributeDefinition` | **定义（schema）** | 一个属性字段的定义：`id`（Key）、`type`（`EFieldType`）、`isArray`（是否数组）、`enumTypeRef`（枚举类型名）、默认值。配置在 道具模板 / 功能标签 / 仓库模板 / 商店模板 / 蓝图模板 / 枚举项 上。 |
| `AttributeValue` | **值（tagged-union）** | 按 `type` 实际存储一个或一组值。采用「标签联合 + 数组」存储，原生支持 SO 序列化与 Undo/Redo。 |
| `AttributeEntry` | **键值对** | `id` + `AttributeValue`，构成具体条目（道具 / 仓库 / 商店 / 蓝图 的 `values` 列表）。 |

一个道具携带哪些属性字段，由其「来源模板自有字段 + 模板锁定标签字段 + 道具自选标签字段」共同决定，`RebuildAttributes` 负责按定义同步实际的 `values` 列表（增补缺失、移除孤立、保留已有值）。仓库 / 商店 / 蓝图同理（仅从各自模板收集）。

# 属性字段类型参考

以下类型均可设置为**数组形态**（勾选「数组」后可存储多个值，支持动态增删）。

| 类型 | 存储 | 编辑器控件 |
|------|------|-----------|
| Bool | 整数 0/1 | Toggle |
| Int | 整数 | IntField |
| Float | 浮点 | FloatField |
| String | 字符串 | TextField |
| **Text** | 启用 `IS_LOCALIZATION` 时 含本地化引用：表+条目（string字段作为fallback）| 本地化条目选择器 + 文本框 |
| Vector2 / 3 / 4 | 2 / 3 / 4 个浮点 | 多列 FloatField |
| VectorInt2 / 3 / 4 | 2 / 3 / 4 个整数 | 多列 IntField |
| Color | 4 个浮点（RGBA） | ColorField |
| **Enum** | 整数（枚举值） | Popup 下拉（需同时选择枚举类型） |
| **StringIntPair** | 字符串 + 整数 | TextField + IntField（如 货币ID → 价格） |
| **EnumIntPair** | 枚举 + 整数（整数后备列表，步长 2） | Popup 下拉 + IntField（需同时选择枚举类型；如 角色属性类型 → 加成数值） |
| Sprite | UnityEngine.Object | 正方形预览 |
| Prefab / Texture / Material / AudioClip / AnimationClip / PhysicsMaterial / PhysicsMaterial2D | UnityEngine.Object | ObjectField |
| AnimationCurve | AnimationCurve | CurveField |

> **底层存储**：所有值按类型分类压平到 `List<int>` / `List<float>` / `List<string>` / `List<Object>` / `List<AnimationCurve>` 五个后备列表中（标量存 `[0]`，向量按步长压平，数组顺序排布）。对象类字段的 Addressable 地址 / 授权 GUID 另存于与 `List<Object>` 平行的地址列表（见 [资源字段的加载](#资源字段的加载addressables)）。详见 [架构说明](Architecture.md#核心数据模型)。

# 枚举类型与稳定引用

- 枚举类型（`EnumType`）维护单调递增的 `nextValue`：添加枚举项时分配并自增，**删除不回收**。
- 属性值存储的是**枚举值（int）**而非显示索引，因此**重排枚举项显示顺序不会破坏已有引用**。
- 枚举项（`EnumItem`）本身可携带一组自定义属性字段（如各品质 / 职业的附加数据）；道具 Inspector 中选中某枚举值后，其子属性会以只读方式展开显示。

# 读取属性值（运行时 API）

道具 / 仓库 / 商店 / 蓝图都继承或实现了属性访问（道具经 `AttributeOwner` 基类）：

```csharp
using InventorySystem.Runtime;

Item item = InventoryDataManager.Instance.GetItem("sword_01");

// 取条目（含 AttributeValue），未找到返回 null
AttributeEntry entry = item.GetEntry("攻击力");

// 取强类型值（带 fallback）；T 与字段类型匹配（int/float/string/Vector3/Color/...）
int   atk   = item.GetAttributeValue<int>("攻击力", 0);
string desc = item.GetAttributeValue<string>("描述");

// 取原始 AttributeValue（需要类型判断 / 数组 / 多值时）
AttributeValue av = item.GetAttributeValue("价格");
```

`AttributeValue` 提供按类型的只读访问器：`GetInt` / `GetFloat` / `GetString` / `GetVector2~4` / `GetColor` / `GetStringIntPair` / `GetObject` / `GetAnimationCurve` 等（均按下标、越界安全）。`Type` / `IsArray` / `Count` 描述其形态。

# 资源字段的加载（Addressables）

对象类字段（Sprite / Prefab / Texture / Material / AudioClip / AnimationClip / PhysicsMaterial / PhysicsMaterial2D）的加载统一经 `InventoryAssets` 门面，与是否启用 Addressables 解耦：

```csharp
using InventorySystem.Runtime;

// 绑定道具某属性的资源到 UI，宿主 GameObject 销毁时自动释放句柄
InventoryAssets.Bind<Sprite>(item, "图标", image.gameObject, s => { image.sprite = s; });
// 或直接传 AttributeValue（可指定数组元素 index）
InventoryAssets.Bind<Sprite>(attrValue, owner, s => image.sprite = s, index);
```

- **未启用 `IS_ADDRESSABLE`（直接模式）**：属性字段直接挂载 Unity 资源引用（`objRefs`），加载配置数据时资源随之载入内存；门面同步返回该实时引用（编辑器控件为 `ObjectField`）。
- **启用 `IS_ADDRESSABLE`（授权模式）**：编辑器中对象字段改用原生 **AssetReference** 可搜索选择器，配置只存 GUID（不硬引用，加载数据库不再把资源一并载入内存）；运行时经 Addressable 按需**异步加载**、按地址引用计数，宿主销毁时**自动卸载**。导出时被引用资源自动登记进 `InventorySystem` Addressable 分组。

> 两种存储的磁盘格式不同、无法靠同名字段自动共用。切换宏后用菜单 **Tools/Inventory System/Addressables** 在「Object 引用 ↔ AssetReference(GUID)」间一键互转某个数据库的全部资源字段。
>
> 底层：授权 GUID / 运行时地址与实时引用平行存于 `AttributeValue`（地址列表 vs `objRefs`）；门面优先用实时引用，无则回退地址异步加载。core 程序集对 Addressables 零依赖，原生选择器经受约束的 Addressable 编辑器程序集注入（同 `InventoryExportResolver` 的注入模式）。
>
> 配置类的**固定资源字段**（具名字段，如 `Skill.icon`、`SkillTemplate.icon`、`FunctionTag.backgroundSprite`）采用同一套机制：各带一个平行的 `xxxAddress` 纯字符串字段，编辑器经 `InventoryAssetRefField` 绘制（直接 `ObjectField` / 授权 AssetReference 选择器），运行时同样经 `InventoryAssets.Bind(liveRef, address, owner, set)` 异步取用。

# 本地化（固定 Text 字段与工具）

全库的本地化显示文本统一由 `EFieldType.Text` 承载：`AttributeValue` 以「纯文本 fallback + 表引用 + 条目 Key」三槽扁平存储（原生序列化 / Undo / 导出友好），运行时 `AttributeValue.ResolveText()` **本地化优先、取不到回退纯文本**。既包括属性系统里 Text 类型的自定义属性值，也包括各配置类的**固定 Text 字段**：

| 配置类 | 固定 Text 字段 |
|--------|---------------|
| `Skill` / `SkillTemplate` / `CraftingBlueprint` | `displayText`（名称）、`descriptionText`（描述） |
| `Shop` / `Inventory` / `EquipmentGroup` / `FunctionTag` | `displayNameText`（名称）、`descriptionText`（描述） |
| `GroupTag`（技能 / 制作 / 装备 分组标签） | `displayName`、`description` |
| `NumberFormatRule` | `suffixText`（数字后缀） |
| `SortOption` | `displayName`（整理下拉显示名） |

编辑器一律经 `AttributeFieldDrawer` 绘制 Text（纯文本框 + 原生可搜索的表 / 条目选择器）；运行时读取用 `ResolveText()`。

## 本地化工具窗口

`Tools > Inventory System > Localization > 本地化工具窗口`（仅 `IS_LOCALIZATION`；欢迎窗口亦有入口按钮）。为一个 `InventoryDatabase` 一站式接入 Unity Localization：

1. **生成 / 关联多语言表**：按当前 Locale 生成一个 String Table 集合（表名 `{前缀}_{数据库名}`，前缀 / 生成文件夹可配并记忆），并把其 `SharedTableData` 的 GUID 记录到数据库（1:1，字段 `InventoryDatabase.LocalizationTableCollectionGuid`）。「关联多语言表」也可手动新建 String Table Collection 后拖入挂载；「编辑」按钮打开该表的 Table Editor。
2. **生成 多语言Key**：遍历库内**所有** Text 字段，逐帧生成唯一**中文 Key**（`道具系统-{类别}-{实例id}-{字段}[-{元素}]`，如 `道具系统-道具条目-{道具id}-名称`、`道具系统-枚举类型-{枚举名}-{枚举项名}-{属性id}`），写回字段的表 / 条目引用，并在表中建 Key→Value 条目。仅处理有纯文本内容的字段，同名 Key 追加 `#n` 去重。
3. **两个勾选项**：
   - **覆盖 已存在多语言Key**：勾选后执行前弹确认；已配 Key 的字段改用自动生成的 Key（命名与现有相同则不动）。不勾选则跳过已配字段。
   - **填入 Text中的String文本**：勾选后把源 Text 的纯文本值作为初始值填入该 Key 在**所有语言表**的空条目（不覆盖已有译文）。

> 中文 Key 完全可用：Unity Localization 支持 Unicode Key，运行时按 Key 解析，中英文性能无实质差异。本工具与 `InventoryAddressableToolWindow`（资源引用迁移）共享基类 `InventoryToolWindowBase`（逐帧步进 + 进度条 + 可选择日志）。

# 显示字符串 ToDisplayString

把属性值拼接为可读字符串，按 `EFieldType` 采用不同规则，供 UI 直接显示（如制作蓝图条目的「属性字段显示」）：

```csharp
string text = item.GetEntry("攻击力")?.value?.ToDisplayString();
```

| 类型 | 显示形式 |
|------|---------|
| Int / Float | 直接转字符串（Float 保留至多 2 位小数） |
| Bool | `是` / `否` |
| String | 原文 |
| Enum | 枚举项显示名 |
| Vector2 / 3 / 4 | `(x, y[, z[, w]])` |
| VectorInt2 / 3 / 4 | `(x, y[, z[, w]])` |
| Color | `RGBA(r, g, b, a)` |
| **StringIntPair** | `key: value`（如 `金币: 120`） |
| **EnumIntPair** | `枚举名: value`（如 `力量: 10`；键解析为枚举项显示名） |
| Text | 纯文本；为空时退回本地化条目键 / 表引用 |
| AnimationCurve | `曲线(N 关键帧)` |
| 对象引用 | 资源对象的 `name` |

数组形态时，对每个元素分别拼接，用分隔符（默认 `、`）连接。读取过程为非破坏性，不会改动底层数据。

# 排序比较数值 ToComparableNumber

整理排序时，自定义属性字段按 `EFieldType` 折算为一个用于比较的 `double`：

| 类型 | 比较依据 |
|------|---------|
| Int / Bool / Enum | 数值本身 |
| Float | 数值本身 |
| Vector2 / 3 / 4 | **模长（magnitude）** |
| Color | 作为 Vector4 的模长 |
| VectorInt2 / 3 / 4 | 模长 |
| **StringIntPair** | 仅取其中的 **Int 值** |
| **EnumIntPair** | 仅取其中的 **Int 值** |
| String / 对象引用 / 曲线 / 本地化 | 无可比数值 → `0` |

> `String` 类型字段在排序时由比较器特殊处理（先按长度、再按字典序），不走数值折算。详见 [仓库系统 - 整理排序](WarehouseSystem.md#整理排序)。

底层入口：`InventoryRuntimeManager.GetAttrNumeric` → `AttributeValue.ToComparableNumber()`；向量分量读取为非破坏性（不扩容底层列表）。

# StringIntPair 与价格 / 货币

`StringIntPair`（字符串 + 整数对，且通常为数组）是「价格 / 货币」的载体：

- 商店「价格属性来源」指向道具上一个 `StringIntPair` 数组属性，每个元素 = `货币ID → 单价`，支持一件商品标多种货币价格。
- 运行时由 `ShopRuntimeManager.GetUnitPrice` 读取并乘以商品价格倍率。详见 [商店系统 - 价格来源](ShopSystem.md#价格来源)。
- 排序时 `StringIntPair` 仅比较 Int 值（价格）；显示时拼为 `货币: 价格`。

# EnumIntPair 与装备属性加成

`EnumIntPair`（枚举 + 整数对，通常为数组）是「角色属性加成」的载体，与 `StringIntPair` 结构对称，区别在于键为**枚举**而非任意字符串：

- 每个元素 = `角色属性类型（枚举键） → 加成数值（整数）`，支持一件装备对多种角色属性各自加成。
- 需为该字段选择对应的**枚举类型**（如「角色属性类型」枚举，同 `Enum` 字段）；编辑器以 Popup + IntField 成对录入，底层平铺存于整数后备列表（步长 2：枚举值 + 整数值），枚举类型经 `EnumTypeRef` 记录。
- 存储的是**枚举值（int）**而非显示索引，重排 / 改名枚举项不破坏已有引用。
- 显示时拼为 `枚举名: 值`（如 `力量: 10`）；排序时仅比较其中的整数值。
- 运行时经 `GetEnumIntPair(index)` 读取 `(enumValue, value)`。装备加成的完整配置与结算详见 [装备系统](EquipmentSystem.md)。
