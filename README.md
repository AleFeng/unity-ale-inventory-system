<p align="center">
  <img alt="Ale Inventory System" src="./Packages/com.ale.inventory/Docs~/Images/InventorySystem_Logo_L.png" width="280">
</p>

<p align="center">
  <img alt="GitHub Release" src="https://img.shields.io/github/v/release/AleFeng/unity-ale-inventory-system?color=blue">
  <img alt="GitHub Downloads (all assets, all releases)" src="https://img.shields.io/github/downloads/AleFeng/unity-ale-inventory-system/total?color=green">
  <img alt="Unity Version" src="https://img.shields.io/badge/Unity-2022.3%2B-black?logo=unity">
    <img alt="Unity Version" src="https://img.shields.io/badge/Unity-6000.3%2B-black?logo=unity">
  <img alt="GitHub Repo License" src="https://img.shields.io/badge/license-MIT-blueviolet">
  <img alt="GitHub Repo Issues" src="https://img.shields.io/github/issues/AleFeng/unity-ale-inventory-system?color=yellow">
</p>

<p align="center">
  🌍
  中文 |
  <a href="./README_EN.md">English</a> |
  <a href="./README_JA.md">日本語</a>
</p>

<p align="center">
  📥
  <a href="#-安装">安装</a> |
  <a href="#-快速开始">快速开始</a> |
  <a href="Packages/com.ale.inventory/README.md">详细文档</a>
</p>

# Ale Inventory System - 仓库系统
Ale Inventory System 是一款面向 `Unity` 的**数据驱动库存系统插件**，把 **道具 / 仓库 / 商店 / 制作 / 装备 / 技能** 六大子系统整合进同一套工具链。  
它用一个 `InventoryDatabase` 资产集中配置六大子系统的**静态定义数据**（枚举类型、功能标签、道具模板、仓库、商店、蓝图、装备组、技能等），配套一整套**开箱即用的运行时 UI**（背包 / 商店 / 制作 / 装备 / 技能界面）与各自的**运行时管理器**（拥有数量、交易进度、制作产出、已装备道具、已学技能、存档均由管理器维护）。  
面向**设计师**：编辑器始终且仅在 ScriptableObject 上工作，全程支持 Undo / Redo；`JSON` / 二进制仅作为**单向导出**格式。文本组件（TextMeshPro）、本地化（Unity Localization）、资源加载（Addressables）均通过**编译宏可选启用**，因此插件包本身不引入任何硬依赖。

![alt text](./Packages/com.ale.inventory/Docs~/Images/image.png)

