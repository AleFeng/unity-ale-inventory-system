# 更新日志（Changelog）

本文件记录 Inventory System（`com.ale.inventory`）的所有重要变更。

格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

> 迁移说明（2026-07-22）：包标识 `com.fs.inventorysystem` → `com.ale.inventory`；程序集 `Fs.InventorySystem.*` → `Ale.Inventory.*`、命名空间 `InventorySystem.*` → `Ale.Inventory.*`；插件位置由 `Assets/Plugins/InventorySystem` 迁移至内嵌 UPM 包 `Packages/com.ale.inventory`。版本号保持 1.4.0。

## [1.5.0] - 2026-07-23

一轮以「修 Bug + 去性能热点 + 对齐文档」为主的维护版本。**含两项破坏性变更**（枚举标识符改名、商店交易进度存档键改造），升级前请先读本节末尾的「破坏性」与「升级指引」。

### 修复
- **整理排序「忽略ID」对枚举字段匹配错误**：`InventoryRuntimeManager.IsIgnoredByField` 此前用 `enumType.items[枚举值]`——把**枚举值当成了列表下标**。而 `EnumItem.value` 是自增、永不回收的不可变值，只要删过枚举项，二者就会错位，导致忽略规则匹配到错误的枚举项名或静默失效。现改用 `EnumType.GetItemByValue`（与 `AttributeValue.ToDisplayString` 的解析链一致）；枚举类型引用在属性定义缺失时回退属性值自身持久化的 `EnumTypeRef`。
- **`AttributeValue` 切换类型 / 数组形态后残留 Addressable 地址**（仅 `IS_ADDRESSABLE` 可见）：`ChangeType` 未清空、`SetIsArray` 未裁剪与 `objRefs` 平行的 `objAddresses`，导致「Sprite → Int → Sprite」后旧资源 GUID 仍在，取用门面会回退加载到**上一个资源**；`SetObject` 也未同步扩容，使两列表长度不一致时 `ReorderElements` 会把地址与对象引用的对应关系错开。现三处均已同步，并对 `objAddresses` 为 null 的旧数据加了容错。
- **非 MonoBehaviour 单例的 `IsQuitting` 恒为 `false`**：该属性从未被赋值（只有 MonoBehaviour 版在 `OnApplicationQuit` 中赋值），是一个会误导调用方的死 API。现接入 `Application.quitting`。

### 新增
- **`InventorySingletonRegistry`（内部）**：单例静态状态的重置中枢。`[RuntimeInitializeOnLoadMethod]` 无法标注在泛型类型的方法上，故由各闭合泛型在首次创建实例时登记重置动作，由本类在每次播放开始（`SubsystemRegistration`）统一执行。**关闭 Domain Reload**（Enter Play Mode Options）时，上一次 Play 注册的数据库、装备 / 商店状态与 `IsQuitting` 不再跨播放会话残留。
- **`InventoryRuntimeManager.SortByItemId<T>(list, itemIdSelector, priorities, db)`**：按道具 ID 对任意列表做显示排序的公开入口。整次排序只建一份字段查表、只用两个复用的临时槽位；此前 UI 层的写法是在比较器里每次比较 `new` 两个 `RuntimeItemSlot`。
- **`InventoryDataManager.InvalidateIndex()`**：手动使跨库查询索引失效（运行期直接改动了已注册数据库内容时使用；注册 / 注销 / 清空会自动失效）。
- **`InventoryDatabase.EnsureShopEntryGuids()` / `NewShopEntryGuid()`**：为商品组 / 商品补发缺失的稳定 `guid`，由配置编辑器自动调用。
- **`package.json` 补全 `documentationUrl` / `changelogUrl` / `licensesUrl`**，Package Manager 面板中可直接跳转文档 / 更新日志 / 许可证。

