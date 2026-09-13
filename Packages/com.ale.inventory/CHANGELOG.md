# 更新日志（Changelog）

本文件记录 Inventory System（`com.ale.inventory`）的所有重要变更。

格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

> 迁移说明（2026-07-22）：包标识 `com.fs.inventorysystem` → `com.ale.inventory`；程序集 `Fs.InventorySystem.*` → `Ale.Inventory.*`、命名空间 `InventorySystem.*` → `Ale.Inventory.*`；插件位置由 `Assets/Plugins/InventorySystem` 迁移至内嵌 UPM 包 `Packages/com.ale.inventory`。版本号保持 1.4.0。

## [1.14.0] - 2026-09-09

**道具右键从「即刻快速装备」改为弹出操作菜单：查看 / 使用 / 丢弃。** 此前道具格子的右键是**唯一**的点击交互，且被「快速装备」独占（`UiwInventoryItemSlotBase.OnPointerClick` 直接广播 `ItemRightClicked`，装备界面订阅后自动装备）；左键完全没有处理。1.12.0 加进来的 `UseItemInSlot` 其文档注释就写着「右键槽位『使用』的入口」，但一直没有调用方；「丢弃」则从来没有 UI。本版把右键改成统一的菜单入口，把快速装备降级为菜单里的一个条目，并补上「查看」「丢弃」两条路径。菜单与模态弹窗的外壳下沉到 toolkit（`1.13.0`）作为通用件，本包只负责组装条目与内容。另为「丢弃」补上数据侧的开关：道具配置新增 **不可丢弃**，让重要剧情道具丢不掉。**除这一个布尔字段外均为纯 UI / 运行时层改动；导出格式版本随之 v9 → v10（向后兼容，旧文件该位取默认 false）。**

### 破坏性变更

- ⚠️ **右键道具不再直接快速装备**。装备界面打开、且该道具确实装得进当前装备组时，菜单里会出现「装备」条目，点它才装备。`UiwInventoryItemEvents.ItemRightClicked` 事件与 `RaiseItemRightClicked` **原样保留**、语义不变，只是触发时机由「右键即触发」变为「右键 → 点『装备』」——包外的订阅方无需改动。
- ⚠️ **最低 `com.ale.toolkit` 版本提至 1.13.0**（`UiwContextMenu` / `UiwModalPopupBase` / `UiPrefabBuilder.MakeSlider` / `MakeFullScreenBlocker`）。安装顺序不变：先 `com.ale.toolkit`、再本包。


#### 移除 Demo 预制体生成向导

向导（`InventoryDemoWizard`，约 3600 行编辑器代码）诞生于插件早期，那时 Demo 资产还没成形。随着样本被手工调整（自定义道具数据、面板布局微调），**生成器与样本资产成了两份会互相覆盖的事实来源**——跑一次「生成全部」就会把定制过的 `InventoryDatabase` 整份盖掉。本版删掉生成器这一侧，**样本内的预制体与配置数据成为唯一事实来源**。

- ⚠️ **删除 `InventoryDemoWizard`**（`Editor/DemoWizard/`，13 个分部文件）及其三语译表 `InventoryEditorL10n.Table.Demo.cs`。外部若有代码调用 `InventoryDemoWizard.GenerateAll()` / `GenerateItem(key)` / `Items` / `Categories`，需自行移除。
- ⚠️ **欢迎窗口移除「预制体生成」折叠区**（生成全部 / 逐项生成列表）与随之失效的「向导字体」文档段落。窗口其余部分（创建数据文件、打开 Inventory Editor、查看文档、数据模板、启动时自动显示、跳转 Ale Toolkit 设置）不变。
- 获取可运行示例的方式改为：Package Manager 选中本包 → `Samples` → 导入 **Inventory System Demo** → 打开示例场景 Play。样本内已含数据库、效果库、全部 UI 预制体与接好线的 `InventoryManager` 预制体。

### 新增

- **道具右键操作菜单 `UiwItemContextMenu`**（`Runtime/UI/Tool/`，继承 toolkit `UiwContextMenu`）：右键任意道具格子（网格 `UiwInventoryItemCell` / 明细行 `UiwInventoryItemDetail` / 装备候选）在光标处弹出。三条内置条目：
  - **查看** → `UiwItemDetailPopup`；
  - **使用** → `UseItemInSlot`，**仅当道具 `onUseEffectRefs` 非空时才显示**——这是本包内唯一的「可使用」信号（没有 `IsUsable` 标志、也没有道具类型枚举），空列表时 `UseItem` 只会返回 `NoEffects` 且不扣减，显示出来没有意义；
  - **丢弃** → `UiwItemDiscardPopup`。
  - 三条的文案均为 `TextValue`（纯文本 fallback + 可选原生本地化引用），可在预制体上改。
- **菜单条目贡献点 `UiwInventoryItemEvents.CollectingItemMenu`**：上层系统在自己打开时挂上去，按目标道具往菜单里追加条目。`UiwEquipmentView` 据此贡献「装备」，其显示条件与执行条件共用同一个判据 `CanQuickEquipFrom`（界面显示中 + 来源仓库匹配 + `TryFindEquipSlot` 或 `TryFindReplaceableSlot` 命中），避免「显示了却点不动」。
- **`UiwItemDetailPopup`**（右键「查看」）：内嵌一个 `UiwInventoryItemDetail` 渲染详情（与悬停弹窗 `UiwItemTooltip` 同一渲染组件），右上角关闭按钮 / 点击遮罩 / ESC 收起。**常驻显示**，不随光标移开消失。内嵌详情的悬停弹窗被强制关闭——本弹窗拦截射线，否则鼠标停在详情上会弹出一个内容完全相同的悬停弹窗压在其上。
- **`UiwItemDiscardPopup`**（右键「丢弃」）：道具预览 + `Slider`（`wholeNumbers`，范围 `[1, 该槽堆叠数]`）+ 数量文本 + 丢弃 / 取消；确认后 `TryRemoveItem(仓库, slotId, n)` 按槽位精确扣减（刷新由其派发的 `OnInventoryChanged` 自动完成）。**上限取打开那一刻的实时槽位数量**而非格子的显示值——菜单弹出后仓库仍可能被其它逻辑改动，按过时上限丢弃会与玩家预期不符。**默认值取 1** 而非全部：丢弃不可撤销，默认值应当最保守。
- **道具配置新增「不可丢弃」`Item.noDiscard`**（默认 `false`）：勾选后玩家丢不掉该道具——右键菜单的「丢弃」**置灰保留**（`UiwItemContextMenu.hideDiscardWhenLocked` 可改为整条隐藏），丢弃弹窗也不会打开。供重要剧情 / 任务道具使用。
  - **只拦「丢弃」这一条玩家主动路径**：出售、制作消耗、装备替换等由业务代码发起的 `TryRemoveItem` 一律不受影响——否则本该合法的消耗会被连带拦下。
  - 判据收口为 `InventoryDataManager.IsItemDiscardable(itemId)`（读 `noDiscard` 取反）。**道具查不到时返回 `true`**：拿不到配置就无法证明它受保护，与该字段默认 `false` 的语义一致，不因数据缺失把普通道具锁死。
  - 三层设闸：菜单条目置灰 → `InventoryRuntimeManager.ShowItemDiscardPopup` 直接静默返回（拦在实现之前，换用自定义 `IItemDiscardPopup` 的项目同样受约束）→ `UiwItemDiscardPopup.Show` 再查一次（它是唯一真正执行不可撤销扣减的地方，且可被任何代码经 `Instance` 直接打开）。
  - 道具模板上同有该字段，从模板创建道具时复制；道具 / 模板的 Inspector 均在「仓库属性」下多一个勾选项（带说明 Tooltip，三语）。
