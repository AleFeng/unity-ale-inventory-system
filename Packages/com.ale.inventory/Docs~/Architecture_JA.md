# アーキテクチャ

<p align="center">
  🌍
  <a href="./Architecture.md">中文</a> |
  <a href="./Architecture_EN.md">English</a> |
  日本語
</p>

- [説明ドキュメント](../README_JA.md) に戻る

## 設計目標

エディタは常に、そして ScriptableObject 上でのみ動作します。JSON / バイナリは一方向のエクスポートフォーマット（ランタイム / ビルドが消費）としてのみ使われます。したがってエディタ側はシリアライズのラウンドトリップ互換を考える必要がなく、Undo/Redo も SO にのみ作用します。

---

## コアデータモデル

### 柔軟な属性システム

`[SerializeReference]` ではなく、**タグ付き共用体（tagged-union / バリアント）+ 配列の組み合わせ**を採用します。

`AttributeValue`（`Runtime/Data/AttributeValue.cs`）が保持するもの：

- `EFieldType _type`：現在の型（全 22 種、下記参照）；
- `bool _isArray`：スカラー / 配列；
- `string _enumTypeRef`：列挙型名（Enum のみ）；
- 型別に分類された 5 つの後備リスト：`List<int>` / `List<float>` / `List<string>` / `List<Object>` / `List<AnimationCurve>`。

**後備リストの規約**：

| 型グループ | 後備リスト | ステップ |
|--------|---------|------|
| Bool / Enum | `ints` | 1 |
| Int | `ints` | 1 |
| VectorInt2 / 3 / 4 | `ints` | 2 / 3 / 4 |
| Float | `floats` | 1 |
| Vector2 / 3 / 4 | `floats` | 2 / 3 / 4 |
| Color | `floats` | 4 |
| String | `strings` | 1 |
| Text | `strings` | 3（プレーンテキスト + テーブル参照 + エントリキー） |
| Sprite / Prefab / Texture / Material / AudioClip / AnimationClip / PhysicsMaterial / PhysicsMaterial2D | `objRefs` | 1 |
| AnimationCurve | `curves` | 1 |

スカラーは要素 `[0]` を格納、配列は `[0..n]` を格納（ステップ > 1 のとき平坦化して格納）。