### 性能
- **`InventoryDataManager` 建立跨库查询索引**：15 个 `GetXxx(id/name)` 由「逐库 + 库内双重线性遍历」改为字典查找（O(1)），另加 3 个「条目 → 所属数据库」映射供 `FindDatabaseForXxx`。索引惰性构建、注册 / 注销 / 清空后置脏。构建按数据库注册顺序**先到先得**，与旧的「第一个命中的数据库优先」语义完全一致。该接口在**每个 UI 格子绑定**与**排序的每次两两比较**中都会被调用，是随道具总量放大的主要热点。
- **排序比较预计算查表（`SortLookup`）**：把原先在每次两两比较里重复做的线性扫描——整理选项忽略列表、属性字段定义、道具模板、枚举类型、功能标签序号——预先算成字典，比较器内查找降到 O(1)。`SortInventory` / `SortSlots` / `SortByItemId` 与 UI 列表的显示排序整次只建一份，用完即弃（不跨帧缓存，无缓存过期问题）。`CompareSlots` / `CompareByField` / `IsIgnoredByField` / `GetTagOrder` 的公开签名保持不变，内部改为薄封装。
- **`InventoryDatabase.RebuildSortOptions` 消除两处二次复杂度**：末尾排序的比较器内原用 `List.IndexOf` 查序号（O(n² log n)）、追加新字段时原逐个 `GetSortOption` 线性查找（O(n²)）；现统一用一个字段序号字典完成去重、可用性判定与排序取序号。
- **整理选项面板改为按需重建**：`SortOptionPanel.DrawMasterList` 此前**每次 OnGUI**（含每次鼠标移动）都无条件调用 `RebuildSortOptions`。现改为对「道具模板属性 + 功能标签属性 + 整理选项 schema」算一个无分配的整数签名，签名不变即跳过——该签名覆盖了重建产出的全部输入，因此不会漏同步（含字段改名这种数量不变的改动）；换库 / Undo-Redo 经已有的 `Invalidate()` 强制重同步。
- **`InventoryRuntimeManager.TryAddItem` 空槽查找改用递进游标**：原先每轮都 `List.Find` 从头扫并分配一个闭包委托（O(n²)）；循环内只填槽从不清空，故游标只进不退，整个循环合计只扫一遍列表。「添加所有配置表道具」测试功能受益最明显。
- **`GetSlots` 未命中返回共享空列表**，不再每次分配。

### 变更
- **`InventoryRuntimeManager.GetSlots` 的返回值契约明确为「仅供读取」**：命中时返回的是运行时状态的**实时引用**（此前即如此，只是未写进文档），未命中时返回**全局共享**的空列表——两种情况下写入都会造成意外后果。修改内容请走 `TryAddItem` / `TryRemoveItem` / `SetSlotContent`；需要自行排序 / 过滤请先拷贝一份。
- **工程配置**：清理两处迁移遗留的悬空 asmdef GUID 引用（`Ale.Inventory.Runtime` / `Ale.Inventory.Editor`，在本工程中均无法解析、本就被 Unity 丢弃）；`package.json` 的 `author.url` 由旧仓库 `Able-Games/unity-fs-game-framework` 改指本仓库。
- **最低 Unity 版本口径统一为 `2022.3`**（`package.json` 声明值），包内 README 此前写作 `6000.3+` 与之矛盾；三语 README 统一表述为「最低 2022.3，基于 Unity 6000.3 开发与维护」。
- **编辑器**：`InventoryEditorWindow` 移除已不可达的 `DrawStub` 与 `DrawBody` 的 `else` 分支（六个系统页签均已实现），页签分发改为 `switch`。

### 文档
- **中文文档同步 1.4.0 的迁移结果**：`README.md` 与 `Docs~` 下 10 份中文文档中的 `InventorySystem.Runtime` / `InventorySystem.UI` / `Assets/Plugins/InventorySystem/` 等陈旧标识符全部更新为 `Ale.Inventory.*` 与 `Packages/com.ale.inventory/`（此前 `_EN` / `_JA` 版本反而是正确的，中文源文档落后于译文；共 30 处）。`UIComponentGuide` 中「asmdef `rootNamespace` 与实际声明不符」的注意事项已随迁移失效，一并删除。CHANGELOG 中的历史记述保留旧名不动。
- **`Architecture` 三语版补全技能系统页签**：编辑器结构树此前完全没有 `SkillSystemTab`，只有一句「技能系统页签当前为占位（DrawStub），后续阶段实现」——技能系统早已实现。现按实际面板补入 `SkillGroupTagPanel` / `SkillTemplatePanel` / `SkillListPanel`。

