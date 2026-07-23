# 倉庫システム（Warehouse System）

<p align="center">
  🌍
  <a href="./WarehouseSystem.md">中文</a> |
  <a href="./WarehouseSystem_EN.md">English</a> |
  日本語
</p>

- [説明ドキュメント](../README_JA.md) に戻る

倉庫システムは「コンテナ」を定義します：容量、重量上限、格納 / 取り出し / 操作の機能タグ制限、フィルタタグ、整理ソートルール。ランタイムでは `InventoryRuntimeManager` が各倉庫のスロットリストを管理し、追加/削除 / クエリ / 整理 / セーブの API を提供します。バックパック、装備バー、ショップの棚、クラフト材料庫はいずれも倉庫で支えられます。

# 📜 目次

- [倉庫システム（Warehouse System）](#倉庫システムwarehouse-system)
- [📜 目次](#-目次)
- [タブ構成](#タブ構成)
- [倉庫テンプレート（左カラム）](#倉庫テンプレート左カラム)
- [倉庫一覧（中央カラム）](#倉庫一覧中央カラム)
- [倉庫 Inspector（右カラム）](#倉庫-inspector右カラム)
- [整理ソート](#整理ソート)
- [ランタイムのセットアップ](#ランタイムのセットアップ)
    - [カバー UI の設定（ポップアップ / ゴーストアイコン）](#カバー-ui-の設定ポップアップ--ゴーストアイコン)
    - [エディタのテストアイテム投入（Play で自動投入）](#エディタのテストアイテム投入play-で自動投入)
- [ランタイム API](#ランタイム-api)
- [RuntimeItemSlot 構造](#runtimeitemslot-構造)
- [セーブとロード](#セーブとロード)
- [データソースと読み込み](#データソースと読み込み)
- [バックパック UI](#バックパック-ui)

# タブ構成

Inventory Editor 上部の「**倉庫システム**」タブをクリックします。3 カラムレイアウト：

```
左カラム：倉庫テンプレート（一覧 + 編集パネル）
中央カラム：倉庫一覧（テンプレートフィルタタグ + 検索 + ドラッグ並べ替え）
右カラム：選択した倉庫の Inspector
```

# 倉庫テンプレート（左カラム）

倉庫テンプレートは倉庫の既定設定を定義し、テンプレートから作成された倉庫はこれらの設定を継承します（倉庫レベルで上書き可能）。

| フィールド | 説明 |
|------|------|
| 名称 | テンプレートの一意な名称 |
| 色 | 倉庫一覧の色ドット |
| 容量上限 | スロット数の上限（0 = 無制限） |
| 重量上限 | 総重量の上限（0 = 無制限） |
| **格納機能タグ** | これらのタグを持つアイテムのみ格納可能。空 = 制限なし |
| **取り出し機能タグ** | これらのタグを持つアイテムのみ取り出し可能。空 = 制限なし |
| **操作機能タグ** | 倉庫内アイテムに対して実行できる操作を制限。空 = 制限なし |
| **フィルタタグ** | 倉庫 UI の機能タグフィルタボタン（「すべて」は常に存在し設定不要） |
| 自動整理 | チェックするとアイテム変化のたびにソートルールで自動整理 |
| ドラッグソート | チェックするとプレイヤーが UI 内でドラッグしてアイテム順序を調整可能 |
| **整理リスト** | 主ソートルール。各項目はソートフィールド + 昇順/降順を選択 |
| **整理優先度** | 副ソートルール。主ソート値が同じとき順に比較 |
| 属性フィールドリスト | 倉庫のカスタム属性フィールド（メモ、区画説明など） |

# 倉庫一覧（中央カラム）

```
[ すべて ][ バックパック ][ ショップ ][ 装備バー ]  ← 倉庫テンプレートフィルタタグバー
[ 🔍 検索ボックス ][ テンプレートから追加 ▾ ][ クイック追加 ]
─────────────────────────────────────
≡ ● テンプレート名 | ID | 容量 | 重量 | 格納 | 取り出し | 操作  ✕
```

| 操作 | 説明 |
|------|------|
| テンプレートフィルタタグバー | 倉庫テンプレートでフィルタ |
| 検索ボックス | 倉庫 ID / テンプレート名でフィルタ |
| テンプレートから追加 | テンプレート設定を継承した新しい倉庫を作成（ID は自動で `inv_N`） |
| クイック追加 | 最後の倉庫を複製 |
| ドラッグ ≡ ハンドル | データベース内での倉庫の順序を調整 |
| 行をクリック | 選択。右カラムに Inspector を表示 |

# 倉庫 Inspector（右カラム）

| フィールド | 説明 |
|------|------|
| ID | 一意な識別子。空または重複のときハイライト |
| ソーステンプレート | 読み取り専用 |
| 容量上限 / 重量上限 | テンプレート値を上書き（0 = 無制限） |
| 格納 / 取り出し / 操作 / フィルタ機能タグ | 複数選択のチェックボックス。テンプレート値を上書き |
| 自動整理 / ドラッグソート | Toggle |
| 整理リスト / 整理優先度 | ドラッグで並べ替え可能なソートルールのリスト |
| 属性フィールド値 | 倉庫テンプレート定義由来のカスタム属性値 |

# 整理ソート

ソートは主ソート「整理リスト」と副ソート「整理優先度」の 2 段階で構成されます。主ソート値が同じとき、副ソートを順に比較して先後が決まるまで続けます。

選択可能なソートフィールド：

| フィールド | 意味 |
|------|------|
| アイテム ID（`__id__`） | アイテム ID でソート |
| タグ順（`__tagOrder__`） | アイテムの最初の機能タグがタグリスト内で持つ順序でソート |
| 任意のカスタム属性フィールド | その属性値でソート |

**カスタム属性は型に応じて異なる比較ルールを用います**（[属性システム - ソート比較数値](AttributeSystem_JA.md#ソート比較数値tocomparablenumber) を参照）：

- Int / Float / Bool / Enum → 数値を直接比較；
- Vector2〜4 / Color / VectorInt2〜4 → 大きさ（magnitude）を比較；
- StringIntPair → その中の Int 値のみ比較；
- String → 特別処理：まず長さ、次に辞書順。

内部実装：`InventorySortService.CompareSlots` → `CompareByField` → `GetAttrNumeric`（→ `AttributeValue.ToComparableNumber()`）。
（1.6.0 より `InventoryRuntimeManager` 上の同名 `public static` 互換フォワードは削除されました。`InventorySortService` を直接呼んでください。）

> 各「整理オプション」は 2 つの組み込みフィールドを持ちます：**名称**（`displayName`、Text：プレーンテキストのフォールバック + 任意のローカライズ参照、ソートドロップダウンの表示名として読み取り）と**無視 ID**（`ignoreIds`、ソート時にスキップするエントリ ID のリスト、既定 0 件、ドラッグで増減可能。意味はフィールドに依存 —— アイテム ID ソート = アイテム ID、機能タブ = タグ名、属性ソート = 属性値）。「倉庫システム → 整理オプション」サブタブで編集し、ランタイムでは `SortOption.ResolveDisplayName` / `SortOption.EffectiveIgnoreIds` で読み取ります。旧バージョンではこの 2 項目を汎用「属性フィールド定義」の値として保存しており、そのパネルを初めて開くと自動的に組み込みフィールドへ移行します。

# ランタイムのセットアップ

シーンに空の GameObject を作成し、`InventoryRuntimeManager` をアタッチして、`.asset` を `databases` 配列にドラッグします。起動時に自動でデータベースを `InventoryDataManager` に登録し、定義済みの各倉庫に空のランタイム状態を作成します。

```
Hierarchy
└── [InventoryManager]
      └── InventoryRuntimeManager
            databases: [GameDatabase.asset]
```

### カバー UI の設定（ポップアップ / ゴーストアイコン）

`InventoryRuntimeManager` の「UI 設定」領域では、**カバー UI**（ホバーポップアップ、ドロップダウンポップアップ、ドラッグのゴーストアイコンなど、すべての UI の上に被せる必要があるもの）を設定できます：

- **ルートノード**（`coverUiRoot`）：カバー UI の親ノード。空の場合はランタイムで自動的にシーンの最初の Canvas を使います。
- **強制 Layer**（`applyCoverUiLayer` + `[Layer] coverUiLayer`）：有効にすると、カバー UI はインスタンス化後に指定 Layer（例：`UI`）へ再帰的に設定されます。「独立した UI カメラで、Culling Mask が UI 層のみをレンダリング」というシーンに適合します —— これらの UI はそれぞれ独立した Canvas を割り当てられて親の Layer を断ち切るため、UI カメラがレンダリングできるよう再指定が必要です。コードでは `SetCoverUiLayer(int)` / `SetCoverUiLayer(string)` / `DisableCoverUiLayer()` で調整でき、自作のカバー UI には `ApplyCoverUiLayer(GameObject)` で手動適用します。

### エディタのテストアイテム投入（Play で自動投入）

`InventoryRuntimeManager` の「テスト機能」領域では、Play モードに入るとき（`Init()`、Awake のタイミング）に自動でテスト倉庫へアイテムを投入できます。**データの投入のみで、いかなる画面も開きません**（画面は各ビューが自ら開きます）：

- **`autoPopulateOnStart`**（マスター切り替え）：自動投入するかどうか。
- **`testInventoryId`**：対象倉庫 ID（データベース内の `Inventory.id` と一致する必要あり）。
- **`testItems`**：「アイテム ID + 数量」を 1 件ずつ指定するリスト。
- **`addAllConfiguredItems` + `addAllItemCount`**：さらに、すべてのデータベース（`databases`）で設定されたアイテムを、それぞれ `addAllItemCount` の数量でテスト倉庫に補充します。`testItems` で既に設定済みのアイテムはスキップされ（指定数量を保持し、重複して追加しない）、同一アイテム ID は複数の DB をまたいでも 1 回だけ追加されます。これもマスター切り替え `autoPopulateOnStart` に従います。

> この領域は Demo ウィザード（`Tools > Inventory System`）が例の値を自動で書き込めます。

# ランタイム API

```csharp
using Ale.Inventory.Runtime;

var rm = InventoryRuntimeManager.Instance;

// 追加（false を返す = 容量/重量/タグ制限を満たさない）
bool ok = rm.TryAddItem("backpack", "potion_hp", 3);

// クエリ
int  total = rm.GetTotalCount("backpack", "potion_hp");   // スロットをまたいで累計
bool has   = rm.HasItem("backpack", "potion_hp", 1);
int  free  = rm.GetFreeSpaceFor("backpack", "potion_hp"); // あといくつ入れられるか
float w    = rm.GetTotalWeight("backpack");
float wMax = rm.GetWeightLimit("backpack");

// スロットリストを取得（順序 = UI 表示順）
// 注意：戻り値は読み取り専用です — ヒット時はランタイム状態への実参照、
// ミス時（存在しない倉庫 ID）はグローバル共有の空リストです。ソート / フィルタする場合は先にコピーしてください。
List<RuntimeItemSlot> slots = rm.GetSlots("backpack");

// 削除：スロット ID で（正確） / アイテム ID で（スロットをまたいで累減）
rm.TryRemoveItem("backpack", slots[0].slotId, 1);
rm.TryRemoveItemById("backpack", "potion_hp", 2);

// 2 つのスロットの内容を交換（ドラッグソート）
rm.SwapSlotContents("backpack", slotA, slotB);

// ドラッグの落下点（同じアイテムは優先してスタック、入り切らない / 別アイテム / 空スロットなら交換）—— UI グリッドのドラッグ整理で使用
rm.StackOrSwapSlots("backpack", srcSlot, targetSlot);

// 整理ソート（倉庫定義のルールでランタイム状態に書き込む）
rm.SortInventory("backpack");

// 倉庫変化をリッスン（UI はこれに従ってリフレッシュ）
rm.OnInventoryChanged += id => RefreshUI(id);
```

> メソッド名は `GetTotalCount`（`GetTotalQuantity` ではない）である点に注意してください。

# RuntimeItemSlot 構造

| フィールド | 型 | 説明 |
|------|------|------|
| `slotId` | string | スロットの一意な ID（`Guid.NewGuid()`） |
| `itemId` | string | アイテム ID |
| `quantity` | int | 現在のスロット内の数量 |

# セーブとロード

`InventoryRuntimeManager` はゲームのセーブシステムと連携するためのインターフェースを提供します：

```csharp
// 保存：すべての倉庫状態のディープコピーを取得（シリアライズ可能）
List<RuntimeInventoryState> save = InventoryRuntimeManager.Instance.GetSaveData();
string json = JsonUtility.ToJson(new SaveWrapper { inventories = save });

// 読み込み：デシリアライズ後、Init 完了後に呼ぶ
var wrapper = JsonUtility.FromJson<SaveWrapper>(json);
InventoryRuntimeManager.Instance.LoadSaveData(wrapper.inventories);
```

`RuntimeInventoryState` = `inventoryId` + `slots`（順序付きスロットリスト）。データベースにあってセーブにない倉庫は空の状態を保ちます。セーブに余分にある倉庫 ID は無視されます。

# データソースと読み込み

`InventoryDataManager` は 3 種類のデータソースに対応します（エディタでエクスポート → ランタイムで読み込み）：

| ソース | 用途 |
|------|------|
| `.asset`（ScriptableObject） | `InventoryRuntimeManager.databases` に直接ドラッグ。開発期は最も簡単 |
| JSON | 可読テキスト。オブジェクト参照は AssetGUID として保持、デバッグ向き |
| バイナリ | コンパクトで効率的、正式リリース向き |

エクスポートはツールバーの「JSON エクスポート / バイナリエクスポート」で行います。ランタイムでは `InventoryDataManager` に読み込んでから登録できます。シリアライズとアセット解決の詳細は [アーキテクチャ - データフロー](Architecture_JA.md#データフロー) を参照してください。

# バックパック UI

`UiwInventoryView`（`Runtime/UI/View/Inventory/`）はバックパックのメイン画面コントローラーで、次を組み合わせます：複数倉庫のタブ、通貨バー、仮想スクロールリスト、フィルタタブバー、ソート整理バー。

```csharp
using Ale.Inventory.Runtime.UI;

inventoryView.Open(new[] { "backpack", "stash" });  // これらの倉庫を開いて表示
inventoryView.Close();
```

> `_inventoryIds` は `UiwInventoryView` の Inspector で、既定で表示する倉庫リストとしてあらかじめ設定できます：引数なしの `Open()` はその値で開き、`Open(inventoryIds)` は上書きしてから開きます —— 設定後、ビューは `Open(...)` または Inspector で変更されるまで常にその値を使います。

プレハブ作成、各コンポーネントの Inspector パラメータ、仮想リストの設定は [UI コンポーネントガイド](UIComponentGuide_JA.md) を参照してください。