- **管理器宿主**：`InventoryRuntimeManager` 新增 `itemContextMenuPrefab` / `itemDetailPopupPrefab` / `itemDiscardPopupPrefab` 三个预制体字段与 `ShowItemContextMenu` / `ShowItemDetailPopup` / `ShowItemDiscardPopup`（及对应 `Hide`），沿用悬停弹窗那套「接口持有 + 惰性全局实例化到 `coverUiRoot`」的依赖倒置模式（新接口 `IItemContextMenu` / `IItemDetailPopup` / `IItemDiscardPopup` 与载荷 `ItemContextTarget` 定义在 `Ale.Inventory.Runtime`，管理器因而不反向依赖 UI 程序集）。四个挂件的实例化逻辑收口为泛型 `EnsureCoverUiWidget<T>`。
- **`InventoryRuntimeManager.UseTargetContextProvider`**（`Func<string, IEffectContext>`）与 `ResolveUseTargetContext(inventoryId)`：UI 的「使用」需要一个不必逐次传参的效果目标上下文取用点，而本包不认识任何领域系统、构造不出 `IEffectContext`，故由宿主注入。未注入时对有使用效果的道具得到 `NoContext`（不施加、不扣减）。直接调用 `UseItem` / `UseItemInSlot` 的业务代码照旧显式传上下文，不受影响。
- **格子暴露槽位 ID**：`UiwInventoryItemSlotBase` 新增 `BoundSlotId`，`SetBoundSlot` 增加 `slotId` 参数并添加 `SetBoundSlot(inventoryId, RuntimeItemSlot)` 重载（此前 `slot.slotId` 在绑定时被丢弃，而 `UseItemInSlot` / `TryRemoveItem` 都按槽位定位）。**不叫 `SlotId`**：`UiwEquipmentSlot` 已用该名表示**装备槽**配置 ID，同名会静默隐藏基类成员。悬停弹窗内的详情行与网格补位空格没有真实槽位，该值为空，右键不弹菜单。
- **Demo**：新增 **演示用效果库** `Demo/Data/EffectDatabase.asset`（一条 Instant 效果 `演示·道具使用`，执行项为 toolkit 内置 `Effect.NoOp`），样本中的消耗品据此获得 `onUseEffectRefs`，使「使用」可端到端验证（`UseItemCore` 要至少一个效果施加成功才扣减）；`InventoryRuntimeManager` 的测试分部新增 `demoEffectDatabase`，启动时登记效果库并注入演示用 `UseTargetContextProvider`（已由宿主注入过则不覆盖）。该效果**不改动任何数值**，只为证明链路通畅——真实数值效果需要角色系统提供属性 Sink。整份测试分部仍由 `UNITY_EDITOR || DEVELOPMENT_BUILD` 门控。
- **Demo 预制体**：新增 `PF_UiwContextMenuRow` / `PF_UiwItemContextMenu` / `PF_UiwItemDetailPopup` / `PF_UiwItemDiscardPopup` 四个预制体（均归 `Tool/`）与管理器接线。

### 修复

- **右键按住拖动会误起拖**：Unity 的输入模块对右键同样跑 `ProcessDrag`，而 `GridCellDragHandler` 从不检查 `eventData.button` —— 右键按住轻移就会生成跟随光标的拖拽幽灵，松手时 `OnEndDrag` 还会按落点执行换位 / 装备。现在 `OnBeginDrag` / `OnDrag` / `OnEndDrag` 三个回调统一只响应左键（只拦起拖不够：起拖被拦下后另外两个回调仍会照常派发）。

### 变更

- `UiwInventoryItemSlotBase.OnPointerClick` 右键改为调用 `ShowItemContextMenu`；新增两条守卫——拖拽进行中（`eventData.dragging`）不弹菜单（松手落点等于拖拽源时，点击会先于 `Drop` / `EndDrag` 派发），空槽不弹菜单。
- **Demo 数据**：`InventoryDatabase` 中 8 个「消耗品」模板道具（粗糙的面包 / 劣质治疗药水 / 初级治疗药水 / 初级法力药水 / 初级体力药水 / 初级迅捷药水 / 初级迟缓药水 / 初级毒素药水）统一挂上 `onUseEffectRefs = ["演示·道具使用"]`，使右键菜单的「使用」在样本中可直接验证（消耗品显示「使用」并扣减 1，装备 / 材料等不显示）。该效果由样本内的 `EffectDatabase` 提供，执行项为 toolkit 内置 `Effect.NoOp`，只保证施加成功以触发扣减，不改动任何数值。
- **导出格式 v9 → v10**：道具 / 道具模板块尾各追加一位 `noDiscard`（JSON 为同名布尔字段）。二进制读取按文件头版本号判断该位是否存在，**v5 ~ v9 导出的 `.bytes` 仍可导入**（缺该位时取默认 `false`）；旧 JSON 同理由 `JsonUtility` 取默认值。
- **Demo 数据**：「任务物品」模板及其两个道具（破损的项链 / 奇怪的雕像）勾上 **不可丢弃**，使该功能在样本中可直接验证——右键这两个道具时「丢弃」置灰，右键药水 / 材料时「丢弃」可点。
- 文档：仓库根与包内共 6 个 README、以及三语 UI 组件指南的 10.5 节，均由「一键生成」改述为「导入样本」。