### ⚠️ 破坏性
- **三个公开枚举的成员标识符由中文改为英文**（Inspector 中 `EListScrollDirection` 仍显示中文）：

  | 枚举 | 旧成员 | 新成员 |
  | --- | --- | --- |
  | `ShopTimeType` | 游戏时间 / 本地时间 / 服务器时间 | `GameTime` / `LocalTime` / `ServerTime` |
  | `ShopRefreshType` | 不刷新 / 每日 / 每周 / 每月 | `Never` / `Daily` / `Weekly` / `Monthly` |
  | `EListScrollDirection` | 纵向 / 横向 | `Vertical` / `Horizontal` |

  **数据无需迁移**：三者原本都用隐式值 `0,1,2…` 且顺序不变，本次把值**显式写死**为相同数字（防止日后重排成员时悄悄改掉序列化值），因此既有 `.asset` / `.prefab` 中已配置的刷新周期、时间类型、滚动方向都会原样保留。**仅项目层代码需要改名**（如 `RegisterTimeGetter(ShopTimeType.服务器时间, …)` → `ShopTimeType.ServerTime`）。
  三者均加了 `[InspectorName("中文")]`。注意该特性**只在经 `SerializedProperty` 绘制时生效**：`EListScrollDirection` 走默认 Inspector，仍显示「纵向 / 横向」；商店刷新面板用 `EditorGUILayout.EnumPopup` 绘制，下拉显示的是英文标识符。
- **商店交易进度的存档键改为稳定 `guid`**：`ShopCommodityGroup` / `ShopCommodity` 新增序列化字段 `guid`。此前的键是 `组名（空名回退 #组索引）` + `组内索引:道具ID`——策划在编辑器里**拖拽重排商品或商品组**，老存档的「已交易次数」就会挂到别的商品上（限购商品凭空恢复或直接买不了）。
  - **旧存档自动迁移**：迁移在**查询进度时惰性完成**——稳定键下无条目、旧键下有，则把该条目的键就地改写为稳定键后复用，保住玩家已积累的次数。不依赖存档加载顺序，幂等，不丢弃任何条目。
  - **guid 补发**：1.4.0 及更早的数据没有该字段，打开配置编辑器时由 `EnsureShopEntryGuids()` 自动补发并标脏（**需保存数据库资产**）。
  - **未补发时行为不变**：`guid` 为空的条目（含运行期合成的回收商品）一律回退旧键，与 1.4.0 逐位一致——因此即使某个项目升级后未打开过编辑器就直接出包，也不会出现「所有商品共用空键」的串档。

### 升级指引
1. 全局搜索并替换项目层对三个枚举中文成员的引用（见上表）。
2. 用配置编辑器打开每个 `InventoryDatabase` 并**保存**，让商品组 / 商品的 `guid` 落盘。
3. 若项目层写过 `GetSlots(...)` 返回值，确认没有对其做增删改。
4. 建议在 `IS_TMP` / `IS_LOCALIZATION` / `IS_ADDRESSABLE` 宏开 / 关的各种组合下重新编译验证一次。

## [1.4.0] - 2026-07-06
### 新增
- **本地化工具窗口 `InventoryLocalizationToolWindow`**（`IS_LOCALIZATION`；菜单 `Tools > Inventory System > Localization > 本地化工具窗口`，欢迎窗口亦有入口按钮）：为指定 `InventoryDatabase` 一站式接入 Unity Localization——
  - **生成 / 关联多语言表**：按当前 Locale 生成一个 String Table 集合（表名 `{前缀}_{数据库名}`，前缀 / 文件夹可配并记忆），把其 SharedTableData GUID 记录在数据库上（1:1）；「关联多语言表」为 `StringTableCollection` 字段，也可手动新建集合后拖入挂载；「编辑」按钮打开该表的 Table Editor。
  - **生成 多语言Key**：遍历库内**所有** `EFieldType.Text` 字段，逐帧生成唯一**中文 Key**（`道具系统-{类别}-{实例id}-{字段}[-{元素}]`，枚举 / 数字格式 / 分组标签等各有命名规则）、写回字段的表 / 条目引用、并在表中建 Key→Value 条目；含进度条 + 可选择日志 + 取消。
  - **两个勾选项**：「覆盖 已存在多语言Key」（勾选时执行前弹确认；已配 Key 改用自动生成的 Key，命名相同则不动）、「填入 Text中的String文本」（把源纯文本作为初始值填入**所有语言表**的空条目，不覆盖已有译文）。