## 📜 目录
- [Ale Inventory System - 仓库系统](#ale-inventory-system---仓库系统)
  - [📜 目录](#-目录)
  - [简介](#简介)
    - [项目特性](#项目特性)
    - [六大子系统](#六大子系统)
  - [💻 环境要求](#-环境要求)
  - [📦 安装](#-安装)
    - [使用 UPM（推荐）](#使用-upm推荐)
    - [导入演示 Sample（可选）](#导入演示-sample可选)
    - [其他方式](#其他方式)
  - [🚀 快速开始](#-快速开始)
    - [1. 创建数据文件](#1-创建数据文件)
    - [2. 打开编辑器并配置](#2-打开编辑器并配置)
    - [3. 导出（可选）](#3-导出可选)
    - [4. 运行时挂载](#4-运行时挂载)
    - [5. 一键 Demo](#5-一键-demo)
  - [🖥️ 欢迎窗口](#️-欢迎窗口)
  - [🧩 可选宏开关](#-可选宏开关)
  - [📖 详细文档](#-详细文档)
  - [📁 目录结构](#-目录结构)
  - [📋 待办事项](#-待办事项)
  - [📄 许可](#-许可)

## 简介
大多数游戏都需要一套「道具 + 背包 + 商店 + 制作 + 装备 + 技能」的数据体系，但这些系统各自零散、互相耦合，反复造轮子成本高。Ale Inventory System 把它们收拢到**同一份数据资产**与**同一套编辑器**下：

1. **集中配置** —— 一个 `InventoryDatabase` 承载六大子系统的全部静态定义，编辑器为「顶部系统页签 + 三列布局（定义配置 / 条目列表 / 详细 Inspector）」，支持模板过滤、搜索、拖拽重排、键盘导航、实时重复 ID 检查。
2. **灵活属性** —— 道具与各配置条目的字段由一套**灵活属性系统**承载（Bool / Int / Float / String / Text / Vector / Color / Enum / Sprite / Prefab / AudioClip / AnimationCurve… 每种都支持数组形态），可按功能标签成组增删，无需改代码即可扩展数据结构。
3. **运行时开箱即用** —— 各子系统配套轻量运行时管理器与虚拟滚动 UI 组件，查询、增删、整理、交易、制作、装备、学习技能、存档 / 读档均有现成接口。
4. **零硬依赖** —— TextMeshPro / Localization / Addressables 全部经编译宏可选启用，未开启时插件照常工作。

### 项目特性
| 特性 | 描述 |
| --- | --- |
| 单资产集中配置 | 一个 `InventoryDatabase` 集中六大子系统全部静态数据；编辑器仅在 ScriptableObject 上工作，全程 Undo / Redo。 |
| 灵活属性系统 | 20+ 字段类型（含数组形态）：Bool / Int / Float / String / **Text**（纯文本 fallback + 可选本地化引用）/ Vector2~4 / VectorInt / Color / Enum / StringIntPair / EnumIntPair / Sprite / Texture / Prefab / Material / AudioClip / AnimationClip / AnimationCurve / PhysicsMaterial。 |
| 自定义枚举 + 功能标签 | 枚举值系统自动分配、永不复用、可拖拽重排；功能标签定义一组属性字段，增删标签自动增删道具对应字段，可锁定到模板。 |
| 六大子系统一体化 | 道具 / 仓库 / 商店 / 制作 / 装备 / 技能，共享同一份数据与属性系统，条目互相引用（如技能挂在装备道具上、商店价格取自道具属性）。 |
| 统一虚拟滚动 UI | 网格与顺序列表均为虚拟滚动（对象池 + 仅渲染可见区）；增量差异刷新、生成限速（`spawnPerSecond`）、逐格浮现，海量条目不卡顿。 |
| 运行时管理器 | `InventoryDataManager`（查询）+ 仓库 / 商店 / 制作 / 装备 / 技能各自的运行时管理器，装备 / 技能状态与商店进度均可存档。 |
| 单向导出 | `InventoryDtoMapper` → JSON / 二进制，**覆盖数据库全部配置数据**（六大子系统 20 个列表）；对象引用以 AssetGUID 承载，可选经 Addressable 异步加载。 |
| 三个可选宏 | TextMeshPro（`IS_TMP`）/ Unity Localization（`IS_LOCALIZATION`）/ Unity Addressables（`IS_ADDRESSABLE`），欢迎窗口一键开关并检测对应包是否安装，插件包本身零硬依赖。 |
| 本地化工具 | 一键为 `InventoryDatabase` 生成 / 关联多语言表，遍历全库 `Text` 字段自动生成中文 Key 并回填条目（进度条 + 日志 + 取消）。 |
| 欢迎窗口向导 | 统一入口：创建数据、打开编辑器 / 工具窗口、宏开关、以及「一键生成完整可运行示例」（数据库 + 全部 UI 预制体 + 管理器）。 |

### 六大子系统
| 子系统 | 配置内容 | 运行时管理器 |
| --- | --- | --- |
| **道具系统** | 枚举类型、功能标签、道具模板、道具 + 灵活属性 | `InventoryDataManager`（查询） |
| **仓库系统** | 仓库模板、仓库、容量 / 重量 / 标签限制、整理排序 | `InventoryRuntimeManager`（格子状态 + 存档） |
| **商店系统** | 商店模板、商店、商品组、价格来源、刷新计划 | `ShopRuntimeManager`（交易 + 进度存档） |
| **制作系统** | 分组标签、蓝图模板、蓝图（配方）、制作仓库 | `CraftingRuntimeManager`（消耗 → 产出） |
| **装备系统** | 分组标签、装备组模板、装备组（槽位列表 / 装备槽 / 道具限制 / 属性加成） | `EquipmentRuntimeManager`（装备 / 卸下 + 加成 + 存档） |
| **技能系统** | 分组标签、技能模板、技能（类型 / 效果 / 数值 / 位阶 由自定义属性承载） | `SkillRuntimeManager`（已学状态 + 存档）+ `SkillCollector`（四来源采集） |

> 每个子系统的完整配置与运行时说明见[详细文档](#-详细文档)。

## 💻 环境要求
- `Unity 2022.3` 或更新版本（`package.json` 声明的最低版本；本仓库基于 `Unity 6000.3` 开发与维护）。
- 核心插件为纯 C#，**不引入任何硬依赖**——TextMeshPro / Unity Localization / Unity Addressables 均通过编译宏**可选**启用（见[可选宏开关](#-可选宏开关)）。
- 未启用 `IS_TMP` 时，UI 文本组件回退到 `UnityEngine.UI.Text`，插件照常工作。

## 📦 安装
### 使用 UPM（推荐）
`Window > Package Manager` → 左上角 `+` → `Install package from git URL...` → 粘贴：

```
https://github.com/AleFeng/unity-ale-inventory-system.git?path=/Packages/com.ale.inventory
```

这样装的是 `main` 的最新提交。**要固定版本，把 `#<tag>` 加在整条 URL 的最末尾**（必须在 `?path=` 之后）：

```
https://github.com/AleFeng/unity-ale-inventory-system.git?path=/Packages/com.ale.inventory#1.6.0
```

可用的 tag 见 [Releases](https://github.com/AleFeng/unity-ale-inventory-system/releases)。

### 导入演示 Sample（可选）
装好后在 Package Manager 里选中本包 → `Samples` → 导入 **Inventory System Demo**（数据库资产 `InventoryDatabase` + 管理器预制体 + UI 示例场景），可直接进 Play 体验。也可以直接用[欢迎窗口](#️-欢迎窗口)的「一键生成」向导现场生成一套完整示例。

### 其他方式
也可以下载仓库，把 `Packages/com.ale.inventory` 整个文件夹拷进你项目的 **`Packages/` 目录**（不是 `Assets/`）—— Unity 会自动把它识别为本地包。

安装成功后，菜单栏会出现 **`Tools → Inventory System`**，Unity 会话首次打开时还会自动弹出**欢迎窗口**。

## 🚀 快速开始
下面是最短路径的使用流程，**完整的子系统配置与 API 说明见 [详细文档](#-详细文档)**。

### 1. 创建数据文件
```
Project 面板右键 > Create > Inventory System > Inventory Database
```
（或在欢迎窗口点击「创建新数据文件」；可先在欢迎窗口配置「数据模板」，新建时从模板深拷贝全部数据。）

### 2. 打开编辑器并配置
- 选中 `.asset`，在 Inspector 顶部点击「在 Inventory Editor 中编辑」；或菜单 `Tools > Inventory System > Inventory Editor`。
- 编辑器为**顶部系统页签 + 三列布局**（左：定义配置 / 中：条目列表 / 右：详细 Inspector）。依次在「道具 / 仓库 / 商店 / 制作 / 装备 / 技能」页签中配置。中间条目列表支持模板过滤、搜索、拖拽重排、↑ / ↓ 键盘导航。

### 3. 导出（可选）
工具栏「导出 JSON」或「导出二进制」（存在非空重复 ID 时按钮禁用；空白 ID 条目导出时自动跳过）。编辑器始终在 ScriptableObject 上工作，导出为单向格式。

> 自 **1.6.0（格式 v6）** 起，导出覆盖数据库的**全部**配置数据——六大子系统的 20 个列表都在内。此前只导出道具系统四项，其余静默丢弃。

### 4. 运行时挂载
在场景中新建 GameObject，添加 `InventoryRuntimeManager` 组件，把 `.asset` 拖入 `databases` 数组。游戏启动时自动注册数据库并初始化各仓库空状态。

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

// 开新游戏：清空全部运行时状态
InventoryRuntimeManager.Instance.ResetAll();
```

### 5. 一键 Demo
在**欢迎窗口**展开「测试工具-预制体生成 → 生成全部」，一键生成完整可运行示例（数据库 + 全部 UI 预制体 + 背包 / 商店 / 制作 / 装备 / 技能面板 + 管理器）。

## 🖥️ 欢迎窗口
插件的统一入口面板，集中了「创建数据 / 打开编辑器 / 查看文档 / 生成示例 / 插件宏开关」等常用操作。每次 Unity 会话首次会自动弹出一次，也可随时手动打开：

```
Tools > Inventory System > Welcome Window
```

![alt text](./Packages/com.ale.inventory/Docs~/Images/image-1.png)

窗口自上而下分为四个区域：**快捷操作**（创建数据 / 打开各编辑器与工具窗口 / 一键生成示例预制体）、**数据模板**（指定一个 `InventoryDatabase` 作为新建蓝本）、**插件支持**（三个可选宏一键开关，见下）、**启动时自动显示**（是否每次会话自动弹窗）。

## 🧩 可选宏开关
三个宏均可在**欢迎窗口**的「插件支持」区一键开关，并实时检测对应 Package 是否已安装（未安装时勾选会弹确认对话框）：

| 开关 | 宏 | 作用 |
| --- | --- | --- |
| TextMeshPro | `IS_TMP` | 开启后 UI 文本组件使用 `TMP_Text`，否则用 `UnityEngine.UI.Text`；可配「默认字体」应用到向导生成的 Prefab。 |
| Unity Localization | `IS_LOCALIZATION` | 开启后 `Text` 字段可挂本地化引用（表 + 条目）；配合「本地化工具窗口」一键建表 / 生成中文 Key，支持多语言。 |
| Unity Addressables | `IS_ADDRESSABLE` | 开启后运行时资源经 Addressable 按需异步加载、引用计数自动卸载；导出时自动登记被引用资源。 |

> 切换宏后需等待 Unity 重新编译生效。

## 📖 详细文档
本 README 面向整体介绍与快速上手。**完整的使用说明**——每个子系统的配置细节、运行时 API、灵活属性参考、UI 组件与架构说明等——请见插件内文档：

👉 **[Packages/com.ale.inventory/README.md](Packages/com.ale.inventory/README.md)**

子系统与参考文档（位于 `Packages/com.ale.inventory/Docs~/`）：

- [道具系统](Packages/com.ale.inventory/Docs~/ItemSystem.md) — 枚举类型 / 功能标签 / 道具模板 / 道具 / 灵活属性
- [仓库系统](Packages/com.ale.inventory/Docs~/WarehouseSystem.md) — 仓库模板 / 仓库 / 整理排序 / 运行时 API / 存档
- [商店系统](Packages/com.ale.inventory/Docs~/ShopSystem.md) — 商店类型 / 价格来源 / 商品组 / 刷新计划 / 交易 API
- [制作系统](Packages/com.ale.inventory/Docs~/CraftingSystem.md) — 分组标签 / 蓝图模板 / 蓝图配方 / 制作仓库 / 制作 API
- [装备系统](Packages/com.ale.inventory/Docs~/EquipmentSystem.md) — 分组标签 / 装备组模板 / 槽位列表 / 装备槽 / 道具限制 / 属性加成 / 装备 API
- [技能系统](Packages/com.ale.inventory/Docs~/SkillSystem.md) — 分组标签 / 技能模板 / 技能 / 道具技能引用 / 位阶枚举 / 四种来源 / 已学技能 API
- [属性系统](Packages/com.ale.inventory/Docs~/AttributeSystem.md) — 字段类型参考、`AttributeValue` 取值 / 显示 / 排序比较
- [UI 组件指南](Packages/com.ale.inventory/Docs~/UIComponentGuide.md) — UI 组件、预制体制作、宏开关、Demo 向导
- [架构说明](Packages/com.ale.inventory/Docs~/Architecture.md) — 设计目标、数据流、编辑器与运行时架构、扩展指南

## 📁 目录结构
```
Packages/com.ale.inventory/          ← 包根
├── package.json  CHANGELOG.md  LICENSE.md  README.md   ← 详细使用文档
├── Runtime/
│   ├── Data/            数据模型（Item / Inventory / Shop / Crafting* / AttributeValue 等）
│   ├── Manager/         DataManager / 仓库 / 商店 / 制作 / 装备 / 技能 运行时管理器 + SkillCollector
│   ├── Serialization/   DTO + JSON / 二进制序列化
│   ├── Assets/          资源加载抽象（直接加载）
│   ├── Addressables/    Addressable 资源加载支持
│   ├── Localization/    TMP 文本 / 字体本地化事件
│   └── UI/              运行时 UI 组件（Item / ItemList / Tab / Tool / View / Common）
├── Editor/
│   ├── ItemSystem/ InventorySystem/ ShopSystem/ CraftingSystem/ EquipmentSystem/ SkillSystem/   ← 六大系统面板
│   ├── Common/         通用属性 / 配置绘制器 + 工具窗口基类
│   ├── Addressables/   Addressable 资源引用迁移工具窗口
│   ├── Localization/   本地化工具窗口（建表 / 生成中文 Key）
│   ├── Create/         数据文件创建菜单
│   └── DemoWizard/     一键生成测试数据与预制体
├── Docs~/              详细文档
└── Samples~/Demo/      演示 Sample（数据库 + 管理器预制体 + UI 示例场景）
```

## 📋 待办事项
- 商店「等价交换」类型的完整实现（当前为占位）。
- 更多示例场景与运行时用例。

## 📄 许可
本项目基于 [MIT License](LICENSE) 开源，可自由用于商业与非商业项目。