## [1.13.0] - 2026-09-07

**效果与 Gameplay 标签外移至 toolkit 1.10.0 的共用效果库 `EffectDatabase`；库存库只保留道具的效果 id 引用。** 1.12.0 把效果存在库存库里、编辑器自带一份「效果系统」页签——角色系统（Chronicle）也有一份结构相同的，效果只能在各自库内定义。本版按 toolkit 1.10.0 的共用化方案改造：效果 / 标签在 Effect Editor 一处配置、所有上层系统按 id 引用；本包只保留「效果引用列表 + 跳转」。`UseItem` 的施加语义不变（Chronicle × Inventory 整合 Demo：回复药水 / 磨刀油 / 剑 三条断言与 1.12.0 一致，效果改由 toolkit 效果库承载）。

### 破坏性变更

- ⚠️ **`InventoryDatabase.Effects` / `GameplayTags` / `GetEffect` 与 `IEffectDefinitionSource` 实现移除**。1.12.0 资产里的效果 / 标签经 `FormerlySerializedAs` 落入隐藏字段 `legacyEffects` / `legacyGameplayTags`（`[Obsolete]` 访问器 `LegacyEffects` / `LegacyGameplayTags`，`HasLegacyEffectData`），运行时不再读取——请用迁移菜单搬入效果库（见下文「迁移指引」）。legacy 字段保留一个版本，下一版删除。`CloneFrom`（新建数据文件「使用模板」）不复制 legacy。
- ⚠️ **`InventoryDataManager` 不再登记为 toolkit 效果定义源、不再并入 Gameplay 标签、删除 `GetEffect`**；效果按 id 经 toolkit `EffectDataManager`（注册效果库时登记为全局来源）/ `EffectDefinitionRegistry.Default` 解析。宿主须注册效果库：放在 `Resources` 下随启动自动登记，或 `EffectDataManager.Instance.Register(effectDatabase)`。
- **导出格式 v9**：不再写出 v8 的效果 / Gameplay 标签两块（效果库改由 toolkit `EffectConfigSerializer` 单独导出）；道具 / 模板块的 `onUseEffectRefs` 保留。读 v8 文件（JSON / 二进制）时两块读入 legacy 字段（同样经迁移菜单进入效果库）；`LoadFromJson` / `LoadFromBinary` 的旧文件效果不再直接生效。
- `InventoryDatabase.Validate` 不再校验效果 / 标签（效果库自身在 Effect Editor 校验）；道具的效果引用照旧不作悬空校验（编辑器标注「未找到」，不阻断）。
- 编辑器删除「效果系统」页签（`Editor/EffectSystem`、`InventoryEffectRefDrawer`、`EInventoryEntityKind.Effect` 及重复 ID 扫描的效果种类）；效果在 toolkit 的 **Effect Editor**（`Tools > Ale Toolkit > Effect System > Effect Editor`）配置。

### 新增

- **迁移**：`Tools > Ale Toolkit > Inventory System > 迁移效果到 Effect Database`（`EditorInventoryEffectMigration` 窗口，三语：来源库存库 + 目标效果库（可就地新建）→ 迁移 → 报告 → 在 Effect Editor 打开）；`InventoryDatabase` 资产 Inspector 检测到 legacy 数据时给出提示与同一入口。纯数据部分 `InventoryLegacyEffects.MigrateInto(source, target)`（运行时程序集）：逐条 `EffectDefinition → EffectEntry`（定义深拷贝并归一，显示名取定义的 `displayName`，不挂模板）并从 legacy 移除；目标库已有同 id 的跳过、报告并留在 legacy（不覆盖，处理后可重跑）；标签按归一名去重并入。
- **道具 / 模板 Inspector 的「使用效果」引用**改用 toolkit `EditorEffectRefListDrawer`：「+」从工程内全部效果库的目录选择（按库分组）、拖拽重排 / 删除、「打开」跳转到 Effect Editor 并定位、未找到标注（不阻断）、自由输入。译表 `InventoryEditorL10n.Table.Effect.cs` 改为只承载引用列表文案与迁移窗口 / 提示（列表本体译文由 toolkit 提供）。

### 依赖

- ⚠️ **最低 `com.ale.toolkit` 版本提至 1.10.0**（效果库 `EffectDatabase` / `EffectDataManager` / Effect Editor / `EditorEffectRefListDrawer`）。安装顺序不变：先 `com.ale.toolkit`、再本包。
- 程序集引用：`Ale.Inventory.Editor` 新增 `Ale.Effect.Runtime`。

### 迁移指引（1.12.0 → 1.13.0）

1. 升级 toolkit 至 1.10.0，重新编译（旧资产的效果 / 标签自动落入 legacy 字段，Inspector 出现黄色提示）。
2. 选中旧的 `InventoryDatabase` 资产 → 「迁移效果到 Effect Database…」→ 目标库「新建…」（或选已有效果库，如角色系统已在用的那份）→ 迁移。报告有同 id 冲突时在目标库处理后重跑。
3. 把效果库放进 `Resources`，或在引导代码里 `EffectDataManager.Instance.Register(effectDatabase)`；`InventoryDataManager.Register` 照旧；`UseItem` 调用不变。
4. 旧的 v8 JSON / 二进制：导入库存库后同样经迁移菜单进入效果库，再用 toolkit `EffectConfigSerializer` 导出效果库。

## [1.12.0] - 2026-09-07

**道具「使用」接入 toolkit 1.9.0 的效果系统（GAS 式 GameplayEffect）与 Gameplay 标签。** 道具可引用效果 id，`UseItem` 把效果施加到调用方给定的目标上下文并扣减道具；本包仍不认识任何领域系统（角色 / 属性 / 特质由 toolkit 效果契约与业务层上下文承接）。

### 新增