- **`InventoryToolWindowBase` 工具窗口基类**：抽出「选数据库 + 逐帧时间预算步进 + 进度条 + 可选择日志 + 取消 + 完成收尾」通用能力；`InventoryAddressableToolWindow`（资源引用迁移）与本地化工具窗口均继承之（Addressable 工具行为不变）。
- **`InventoryDatabase.LocalizationTableCollectionGuid`**：记录数据库关联的 String Table 集合 GUID（供本地化工具读写）。
- **中间条目列表显示「名称 / 描述」**：六大系统「中间条目列表」在 ID 之后显示各条目的名称 / 描述（读 `displayNameText` / `descriptionText` 的纯文本 fallback）；仓库列表此前不显示名称，现一并补齐。

### 变更
- **本地化机制统一为 `EFieldType.Text`**：把此前各配置类的固定 `LocalizedString` 字段（Skill / SkillTemplate 的名 + 描述、Shop / FunctionTag / CraftingBlueprint 的名、NumberFormatRule 的后缀）全部重构为 `AttributeValue`(Text)（纯文本 fallback + 可选本地化引用，字段无条件存在、不再受 `#if IS_LOCALIZATION` 门控）；`ISkillConfig.DisplayName` / `Description` 改为 get-only `AttributeValue`。至此全库本地化显示文本只剩 Text 一套机制。
- **`Inventory` / `Shop` / `EquipmentGroup` / `FunctionTag` / `CraftingBlueprint` 名称 + 描述统一为固定 Text 字段** `displayNameText` + `descriptionText`（Skill / SkillTemplate / CraftingBlueprint 为 `displayText` + `descriptionText`）；`FunctionTag.description`、`CraftingBlueprint.description` 由「纯 string / 编辑器提示」升级为正式 Text 配置数据。
- **移除「标题取自自定义属性ID」机制**：删除 `UiwViewBase.titleAttributeId` 与 `EquipmentGroup.NameAttrId`；背包 / 商店 / 装备视图标题一律取固定 `displayNameText.ResolveText()`（`ResolveTitleText` 签名简化为 `(displayName, id)`）。
- 各 Inspector 面板的名称 / 描述改用 `AttributeFieldDrawer` 统一绘制 Text（纯文本框 + 原生可搜索本地化选择器）；运行时读取改用 `AttributeValue.ResolveText()`。
- 编辑器 asmdef 增加 `Unity.Localization.Editor` 引用（供本地化工具用）。

### 说明
- **一次性迁移已完成并清理**：重构伴随一次性数据迁移（旧固定 `LocalizedString` / 旧纯文本 / 装备组「名称·描述」自定义属性 → 新 Text 字段）；Unity 侧验证后已移除迁移器与全部 legacy 字段（`[FormerlySerializedAs]`），并删除已无用的 `LocalizedStringUtil` / `LocalizedStringEditorField`（`LocalizedStringHolder` 仍被 `AttributeFieldDrawer` 使用，保留）。
- **破坏性**：`ISkillConfig.DisplayName` / `Description` 类型由 `string` / `LocalizedString` 改为 `AttributeValue`（get-only）；`EquipmentGroup.NameAttrId`、`UiwViewBase.titleAttributeId` 移除——项目层若引用需改用固定 Text 字段（`displayNameText.ResolveText()`）。
- 导出层：这些显示名 / 描述除 `FunctionTag.description` 外均不入 JSON / 二进制导出（与 `displayNameText` 一致）；`FunctionTag` 导出改取 `descriptionText` 纯文本 fallback。
- **中文 Key**：Unity Localization 完全支持 Unicode Key，运行时按 Key 解析，中英文性能无实质差异；导出 CSV 请用 UTF-8。
- 请在 `IS_LOCALIZATION` / `IS_ADDRESSABLE` 宏开 / 关组合下重新编译验证。