オブジェクト系フィールドには `objRefs` と並行する文字列アドレスリスト `_objAddresses`（Addressable アドレス / AssetReference 授権 GUID を保持）があります：`IS_ADDRESSABLE` 有効時、エディタはネイティブの AssetReference セレクターで授権し（GUID のみ格納、objRefs スロットは空、データベース読み込みでリソースも一緒に載せない）、ランタイムでは `InventoryAssets` ファサードを通じて「実時参照を優先、なければアドレスの非同期読み込み」、ホスト破棄時に自動アンロードします。詳細は [属性システム - リソースフィールドの読み込み](AttributeSystem_JA.md#リソースフィールドの読み込みaddressables) を参照してください。

**なぜ `[SerializeReference]` を使わないか**：タグ付き共用体は SO でネイティブにシリアライズされ、Undo/Redo をネイティブに対応し、多態的な PropertyDrawer が不要です。`[SerializeReference]` にはマネージド参照/Undo の破損リスクがあります。コスト（値ごとにいくつかの空リスト）は設定期のデータでは無視できます。

### 列挙の安定した参照

`EnumType`（`Runtime/Data/EnumType.cs`）は単調増加する `nextValue` を維持します。`AddItem` 時に割り当てて自増し、削除しても回収しません。属性値が格納するのは**列挙値（int）**でありインデックスではないため、列挙項目を並べ替えても既存の参照は壊れません。

列挙項目（`EnumItem`）自身も一組の `AttributeDefinition` カスタム属性フィールドを持て、列挙項目の付加データ（各職業の基礎属性ボーナスなど）を記述するために使えます。

### アイテムと倉庫

```
InventoryDatabase (ScriptableObject)
├── List<EnumType>                  列挙型定義
├── List<FunctionTag>               機能タグ定義（属性フィールドを含む）
├── List<ItemTemplate>              アイテムテンプレート（属性フィールド + 固定された機能タグ参照を含む）
├── List<Item>                      アイテムエントリ（ソーステンプレート参照 + 自選タグ参照 + 属性値を含む）
├── List<InventoryTemplate>         倉庫テンプレート（設定パラメータ + 属性フィールドを含む）
├── List<Inventory>                 倉庫エントリ（ソーステンプレート参照 + 設定パラメータの上書き + 属性値を含む）
├── List<AttributeDefinition>       整理オプションの任意の追加属性フィールド（スキーマ。名称/無視ID は SortOption に組み込み済み、これは既定で空）
├── List<SortOption>                整理オプション（RebuildSortOptions が自動生成。組み込み displayName(Text) + ignoreIds を含む）
├── List<NumberFormatConfig>        数字フォーマット設定（名前で参照）
├── List<ShopTemplate> / List<Shop> ショップテンプレート / ショップ（商品グループ、商品、更新スケジュールを含む）
├── クラフト：List<CraftingGroupTag> / List<CraftingBlueprintTemplate> / List<CraftingBlueprint>
└── 装備：List<EquipmentGroupTag> / List<EquipmentGroupTemplate> / List<EquipmentGroup>
                                   （装備グループは スロットリスト → 装備スロット + アイテム制限 + 装備属性フィールド を含む）
```

`Item.RebuildAttributes(db)` は優先度（テンプレート固有 → テンプレート固定タグ → アイテム自選タグ）で期待フィールドを収集し、`values` リストを増減/並べ替えて、ソース定義と同期を保ちます（冪等、Undo 履歴に影響しない）。

`Inventory` / `Shop` / `CraftingBlueprint` / `EquipmentGroup` の `RebuildAttributes(db)` はアイテムと同様で、それぞれ各自のテンプレート（倉庫テンプレート / ショップテンプレート / ブループリントテンプレート / 装備グループテンプレート）からのみ属性フィールドを収集します。`SortOption` は `InventoryDatabase.RebuildSortOptions` がすべての倉庫テンプレートのソートフィールドから自動同期します。

共有基盤 3 点（1.6.0 で集約。以前は各エンティティが個別に持っていました）：

| 基盤 | 役割 |
|------|------|
| `AttributeOwner` | 属性値の集合を持つオブジェクトの基底クラス：遅延構築の O(1) 辞書キャッシュ + `GetEntry` / `GetAttributeValue<T>` / `SetAttributeValue<T>`。`Item` / `EnumItem` / `Inventory` / `Shop` / `SortOption` / `CraftingBlueprint` / `EquipmentGroup` / `Skill` が継承。**エントリリストを変更したら `InvalidateEntryCache()` を呼ぶこと** |
| `AttributeSync.Sync` | 各 `RebuildAttributes` 内の「スキーマに従って不足を追加 / 孤立を削除 / 型ドリフト時にリセット」の共通実装 |
| `ConfigTemplateBase` | 6 種のテンプレートが共有する 名称 / カラードット / 属性フィールド定義（`ItemTemplate` / `InventoryTemplate` / `ShopTemplate` / `CraftingBlueprintTemplate` / `EquipmentGroupTemplate` / `SkillTemplate`）。グループタグも同様に `GroupTag` を共有 |

> 装備グループと装備グループテンプレートは `IEquipmentConfig`（装備倉庫 + スロットリスト + 装備属性フィールド）を共有し、テンプレートがすべての設定可能項目を保持します。テンプレートから装備グループを作成するときこれらの設定をディープコピーし、以降は装備グループが独立して編集可能です（クラフトシステムの「テンプレートレベル読み取り専用」の逆）。装備倉庫リストは装備システム / UI が対話できる倉庫を指定し、解除時は Index0 から最初に入れられる倉庫を探します。

---

## データフロー

```
InventoryDatabase (SO)  ──編集──▶  依然として SO
        │
        ├─ エクスポート ─▶ InventoryDtoMapper.ToDto ─▶ JsonUtility / BinaryWriter ─▶ .json / .bytes
        │                                   （Sprite などの参照は EditorAssetGuidResolver が GUID に変換）
        │
        └─ ランタイム読み込み ◀─ InventoryJson/BinarySerializer.Import ◀─ .json / .bytes
                       （NullAssetRefResolver：オブジェクト参照は空のまま。
                         Addressable モード：AddressableAssetRefResolver がアドレスで非同期読み込み）
```

DTO 層はデータモデルと 1 対 1 でミラーする平坦な構造で、唯一の違いはオブジェクト参照を GUID 文字列で保持する点です。
`InventoryDtoModels.cs` には DTO 定義のみを置き、双方向マッピングは `InventoryDtoMapper*.cs` にシステム別の partial として、バイナリブロックの読み書きも同様に `InventoryBinarySerializer*.cs` に分割されています。

**フォーマットバージョン**（`InventoryDtoMapper.Version`）：v5 から属性値が `curveData`（AnimationCurve）を持ち、**v6 からエクスポートがデータベースの全 20 リストをカバー**（倉庫 / 整理オプション / 数値フォーマット / ショップ / クラフト / 装備 / スキルを追加）し、アイテムシステムでこれまで黙って捨てられていたフィールド（テンプレートのカラードット、`weight` / `stackLimit` / `hideInInventory`、機能タグの UI 表示設定）も補完されました。バイナリ読み込みはヘッダのバージョンに応じて新規ブロックをスキップするため、v5 でエクスポートした `.bytes` も引き続きインポートできます。

---

## エディタ構造

```
InventoryEditorWindow          メインウィンドウ + IInventoryEditorContext 実装（上部システムタブ + エクスポートボタン）
├── ItemSystemTab              アイテムシステムタブ
│   ├── EnumTypePanel          列挙型一覧 + 編集パネル
│   ├── FunctionTagPanel       機能タグ一覧 + 編集パネル
│   ├── ItemTemplatePanel      アイテムテンプレート一覧 + 編集パネル
│   ├── ItemListPanel          アイテム一覧（テンプレートフィルタタグ + 検索 + ドラッグ並べ替え）
│   └── ItemInspectorPanel     アイテム Inspector（グループ化属性 + 機能タグ）
├── InventorySystemTab         倉庫システムタブ
│   ├── InventoryTemplatePanel 倉庫テンプレート一覧 + 編集パネル
│   ├── InventoryListPanel     倉庫一覧 + InventoryInspectorPanel
│   └── （他に SortOptionPanel / NumberFormatConfigPanel）
├── ShopSystemTab             ショップシステムタブ
│   ├── ShopTemplatePanel      ショップテンプレート一覧 + 編集パネル
│   ├── ShopListPanel          ショップ一覧 + ShopInspectorPanel（商品グループ / 商品）
├── CraftingSystemTab         クラフトシステムタブ
│   ├── CraftingGroupTagPanel  グループタグ一覧 + 編集パネル
│   ├── CraftingTemplatePanel  ブループリントテンプレート一覧 + 編集パネル
│   └── CraftingListPanel      ブループリント一覧 + CraftingInspectorPanel
├── EquipmentSystemTab        装備システムタブ
│   ├── EquipmentGroupTagPanel グループタグ一覧 + 編集パネル
│   ├── EquipmentTemplatePanel 装備グループテンプレート一覧 + 編集パネル（名称/色 + 共有設定 + カスタム属性フィールド）
│   └── EquipmentListPanel     装備グループ一覧 + EquipmentInspectorPanel（ネストしたスロットリスト / 装備スロット / アイテム制限 / 属性フィールド）
└── SkillSystemTab            スキルシステムタブ
    ├── SkillGroupTagPanel     グループタグ一覧 + 編集パネル
    ├── SkillTemplatePanel     スキルテンプレート一覧 + 編集パネル（スキル既定情報 + カスタム属性フィールド）
    └── SkillListPanel         スキル一覧 + SkillInspectorPanel（名称 / 説明 / アイコン / グループタグ / カスタム属性値）
```

> 装備システムの「スロットリスト + 装備属性フィールド」は `Editor/Common/EquipmentConfigDrawer` が統一的に描画します（装備グループ Inspector と装備グループテンプレート Inspector で再利用）。ネストしたサブリストのドラッグ並べ替えは、パスをキーとする `Dictionary<string, EditorReorderableDrag>` で分離します。

### 汎用ドロワー

| クラス | 責務 |
|----|------|
| `AttributeFieldDrawer` | `EFieldType` に応じて単一の `AttributeValue` を描画。GUILayout パスと Rect パスは**型ディスパッチの実装を共有**します（1.6.0 以前は並行して保守される 2 つの大きな switch でした） |
| `AttributeDefinitionDrawer` | `AttributeDefinition` の完全な編集パネルを描画（Rect ベース、ReorderableList 用） |
| `AttributeDefinitionListDrawer` | ドラッグ並べ替え付きの属性フィールド定義リスト（内部で `ReorderableList` を使用、drawElementCallback は全 Rect ベース） |
| `EditorEntityListPanel<TEntity,TTemplate>` | **中央カラムのエンティティ一覧のジェネリック基底**：テンプレートフィルタタブ + 検索バー +「テンプレートから追加」/「クイック追加」+ 2 行構成の項目行（ドラッグハンドル / テンプレートのカラードット / 各列 / 削除ボタン）+ ドラッグ並べ替え + 遅延削除 + 上下キーナビゲーション。6 システムの中央カラムはすべてこれを継承し、各自は `DrawRowColumns`（列レイアウト）と追加 / 検索ルールのみを実装します |
| `EquipmentConfigDrawer` | 装備の「スロットリスト + 装備属性フィールド」の共有描画（装備グループ Inspector と装備グループテンプレート Inspector で再利用） |

**Rect ベースの原則**：`ReorderableList.drawElementCallback` 内で `GUILayout.BeginArea / GUI.BeginGroup` を呼ぶことは厳禁です。さもないと Layout と Repaint の GUILayout スロット数が一致しないとき「Getting control X's position...」例外が投げられます。すべての `DrawRect` メソッドは `EditorGUI.*` のみを使います。

### 統一された変更フロー

すべての編集操作は次に従います：`ctx.RecordUndo(説明) → データを変更 → ctx.MarkDirty()`。

ID 重複検出は `DuplicateIdChecker` が Layout フェーズで再計算・キャッシュ（`HashSet<string>`）し、コストは極めて低い（Layout イベントごとに O(n)）です。

---

## ランタイムアーキテクチャ

```
InventoryRuntimeManager (MonoBehaviour シングルトン)
├── InventoryDatabase[]    ─登録→  InventoryDataManager（静的定義クエリ）
└── Dictionary<inventoryId, RuntimeInventoryState>  ─管理→  各倉庫のランタイムスロットリスト
```

`InventoryRuntimeManager` は責務ごとに partial ファイルへ分割されています：コア（フィールド / 初期化 / データ取得 /
アイテム管理 / 整理の入口 / セーブ）、`.Time`（時間ゲッター）、`.UiHost`（カバー UI とホバーポップアップのホスト）、
`.TestSeed`（テストアイテム投入。エディタ / 開発ビルドのみ）。インスタンス状態に依存しないソート実装は
`InventorySortService` として独立しています。

`InventoryRuntimeManager` の責務：
- 初期化時にデータベースを `InventoryDataManager` に登録；
- 定義済みの各倉庫に空の `RuntimeInventoryState`（スロットリスト）を作成；
- ランタイム操作 `TryAddItem / TryRemoveItem / TryRemoveItemById / SortInventory` を提供；
- `GetSaveData / LoadSaveData / ResetAll` インターフェースでゲームのセーブシステムと連携（契約は下記「セーブ契約」）；
- `OnInventoryChanged(inventoryId)` イベントを発行、UI 層が購読してリフレッシュ。

`InventoryDataManager` の責務（純粋なデータクエリ、状態なし）：
- `InventoryDatabase` を登録（複数対応、統合クエリ）；
- ID で `Item / Inventory / EnumType / FunctionTag / ItemTemplate / InventoryTemplate` をクエリ；
- `.asset`、JSON テキスト、バイナリバイトの 3 ソースからの登録に対応。

> **クエリインデックス（1.5.0）**：すべての `GetXxx(id/name)` は、データベースを順に線形走査するのではなく
> 遅延構築される辞書（O(1)）を経由します。これらの呼び出しは **UI セルのバインドごと**、
> および**ソート中の 2 要素比較ごと**に発生するため、線形探索はアイテム総数に応じて顕著なコストになります。
> インデックスはデータベースの登録 / 登録解除 / クリア時に無効化され、次回クエリ時に再構築されます。
> 登録順に**先着優先**で構築されるため、「最初にヒットしたデータベースを優先」という従来の意味と一致します。
> ランタイムで登録済みデータベースの内容を直接変更した場合は `InvalidateIndex()` を呼んでください。

> **ソート用ルックアップ（1.5.0）**：`SortInventory` / `SortSlots` / `SortByItemId` と UI リストの表示ソートは、
> ソート前に `SortLookup` を 1 つ構築し（整理オプションの無視リスト、属性フィールド定義、アイテムテンプレート、
> 列挙型、機能タグの序数）、比較器内の検索を O(1) に落とします。このルックアップは 1 回のソートの間だけ存在して
> 破棄されるため、キャッシュが古くなる問題は起きません。ソート実装はすべて `InventorySortService`（静的・インスタンス状態なし）にあります。
> **1.6.0 より**、プロジェクト層互換のために `InventoryRuntimeManager` に残していた `public static` フォワード
> （`SortSlots` / `SortByItemId` / `CompareSlots` / `CompareByField` / `IsIgnoredByField` / `FindAttrDef` /
> `ContainsStr` / `GetTagOrder`）は削除されました —— `InventorySortService.Xxx` を直接呼んでください。
> ランタイム状態を書き換えるインスタンスメソッド `InventoryRuntimeManager.SortInventory` は影響を受けません。

### サブシステムのランタイムマネージャー

ショップとクラフトのランタイムロジックは、2 つの**軽量シングルトン**（`InventorySystemSingleton<T>`、初回アクセスで自動作成、非 MonoBehaviour）が担います。どちらも自身はカタログデータを持たず（カタログは登録済みデータベース由来）、倉庫の読み書きは一律 `InventoryRuntimeManager` を通します：

- `ShopRuntimeManager`：価格解決（複数通貨）、取引倉庫をまたいだ通貨 / 保有量の集計、更新スケジュールに従う取引可能回数のリセット、購入 / 買い取り（回数 / 通貨 / 容量 に応じて成立を自動で下方調整）、**プレイヤーごとの取引進捗のセーブ**。イベント `OnShopChanged`。
- `CraftingRuntimeManager`：作成可能回数の計算、クラフト倉庫をまたいだ材料の差し引き / 産出の配置（1 回の作成を実行）。**自身の状態を持たず、セーブしない**。連続作成は UI 層がループで駆動。
- `EquipmentRuntimeManager`：`装備グループ ID → (スロット ID → 装備中アイテム ID)` として装備中状態を管理。装備 / 解除 / 交換は `InventoryRuntimeManager` と連携してアイテムを搬送（旧アイテムを戻せないときはロールバック）、制限マッチングは**すべて AND**、スロットの自動検索、「装備属性フィールドリスト」に従い装備中アイテムをまたいで合算し合計ボーナスを算出（集計アルゴリズムは `EquipmentBonusCalculator` にあり、インスタンス状態と分離され単独で呼び出せます）。**装備中状態のセーブあり**（`GetSaveData` / `LoadSaveData` / `ResetAll`）。イベント `OnEquipmentChanged`。

> **プレイセッションをまたぐ静的状態のリセット（1.5.0）**：上記の軽量シングルトンと MonoBehaviour
> シングルトンのインスタンス、および `IsQuitting` フラグはいずれも静的フィールドです。Domain Reload を
> 無効化している場合（Project Settings → Editor → Enter Play Mode Options）、静的フィールドはプレイ
> セッションをまたいで残り、前回の装備 / ショップ進捗が次回に持ち越されます。
> `[RuntimeInitializeOnLoadMethod]` はジェネリック型のメソッドには付けられないため、各クローズド
> ジェネリックが初回インスタンス生成時に、非ジェネリックの `InventorySingletonRegistry` へリセット処理を
> 登録し、同クラスが毎回のプレイ開始時（`SubsystemRegistration`）にまとめて実行します。
> Domain Reload が有効（既定）なら本機構は無害な空処理です。

ショップ更新に必要な 3 種の時計（ゲーム / ローカル / サーバー時間）は `InventoryRuntimeManager.RegisterTimeGetter` で登録し、未登録のときはシステムのローカル時間にフォールバックします。

**セーブ契約（`IInventorySaveable<TState>`）**：セーブ対象の状態を持つ 4 つのマネージャ（倉庫 / 装備 / ショップ / スキル）がこのインターフェースを実装し、ゲーム層の SaveManager がこれを呼びます。契約は 3 点を固定します：`GetSaveData` は**ディープコピー**を返す。`LoadSaveData` は**マージではなく上書き**（セーブに無くメモリに在るエントリを残してはいけない）。3 つのメソッドはいずれも変更イベントを**発火しない** —— 一括差し替えの後は呼び出し側が UI を更新します。非ジェネリックの `IInventorySaveable` は `ResetAll` のみを持ち、「ニューゲーム」で全システムを一括リセットできます。各システムのセーブ型は異なる（`RuntimeInventoryState` / `RuntimeEquipmentState` / `ShopRuntimeState` / `RuntimeLearnedSkillState`）ため、統一するのは契約のみで、ストレージ実装は統一しません。

### アセンブリ分割

| asmdef | 内容 |
|--------|------|
| `Ale.Inventory.Runtime` | データモデル、マネージャー、シリアライズ（ランタイムコア） |
| `Ale.Inventory.UI` | ランタイム UI コンポーネント。Runtime と TextMeshPro を参照 |
| `Ale.Inventory.Editor` | エディタウィンドウとパネル |
| `Ale.Inventory.Addressables.Runtime` / `.Editor` | Addressables アセット読み込みサポート |
| `Ale.Inventory.UI.Localization` | TMP テキスト / フォントのローカライズイベント |

---

## 拡張ガイド

新しいサブシステム（スキルなど）を追加するとき（ショップ / クラフト / 装備はこのパターンで実装済みで、参考になります）：

1. `InventoryDatabase` に対応するデータリストを追加（+ getter / `CloneFrom` / `Validate`）；
2. `Editor/` 以下にサブディレクトリを新規作成し、3 カラムパネルを実装（`AttributeDefinitionListDrawer` と `AttributeFieldDrawer` を再利用）；
3. `InventoryEditorWindow` に新しいタブを登録（+ ID 重複スキャン / `RebuildAllAttributes`）；
4. `InventoryDataManager` に対応するクエリメソッドを追加。ランタイムロジックは軽量シングルトン（`InventorySystemSingleton<T>`）が担う；
5. `InventoryDtoModels.cs` に DTO ミラーを追加し、あわせて `InventoryDtoMapper.<システム>.cs` / `InventoryBinarySerializer.<システム>.cs` の partial を 1 組作成（既存 5 組のいずれかを真似ればよい）し、`ToDto` / `FromDto` とバイナリの Export / Import に新ブロックを繋ぎます —— **これを忘れるとそのシステムのデータはエクスポート時に黙って捨てられます**。グループタグを持つシステムは `GroupTagDto` とジェネリックの `FromDto<T>` をそのまま再利用できます。

属性システム（`AttributeValue / AttributeDefinition / EFieldType`）、列挙型、機能タグ、DTO シリアライズフレームワークはいずれも直接再利用できます。