- **效果系统页签**：`InventoryDatabase.Effects`（toolkit `EffectDefinition` 原样入库：时长策略 / 周期 / 叠加 / 资产·授予·免疫标签 / 施加条件 / 概率 / 修饰器 / 执行阶段）与 `InventoryDatabase.GameplayTags`（本库声明的层级标签，注册数据库时并入 toolkit 标签注册表）；编辑器新增「效果系统」页签（左列 Gameplay 标签目录、中列效果列表以时长策略充当过滤 / 新建入口、右列 ID / 显示名 + 内联 toolkit 效果定义绘制器 + 校验摘要）；`GetEffect` 查询；`Validate` 校验效果 id 重复、定义错误（toolkit 警告不阻断）、非法标签名。
- **道具使用效果**：`Item.onUseEffectRefs` / `ItemTemplate.onUseEffectRefs`（模板默认值，从模板创建时复制）；道具 / 模板 Inspector 增「使用时施加的效果」列表（可选本库效果，也可自由输入其它库 / 其它系统的效果 id——运行时经全局效果注册表解析，编辑器标注「外部」）。
- **`InventoryRuntimeManager.UseItem(inventoryId, itemId, targetContext)` / `UseItemInSlot(inventoryId, slotId, targetContext)`**：按序经 toolkit `EffectApplier` 施加使用效果，至少一个成功才扣减 1 个；返回 `ItemUseResult`（类别 / 是否扣减 / 逐效果结果），派发 `OnItemUsed(ItemUseEvent)`。无引用 → `NoEffects` 不扣减；未持有 → `NotOwned`。
- **`InventoryDataManager`** 实现 toolkit `IEffectDefinitionSource`：注册数据库时把本库 Gameplay 标签并入注册表、把自身登记为 `EffectDefinitionRegistry.Default` 的来源（其它系统按 id 也能解析本库定义的效果）；`GetEffect` 跨库查询。

### 变更

- 导出格式 **v8**：道具 / 道具模板块尾追加 `onUseEffectRefs`；尾部追加 效果（Effect System JSON 串）/ Gameplay 标签 两块。JSON 导出直接内嵌 `EffectDefinition`。v7 及更早导出仍可导入。`CloneFrom` 同步拷贝两个新列表。
- 编辑器重复 ID 扫描新增「效果」种类；状态栏 / 导出拦截一并覆盖。

### 依赖

- ⚠️ **最低 `com.ale.toolkit` 版本提至 1.9.0**（`Ale.Effect` GAS 层、`Ale.GameplayTags`、`Ale.Modifier.Core`、`Ale.Condition.Core`）。安装顺序不变：先 `com.ale.toolkit`、再本插件。

## [1.11.1] - 2026-08-04

列表 UI 的一次「淡入淡出 + 去重下沉」迭代：为虚拟滚动列表的单元格加入**分配（滚入）淡入 / 回收（滚出）淡出**，并把该能力**下沉为 `com.ale.toolkit` 的通用基类与接口**，令所有列表（背包 / 商店 / 制作 / 装备候选）统一获得。**纯 UI / 运行时层改动，导出 DTO 与数据零变化。**

### 新增

- **列表单元格淡入淡出**：滚动时格子被分配 → 整格根 `CanvasGroup` 淡入；被回收 → 淡出后再清空归还（虚拟列表引擎在回收时保持格子存活播放淡出，完成后统一清空 / 归还对象池）。道具格额外支持**图标 / 品质背景框的「加载门控」逐图片淡入**——Sprite（可能经 Addressable 异步加载）就位后该图片才淡入。时长可在格子 Inspector 调整（`rootFadeInDuration` / `rootFadeOutDuration` / `imageFadeDuration`）。
- **淡入淡出下沉为 toolkit 通用能力**：新增 `UiwListFadeCell`（根 CanvasGroup 淡入淡出基类）+ `IUiwRecycleFadeCell` / `IUiwDiffCell<TData>` 接口，由 `UiwVirtualListBase` 的默认 hook 自动驱动——**任何继承 `UiwListFadeCell` 的单元格白得对称淡入淡出**。本插件 `UiwInventoryItemBase` 改继承之，故背包 / 商店 / 制作 / 装备候选列表统一生效。

### 变更

- **依赖 `com.ale.toolkit` 升级**：需要 toolkit 新增的 `UiwListFadeCell` / `IUiwRecycleFadeCell` / `IUiwDiffCell` 与 `UiwVirtualListBase` 默认 hook（`ToolkitTween` 亦新增 `Graphic` / `Image` 的 alpha 淡入）。安装顺序不变：先 `com.ale.toolkit`、再本插件。
- **悬停高亮 / 堆叠已满提示**的淡入由手写协程迁至 `ToolkitTween`（统一走中央 Tween 作业表；回收 / 停用时打断并复位，顺带修复「悬停中被回收 → 复用格残留高亮」的隐患）。
- **回收淡出 / 增量差异下沉引擎默认**：两个背包列表删除各自的 `TryPlayRecycleAnim` / `CancelRecycleAnim` / `NeedsRebind` override，改由 `UiwVirtualListBase` 默认 hook 经接口驱动（`UiwInventoryItemSlotBase` 实现 `IUiwDiffCell<RuntimeItemSlot>`，既有 `MatchesSlot` 即满足）。
- **命名规范化**：文本别名 `InventoryText` → `UiText`（多处 UI 脚本）；列表脚本 `UiwGridItemList` / `UiwOrderItemList` → `UiwInventoryItemGridList` / `UiwInventoryItemOrderList`（`.meta` GUID 保留，预制体引用不受影响）；若干虚拟列表脚本命名统一。

### 说明

- 网格空槽（拖拽落点）在生成时也会淡入（原为即时），功能无碍。
- 若某单元格的根 CanvasGroup 另作它用、与淡入淡出冲突，可 override toolkit `UiwListFadeCell.RecycleFadeEnabled => false` 退出通用淡入淡出。

## [1.11.0] - 2026-08-02

### 移除

- **删除整个技能（Skill）子系统**：技能功能已迁移到独立包 `com.ale.chronicle`，本包移除全部技能相关内容——
  数据类型（`Skill` / `SkillTemplate` / `SkillGroupTag` / `ISkillConfig` / `RuntimeLearnedSkillState`）、
  运行时管理（`SkillRuntimeManager` / `SkillCollector` / `ESkillSource`）、
  UI（`UiwSkillView` / `UiwSkillEntry` / `UiwSkillTooltip` / `UiwSkillGridList` / `UiwSkillOrderList` / `SkillDisplay` / `ISkillTooltip`）、
  编辑器（「技能系统」页签及其面板 / `UiwSkillViewEditor` / `SkillConfigDrawer` / 向导的技能预制体生成）、
  序列化（技能 DTO 与二进制 / JSON 读写块）、以及 `InventoryDatabase` 的技能字段与查询。
  **编辑器主窗口由六页签减为五页签**（道具 / 仓库 / 商店 / 制作 / 装备）。