## [1.3.3] - 2026-07-06
### 新增
- **仓库管理器「添加所有配置表道具」测试功能**：`InventoryRuntimeManager` 新增 `addAllConfiguredItems`（开关）+ `addAllItemCount`（每种道具数量，最小 1）。开启后进入 Play 遍历所有数据库（`databases`）的 `InventoryDatabase.Items`，把每种道具各按 `addAllItemCount` 添加到测试仓库；已在 `testItems` 中配置的道具会跳过（保留其指定数量、不重复添加），同一道具 ID 跨多个库仅添加一次。受 `autoPopulateOnStart` 主开关约束。
- **主视图目标 ID 暴露到 Inspector**：`UiwInventoryView`（`_inventoryIds`）、`UiwEquipmentView`（`_groupId`，默认「角色装备」）、`UiwShopViewBase`（`_shopId`）把打开目标 ID 以私有 `[SerializeField]` 暴露（`UiwShopViewBase` 另配 `protected ShopId` 属性）。可在 Inspector 预设默认 ID，视图始终使用该值，直到经 `Open(id)` 或 Inspector 改动。

### 变更
- **编辑器测试道具填充下沉到运行时管理器**：把「进入 Play 自动填入测试道具」从 `UiwInventoryView` 迁到 `InventoryRuntimeManager`（`autoPopulateOnStart` / `testInventoryId` / `testItems`，在 `Init()`（Awake）时机填充；**仅填充数据、不打开任何界面**，界面由各视图自行打开）。背包视图不再随 Play 自动打开；`DemoWizard` 的测试数据写入相应改到管理器。
- **视图 `Open` 方法模板化上提到基类**：`UiwViewBase` 新增 `public virtual void Open()` 模板方法（承载各视图唯一公共步骤——激活面板 `gameObject.SetActive(true)`）；背包 / 商店 / 制作 / 装备 / 技能五个主视图改为 `override Open()`，首行 `base.Open()` 复用公共代码；带参数的 `Open(...)` 退化为「缓存参数 → 调用无参 `Open()`」的薄重载。商店的 `Shop` 解析 + 类型校验下移进无参 `Open()`，使仅在 Inspector 预设 `_shopId`（不经 `Open(shopId)`）也能正常打开。
- **合并编辑器测试字段**：移除 `UiwEquipmentView.testGroupId` / `UiwShopViewBase.testShopId`，其「编辑器测试自动打开」（`autoOpenOnStart`）改用暴露的 `_groupId` / `_shopId`（调无参 `Open()`）；`DemoWizard` 两处 `FindProperty` 同步改名（`_groupId` / `_shopId`）。

### 说明
- **破坏性**：`UiwShopViewBase.ShopId` 由 `protected` 字段改为 `protected` 属性（backing 字段 `_shopId`）——读写保持兼容，但项目层若直接引用该字段需留意。
- 旧 Demo 预制体上的 `testGroupId` / `testShopId` 变为孤立序列化字段（Unity 重导入时自动丢弃）；商店视图的 `_shopId` 会为空（原 `testShopId` 值不迁移），需重跑 DemoWizard 或在 Inspector 手填才能恢复「Play 自动打开」。未手改任何 `.prefab` / `.meta`。
- 请在 `IS_ADDRESSABLE` / `IS_LOCALIZATION` 宏开 / 关组合下重新编译验证。

## [1.3.2] - 2026-07-06
### 新增
- **配置编辑器 · 条目列表键盘导航 + 自动滚动**：六大系统「中间条目列表」选中某条目后，可用 ↑ / ↓ 方向键在可见（已过滤）条目间逐行切换选中；新选中项超出可视区时自动滚动一行将其带回视野。正在编辑搜索框 / 文本框时不劫持方向键。（`EditorListKeyboardNav`）

### 变更
- **整理选项「名称 / 忽略ID」升级为内置专属字段**：不再依赖在通用「属性字段定义」中手工添加。`SortOption` 新增内置 `displayName`（`Text`：纯文本 fallback + 可选本地化引用，作排序下拉显示名）与 `ignoreIds`（可拖拽的字符串列表，排序时跳过的条目 ID，默认 **0 条**）。运行时经 `SortOption.ResolveDisplayName` / `SortOption.EffectiveIgnoreIds` 读取；`UiwSortToolbar` 移除 `sortOptionNameAttrId` / `sortOptionIgnoreIdAttrId` 映射字段；`InventoryRuntimeManager.CompareSlots` / `SortSlots` / `CompareByField` 移除 `ignoreAttrId` 参数。旧数据首次打开「仓库系统 → 整理选项」面板时自动迁移（把旧通用属性值搬入内置字段并从 schema 移除这两项，跳过空占位串），运行时对未迁移数据仍有兜底读取。
- **配置编辑器 · 条目列表统一为两行结构**：六大系统「中间条目列表」统一为「列名表头行 + 值行」两行布局、行高一致（此前商店 / 制作 / 装备 / 技能为单行）；表头显示各列字段名。
- `package.json` 的 `documentationUrl` 改为指向包内 `README.md`。

