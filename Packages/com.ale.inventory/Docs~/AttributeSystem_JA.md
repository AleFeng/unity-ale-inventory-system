# 属性システム（Attribute System）

- [説明ドキュメント](../README_JA.md) に戻る

柔軟な属性システムは、アイテム / 倉庫 / ショップ / クラフトの 4 大サブシステムが共有するデータ基盤です。本ドキュメントでは、属性フィールドの型、`AttributeValue` の格納と取得、表示文字列とソート比較ルールを説明します。各サブシステムの「カスタム属性フィールド」「属性フィールド表示」「整理ソート」はすべてこの上に構築されています。

# 📜 目次

- [属性システム（Attribute System）](#属性システムattribute-system)
- [📜 目次](#-目次)
- [概念](#概念)
- [属性フィールド型リファレンス](#属性フィールド型リファレンス)
- [列挙型と安定した参照](#列挙型と安定した参照)
- [属性値の読み取り（ランタイム API）](#属性値の読み取りランタイム-api)
- [リソースフィールドの読み込み（Addressables）](#リソースフィールドの読み込みaddressables)
- [ローカライズ（固定 Text フィールドとツール）](#ローカライズ固定-text-フィールドとツール)
- [表示文字列（ToDisplayString）](#表示文字列todisplaystring)
- [ソート比較数値（ToComparableNumber）](#ソート比較数値tocomparablenumber)
- [StringIntPair と価格 / 通貨](#stringintpair-と価格--通貨)
- [EnumIntPair と装備属性ボーナス](#enumintpair-と装備属性ボーナス)

# 概念

属性システムは 3 つの型で構成されます：

| 型 | 役割 | 説明 |
|------|------|------|
| `AttributeDefinition` | **定義（スキーマ）** | 1 つの属性フィールドの定義：`id`（キー）、`type`（`EFieldType`）、`isArray`（配列かどうか）、`enumTypeRef`（列挙型名）、既定値。アイテムテンプレート / 機能タグ / 倉庫テンプレート / ショップテンプレート / ブループリントテンプレート / 列挙項目 に設定する。 |
| `AttributeValue` | **値（タグ付き共用体）** | `type` に応じて 1 つまたは一群の値を格納。「タグ付き共用体 + 配列」で格納し、SO シリアライズと Undo/Redo をネイティブに対応。 |
| `AttributeEntry` | **キー・値ペア** | `id` + `AttributeValue`。具体的なエントリ（アイテム / 倉庫 / ショップ / ブループリントの `values` リスト）を構成する。 |

あるアイテムがどの属性フィールドを持つかは、その「ソーステンプレート固有のフィールド + テンプレート固定タグのフィールド + アイテム自選タグのフィールド」が共同で決めます。`RebuildAttributes` が定義に従って実際の `values` リストを同期します（不足を補い、孤立を除去、既存値を保持）。倉庫 / ショップ / ブループリントも同様です（各自のテンプレートからのみ収集）。

# 属性フィールド型リファレンス

以下の型はすべて**配列形式**に設定できます（「配列」にチェックすると複数の値を格納でき、動的な増減に対応）。

| 型 | 格納 | エディタコントロール |
|------|------|-----------|
| Bool | 整数 0/1 | Toggle |
| Int | 整数 | IntField |
| Float | 浮動小数点 | FloatField |
| String | 文字列 | TextField |
| **Text** | `IS_LOCALIZATION` 有効時はローカライズ参照を含む：テーブル + エントリ（string フィールドがフォールバック） | ローカライズエントリセレクター + テキストボックス |
| Vector2 / 3 / 4 | 2 / 3 / 4 個の浮動小数点 | 複数列 FloatField |
| VectorInt2 / 3 / 4 | 2 / 3 / 4 個の整数 | 複数列 IntField |
| Color | 4 個の浮動小数点（RGBA） | ColorField |
| **Enum** | 整数（列挙値） | Popup ドロップダウン（列挙型の同時選択が必要） |
| **StringIntPair** | 文字列 + 整数 | TextField + IntField（例：通貨ID → 価格） |
| **EnumIntPair** | 列挙 + 整数（整数後備リスト、ステップ 2） | Popup ドロップダウン + IntField（列挙型の同時選択が必要。例：キャラクター属性タイプ → ボーナス値） |
| Sprite | UnityEngine.Object | 正方形プレビュー |
| Prefab / Texture / Material / AudioClip / AnimationClip / PhysicsMaterial / PhysicsMaterial2D | UnityEngine.Object | ObjectField |
| AnimationCurve | AnimationCurve | CurveField |

> **内部格納**：すべての値は型ごとに `List<int>` / `List<float>` / `List<string>` / `List<Object>` / `List<AnimationCurve>` の 5 つの後備リストに平坦化されます（スカラーは `[0]`、ベクトルはステップで平坦化、配列は順に配置）。オブジェクト系フィールドの Addressable アドレス / 授権 GUID は `List<Object>` と並行するアドレスリストに別途格納されます（[リソースフィールドの読み込み](#リソースフィールドの読み込みaddressables) を参照）。詳細は [アーキテクチャ](Architecture_JA.md#コアデータモデル) を参照してください。

# 列挙型と安定した参照

- 列挙型（`EnumType`）は単調増加する `nextValue` を維持します：列挙項目の追加時に割り当てて自増し、**削除しても回収しません**。
- 属性値が格納するのは**列挙値（int）**であり表示インデックスではないため、**列挙項目の表示順序を並べ替えても既存の参照は壊れません**。
- 列挙項目（`EnumItem`）自身も一組のカスタム属性フィールド（例：各品質 / 職業の付加データ）を持てます。アイテム Inspector である列挙値を選択すると、そのサブ属性が読み取り専用で展開表示されます。

# 属性値の読み取り（ランタイム API）

アイテム / 倉庫 / ショップ / ブループリントはいずれも属性アクセスを継承または実装しています（アイテムは `AttributeOwner` 基底クラス経由）：

```csharp
using Ale.Inventory.Runtime;

Item item = InventoryDataManager.Instance.GetItem("sword_01");

// エントリ（AttributeValue を含む）を取得、見つからなければ null
AttributeEntry entry = item.GetEntry("攻撃力");

// 強く型付けした値を取得（フォールバック付き）。T はフィールド型に一致（int/float/string/Vector3/Color/...）
int   atk   = item.GetAttributeValue<int>("攻撃力", 0);
string desc = item.GetAttributeValue<string>("説明");

// 生の AttributeValue を取得（型判定 / 配列 / 多値が必要なとき）
AttributeValue av = item.GetAttributeValue("価格");
```

`AttributeValue` は型別の読み取り専用アクセサを提供します：`GetInt` / `GetFloat` / `GetString` / `GetVector2~4` / `GetColor` / `GetStringIntPair` / `GetObject` / `GetAnimationCurve` など（いずれもインデックス指定、範囲外安全）。`Type` / `IsArray` / `Count` がその形態を表します。

# リソースフィールドの読み込み（Addressables）

オブジェクト系フィールド（Sprite / Prefab / Texture / Material / AudioClip / AnimationClip / PhysicsMaterial / PhysicsMaterial2D）の読み込みは一律 `InventoryAssets` ファサードを通し、Addressables を有効にするかどうかと分離されています：

```csharp
using Ale.Inventory.Runtime;

// アイテムのある属性のリソースを UI にバインド。ホスト GameObject の破棄時にハンドルを自動解放
InventoryAssets.Bind<Sprite>(item, "アイコン", image.gameObject, s => { image.sprite = s; });
// または AttributeValue を直接渡す（配列要素 index を指定可能）
InventoryAssets.Bind<Sprite>(attrValue, owner, s => image.sprite = s, index);
```

- **`IS_ADDRESSABLE` 無効（直接モード）**：属性フィールドは Unity のリソース参照（`objRefs`）を直接保持し、設定データの読み込み時にリソースもメモリに読み込まれます。ファサードはその実時参照を同期的に返します（エディタコントロールは `ObjectField`）。
- **`IS_ADDRESSABLE` 有効（授権モード）**：エディタのオブジェクトフィールドはネイティブの **AssetReference** 検索可能セレクターに切り替わり、設定は GUID のみ格納します（ハード参照せず、データベース読み込みでリソースも一緒にメモリへ載せない）。ランタイムでは Addressables 経由でオンデマンドに**非同期読み込み**し、アドレスで参照カウントし、ホスト破棄時に**自動アンロード**します。エクスポート時に参照されたリソースは `InventorySystem` Addressable グループに自動登録されます。

> 2 つの格納形式はディスク上のフォーマットが異なり、同名フィールドで自動共有はできません。マクロ切り替え後、メニュー **Tools/Inventory System/Addressables** で、あるデータベースの全リソースフィールドを「Object 参照 ↔ AssetReference(GUID)」間でワンクリック相互変換できます。
>
> 内部：授権 GUID / ランタイムアドレスは実時参照と並行して `AttributeValue` に格納されます（アドレスリスト vs `objRefs`）。ファサードは実時参照を優先し、なければアドレスの非同期読み込みに退避します。core アセンブリは Addressables にゼロ依存で、ネイティブセレクターは制約付きの Addressable エディタアセンブリから注入されます（`InventoryExportResolver` と同じ注入パターン）。
>
> 設定クラスの**固定リソースフィールド**（`Skill.icon`、`SkillTemplate.icon`、`FunctionTag.backgroundSprite` などの名前付きフィールド）も同じ仕組みを採用します：各々が並行する `xxxAddress` の純文字列フィールドを持ち、エディタでは `InventoryAssetRefField` で描画（直接 `ObjectField` / 授権 AssetReference セレクター）、ランタイムでも同様に `InventoryAssets.Bind(liveRef, address, owner, set)` で非同期取得します。

# ローカライズ（固定 Text フィールドとツール）

全ライブラリのローカライズ表示テキストは一律 `EFieldType.Text` が保持します：`AttributeValue` は「プレーンテキストのフォールバック + テーブル参照 + エントリキー」の 3 スロットで平坦に格納し（ネイティブなシリアライズ / Undo / エクスポートに優しい）、ランタイムの `AttributeValue.ResolveText()` は**ローカライズ優先、取れなければプレーンテキストに退避**します。属性システム内の Text 型カスタム属性値だけでなく、各設定クラスの**固定 Text フィールド**も含みます：

| 設定クラス | 固定 Text フィールド |
|--------|---------------|
| `Skill` / `SkillTemplate` / `CraftingBlueprint` | `displayText`（名称）、`descriptionText`（説明） |
| `Shop` / `Inventory` / `EquipmentGroup` / `FunctionTag` | `displayNameText`（名称）、`descriptionText`（説明） |
| `GroupTag`（スキル / クラフト / 装備 のグループタグ） | `displayName`、`description` |
| `NumberFormatRule` | `suffixText`（数字の接尾辞） |
| `SortOption` | `displayName`（整理ドロップダウンの表示名） |

エディタは一律 `AttributeFieldDrawer` で Text を描画します（プレーンテキストボックス + ネイティブの検索可能なテーブル / エントリセレクター）。ランタイムの読み取りは `ResolveText()` を使います。

## ローカライズツールウィンドウ

`Tools > Inventory System > Localization > ローカライズツールウィンドウ`（`IS_LOCALIZATION` のみ。ウェルカムウィンドウにも入口ボタンあり）。1 つの `InventoryDatabase` に Unity Localization をワンストップで接続します：

1. **多言語テーブルの生成 / 関連付け**：現在の Locale に基づき String Table 集合を生成し（テーブル名 `{接頭辞}_{データベース名}`、接頭辞 / 生成フォルダは設定・記憶可能）、その `SharedTableData` の GUID をデータベースに記録（1:1、フィールド `InventoryDatabase.LocalizationTableCollectionGuid`）。「多言語テーブルを関連付け」では手動で String Table Collection を新規作成してドラッグ挂载することもできます。「編集」ボタンでそのテーブルの Table Editor を開きます。
2. **多言語キーの生成**：ライブラリ内の**すべての** Text フィールドを走査し、フレームごとに一意の**中国語キー**（`道具系统-{カテゴリ}-{インスタンスid}-{フィールド}[-{要素}]`、例：`道具系统-道具条目-{道具id}-名称`、`道具系统-枚举类型-{枚举名}-{枚举项名}-{属性id}`）を生成し、フィールドのテーブル / エントリ参照を書き戻し、テーブルに Key→Value エントリを作成します。プレーンテキスト内容のあるフィールドのみ処理し、同名キーは `#n` を付けて重複排除します。
3. **2 つのチェック項目**：
   - **既存の多言語キーを上書き**：チェックすると実行前に確認をポップ。キー設定済みのフィールドは自動生成されたキーに切り替え（命名が既存と同じなら変更しない）。チェックしなければ設定済みフィールドをスキップ。
   - **Text 内の String テキストを埋める**：チェックするとソース Text のプレーンテキスト値を初期値として、そのキーの**すべての言語テーブル**の空エントリに埋めます（既存の翻訳は上書きしない）。

> 中国語キーは問題なく使えます：Unity Localization は Unicode キーに対応し、ランタイムはキーで解決するため、中国語と英語で実質的な性能差はありません。本ツールは `InventoryAddressableToolWindow`（リソース参照の移行）と基底クラス `InventoryToolWindowBase`（フレームごとのステップ + 進度バー + 選択可能ログ）を共有します。

# 表示文字列（ToDisplayString）

属性値を可読な文字列に組み立て、`EFieldType` に応じて異なるルールを用い、UI に直接表示します（例：クラフトブループリントエントリの「属性フィールド表示」）：

```csharp
string text = item.GetEntry("攻撃力")?.value?.ToDisplayString();
```

| 型 | 表示形式 |
|------|---------|
| Int / Float | 直接文字列化（Float は最大 2 桁の小数を保持） |
| Bool | `是` / `否`（はい / いいえ） |
| String | 原文 |
| Enum | 列挙項目の表示名 |
| Vector2 / 3 / 4 | `(x, y[, z[, w]])` |
| VectorInt2 / 3 / 4 | `(x, y[, z[, w]])` |
| Color | `RGBA(r, g, b, a)` |
| **StringIntPair** | `key: value`（例：`ゴールド: 120`） |
| **EnumIntPair** | `列挙名: value`（例：`筋力: 10`。キーは列挙項目の表示名に解決） |
| Text | プレーンテキスト。空のときローカライズエントリキー / テーブル参照に退避 |
| AnimationCurve | `曲线(N 关键帧)`（カーブ(N キーフレーム)） |
| オブジェクト参照 | リソースオブジェクトの `name` |

配列形式のときは、各要素を個別に組み立て、区切り文字（既定 `、`）で連結します。読み取り処理は非破壊で、内部データを変更しません。

# ソート比較数値（ToComparableNumber）

整理ソート時、カスタム属性フィールドは `EFieldType` に応じて比較用の `double` に換算されます：

| 型 | 比較基準 |
|------|---------|
| Int / Bool / Enum | 値そのもの |
| Float | 値そのもの |
| Vector2 / 3 / 4 | **大きさ（magnitude）** |
| Color | Vector4 としての大きさ |
| VectorInt2 / 3 / 4 | 大きさ |
| **StringIntPair** | その中の **Int 値** のみ |
| **EnumIntPair** | その中の **Int 値** のみ |
| String / オブジェクト参照 / カーブ / ローカライズ | 比較可能な数値なし → `0` |

> `String` 型フィールドはソート時に比較器で特別処理されます（まず長さ、次に辞書順）。数値換算は経由しません。詳細は [倉庫システム - 整理ソート](WarehouseSystem_JA.md#整理ソート) を参照してください。

内部入口：`InventoryRuntimeManager.GetAttrNumeric` → `AttributeValue.ToComparableNumber()`。ベクトル成分の読み取りは非破壊です（内部リストを拡張しない）。

# StringIntPair と価格 / 通貨

`StringIntPair`（文字列 + 整数のペア、通常は配列）は「価格 / 通貨」の担い手です：

- ショップの「価格属性ソース」はアイテム上の `StringIntPair` 配列属性を指し、各要素 = `通貨ID → 単価`。1 つの商品に複数の通貨価格を付けられます。
- ランタイムでは `ShopRuntimeManager.GetUnitPrice` が読み取り、商品の価格倍率を掛けます。詳細は [ショップシステム - 価格ソース](ShopSystem_JA.md#価格ソース) を参照してください。
- ソート時、`StringIntPair` は Int 値（価格）のみ比較。表示時は `通貨: 価格` に組み立て。

# EnumIntPair と装備属性ボーナス

`EnumIntPair`（列挙 + 整数のペア、通常は配列）は「キャラクター属性ボーナス」の担い手で、`StringIntPair` と構造が対称です。違いはキーが任意の文字列ではなく**列挙**である点です：

- 各要素 = `キャラクター属性タイプ（列挙キー） → ボーナス値（整数）`。1 つの装備が複数のキャラクター属性にそれぞれボーナスを与えられます。
- そのフィールドに対応する**列挙型**（例：「キャラクター属性タイプ」列挙、`Enum` フィールドと同じ）を選ぶ必要があります。エディタは Popup + IntField のペアで入力し、内部では整数後備リストに平坦格納（ステップ 2：列挙値 + 整数値）、列挙型は `EnumTypeRef` で記録します。
- 格納するのは**列挙値（int）**であり表示インデックスではないため、列挙項目の並べ替え / 改名は既存の参照を壊しません。
- 表示時は `列挙名: 値`（例：`筋力: 10`）に組み立て。ソート時はその中の整数値のみ比較します。
- ランタイムでは `GetEnumIntPair(index)` で `(enumValue, value)` を読み取ります。装備ボーナスの完全な設定と精算は [装備システム](EquipmentSystem_JA.md) を参照してください。