- **装备联动技能改由业务层跨包完成**：装备道具在其属性上配置 Chronicle 技能 ID，游戏层桥接读取并同步到 `com.ale.chronicle`（不再依赖本包的技能类型）。
- **序列化向后兼容**：`InventoryDtoMapper.Version` 维持 7；技能块本在二进制末尾，删除后旧文件尾部技能字节被忽略，旧 `.bytes` / `.json` 仍可导入（其中的技能数据被丢弃）。

## [1.10.0] - 2026-07-26

配合 `com.ale.toolkit` 1.2.0：把「界面语言」与「可选依赖宏」下沉到 toolkit 欢迎窗口、菜单收拢到 `Tools > Ale Toolkit` 下，并把两个库存专用工具窗口（本地化 / Addressable）整合进 toolkit 的**通用工具窗口**（图标字段并入属性系统后即可通用）。**全局设定 / 菜单部分为纯结构调整；图标整合涉及一次性资产迁移（见「整合」），导出 DTO 表示不变但格式版本号 6 → 7。** 另把**数据模板**等项目级设定改存 `ProjectSettings`（入库共享，旧 EditorPrefs 设置自动迁移）。

### 变更

- **依赖 `com.ale.toolkit` 1.2.0**（安装顺序不变：先 `com.ale.toolkit`、再本插件）。
- **⚠️ 可选依赖宏随 toolkit 改名**：`IS_TMP` / `IS_LOCALIZATION` / `IS_ADDRESSABLE` → `ATK_TMP` / `ATK_LOCALIZATION` / `ATK_ADDRESSABLE`。老项目已设的旧宏由 toolkit 的 `ToolkitDefineChecker` 加载时自动迁移，无需手改。
- **全局设定下沉 toolkit**：欢迎窗口移除界面语言按钮与插件宏开关区，改为「打开 Ale Toolkit 设置（语言 / 插件宏）」跳转按钮（语言 / 枚举翻译 / 宏开关统一在 toolkit 欢迎窗口配置）；`InventoryEditorPrefs` / `InventoryDefineChecker` 移除已下沉的宏常量 / 检测 / 一致性检查；向导 TMP / 本地化字体设置从宏区迁为独立「向导字体」区。
- **菜单收拢**：库存四个菜单项由 `Tools > Inventory System > *` 收入 `Tools > Ale Toolkit > Inventory System > *`（欢迎窗口 / Inventory Editor / 本地化工具窗口 / Addressable 工具窗口）。`Assets > Create > Inventory System > Inventory Database` 不变。
- **欢迎窗口可手动调整高宽**：移除 `min==max` 尺寸锁、改为只设缩放下限（初始仍 520×600 居中），窗口可自由拉伸；「预制体生成」滚动列表高度由 200 下调至 130。

### 整合（删除库存专用工具窗口，改用 toolkit 通用工具）

- **删除库存专用工具窗口**：`InventoryLocalizationToolWindow` + `InventoryTextFieldCollector`、`InventoryAddressableToolWindow` + 空的 `Ale.Inventory.Addressables.Editor` 程序集。欢迎窗口「本地化」按钮改开 toolkit 通用窗口；「Addressable」按钮与注入钩子移除（改从 `Tools > Ale Toolkit > Addressable` 打开）。
- **图标并入属性系统**：`Skill` / `SkillTemplate` 的 `icon` + `iconAddress` → `iconValue`（`AttributeValue` Sprite）；功能标签背景图同理（toolkit `Tag.backgroundSpriteValue`）。UI / DTO 导出 / 编辑器绘制 / 标签面板全部改读新字段，`ISkillConfig.Icon` 代理到 `iconValue`。**升级须知**：本版提供一次性迁移菜单 `Tools > Ale Toolkit > Inventory System > 迁移 > 图标字段迁移到属性系统`——升级后请**先运行它**把旧图标搬入新字段并保存。
- **移除 `InventoryDatabase.LocalizationTableCollectionGuid`**：本地化表绑定已回归到各 Text 属性值的 `tableRef`（由 toolkit 通用窗口反推）。含数据模型 + DTO + JSON + 二进制四处；`InventoryDtoMapper.Version` 6 → 7（该字段位于二进制末尾，删除**向后兼容**——旧存档可读，末尾多余字节忽略）。

### 项目级设置（改存 ProjectSettings，版本控制友好）

- **数据模板改存项目级设置**：「创建新数据文件」使用的模板数据库由 EditorPrefs 改存 `ScriptableSingleton` → `ProjectSettings/AleInventorySettings.asset`（**随仓库入库、按 GUID 引用、团队共享**）。新增 `InventoryProjectSettings`，`InventoryEditorPrefs.Load/SaveTemplateDatabase` 转为其门面（4 处调用方无感）；`LoadTemplateDatabase` 首次调用时把旧 EditorPrefs 路径**一次性迁入**新文件并清除旧键。启动自动显示、上次打开的数据库路径仍为每人偏好（EditorPrefs）。
- **向导字体随 toolkit 改存 ProjectSettings**：预制体向导的默认 / 本地化字体现由 toolkit 存入 `ProjectSettings/AleToolkitSettings.asset`（见 toolkit 1.2.0）；本插件 `AttachFontEvent` 改用 `LocalizedReference.SetReference` 复制本地化字体引用（弃 `JsonUtility` 往复，避免嵌套回调丢失致引用变空）。

### 文档

- 三语 `README` / `Docs~` 同步：宏名 `ATK_*`、新菜单路径、全局设定改在 toolkit 欢迎窗口配置、toolkit 通用工具窗口替代库存专用工具、图标字段并入属性系统。
- 三语 `README` 补充：数据模板 / 向导字体改存 `ProjectSettings`（入库、团队共享，旧 EditorPrefs 设置自动迁移）。

## [1.9.0] - 2026-07-26

配合 `com.ale.toolkit` 1.1.0 的完整性补齐所做的跟随式清理与文档同步。**纯结构 / 文档调整，功能与数据零变化，旧数据与旧存档完全兼容。**

### 变更

- **依赖 `com.ale.toolkit` 1.1.0**（安装顺序不变：先 `com.ale.toolkit`、再本插件）。
- **Addressables 运行时层迁往 toolkit**：`AddressableAssetLoader` / `InventoryAddressableManager` / `InventoryAssetOwnerTracker` 三个零领域耦合的运行时件下沉为 `Ale.Toolkit.Runtime.AddressableSupport` 下的 `AddressableAssetLoader` / `AddressableManager` / `AssetOwnerTracker`；空的 `Ale.Inventory.Addressables.Runtime` 程序集随之移除（编辑器工具程序集 `Ale.Inventory.Addressables.Editor` 保留）。
- **宏开关工具改用 toolkit**：`InventoryDefineUtils` → toolkit `DefineUtils`（`InventoryDefineChecker` 因耦合欢迎窗口 / 库存偏好，仍留本插件）。
- **编辑器多语言表精简**：由 toolkit 组件渲染的通用键（三列框架按钮、属性绘制器标签、搜索 / 页签、标签面板等）上移至 toolkit 登记，本插件译表移除对应重复项——同一张共享字典，界面显示不变。