### 说明
- `SortOption` 移除通用属性值读取路径属破坏性 API 变更：若项目层直接调用过 `CompareSlots` / `SortSlots` / `CompareByField`，需同步去掉 `ignoreAttrId` 参数。
- 迁移在打开整理选项面板时进行、幂等；保存数据库后持久化。若旧「属性字段定义」中还配过「名称 / 忽略ID」以外的自定义属性，编辑器不再展示（数据保留于 `attributeValues`）。

## [1.3.1] - 2026-07-06
### 新增
- **覆盖式 UI 的 Layer 配置**：`InventoryRuntimeManager` 新增「覆盖式UI Layer」可选配置（`[Layer]` 特性 + `applyCoverUiLayer` 开关）与 `SetCoverUiLayer(int / string)` / `DisableCoverUiLayer()` / `ApplyCoverUiLayer(GameObject)` API。弹窗 / 悬停 Tooltip / 拖拽幽灵图标等覆盖式 UI 实例化后会重新套用指定 Layer——适配「独立 UI 摄像机、Culling Mask 仅渲染 UI 层」的场景（这些 UI 会分配独立 Canvas，打断父级 Layer，需重设）。
- **商店视图筛选 / 排序**：`UiwShopViewBase` 支持 `UiwFilterTabBar`（功能标签筛选页签）与 `UiwSortToolbar`（排序下拉 + 升降序 + 自动整理）；`Shop` / `ShopTemplate` 新增 `sortPriorities` / `sortTiebreakers`（整理排序）。
- **装备候选列表整理排序**：`EquipmentGroupTemplate` 与 `EquipmentGroup` 均新增「整理排序」（排序条件 + 整理优先级），经共享 `IEquipmentConfig` + `EquipmentConfigDrawer` 编辑；应用于装备选择面板可装备道具候选列表（`UiwEquipmentCandidateList`）的显示排序——从模板创建装备组时复制、之后可独立编辑。

### 变更
- **筛选 / 排序管线下沉到列表基类**：把 `UiwFilterTabBar` + `UiwSortToolbar` 的接线统一封装进 `UiwInventoryListBase`（可选、增量式：源 → 主 / 次页签筛选 → 额外筛选 → 排序 → 显示），提供 `ConfigureFilter` / `SetExtraFilter` / `ConfigureSort` / `SetSourceItems` 等 API；商店 / 制作 / 技能列表迁移到该管线复用，避免各视图重复接线（背包因拖拽整理 / 写运行时排序等耦合，保留自有逻辑）。
- **道具悬停 Tooltip 显示持有数量**：`UiwItemTooltip.Show(itemId, count, screenPos)` 贯通实际数量（此前恒为 1）。
- **DemoWizard** 相应更新：排序栏 / 筛选栏改接到列表组件而非视图。

### 说明
- 请在 `IS_ADDRESSABLE` / `IS_LOCALIZATION` 宏开 / 关组合下重新编译验证。

