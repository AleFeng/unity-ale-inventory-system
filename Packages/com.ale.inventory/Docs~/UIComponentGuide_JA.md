# UI コンポーネントとプレハブ作成ガイド

<p align="center">
  🌍
  <a href="./UIComponentGuide.md">中文</a> |
  <a href="./UIComponentGuide_EN.md">English</a> |
  日本語
</p>

- [説明ドキュメント](../README_JA.md) に戻る

本ドキュメントでは、`Ale.Inventory.UI` アセンブリ内の各 UI コンポーネントの機能、Inspector パラメータ、プレハブの作成方法を説明します。バックパック、ショップ、クラフト、装備の 4 つの画面と、再利用可能な共通コンポーネントをカバーします。

> **名前空間**：すべての UI スクリプトは `Ale.Inventory.Runtime.UI` に宣言されています。参照するときは `using Ale.Inventory.Runtime.UI;`。（asmdef の `rootNamespace` はこの名前空間と一致します。）

---

## 目次

1. [概要とアセンブリ設定](#1-概要とアセンブリ設定)
2. [NumberFormatConfig — 数字フォーマット設定](#2-numberformatconfig--数字フォーマット設定データベース内蔵)
3. [UiwInventoryTab — 倉庫タブボタン](#3-uiwinventorytab--倉庫タブボタン)
4. [UiwInventoryItemSimple — 簡易アイテムセル](#4-uiwinventoryitemsimple--簡易アイテムセル)
5. [UiwInventoryItemDetail — 完全アイテムセル](#5-uiwinventoryitemdetail--完全アイテムセル)
6. [仮想スクロールリスト — 基底 + グリッド / 順序](#6-仮想スクロールリスト--基底--グリッド--順序)
7. [ツールバーコンポーネント — 通貨バー / フィルタバー / ソートバー](#7-ツールバーコンポーネント--通貨バー--フィルタバー--ソートバー)
8. [UiwInventoryView — バックパックメイン画面](#8-uiwinventoryview--バックパックメイン画面)
9. [完全なシーン構築例](#9-完全なシーン構築例)
10. [他システムの画面と共通コンポーネント](#10-他システムの画面と共通コンポーネント)
11. [よくある質問](#11-よくある質問)

---

## 1. 概要とアセンブリ設定

### 位置とディレクトリ構成

スクリプトは型ごとに `Runtime/UI/` 以下のサブフォルダに配置されます（名前空間は一律 `Ale.Inventory.Runtime.UI`、フォルダによって変わりません）：

```
Runtime/UI/
├── Item/      単一セル（UiwInventoryItemBase / SlotBase / Cell / Simple / Detail、UiwShopItemDetail、UiwCraftingBlueprintCell、UiwCraftingInputCell、UiwEquipmentSlot、UiwEquipmentBonusEntry、UiwInventoryItemEvents）
├── ItemList/  仮想スクロールリスト族：基底 UiwInventoryItemListBase + 汎用 UiwInventoryGridList / UiwInventoryOrderList + リーフ（倉庫 UiwInventoryItemGridList / UiwInventoryItemOrderList、クラフト UiwCraftingBlueprintList、スキル UiwSkillGridList / UiwSkillOrderList）+ GridCellDragHandler + ViewportSizeWatcher（装備候補リストは View/Equipment/、ショップ商品リストは View/Shop/）
├── Tab/       タブ / フィルタ（UiwInventoryTab、UiwShopGroupTab、UiwFoldTab、UiwFilterTabBar、UiwCraftingGroupFilter）
├── Tool/      汎用ユーティリティコンポーネント（UiwCurrencyBar、UiwSortToolbar、UiwItemTooltip、UiwNumberCounter）
├── View/      メイン画面：UiwViewBase 基底クラス
│   ├── Inventory/  UiwInventoryView
│   ├── Shop/       UiwShopViewBase + UiwSellShopView / UiwRecycleShopView / UiwBarterShopView
│   ├── Crafting/   UiwCraftingView、UiwCraftingDetail
│   └── Equipment/  UiwEquipmentView、UiwEquipmentGroupPanel、UiwEquipmentSlotList、UiwEquipmentBonusPanel、UiwEquipmentSelectPanel、UiwEquipmentCandidateList、UiwEquipmentDragContext
├── Common/    汎用ウィジェット（UiwTextLabel）
└── Ale.Inventory.UI.asmdef   （ルートにあり、すべてのサブフォルダを自動的にカバー）
```

> 通貨バー / ソートバー / ホバーポップアップ / 数値カウンター（`Tool/`）とフィルタタブバー / 折りたたみタブ（`Tab/`）はいずれも独立した汎用コンポーネントです。各メイン画面（`UiwInventoryView`、`UiwShopViewBase`、`UiwCraftingView`、いずれも `UiwViewBase` から派生）は「コンポジション」でそれらの参照を持ち、バックパック / ショップ / クラフトの各システム UI 間で再利用します。

### アセンブリ

`Ale.Inventory.UI`（`Ale.Inventory.UI.asmdef`）

- `Ale.Inventory.Runtime`（ランタイムデータとマネージャー）を参照
- `Unity.TextMeshPro` を参照（マクロで切り替え可能）
- コードの名前空間：`Ale.Inventory.Runtime.UI`

### TextMeshPro マクロ

すべてのテキストコンポーネントはコンパイルマクロ `IS_TMP` で切り替わります：

| マクロの状態 | テキストコンポーネント型 |
|--------|------------|
| **定義済み** `IS_TMP` | `TMPro.TMP_Text` |
| **未定義**（既定） | `UnityEngine.UI.Text` |

**TMP を有効化**：`Project Settings > Player > Scripting Define Symbols` に `IS_TMP` を追加し、プロジェクトに TextMeshPro パッケージが導入済みであることを確認します。

> マクロを切り替えた後は再コンパイルが必要で、テキストコンポーネントを使うすべてのプレハブの参照フィールドは、対応する型のコンポーネントに再割り当てが必要です。

---

## 2. NumberFormatConfig — 数字フォーマット設定（データベース内蔵）

`NumberFormatConfig` は大きな数値をローカライズされた短い文字列にフォーマットします（例：`1500 → "1.5K"`、`10000000 → "1000万"`）。

> **重要な変更**：これは**もう独立した ScriptableObject アセットではなく**、`InventoryDatabase` 内の一群の**名前付き設定**（`numberFormatConfigs` リスト）です。Inventory Editor の「倉庫システム」タブの数字フォーマットパネルで編集し、倉庫 / 倉庫テンプレート / ショップ / ブループリント（テンプレート）が `numberFormatRef`（名前で参照）で選択します。

### データ構造

```
NumberFormatConfig
├── name                設定名（データベース内で一意、numberFormatRef が参照）
└── locales: List<NumberFormatLocale>
        ├── languageCode   言語コード（"zh-CN"/"en-US"…。空文字列 = 既定のフォールバック言語）
        └── rules: List<NumberFormatRule>   （threshold の大きい順）
                ├── threshold       このルールを発動する最小値（以上）
                ├── divisor         除数（例：1000 → 千分の 1 に縮小して表示）
                ├── suffix          接尾辞（"K"/"万"/"M"。任意のローカライズ接尾辞テーブル/キー）
                └── decimalPlaces   小数桁数（0 = 整数に丸め）
```

効果の例：中国語 `15000 → "1万"`、`2_0000_0000 → "2.0亿"`。英語 `15000 → "15.0K"`。ルールにヒットしないときは数値をそのまま返します。

### フロー：設定から UI へ

1. データベースに名前付き `NumberFormatConfig`（例：`Default`）を作り、各言語のルールを設定。
2. 倉庫 / ショップ / ブループリント（またはそのテンプレート）の `numberFormatRef` にその名前を入力。
3. ランタイムで各メイン画面（`UiwViewBase` 派生）が現在の言語でヒットする `NumberFormatLocale` を解決し（`ResolveNumberFormatLocale`）、各セルコンポーネントに渡す。

各セルコンポーネント（`UiwInventoryItemBase` 派生）が持つフォーマットフィールドは
`[HideInInspector] public NumberFormatLocale numberFormat;` です —— **ビューがランタイムで割り当て、Inspector では手動指定しません**。以降の章で「数字フォーマットを `numberFormat` に割り当てる」とあるのは、このランタイムフローを指します。

### API

```csharp
// 直接フォーマット（言語別）
string text = config.Format(value, langCode);
// またはある言語の locale を先に解決してからフォーマット
string t2 = locale.Format(value);
```

---

## 3. UiwInventoryTab — 倉庫タブボタン

`UiwInventoryTab` は軽量な MonoBehaviour で、**倉庫 ID 名**を表示し、選択されているかどうかで視覚状態を切り替えます。`UiwInventoryView` が自動でインスタンス化・管理するため、通常は手動で駆動する必要はありません。

### プレハブの作成

1. UI **Button** ノード（Canvas 下）を作成し、`Prefab_InventoryTab` と命名。
2. `UiwInventoryTab` コンポーネントをアタッチ。
3. Button の子ノードにテキストコンポーネント（`Text` または `TMP_Text`）を用意し、参照を `label` フィールドに割り当て。
4. Button 内に子 GameObject を**選択インジケーター**（下部ハイライトバー、色オーバーレイなど）として作成し、`selectedIndicator` フィールドに割り当て。`UiwInventoryView` はタブ切り替え時にそれに `SetActive(isSelected)` を呼びます。

### Inspector パラメータ

| パラメータ | 説明 |
|------|------|
| `label` | 倉庫 ID を表示するテキストコンポーネント（`Text` / `TMP_Text`） |
| `selectedIndicator` | 選択状態のインジケーター GameObject（選択時に `SetActive(true)`） |

### 公開プロパティとメソッド

| メンバー | 説明 |
|------|------|
| `string InventoryId` | 現在バインドされている倉庫 ID（読み取り専用） |
| `SetData(inventoryId, displayName, isSelected)` | `UiwInventoryView` / `UiwCraftingView` が呼び、テキストと選択状態をリフレッシュ |

> **注意**：Button の `onClick` イベントは `UiwInventoryView.BuildTabs()` がランタイムで動的にバインドします。プレハブ内で自分でイベントをバインドする**必要はありません**。

---

## 4. UiwInventoryItemSimple — 簡易アイテムセル

**アイコン**と**数量**のみを表示し、通貨アイテムバーなど詳細情報が不要な場面に適します。`UiwInventoryView` が通貨バー領域でインスタンス化します。

### プレハブの作成

1. UI ノードを作成し、`Prefab_InventoryItemSimple` と命名。
2. `UiwInventoryItemSimple` コンポーネントをアタッチ。
3. 子ノードを用意：
   - **アイコン**：`Image` コンポーネント → `iconImage` に割り当て
   - **数量テキスト**：`Text` / `TMP_Text` → `quantityText` に割り当て
4. 数字フォーマットはプレハブで指定不要：`numberFormat` フィールドは `[HideInInspector]` で、メイン画面がランタイムで `numberFormatRef` + 現在の言語に応じて割り当て（[§2](#2-numberformatconfig--数字フォーマット設定データベース内蔵) を参照）。未割り当ての場合は整数文字列を直接表示。
5. `iconAttrId` をアイテムの静的データ内のアイコン属性の ID に設定（既定 `"图标"` /「アイコン」）。

### Inspector パラメータ

| パラメータ | 既定値 | 説明 |
|------|--------|------|
| `iconAttrId` | `"图标"`（アイコン） | アイコン属性 ID（`Sprite` 型属性フィールドの ID） |
| `iconImage` | — | アイコン `Image` コンポーネント参照 |
| `quantityText` | — | 数量テキストコンポーネント参照 |
| `numberFormat` | — | `NumberFormatLocale`、`[HideInInspector]`、メイン画面がランタイムで割り当て |

### 公開メソッド

| メソッド | 説明 |
|------|------|
| `SetItem(itemId, quantity)` | 指定アイテムと数量を表示（静的データを自動クエリしてアイコン取得） |
| `SetEmpty()` | 表示をクリア |

### 拡張

ローカライズ数字を接続するには、`UiwInventoryItemSimple` を継承して次をオーバーライド：

```csharp
protected override string GetCurrentLanguage() => LocalizationManager.CurrentLanguage;
```

---

## 5. UiwInventoryItemDetail — 完全アイテムセル

アイコン、名称、説明、品質背景、数量、価格、購入済み数量、および**ホバーハイライト**と**スタック満杯アニメーション**に対応した、機能完全なアイテムセルコンポーネント。`UiwInventoryItemOrderList` などの仮想スクロールリストが統一的に駆動します。

### プレハブの作成

推奨階層構造：

```
Prefab_InventoryItemDetail  [UiwInventoryItemDetail]
├── Background              [Image]  品質背景
├── Icon                    [Image]  アイテムアイコン
├── StackFullIcon           [Image]  スタック満杯アイコン（初期 alpha=0）
├── HoverBorder             [Image]  ホバーハイライト枠（初期 alpha=0）
├── NameText                [Text / TMP_Text]
├── DescText                [Text / TMP_Text]（任意）
├── QuantityText            [Text / TMP_Text]
├── PriceGroup              [GameObject]  （任意）
│   ├── PriceIconImage      [Image]
│   └── PriceText           [Text / TMP_Text]
└── PurchaseCountText       [Text / TMP_Text]（任意）
```

手順：
1. UI ノードを作成し、上記の階層で子ノードを組織。
2. `UiwInventoryItemDetail` コンポーネントをアタッチ。
3. 各子ノードの参照を Inspector の対応フィールドに入力。
4. 属性フィールド ID を設定（下表参照）。`InventoryDatabase` 内のアイテムの属性フィールド ID と一致させる。
5. `RectTransform` の高さがエントリの行の高さ（仮想スクロールリストがプレハブの実サイズから**自動測定**、数値入力不要）。アンカータイプは不問（仮想スクロールが位置を自動上書き）。

### Inspector パラメータ

**属性フィールド ID**（データベースの `AttributeDefinition.id` に対応）

| パラメータ | 既定値 | 説明 |
|------|--------|------|
| `iconAttrId` | `""` | アイコン属性 ID（`Sprite` 型） |
| `nameAttrId` | `"名称"`（名称） | 名称属性 ID（`String` 型） |
| `descAttrId` | `""` | 説明属性 ID（空なら非表示） |
| `qualityAttrId` | `"品质"`（品質） | 品質属性 ID（`Enum` 型、整数値を `qualitySprites` のインデックスに） |
| `priceAttrId` | `""` | 価格属性 ID（空なら非表示） |
| `currencyItemId` | `""` | 通貨アイテム ID（価格横の通貨アイコン表示用） |
| `purchaseCountAttrId` | `""` | 購入済み数量属性 ID（空なら非表示） |

**表示制御**

> どの表示要素にも**独立したブール切り替えはありません**：表示するかどうかは、プレハブに対応する子コンポーネントがアタッチされているかに完全に依存します
> （名称 `nameText`、説明 `descText`、品質背景 `qualityBackground`、数量 `countText`、
> 価格 `priceContainer` + `priceCurrencyPrefab` など）。アタッチされていなければ非表示。価格エリアはアイテムに価格データがないときも自動的に隠れます。

**子コンポーネント参照**

| パラメータ | 説明 |
|------|------|
| `iconImage` | アイテムアイコン `Image` |
| `nameText` | 名称テキスト |
| `descText` | 説明テキスト |
| `qualityBackground` | 品質背景 `Image` |
| `qualitySprites` | 品質背景 Sprite 配列、**インデックス = 列挙整数値**（品質列挙値 0 は `qualitySprites[0]` に対応） |
| `quantityText` | 数量テキスト |
| `priceIconImage` | 通貨アイコン `Image` |
| `priceText` | 価格テキスト |
| `purchaseCountText` | 購入済み数量テキスト |

**ホバーハイライト**

| パラメータ | 既定 | 説明 |
|------|------|------|
| `hoverBorder` | — | ホバー時にフェードインする枠 `Image`（初期 `Color.a = 0`） |
| `hoverFadeDuration` | `0.15` | フェードイン/アウト時間（秒） |

**スタック満杯ヒント**

| パラメータ | 既定 | 説明 |
|------|------|------|
| `stackFullIcon` | — | スタック満杯時に右上に表示するアイコン `Image`（初期 `Color.a = 0`） |
| `stackFullFadeDuration` | `0.15` | フェードイン/アウト時間（秒） |

スタック満杯の判定条件：アイテムの `stackLimit > 0` かつ現在のセルの `quantity >= stackLimit`。

**数字フォーマット**

| パラメータ | 説明 |
|------|------|
| `numberFormat` | `NumberFormatLocale`（`[HideInInspector]`）。数量と価格の表示フォーマットを制御。メイン画面がランタイムで割り当て |

### 公開メソッド

| メソッド | 説明 |
|------|------|
| `SetSlot(inventoryId, slot)` | 指定スロットにバインドしてすべての表示をリフレッシュ。`UiwInventoryItemOrderList` / `UiwInventoryItemGridList` が呼ぶ |
| `SetEmpty()` | すべての表示をクリアし、GameObject を非表示 |

### 拡張

このクラスを継承して `GetCurrentLanguage()` をオーバーライドすると、ローカライズシステムを接続できます（`UiwInventoryItemSimple` と同じ）。

---

## 6. 仮想スクロールリスト — 基底 + グリッド / 順序

「リスト / グリッドで大量のエントリ / アイテムを表示する」すべてのリストは、**同じ仮想スクロールエンジン**の上に構築されています：エントリがいくつあっても、画面上には少数のセルインスタンス（= 可視領域のセル数 + 両端各 `bufferCount` 個のバッファ）だけを維持し、スクロール時は位置とデータを更新するだけで、オブジェクトを動的に生成 / 破棄しません。**グリッドも順序リストも仮想スクロール**です。

### 3 層アーキテクチャ

```
UiwInventoryItemListBase<TData, TCell>        ← 基底：仮想スクロールエンジン（オブジェクトプール + ビューポート監視 + 回収/再利用）+ 抽象「レイアウト戦略」
   ├─ UiwInventoryGridList<TData, TCell>       ← 汎用グリッドレイアウト（複数列/行、縦 / 横スクロール、交差軸の数は自動）
   │     ├─ UiwInventoryItemGridList           ← 倉庫グリッド（RuntimeItemSlot + UiwInventoryItemCell、ドラッグ整理あり）
   │     ├─ UiwSkillGridList                    ← スキルグリッド（Skill + UiwSkillEntry）
   │     └─ UiwEquipmentCandidateList          ← 装備候補（整理ドラッグなし、装備ドラッグは保持）
   └─ UiwInventoryOrderList<TData, TCell>      ← 汎用順序レイアウト（単一列縦）
         ├─ UiwInventoryItemOrderList          ← 倉庫リスト（RuntimeItemSlot + UiwInventoryItemDetail）
         ├─ UiwCraftingBlueprintList           ← クラフトブループリントリスト（+ 選択）
         ├─ UiwSkillOrderList                   ← スキルリスト（Skill + UiwSkillEntry）
         └─ UiwShopCommodityList               ← ショップ商品リスト（回数はデータモデルに格納、ショップ画面参照）
```

- **基底**（ジェネリック抽象、直接アタッチしない）はオブジェクトプール、ビューポートサイズ監視、回収 / 再利用ループ、共通入口 `SetItems` / `UpdateItems` / `ScrollToStart` を持つ。
- **汎用層**（ジェネリック抽象、直接アタッチしない）はレイアウト戦略のみを提供：`UiwInventoryOrderList` = 単一列縦。`UiwInventoryGridList` = 2 次元グリッド、`scrollDirection` により**縦**（列数 = ビューポート幅 ÷ セル幅）/ **横**（行数 = ビューポート高 ÷ セル高）に分かれ、交差軸の数はビューポートサイズに応じて自動再計算。
- **リーフ層**（非ジェネリック、プレハブにアタッチ）はジェネリック `<データ型, セル型>` を閉じ、「1 件のデータをセルに表示 / セルをクリア」を実装し、各システムのコンテキストに対応。**新システム**向けにリストを追加するには：`UiwInventoryGridList<T,TCell>` または `UiwInventoryOrderList<T,TCell>` を継承し、`BindCell` / `ClearCell`（および任意で `InitCell` / `OnCellAssigned`）をオーバーライドします。

### プレハブの作成

標準的な Unity UGUI ScrollView 構造。**Content に `GridLayoutGroup` / `VerticalLayoutGroup` / `ContentSizeFitter` を付けない** —— セルの位置と Content サイズは仮想スクロールが引き継ぎます：

```
Prefab_List  [リーフコンポーネント、例：UiwInventoryItemGridList]
└── ScrollRect      [ScrollRect]
    └── Viewport    [RectTransform]（Mask コンポーネント）
        └── Content [RectTransform]  ← LayoutGroup / SizeFitter なし
```

手順：
1. UI **ScrollView**（`GameObject > UI > Scroll View`）を作成し、Content 上のあらゆる LayoutGroup / ContentSizeFitter を削除。
2. 必要なリーフコンポーネントをルートにアタッチ（例：バックパックグリッド `UiwInventoryItemGridList`、バックパックリスト `UiwInventoryItemOrderList`）。
3. `ScrollRect` と `Content` の参照をコンポーネントの `scrollRect` / `content` フィールドに割り当て。
4. エントリプレハブを `cellPrefab` フィールドに割り当て（グリッドは `UiwInventoryItemCell`、リストは `UiwInventoryItemDetail`。セルの高さ / 幅はプレハブの `RectTransform` から自動測定、手入力不要）。

> **Content アンカー**：順序リストは上部で左右にストレッチ（`anchorMin=(0,1), anchorMax=(1,1), pivot=(0.5,1)`）、縦スクロールのグリッドも同様（横スクロールなら左側で上下ストレッチに変更）。仮想スクロールがデータ量に応じて Content サイズを自動で広げ、セルごとに配置します。

### Inspector パラメータ

| パラメータ | 層 | 既定 | 説明 |
|------|------|------|------|
| `cellPrefab` | 基底 | — | エントリセルのプレハブ（`TCell` コンポーネント、必須）。サイズはその `RectTransform` から自動測定 |
| `scrollRect` | 基底 | — | 所属する `ScrollRect`（その viewport で可視領域を測定、サイズ変化を監視） |
| `content` | 基底 | — | Content ノードの `RectTransform` |
| `bufferCount` | 基底 | `1` | ビューポートのスクロール方向の両端に余分に保持するバッファセル数（高速スクロール時の空白露出を防ぐ） |
| `spawnPerSecond` | 基底 | `30` | 毎秒最大**生成 / 割り当て**するセル数（レート制限）。インスタンス化とバインド（アイコン非同期読み込み含む）を複数フレームに分散し、単一フレームのピークを回避。`≤ 0` = 制限なし（1 フレームで埋める） |
| `scrollDirection` | グリッド | `縦` | `縦`（交差軸=列、ビューポート幅で列数）/ `横`（交差軸=行、ビューポート高で行数） |
| `spacing` / `padding` | グリッド | `(6,6)` | セル間隔 / コンテンツ開始の内側余白（ピクセル） |

### 公開メソッド（基底）

| メソッド | 説明 |
|------|------|
| `SetItems(items)` | データを設定し**起点**から再表示（タブ / フィルタ / ソート切り替えなど、先頭 / 起点に戻る場面） |
| `UpdateItems(items)` | データを増分更新するが**現在のスクロール位置を保持**（内容変化でプレイヤーのスクロールを中断しない） |
| `RefreshItemsData(items)` | 増分**差分リフレッシュ**（スクロール位置を保持）：エントリ数が不変のとき、データが変わった可視セルのみ再バインド（`NeedsRebind` が判定）、変わらないものは触らない —— アイコン非同期再読み込みのちらつきを回避。倉庫のドラッグ入れ替え / スタックはこの経路 |
| `ScrollToStart()` | 起点（縦=先頭 / 横=最左）へスクロールし、可視セルをリフレッシュ |

各リーフはこの上にドメインメソッドを提供します（例：倉庫 `SetItemSlotList` / `UpdateItemSlotList` / `SetNumberFormat`、ブループリント `SetBlueprints` / `SetSelectedById`、スキル `SetSkills`）。通常は所属メイン画面がデータ変化後に呼ぶため、手動は不要です。

### パフォーマンスと体験（基底に内蔵）

エンジンは「可視領域のみ描画」以外に、3 つの最適化を内蔵し、いずれも基底が統一的に提供、すべてのリーフに有効です：

- **増分差分リフレッシュ**（`RefreshItemsData` + `NeedsRebind`）：倉庫内容の変化時、**「表示内容が変わった」可視セルのみ再バインド**（ドラッグ入れ替え / その場スタックは通常 2 セルのみ）、変わらないセルは触らない —— アイコン非同期再読み込みのちらつきと無駄なオーバーヘッドを回避。判定は「セルの現在の表示内容」と新データの比較（アイテムセルは `UiwInventoryItemSlotBase.MatchesSlot` でアイテム ID + 数量を比較）。倉庫のドラッグ入れ替えはこれにより**スクロールバーを先頭に戻さなくなります**（現在のスクロール位置を保持）。
- **生成 / 割り当てのレート制限**（`spawnPerSecond`、既定 `30` 個/秒）：セルのインスタンス化とバインド（アイコン非同期読み込み含む）を**複数フレームに分散**し、単一フレームで一斉に大量のセルを生成 / 読み込みしてカクつきやアセット読み込みの詰まりを起こすのを回避。インスタンスは必要に応じて**遅延生成**され、目標プール上限まで。予算に上限（約 0.1 秒分）があり、「画面を開いた最初のフレーム」でも一斉インスタンス化せず、設定レートに厳密に近づく。`≤ 0` = 制限なし（1 フレームで埋める、旧挙動）。
- **スクロール方向に追従するセルごとのフェードイン**：割り当て待ちのセルは**ビューポートに入る順**に出現 —— 末尾へスクロールは前から後（縦「上から下」）、起点へスクロールは後から前（縦「下から上」）。初回オープン / ページ切替 / 全表更新は昇順（上から下）。

> **倉庫グリッドのドラッグ整理**：`UiwInventoryItemGridList` は仮想スクロール下でもドラッグ入れ替えに対応 —— セルのデータインデックスはバインドとともに動的更新、ビューポート端へのドラッグで自動スクロール、ドラッグ中はソースセルを「ピン留め」して回収・無効化を防止。`dragSort=true` の倉庫のみ有効（順序リストはドラッグ整理非対応、右クリックを使う）。

---

## 7. ツールバーコンポーネント — 通貨バー / フィルタバー / ソートバー

通貨バー（`Tool/`）、フィルタタブバー（`Tab/`）、ソートバー（`Tool/`）はいずれも**独立した汎用コンポーネント**で、特定システムと分離しています：コンポーネントは「表示 + 入力イベント」のみを担い、データとコールバックはホスト画面が注入します。`UiwInventoryView`、`UiwShopViewBase`、`UiwCraftingView` などが「コンポジション」でそれらの参照を持ち、イベントを購読することで、各システム UI 間で同じツールバーを再利用します。

### 7.1 UiwCurrencyBar — 通貨バー

ホストが「通貨 ID リスト」と「ID で保有量を取得する」ゲッターを提供し、コンポーネントが通貨セルをインスタンス化してリフレッシュします。

| Inspector パラメータ | 説明 |
|------|------|
| `currencyContainer` | 通貨セルの親コンテナ |
| `currencyPrefab` | 通貨セルのプレハブ（`UiwInventoryItemSimple`） |
| `currencyItemIds` | **通貨アイテム ID リスト（本コンポーネントに直接設定）**。ids 引数付きの `Setup` オーバーロードでランタイムに上書き可能 |

| 公開メソッド | 説明 |
|------|------|
| `Setup(ownedGetter, fmt)` | 本コンポーネントの `currencyItemIds` でセルを構築。`ownedGetter(id)` が保有量を返す。`fmt` は数字フォーマット |
| `Setup(currencyIds, ownedGetter, fmt)` | 明示的な `currencyIds` で上書き（null なら `currencyItemIds` に退避）。ショップなど通貨を動的収集する用途向け |
| `SetNumberFormat(fmt)` / `Refresh()` / `Clear()` | フォーマット更新 / 保有量を再読み込みしてリフレッシュ / クリア |

> バックパック：getter = 開いている倉庫をまたいだ `GetTotalCount` の合計。ショップ：getter = `ShopRuntimeManager.GetOwnedCount(shop, id)`。

### 7.2 UiwFilterTabBar — フィルタタブバー

フィルタ項目を機能タグボタンとして表示し（先頭に固定の「すべて」）、選択ハイライトを管理し、変化時にホストへコールバックします。

| Inspector パラメータ | 既定 | 説明 |
|------|------|------|
| `filterContainer` | — | フィルタボタンの親コンテナ |
| `filterButtonPrefab` | — | フィルタ `Button` プレハブ（タグ名を表示する `Text`/`TMP_Text` 子ノードを含む） |
| `allLabel` | `全部`（すべて） | 「すべて」ボタンの表示名 |
| `activeColor` / `inactiveColor` | 金 / 白 | 選択 / 未選択ボタンの normalColor |

| 公開メンバー | 説明 |
|------|------|
| `event OnFilterChanged(string)` | フィルタ変化（引数はタグ名、`null` = すべて） |
| `SetFilters(tagNames, selectAll=true)` | ボタンを再構築、既定で「すべて」を選択し 1 回コールバック |
| `string ActiveFilter` / `Clear()` | 現在有効なフィルタ / クリア |

### 7.3 UiwSortToolbar — ソートバー

ソートドロップダウン + 昇順/降順切り替え + 自動整理 の 3 コントロールを集約し、イベントコールバックで駆動します。

| Inspector パラメータ | 既定 | 説明 |
|------|------|------|
| `sortDropdown` | — | ソート条件ドロップダウン |
| `sortDirectionButton` | — | 昇順/降順切り替えボタン |
| `sortDirectionLabel` | — | 「昇順」/「降順」を表示するテキスト |
| `autoSortButton` | — | 自動整理ボタン |
| `ascText` / `descText` | `升序`（昇順） / `降序`（降順） | 昇順/降順テキスト |

> ドロップダウンの**表示名**とソートの**無視 ID** は本コンポーネントには設定せず、整理オプション自身の組み込みフィールド（`SortOption.displayName` / `SortOption.ignoreIds`、「倉庫システム → 整理オプション」で編集）です。本コンポーネントの表示名は `SortOption.ResolveDisplayName` で自動解決、無視 ID はソートロジックが `SortOption.EffectiveIgnoreIds` で読みます。

| 公開メンバー | 説明 |
|------|------|
| `event OnSortChanged(int,bool)` | ソート条件 / 方向の変化（ドロップダウンインデックス, 昇順か） |
| `event OnAutoSort` | 自動整理ボタンのクリック |
| `SetOptions(displayNames)` | ドロップダウン項目を充填（項目がないときドロップダウンと昇順/降順ボタンを隠す） |
| `SetSortPriorities(priorities, db)` | ソート条件（`SortPriority`）でドロップダウンを充填、表示名は対応する整理オプションの組み込み `displayName`（`SortOption.ResolveDisplayName`）で解決（`SetOptions` の便利ラッパー） |
| `int SortIndex` / `bool Ascending` | 現在の選択インデックス / 昇順か |

### 7.4 フィルタ / ソートパイプライン（リスト基底にカプセル化）

`UiwFilterTabBar` + `UiwSortToolbar` の配線は、仮想スクロールリスト基底 `UiwInventoryListBase` に統一的にカプセル化されています。各システムのビューは**2 つのコンポーネント参照をリストにアタッチして 1 回設定するだけ**で、各ビューでフィルタ / ソートコードを繰り返し書く必要はありません。パイプラインは任意・増分式：

```
ソースエントリ → 主/副タブフィルタ(filterBar / secondaryFilterBar) → 追加フィルタ(SetExtraFilter、例：検索) → ソート(sortToolbar) → 表示
```

| リスト基底 API | 説明 |
|------|------|
| `ConfigureFilter(predicate, primaryTokens, secondaryTokens=null, showAll=true)` | タブフィルタの述語とタブ項目を設定 |
| `SetExtraFilter(predicate, refresh=true)` | 追加フィルタ（検索ボックス / グループ分けなど）、タブフィルタの上に重ねる |
| `ConfigureSort(keySelector, db, priorities, tiebreakers, writeRuntime=null)` | ソートを設定：表示ソートまたはランタイムソートの書き込み |
| `SetSourceItems(items, preserveScroll=false)` | ソースデータを設定、フィルタ → ソート → 表示 を発火 |

- **再利用範囲**：ショップ商品リスト（`UiwShopCommodityList`）、クラフトブループリントリスト、スキルリストはいずれもこのパイプラインを通す。`UiwShopViewBase` は `UiwFilterTabBar` / `UiwSortToolbar` を直接サポート。
- **バックパックの例外**：バックパックはドラッグ整理（`dragSort`）とランタイムソート書き込みなどが結合しているため、自身のフィルタ / ソートロジックを保持し、本パイプラインを通しません。

---

## 8. UiwInventoryView — バックパックメイン画面

`UiwInventoryView` は最上位のバックパックメイン画面コントローラーで、以下のコンポーネントと機能を**コンポジション**します：

- **複数倉庫タブ切り替え**（`UiwInventoryTab` プレハブを使用）
- **通貨バー**（`UiwCurrencyBar` コンポーネント）
- **仮想スクロールリスト**（グリッド `UiwInventoryItemGridList` / 順序 `UiwInventoryItemOrderList`、ビューがいずれかを表示切り替え）
- **フィルタタブバー**（`UiwFilterTabBar` コンポーネント）
- **ソートバー**（`UiwSortToolbar` コンポーネント：ソートドロップダウン + 昇順/降順 + 自動整理）

### プレハブの作成（推奨階層構造）

```
Prefab_InventoryView  [UiwInventoryView]
│
├── TabContainer      [HorizontalLayoutGroup]   ← タブコンテナ
│
├── CurrencyContainer [HorizontalLayoutGroup + UiwCurrencyBar]   ← 通貨バーコンポーネント
│
├── ToolbarRow        [HorizontalLayoutGroup]
│   ├── FilterContainer  [HorizontalLayoutGroup + UiwFilterTabBar] ← フィルタタブバーコンポーネント
│   └── SortBar          [HorizontalLayoutGroup + UiwSortToolbar]   ← ソートバーコンポーネント
│       ├── SortDropdown     [Dropdown]
│       ├── SortDirButton    [Button → SortDirLabel(Text/TMP_Text)]
│       └── AutoSortButton   [Button]
│
├── ItemOrderList     [UiwInventoryItemOrderList] ← 順序（リスト）仮想スクロール
└── ItemGridList      [UiwInventoryItemGridList]  ← グリッド仮想スクロール（いずれかを表示切り替え）
```

> 通貨バー / フィルタバー / ソートバーの具体的な子ノード参照（コンテナ、ボタン、ドロップダウン、テキスト）は**それぞれのツールバーコンポーネント**に配線します（前節参照）。`UiwInventoryView` はこれら 3 コンポーネント自体を参照するだけです。

### Inspector パラメータ

**タブ**

| パラメータ | 説明 |
|------|------|
| `tabContainer` | タブボタンの親ノード（`UiwInventoryTab` がこの下にインスタンス化） |
| `tabPrefab` | `UiwInventoryTab` プレハブ |

**仮想スクロールリスト**

| パラメータ | 説明 |
|------|------|
| `itemOrderList` | 順序（リスト）仮想スクロールリスト `UiwInventoryItemOrderList` の参照 |
| `itemGridList` | グリッド仮想スクロールリスト `UiwInventoryItemGridList` の参照（順序リストと切り替えボタンでいずれかを表示） |

**通貨バー**

| パラメータ | 説明 |
|------|------|
| `currencyBar` | `UiwCurrencyBar` コンポーネント参照（通貨セルのインスタンス化とリフレッシュを担当。**通貨アイテム ID はそのコンポーネントに設定**） |

**フィルタ / ソートツールバー**

| パラメータ | 説明 |
|------|------|
| `filterBar` | `UiwFilterTabBar` コンポーネント参照（フィルタタブバー） |
| `sortToolbar` | `UiwSortToolbar` コンポーネント参照（ソートドロップダウン + 昇順/降順 + 自動整理。**ドロップダウン表示名 / 無視 ID は整理オプション自身の組み込みフィールド**、「倉庫システム → 整理オプション」で編集） |

### 公開 API

```csharp
using Ale.Inventory.Runtime.UI;

// バックパックを開く（表示する倉庫 ID 配列を渡す、既定フィルタタグは任意）
inventoryView.Open(new[] { "backpack", "stash" }, defaultFilter: null);

// バックパックを閉じる
inventoryView.Close();
```

**`Open` の挙動**：
1. GameObject を有効化。
2. `inventoryIds` の順にタブをインスタンス化し、切り替えイベントをバインド。
3. `currencyBar` コンポーネントで通貨アイテムセルをインスタンス化（開いているすべての倉庫をまたいで数量集計。倉庫変化時に自動リフレッシュ）。
4. `InventoryRuntimeManager.OnInventoryChanged` イベントを購読し、倉庫内容変化時にリストを自動リフレッシュ。
5. 既定で最初のタブに切り替え（`SwitchTab(0)`）。

**`Close` の挙動**：イベントの購読を解除し、GameObject を非表示。

### ソートロジックの説明

UI のソートは**ローカルビューソート**（ランタイム倉庫データを変更しない）で、`itemOrderList` / `itemGridList` に渡す slot 順序のみに影響します：

- ユーザーがドロップダウンで主ソートフィールドを選択。
- 副ソート（tiebreakers）は現在の倉庫定義（`Inventory.sortTiebreakers`）由来で、自動的に有効。
- 昇順/降順は切り替えボタンで制御。

**永続ソート**（セーブ後も保持）が必要なら、ゲームロジック層で `InventoryRuntimeManager.SortInventory` を呼んでランタイム状態に書き込みます。

---

## 9. 完全なシーン構築例

### ステップ 1：数字フォーマットを設定

Inventory Editor「倉庫システム」タブの数字フォーマットパネルで名前付き `NumberFormatConfig`（例：`Default`）を新規作成してルールを設定。使いたい 倉庫 / ショップ / ブループリント（またはそのテンプレート）で `numberFormatRef` にその名前を入力。ランタイムでメイン画面が現在の言語で解決し、各セルコンポーネントに配布します（[§2](#2-numberformatconfig--数字フォーマット設定データベース内蔵) を参照）。

### ステップ 2：プレハブを作成

以下の順で作成（後のプレハブが前のものに依存）：

| 順 | プレハブ名 | コンポーネント |
|------|------------|------|
| 1 | `Prefab_InventoryTab` | `UiwInventoryTab` + `Button` |
| 2 | `Prefab_ItemSimple` | `UiwInventoryItemSimple` |
| 3 | `Prefab_ItemDetail` | `UiwInventoryItemDetail` |
| 4 | `Prefab_ItemOrderList` / `Prefab_ItemGridList` | `UiwInventoryItemOrderList` / `UiwInventoryItemGridList`（エントリプレハブを参照。Content に LayoutGroup なし） |
| 5 | `Prefab_FilterButton` | `Button`（テキスト子ノードを含む） |
| 6 | `Prefab_InventoryView` | `UiwInventoryView`（上記すべてのプレハブを参照） |

### ステップ 3：シーンへのアタッチ

```
Hierarchy
├── [InventoryManager]
│     └── InventoryRuntimeManager
│           databases: [GameDatabase.asset]
│
└── Canvas
      └── InventoryViewRoot          ← あらかじめ非表示（SetActive false）
            └── Prefab_InventoryView
                  tabPrefab:        Prefab_InventoryTab
                  tabContainer:     TabContainer
                  itemList:         （同じ階層の Prefab_ItemList インスタンスを指す）
                  currencyBar:      CurrencyContainer 上の UiwCurrencyBar
                  filterBar:        FilterContainer 上の UiwFilterTabBar
                  sortToolbar:      SortBar 上の UiwSortToolbar

  各ツールバーコンポーネント自体が子ノード参照と設定を配線：
    UiwCurrencyBar  → currencyContainer / currencyPrefab(Prefab_ItemSimple) / currencyItemIds:["gold_coin"]
    UiwFilterTabBar → filterContainer / filterButtonPrefab(Prefab_FilterButton)
    UiwSortToolbar  → sortDropdown / sortDirectionButton / sortDirectionLabel / autoSortButton
```

### ステップ 4：バックパックを開く

```csharp
using Ale.Inventory.Runtime.UI;
using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    [SerializeField] private UiwInventoryView inventoryView;

    // B キーでバックパックを開閉
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (inventoryView.gameObject.activeSelf)
                inventoryView.Close();
            else
                inventoryView.Open(new[] { "backpack" });
        }
    }
}
```

---

## 10. 他システムの画面と共通コンポーネント

第 2〜9 節はバックパック UI に焦点を当てました。ショップ、クラフト画面とその他の共通コンポーネントは構造が対称で、同じツールバーコンポーネントを再利用します。以下にクイックリファレンスを示します。挙動の詳細は各サブシステムのドキュメントを参照してください。

### 10.1 共通基底クラスと追加のツールコンポーネント

| コンポーネント | サブフォルダ | 説明 |
|------|------|------|
| `UiwViewBase` | `View/` | ビュー基底クラス：タイトル、開く / 閉じる切り替え（`Close` / `ToggleOpenClose`）、言語別の数字フォーマット解決。引数なしの `Open()` は**テンプレートメソッド**（共通ステップ `SetActive(true)` を含む）で、サブクラスがオーバーライドしてそれぞれの開く処理を実装。引数付き `Open(...)` オーバーロードは引数をキャッシュしてから引数なし `Open()` を呼ぶ。バックパック / 装備 / ショップビューは対象 ID（`inventoryIds` / `groupId` / `shopId`）を Inspector に公開し、既定を事前設定可能 |
| `UiwItemTooltip` | `Tool/` | グローバルに唯一のアイテムホバーポップアップ：`UiwInventoryItemDetail` を再利用して内容を表示、マウスに追従し画面内に制限 |
| `UiwNumberCounter` | `Tool/` | 数値カウンター：+/- ステップ + 長押し連発（任意の入力ボックス）、ショップ回数 / クラフト回数で再利用。イベント `OnValueChanged`、メソッド `Configure / SetRange / SetValue / SetInteractable` |
| `UiwFoldTab` | `Tab/` | 汎用折りたたみタブ：クリック可能なボタン + 左アイコン + 右テキスト、普通のタブまたは折りたたみ可能なグループタイトルとして使用 |

### 10.2 ショップ画面

`Runtime/UI/View/Shop/`：`UiwShopViewBase` + 種類別に分化した `UiwSellShopView`（販売）、`UiwRecycleShopView`（買い取り）、`UiwBarterShopView`（プレースホルダー）。

- `UiwCurrencyBar`（通貨バー）、`UiwFilterTabBar`（フィルタ）、`UiwShopGroupTab`（商品グループタブ）を再利用。
- 商品セルは `UiwShopItemDetail`（品質背景 + アイコン + 名称 / 単価 + 残り取引可能回数 + 数量選択）を使用。
- 商品リストは仮想スクロール `UiwShopCommodityList`（ScrollRect ルートにアタッチ、`cellPrefab` / `scrollRect` / `content` を接続。Content に LayoutGroup なし）を使用。**選択した取引回数はデータモデル `ShopCommodityEntry.times` に格納（セル上ではない）**：仮想スクロールは可視セルのみ保持するため、回数はデータモデルに格納する必要があり、ページ送り / スクロールで画面外に出ても失われない。カート合計と決済は**すべての商品データ**（`UiwShopViewBase.Entries`）で集計し、可視セルではない。
- 挙動（価格、取引、更新）は [ショップシステム](ShopSystem_JA.md) を参照。

### 10.3 クラフト画面

`Runtime/UI/View/Crafting/`：

| コンポーネント | 説明 |
|------|------|
| `UiwCraftingView` | メイン画面：ブループリントテンプレートタブ + 名称検索 + グループ折りたたみタブ（`UiwCraftingGroupFilter`）+ ソートバー + ブループリント仮想リスト + 詳細 |
| `UiwCraftingDetail` | ブループリント詳細：主 / 副産出、消費リスト、作成可能回数、作成回数選択（`UiwNumberCounter`）、作成 / 停止 + プログレスバー |
| `UiwCraftingBlueprintCell` | ブループリント一覧エントリ（主産出アイコン + 名称 + 属性表示行） |
| `UiwCraftingInputCell` | 消費アイテム行（アイコン / 名称 / 要求 / 保有） |
| `UiwCraftingBlueprintList` | ブループリント仮想スクロールリスト（`UiwInventoryOrderList` を継承、選択ハイライト + 選択イベントを追加サポート） |
| `UiwCraftingGroupFilter` | グループ折りたたみタブ（主グループから副グループを展開、`UiwFoldTab` を再利用） |

挙動（レシピ、クラフト倉庫、作成可能回数）は [クラフトシステム](CraftingSystem_JA.md) を参照。

### 10.4 装備画面

`Runtime/UI/View/Equipment/` + `Runtime/UI/Item/`：

| コンポーネント | 説明 |
|------|------|
| `UiwEquipmentView` | 装備メイン画面：装備グループパネル + 属性ボーナスパネル + 装備選択パネル。`Open(groupId)`（倉庫は装備グループの「装備倉庫」から取得）。装備スロットを右クリックで解除、左クリックで選択パネルを開く。`OnEquipmentChanged` を購読してリフレッシュ |
| `UiwEquipmentGroupPanel` | 装備グループパネル：装備グループ名 + すべてのスロットリストを表示。`displayMode` の 2 レイアウト：`Auto`（各スロットリストを自動インスタンス化）/ `Manual`（手動モード：階層でスロットリストのオブジェクトを自分で配置し、`manualSlotLists` でスロットリスト ID ごとにバインド、自由レイアウト）。カスタム Inspector はモードに応じて対応フィールドのみ表示。`groupId` + `bindOnStart` を設定して単独使用可 |
| `UiwEquipmentSlotList` | スロットリスト：名称 + すべての装備スロット。`displayMode` の 2 レイアウト：`Auto`（HorizontalLayoutGroup 下に各装備スロットをインスタンス化）/ `Manual`（手動モード：階層で装備スロットのオブジェクトを自分で配置し、`manualSlots` でスロット ID ごとにバインド）。カスタム Inspector はモードに応じて対応フィールドのみ表示 |
| `UiwEquipmentSlot` | 装備スロット（`UiwInventoryItemSlotBase` を継承）：装備中アイテムを表示。左 / 右クリックイベント。ドラッグソース（ドラッグアウトで交換）+ ドロップ対象（装備 / 交換）+ 緑/赤の有効性オーバーレイ（`selectedIndicator` / `validityOverlay` 任意） |
| `UiwEquipmentBonusPanel` / `UiwEquipmentBonusEntry` | 属性ボーナスパネル：グループタグごとに合計属性ボーナスを表示 |
| `UiwEquipmentSelectPanel` | 装備選択パネル：切り替えバー（左右 + 名称 + N/M）+ 現在のスロットリスト + 装備可能アイテムリスト + 退出（ボタン / 空白部の右クリック） |
| `UiwEquipmentCandidateList` | 装備可能アイテムリスト（仮想スクロールグリッド、`UiwInventoryGridList` を継承）：装備グループの「装備倉庫」をまたいで現在のスロットリストの制限でフィルタ（各セルがソース倉庫を記録）。**右クリックでクイック装備 / 左ドラッグで装備スロットへ装備**。候補セルは `UiwInventoryItemCell` + `GridCellDragHandler` を再利用（整理ドラッグは受けないので handler が装備ドラッグを駆動） |
| `GridCellDragHandler` | アイテムセルのドラッグ中継コンポーネント（`UiwInventoryItemCell` またはその子に付与）：**グリッドリストに接続済み**（バックパック）のときは `UiwInventoryItemGridList` へ転送、ドラッグ終了時に落下点で判定（装備スロット→装備、アイテムセル→入れ替え）。**グリッドリストに未接続**（候補リスト）のときは「装備スロットへドラッグして装備」を駆動（`UiwEquipmentDragContext` 経由）。右クリッククイック装備は本コンポーネントでは処理しない（`UiwInventoryItemCell` がブロードキャスト → `UiwEquipmentView` が購読） |
| `UiwEquipmentDragContext` | 装備ドラッグのグローバルコンテキスト（ペイロード + カーソル追従のゴースト + ソースアイコンの復帰）。非 MonoBehaviour の静的クラス |
| `UiwInventoryItemEvents` | 汎用の静的イベントバス：バックパック / 明細アイテムセルが (倉庫ID, アイテムID) の右クリックをブロードキャストし、装備画面が開いているとき `UiwEquipmentView` が購読して自動装備（装備概念と分離、グリッドセルと順序 / 明細行の両方に対応） |

> プレハブのヒント：装備スロットのプレハブに既定で無効の「有効性オーバーレイ画像」（`validityOverlay` に接続）を付けて初めて緑/赤を表示。選択パネルのルートノードには「右クリックで空白退出」のために raycast グラフィックが必要。ドラッグにはシーン内に EventSystem が必要。バックパックグリッドセルのドラッグ装備にはそのバックパックの `dragSort=true` が必要（順序リストはドラッグ非対応、右クリックを使う）。

挙動（装備 / 解除 / 交換、アイテム制限、属性ボーナス、セーブ）は [装備システム](EquipmentSystem_JA.md) を参照。

### 10.5 すべてのプレハブをワンクリック生成（Demo ウィザード）

**ウェルカムウィンドウ**（`Tools > Inventory System > Welcome Window`）を開く → 「テストツール-プレハブ生成」を展開：

- 「すべて生成（データベース + 全 Prefab）」でサンプルデータベース + 全 UI プレハブ + バックパック / ショップ / クラフト / 装備パネル + マネージャーをワンクリック生成（装備パネルは「キャラクター装備」を自動で開き、バックパック右クリック装備のブリッジをアタッチ）。
- 一覧では個別のプレハブを生成可能（依存するプレハブを生成する際は子プレハブも一緒に生成するか確認し、既存アセットの上書き前にも確認）。

> ウェルカムウィンドウの「プラグインサポート」領域では、`IS_TMP` / `IS_LOCALIZATION` / `IS_ADDRESSABLE` の 3 マクロをワンクリックで切り替え、ウィザードが Prefab 生成時に使う既定の TMP フォントも設定できます。

---

## 11. よくある質問

**Q：バックパックを開いたらリストが空？**  
A：`InventoryRuntimeManager` がシーンにあり `databases` が割り当て済みか確認。`InventoryRuntimeManager.Awake` が `Open` より前に実行されるか確認（スクリプト実行順）。

**Q：アイコンが表示されない？**  
A：`iconAttrId` がデータベースのアイテムのアイコン属性フィールド ID と完全一致するか確認（大文字小文字を区別）。属性フィールド型が `Sprite` で、Sprite が Inspector で割り当て済みか確認。

**Q：品質背景が誤表示 / 空白？**  
A：`qualitySprites` 配列の**インデックス**は列挙整数値に対応する必要があります。品質列挙値が 0〜6 なら配列は少なくとも 7 要素必要（一部は空でも可）。

**Q：ソートドロップダウンに選択肢が出ない？**  
A：倉庫定義（`Inventory`）の `sortPriorities` リストが空です。倉庫 Inspector で少なくとも 1 つのソートルールを追加してください。

**Q：`IS_TMP` マクロ切り替え後、プレハブのテキスト参照が失われた？**  
A：マクロ切り替えでフィールド型が変わるため、プレハブ Inspector で `label`、`nameText` などのフィールドに `TMP_Text` コンポーネントを手動で再ドラッグする必要があります。テキスト方針を決めたらマクロを固定し、頻繁に切り替えないことを推奨します。

**Q：仮想リストのスクロール時に空白セルが出る？**  
A：`bufferCount` を増やす（1〜2 推奨）。または Content に `LayoutGroup` / `ContentSizeFitter` が付いていないか確認（仮想スクロールは手動配置、行の高さはプレハブの `RectTransform` から自動測定、手入力不要）。高速スクロール時の一時的な空白露出は `spawnPerSecond` レート制限による可能性もあります（次項参照）。

**Q：開く / ページ切替 / 高速スクロール時にセルが「1 つずつフェードイン」して瞬時に埋まらない？**  
A：これは `spawnPerSecond`（既定 `30`）レート制限の効果です —— セルのインスタンス化とバインド（アイコン非同期読み込み含む）を複数フレームに分散し、単一フレームで一斉に大量のセルを生成 / 読み込みしてカクつきやアセット読み込みの詰まりを起こすのを回避します。より速く埋めたい場合はこの値を大きく、`0`（または負）にすると制限なしで 1 フレームで埋まります（旧挙動）。フェードインの順序は**スクロール方向に追従**（ビューポートに入る順）：末尾へスクロールは前から後（縦「上から下」）、起点へスクロールは後から前（縦「下から上」）。注意：数量増減による全表更新もこの速度でセルごとにフェードインします（ドラッグ入れ替え / スタックなど「変化セルのみ再バインド」の差分リフレッシュはレート制限を受けず、即時完了）。

**Q：ユーザーがあるアイテムセルをクリックしたときに応答するには？**  
A：`UiwInventoryItemDetail` コンポーネントに `Button` コンポーネントを追加してイベントをバインドするか、`SetSlot` 内で `EventSystem` 方式で実装します。`UiwInventoryItemDetail` を継承し、サブクラスで `OnPointerEnter`/`OnPointerExit` をオーバーライドするか `IPointerClickHandler` を追加することもできます。