### 移除

- 删除空的 `Ale.Inventory.UI.Localization` 程序集（本地化组件 1.8.0 已迁入 toolkit）。

### 修复

- `Ale.Inventory.UI.Localization` asmdef 悬空引用：`Ale.Inventory.UI` → `Ale.Inventory.Runtime.UI`（`IS_LOCALIZATION` 门控）。

### 文档

- 三语 `README` / `Docs~` 同步为**双包**结构：程序集表区分本插件与 `Ale.Toolkit.*`；抽取后类名更新（`UiwInventoryTab` → `UiwTabButton`、虚拟列表基类 → `UiwVirtual*`、`InventoryExportResolver` → `EditorExportResolver`、`InventoryAssetRefField` → `EditorAssetRefField`、`InventoryTmpFontEvent` → `LocalizedFontEvent`、程序集 `Ale.Inventory.UI` → `Ale.Inventory.Runtime.UI` 等）。

### 兼容性

- **数据无需迁移**：`InventoryDatabase` 资产、导出的 JSON / 二进制、运行时存档格式均不变；旧存档与旧资产完全兼容。

## [1.8.0] - 2026-07-26

**纯结构重整，功能零变化。** 把原先埋在本插件里、与库存业务无关的通用能力抽成独立包 [`com.ale.toolkit`](../com.ale.toolkit)（自定义属性系统、虚拟滚动列表、编辑器三列框架、编辑器界面三语、排序引擎、标签系统，及 TMP / Localization / Addressables 支持层），本插件反过来依赖它。**导出格式与序列化结构完全不变，旧数据与旧存档完全兼容。**

### ⚠ 安装顺序（重要）

本版本起 `com.ale.inventory` **依赖 `com.ale.toolkit`**。Unity 的 Package Manager 不支持在 `package.json` 的 `dependencies` 里写 git URL，故 `dependencies` 留空，**必须手动先安装 `com.ale.toolkit`、再安装 `com.ale.inventory`**。详见 README 安装章节。

### 变更

- **通用能力下沉 toolkit**：属性系统（`AttributeValue` 全家、`EnumType`、`NumberFormatConfig`、`GroupTag`、`ConfigTemplateBase`）、排序（`SortPriority` / `SortOption` / `AttributeSortService` / `ISortContext<TData>`）、运行时基础（单例基类、资源加载抽象、存档契约）、UI 虚拟滚动引擎与通用控件、编辑器三列框架与属性 / 排序绘制器、编辑器界面三语服务、UGUI 预制体搭建工具箱、两个工具窗口（本地化 / Addressable）等，一律迁至 `Ale.Toolkit.*` 命名空间。六大子系统的领域模型与成品视图仍在 `Ale.Inventory.*`。
- **标签类型通用化**：功能标签 `FunctionTag` 更名为通用的 `Ale.Toolkit.Runtime.Tag` 并迁入 toolkit（`InventoryDatabase.FunctionTags` 等对外 API 名称不变；序列化字段结构不变，**资产数据无损**）。
- **本地化组件更名**：`InventoryTmpTextEvent` / `InventoryTmpFontEvent` → `LocalizedTextEvent` / `LocalizedFontEvent`（迁入 toolkit，**脚本 GUID 保留**，既有预制体引用不受影响）。
- 依赖方向单向：`Ale.Inventory.* → Ale.Toolkit.*`，toolkit 绝不反向引用库存。

### 兼容性

- **数据无需迁移**：`InventoryDatabase` 资产、导出的 JSON / 二进制、运行时存档格式均不变；旧存档与旧资产完全兼容。
- 项目层若直接 `using` 了被下沉的类型（如 `AttributeValue`、`SortPriority`、`EnumType`），需把命名空间由 `Ale.Inventory.*` 改为 `Ale.Toolkit.*`（**类型名不变**）。

## [1.7.0] - 2026-07-24

编辑器 UI 支持**中文 / English / 日本語**三语切换。纯编辑器界面改动，**不涉及运行时、不改变任何数据结构与序列化格式**，旧数据与旧存档完全兼容。

### 新增

- **编辑器界面三语切换**：欢迎窗口顶部副标题下新增居中的「中文 / English / 日本語」三个按钮，当前语言高亮。切换后 **`InventoryWelcomeWindow` 与 `InventoryEditorWindow`（含六大系统页签、左/中/右三列全部面板与配置绘制器）** 的界面文本整体切换；语言选择经 `EditorPrefs` 持久化，跨会话保留。
- **「多语言设定」区**（欢迎窗口）：含「枚举值」勾选项（**默认不勾**）。勾选后，属性字段类型（`EFieldType`）、商店类型、刷新周期、刷新时间类型等**枚举下拉的显示名**也随语言切换；不勾选则一律显示代码中的英文枚举原名（与 1.6.0 行为一致）。
- **运行时组件的自定义 Inspector 也随语言切换**：`UiwEquipmentGroupPanel` / `UiwEquipmentSlotList` 的「手动 / 自动模式」标题与说明框、`UiwSkillView` 按技能来源显示的「装备组 ID / 仓库 ID / 角色 ID」字段标签与 tooltip。这几个面板画在 Unity 标准 Inspector 中，不属于插件的两个窗口，故单列一张译表。
- 新增独立的编辑器 UI 本地化服务 `InventoryEditorL10n`（`Editor/Common/`），译表按区域拆为多个分部文件，与运行时内容本地化（`IS_LOCALIZATION` / Unity Localization）完全无关、互不影响。

### 变更

- 勾选「枚举值」后，商店**刷新周期 / 刷新时间类型**下拉可正确显示中文（或英/日）。此前这两个枚举虽标注了 `[InspectorName("不刷新")]` 等中文名，但配置编辑器用 `EditorGUILayout.EnumPopup` 绘制，该特性仅在 `SerializedProperty` 路径生效，故下拉一直显示英文标识符（见 1.5.0 条目）。现改由 `TrEnumPopup` 绘制，绕开了该限制。
- 欢迎窗口「预制体生成」区的**分类名与生成项显示名**随语言切换（预制体资产名恒为英文，不参与翻译）。
- 语言 / 开关变更后的刷新范围由「插件的两个窗口」扩大到**全部已打开的编辑器窗口**，使 Inspector 面板上的组件自定义 Inspector 也能即时更新，无需手动点一下。