## [1.3.0] - 2026-07-05
### 新增
- **统一虚拟滚动列表引擎**：新增三层泛型架构——基类 `UiwInventoryItemListBase<TData,TCell>`（轴无关虚拟滚动引擎：对象池 + 视口尺寸监听 + 回收 / 复用循环）→ 通用 `UiwInventoryGridList` / `UiwInventoryOrderList`（网格 / 顺序布局策略）→ 各系统叶子（闭合泛型、塞入各自条目脚本）。**网格与顺序列表均为虚拟滚动**，仅渲染可见区域 + 缓冲、滚动循环复用。
- **网格纵向 / 横向滚动**：`EListScrollDirection` 枚举在 Inspector 切换；跨轴数量（列数 / 行数）按视口尺寸自动计算并随视口变化重排（弃用 `GridLayoutGroup`，改为手动定位）。
- **增量差异刷新**（`RefreshItemsData` + `NeedsRebind`）：仓库内容变化时只重绑"显示内容已变"的可见格（拖拽换位 / 就地堆叠通常仅 2 格），未变的格子不动——避免图标异步重载闪烁与无谓开销；仓库网格换位不再把滚动条复位到顶部（保留滚动位置）。新增 `UiwInventoryItemSlotBase.DisplayedCount` / `MatchesSlot` 供格子比较。
- **生成 / 分配限速**（`spawnPerSecond`，默认 30 个/秒）：把格子的实例化与绑定（含图标异步加载）分摊到多帧，避免单帧一次性生成 / 加载大量格子导致卡顿或资源加载堵塞；实例按需惰性创建到目标池上限，预算带封顶（约 0.1 秒的量）防"打开界面那一重帧"爆发实例化。`≤ 0` = 不限速（一帧填满）。
- **逐格浮现跟随滚动方向**：待分配格子按进入视口的先后顺序出现——向末尾滚动从前往后（纵向"从上往下"）、向起点滚动从后往前（纵向"从下往上"）。

### 变更
- **各系统列表迁移到统一引擎**：仓库（`UiwInventoryItemGridList` / `UiwInventoryItemOrderList`）、制作蓝图列表（`UiwCraftingBlueprintList`）、技能列表（拆为 `UiwSkillGridList` / `UiwSkillOrderList`）、装备候选列表（`UiwEquipmentCandidateList`）、商店商品列表（`UiwShopCommodityList`）均改为继承通用网格 / 顺序层，只重写"绑定 / 清空格子"。
- **仓库网格拖拽整理适配虚拟滚动**：格子数据索引随绑定动态更新；拖到视口边缘自动滚动；拖拽期间"钉住"源格子防止被回收停用而收不到拖拽事件。
- **商店选中次数迁到数据模型**（`ShopCommodityEntry.times`）：虚拟化后仅保留可见格，购物车总价 / 结算改为遍历全部商品数据（`UiwShopViewBase.Entries`）而非可见格，保证离屏商品的次数与结算正确。
- **DemoWizard** 相应更新列表预制体生成（ScrollRect + Viewport + Content 结构，接 `cellPrefab` / `scrollRect` / `content` / `scrollDirection`）。
- 使用文档更新：`UIComponentGuide.md` 重写"虚拟滚动列表"章节（三层架构 / 制作 Prefab / 参数 / 性能与体验 / FAQ），`README.md`、`SkillSystem.md`、`EquipmentSystem.md` 等同步。

### 说明
- **列表相关预制体结构调整（破坏性）**：Content 不再挂 `GridLayoutGroup` / `LayoutGroup` / `ContentSizeFitter`；网格改为 ScrollRect + Viewport + Content。建议重新运行 DemoWizard 生成，或手动为列表组件接好 `scrollRect` / `content` 引用并移除旧 LayoutGroup。
- 删除旧 `UiwSkillList`（拆为网格 / 顺序两类）；引用它的预制体需替换为 `UiwSkillGridList` / `UiwSkillOrderList`。
- 请在 `IS_ADDRESSABLE` / `IS_LOCALIZATION` 宏开 / 关组合下重新编译验证。

## [1.2.0] - 2026-07-05
### 新增
- **Unity Addressables 支持**：核心程序集对 Addressables 零依赖，通过 `[InitializeOnLoad]` 注入钩子桥接；属性系统的对象字段与固定资源字段（如 `Skill.icon` / `SkillTemplate.icon` / `FunctionTag.backgroundSprite`）改为以 GUID 授权存储（宏 `IS_ADDRESSABLE`）。
- **配置数据迁移工具窗口**：Object ↔ GUID 双向迁移；分帧处理并限制最小处理帧数与每帧处理时长，进度条可视刷新。
- **属性字段新增 `Text` 类型**：始终携带纯文本 fallback，启用 `IS_LOCALIZATION` 时额外携带 Unity Localization 引用（表 + 条目），运行时本地化优先、取不到回退纯文本。
- **`Sprite` 类型属性字段预览**：授权（Addressables）模式下显示左对齐正方形预览，可直接拖入图片替换。