### 不在本次范围

- **撤销 / 重做操作名**（Unity `Edit ▸ Undo/Redo` 菜单中的文案）保持中文，不随界面语言切换。
- DemoWizard 独立窗口、本地化工具窗口、Addressable 工具窗口保持中文。
- **生成到数据库的 Demo 内容**（道具名等）保持中文——生成的资产内容不随编辑器界面语言变化。

## [1.6.0] - 2026-07-23

一轮以「**修 Bug + 补齐导出 + 结构重构**」为主的版本。**含破坏性变更**（移除已零调用的兼容 API、导出格式升版），升级前请先读本节末尾的「⚠️ 破坏性」与「升级指引」。

### 修复

- **无整理栏时自动整理一律降序**：`UiwInventoryView` 在未接 `UiwSortToolbar`（合法配置——不想给玩家排序栏时）
  的情况下，把升降序恒当作降序，忽略每条排序条件自己配置的 `ascending`；基类 `UiwInventoryListBase`
  的同位置逻辑并非如此，两者语义不一致。现改为：无排序栏时保留各条件自身的升降序，有排序栏时统一取排序栏的方向。
- **载入存档后旧仓库残留**：`InventoryRuntimeManager.LoadSaveData` 此前不清空现有状态，直接按存档覆盖同名仓库——
  于是「存档里没有、但内存里已存在」的仓库会带着上一局的内容留下来，与 `EquipmentRuntimeManager` /
  `ShopRuntimeManager` 的覆盖语义不一致。现统一为覆盖（详见下方「存档契约」）。
- **DemoWizard「生成」必定栈溢出**：`BeginPrefab` 误写成调用自身，导致生成的任一入口都会抛
  `StackOverflowException`——该异常在 .NET 中不可捕获，会直接终止编辑器进程。
- **DemoWizard 重新生成会静默打断资产引用**：预制体与数据库此前都是「先删除、再创建」，删除连带删掉 `.meta`，
  资产 GUID 因此改变。单独重生成某个被依赖的预制体（如 `PF_UiwInventoryItemCell`）或数据库，会**静默打断**
  依赖它的预制体 / 管理器对它的引用，而生成窗口的依赖对话框只**向下**遍历依赖、从不向上遍历被依赖者，因此不会提示。
  现改为就地覆盖，GUID 保持不变。
- **制作系统模板页签切换后显示名异常**：四处页签条（仓库 / 商店商品组 / 蓝图模板 / 过滤页签栏）此前各写一遍
  「销毁旧的 → 逐个 Instantiate → 挂 onClick → 整排重刷高亮」，切换模板页签后显示名会被重新构建。
  现统一走 `UiwTabStrip` 的**差异复用**（数量不变时原地复用实例、只重绑取值与显示名），问题随之修正。
- **同时打开两个配置编辑器窗口时列表选中串台**：中栏实体列表的 `_pendingSelect` 原为 `static`，
  两个窗口共享同一份待选中状态。接入 `EditorEntityListPanel` 时改为实例字段。
- **文档修正**：`Docs~/WarehouseSystem` 原称「存档中多余的仓库 ID 被忽略」，与实现不符（这类仓库实际会被载入内存）。

### 新增

- **`InventoryRuntimeManager.ResetAll()`**：清空全部仓库运行时状态并重建为初始空态（固定容量仓库恢复预分配空槽）。
  此前 `EquipmentRuntimeManager` / `ShopRuntimeManager` / `SkillRuntimeManager` 都有 `ResetAll`，唯独仓库没有——
  开新游戏 / 重置存档后仓库仍是上一局的满状态。
- **`IInventorySaveable<TState>` / `IInventorySaveable`**：持有存档状态的四个运行时管理器
  （仓库 / 装备 / 商店 / 技能）统一实现，把此前散落在四处注释里的存档契约钉在一处——
  `GetSaveData` 返回**深拷贝**、`LoadSaveData` 为**覆盖而非合并**、三个方法都**不触发**变更事件。
  非泛型部分只含 `ResetAll`，供游戏层「开新游戏」一次遍历重置全部系统。

### 性能

- **UI 子项实例改为复用而非销毁重建**：新增 `UiwWidgetPool`（纯 C# 实例池），包内十余处
  「价格格 / 标签行 / 属性行 / 装备槽 / 货币格 / 加成条目」共用；技能条目、技能弹窗、装备加成面板
  由「每次刷新销毁重建」改为按需实例化 + 逐帧复用 + 多余的回收隐藏。
- **消除运行时 UI 热路径的重复分配**：抽出 `SpriteSlot`（图标槽位的加载 / 释放收口）与 `UIFormat`
  （数值 / 多货币价格串格式化），替换掉道具格、商店视图、商店商品详情里各写一遍的实现；
  根 Canvas 解析也收口为一处。
- **仓库运行时管理器改走 `InventoryDataManager` 的 O(1) 索引**：数据库查找收口为一个泛型实现；
  拖放路径上的闭包分配一并消除。

### 变更

- **导出格式版本 v5 → v6，覆盖数据库全部 20 个列表**：此前 JSON / 二进制导出只含
  枚举类型 / 功能标签 / 道具模板 / 道具四项，其余 16 个列表（仓库模板 / 仓库 / 整理选项 schema / 整理选项 /
  数字格式配置 / 商店模板 / 商店 / 制作分组标签 / 蓝图模板 / 蓝图 / 装备分组标签 / 装备组模板 / 装备组 /
  技能分组标签 / 技能模板 / 技能）在导出时**被静默丢弃**——这是 1.4.0 起的已知限制。现已全部纳入，另加
  `localizationTableCollectionGuid`。
  - 同时补上道具系统自身此前漏掉的字段：模板色点、`ItemTemplate` / `Item` 的 `weight` / `stackLimit` /
    `hideInInventory`、功能标签的 UI 显示配置（显示名 / 描述的完整 `Text` 值含本地化引用、背景 Sprite、背景色、`hideInUI`）。
  - **向后兼容**：二进制读取按文件头版本号跳过新增数据块，v5 导出的 `.bytes` 仍可正常导入（新增字段取默认值）；
    反之 v6 导出的文件旧版本读不了——单向导出格式，升级后重新导出即可。
  - **`IS_ADDRESSABLE` 下的行为变化**：功能标签的背景 Sprite 与技能 / 技能模板的图标现在也会经解析器登记进
    Addressable 分组，未登记的资源会在导出时新增「未登记」告警。
- **新建无模板数据库保持空白**：`InventoryDatabase.SeedDefaults()` 方法体为空却被创建流程调用，
  「填充默认数据」的承诺实际是空操作。现移除该方法与两处调用，文档措辞同步改为「未配模板时创建空数据库」。
  行为不变，只是不再误导。
- **DemoWizard 生成的 Demo 道具属性可复现**：品质 / 部位 / 攻击力等随机属性此前取自全局 `UnityEngine.Random`，
  每次重新生成都不同；现由**道具 ID 派生**固定种子（FNV-1a），同一 ID 恒定，且与 `AddItem` 的调用顺序无关。
  **注意**：本次改动后重新生成的 Demo 数据，其随机属性值与旧版不同。
- **预制体保存失败不再静默**：DemoWizard 此前无论成败都打印「已保存」，现改用带结果的重载并在失败时报错。

### 重构（不改变行为）

> 本轮结构重构逐步进行、每步单独验证，公开行为保持不变；下列为对二次开发者有意义的结构变化。

- **数据层共享基类**：
  - `AttributeOwner`——承载属性值集合的对象基类，封装懒加载 O(1) 字典缓存与属性值泛型 Get / Set。
    本版起 `Inventory` / `Shop` / `CraftingBlueprint` / `EquipmentGroup` 也接入（此前只有 `Item` / `EnumItem`）。
  - `AttributeSync.Sync`——各实体 `RebuildAttributes` 中「按 schema 增补 / 移除 / 类型漂移重置」的共用实现。
  - `ConfigTemplateBase`——六大系统模板共有的名称 / 色点 / 属性字段定义。
- **运行时服务抽取**：
  - `InventorySortService`——排序实现从 `InventoryRuntimeManager` 独立为静态服务（无实例状态）。
  - `EquipmentBonusCalculator`——装备总加成汇总从 `EquipmentRuntimeManager` 拆出，与实例状态解耦。
  - `InventoryRuntimeManager` 另拆出 UI 宿主、时间服务与测试功能三个分部。
- **UI 层可复用构件**（详见 [UI 组件指南](Docs~/UIComponentGuide.md)）：
  - `UiwTabStrip<TTab,TValue>`——页签条，统一四处「一排页签 + 取值 / 显示名 + 单选高亮」。
  - `UiwWidgetPool<T>`——子项实例池，统一十余处子项管理。
  - `UiwTooltipBase<TPayload>`——悬停弹窗基类（光标定位 + 淡入淡出状态机 + 淡出期待显示队列），
    道具与技能弹窗此前是同一套状态机的两份逐字拷贝。
  - `UiwHoverTooltipSource`——「悬停弹出详情」能力基类，含 `OnDisable` 兜底（列表回收 / 面板关闭时
    Unity 不派发 `OnPointerExit`，弹窗会残留）。
  - `SpriteSlot` / `UIFormat`——图标槽位与数值 / 价格格式化。
- **编辑器中栏列表收口**：六个系统的实体列表接入泛型基类 `EditorEntityListPanel<TEntity,TTemplate>`，
  各面板只需实现列布局与新增 / 搜索规则；顺带统一了三处样式与行为漂移。
- **巨型文件按职责拆为分部**：`AttributeValue`（核心 / `.Elements` / `.Text`，类型步长映射收口为 `GetStrides`）、
  `InventoryRuntimeManager`（核心 / `.Time` / `.UiHost` / `.TestSeed`）、`InventoryDemoWizard`（按子系统拆为 13 个文件，
  另提取 `MakeVerticalScrollView` 消除五处纵向滚动骨架）。
- **序列化层重组**：`InventoryDtoModels.cs` 只放 DTO 定义；双向映射拆为 `InventoryDtoMapper*.cs`、
  二进制块读写拆为 `InventoryBinarySerializer*.cs`，均按系统分部（核心 + 仓库 / 商店 / 制作 / 装备 / 技能）。
- **属性字段绘制的 Layout / Rect 两条路径合并**为一套实现，此前是两个并行维护的大 switch。
- **`#region` 覆盖**：全部 180 行以上的源文件均已分节。

### ⚠️ 破坏性

- **移除 `InventoryRuntimeManager` 上的 `public static` 排序兼容转发**：`SortSlots` / `SortByItemId` /
  `CompareSlots` / `CompareByField` / `IsIgnoredByField` / `FindAttrDef` / `ContainsStr` / `GetTagOrder`。
  这批成员是排序实现迁到 `InventorySortService` 时为项目层保留的薄封装，现已无任何调用。
  写运行时状态并触发事件的**实例**方法 `InventoryRuntimeManager.SortInventory` 不受影响。
- **移除 `InventoryDatabase.SeedDefaults()`**（空实现，见上）。
- **移除零调用的公开成员**：`RuntimeInventoryState.FindByItemId`、`InventoryAssets.PreloadItem` /
  `InventoryAssets.ReleaseItem`（后者还忽略自己的 `item` 参数）、`InventoryEditorStyles.ListRow` /
  `ListRowSelected`。
- **导出格式升版**：v6 导出的 `.json` / `.bytes` 无法被 1.5.0 及更早版本读取（反向兼容，见「变更」）。

### 升级指引

1. **项目层若调用过上述 `public static` 排序转发**：把 `InventoryRuntimeManager.Xxx(...)` 改为
   `InventorySortService.Xxx(...)`，参数与语义完全一致，无其它改动。
2. **若使用 JSON / 二进制导出**：升级后重新导出一次。旧的 `.bytes` 仍可被新版导入，但只含道具系统数据；
   重新导出才能拿到仓库 / 商店 / 制作 / 装备 / 技能的完整数据。
3. **若用 DemoWizard 生成过 Demo**：可直接重新生成——本版起就地覆盖、GUID 不变，不会再打断引用。
   注意 Demo 道具的随机属性值会因随机源更换而与旧版不同（此后固定）。
4. **存档兼容**：存档数据结构未变，旧存档可直接读取。但 `InventoryRuntimeManager.LoadSaveData` 现为覆盖语义——
   若你的游戏层依赖「载入后保留存档中未出现的仓库」这一旧行为，需自行在载入后补回。

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