### 变更
- **装备组**：删除固定的"名称""描述"字段，改由自定义属性字段（`Text` 类型，在装备组模板中定义）承载；仓库 / 商店 / 装备 UI 标题改为显示指定属性字段（默认 ID"名称"，可在 UI 上配置）。
- **分组标签**：制作 / 装备 / 技能三系统的分组标签提升到 `GroupTag` 基类；名称 / 描述改用 `Text` 类型属性值（不再单独声明 `LocalizedString` 字段）。装备属性字段的显示名同样改用 `Text` 类型。
- **Addressables 模式**：配置资源不再自动创建 Addressable Group / Entry，改为 `LogWarning` 提示，交由用户显式登记。
- 属性字段类型使用文档（`AttributeSystem.md`）更新，补全全部字段类型说明。

### 说明
- `Text` 字段与固定 `LocalizedString` 字段的序列化结构不同；`string` 字段改为 `Text`（`AttributeValue`）属于破坏性数据变更，旧序列化值会丢失，需重新填写（编辑器绘制前会自动归一为 `Text` 类型，不会报错）。
- 请在 `IS_ADDRESSABLE`、`IS_LOCALIZATION` 宏开 / 关的组合下分别重新编译验证。

## [1.1.0] - 2026-07-04
### 新增
- **Unity Localization 支持（基础框架）**：各配置数据的固定字段（名称 / 描述 / 数字后缀等）支持本地化，均带纯文本 fallback；提供 `LocalizedStringUtil` / `LocalizedStringEditorField` 统一解析与编辑。

### 变更
- UI 工具类优化：Canvas 缓存机制提升性能；补充 UI 空间坐标转换与设置 API。

## [1.0.2] - 2026-07-04
### 变更
- **UPM 插件化**：拆分为独立 UPM 包（`com.ale.inventory`），调整依赖插件包。
- 程序集命名空间统一更新。
- 新增弹窗 UI 父节点设置 API。
- WelcomeWindow 界面优化：测试工具新增"预制体自动生成"列表。

## [1.0.0] - 2026-06-06
初始基线版本：六大系统 + 属性系统 + 一体化编辑器。

### 新增
- **属性系统（Attribute）**：`AttributeValue` 标签联合，覆盖 Int / Float / String / Bool / Enum / Vector2·3·4 / Color / VectorInt2·3·4 / 资源引用（Sprite / Prefab / Texture / Material / AudioClip / AnimationClip / 物理材质）/ AnimationCurve / StringIntPair / EnumIntPair 等类型；支持数组形态、Inspector 拖拽重排、按类型排序比较、以及配置字段值 Ctrl+C / Ctrl+V 复制粘贴。
- **道具系统（Item）**：道具模板、功能标签、自定义属性字段。
- **仓库系统（Inventory）**：仓库配置、道具增删查、重量 / 容量；仓库 UI（网格道具列表、顺序 / 明细列表、功能标签筛选页签、排序与升 / 降序切换、无限循环列表、道具详情弹窗、数量计数器、折叠标签组件）。
- **商店系统（Shop）**：商店模板与类型、购买次数长按加速、结算时校验背包货币与容量并自动下调至最大可购买次数、交易功能标签限制、商店 UI（网格 / 列表模式，过滤标签下仅显示符合条件道具）。
- **制作系统（Crafting）**：蓝图、分组标签（主 + 副分组）、蓝图自定义属性字段显示、运行时管理器、制作 UI（含分组折叠页签）。
- **装备系统（Equipment）**：装备组 / 槽位列表 / 装备槽、装备仓库配置、属性加成汇总面板（按分组标签分组、无加成不显示）、拖拽装备 / 卸下、右键快速装备与替换、装备选择面板；槽位列表支持自动生成与手动配置两种方式；新增 `EnumIntPair` 属性类型用于配置"角色属性:加成数值"。
- **技能系统（Skill）**：技能数据 / 模板、运行时管理器（已学技能状态）、技能 UI（列表 / 条目 / Tooltip / 位阶背景框）。
- **数字格式配置**：全局配置，支持多语言后缀。
- **编辑器**：一体化配置窗口（各系统标签页）、DemoWizard 自动生成测试预制体（含 Localization 字体 / 文本按语言自动替换）、WelcomeWindow 引导与测试工具。
