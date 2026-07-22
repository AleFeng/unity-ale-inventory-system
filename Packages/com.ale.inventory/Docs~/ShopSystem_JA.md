# ショップシステム（Shop System）

- [説明ドキュメント](../README_JA.md) に戻る

ショップシステムは、アイテム / 倉庫の上に売買を実装します。ショップは設定カタログ（商品がエントリ）で、ランタイムのプレイヤーごとの取引進捗（購入回数、更新タイムスタンプ）は `ShopRuntimeManager` が管理します。価格はハードコードせず、アイテムの `StringIntPair` 属性（通貨 ID → 価格）から取得し、複数通貨、価格倍率、周期的に更新される取引可能回数に対応します。

# 📜 目次

- [コアコンセプト](#コアコンセプト)
- [ショップ種類](#ショップ種類)
- [タブ構成](#タブ構成)
- [ショップテンプレート（左カラム）](#ショップテンプレート左カラム)
- [ショップ Inspector（右カラム）](#ショップ-inspector右カラム)
- [価格ソース](#価格ソース)
- [取引倉庫](#取引倉庫)
- [商品グループと商品](#商品グループと商品)
- [更新スケジュール](#更新スケジュール)
- [ランタイム API](#ランタイム-api)
- [時間注入](#時間注入)
- [セーブとロード](#セーブとロード)
- [ショップ UI](#ショップ-ui)

# コアコンセプト

| 概念 | 説明 |
|------|------|
| ショップ（`Shop`） | 1 つの店舗：種類 + 取引倉庫 + 価格ソース + フィルタタグ + いくつかの商品グループ |
| 商品グループ（`ShopCommodityGroup`） | UI でタブとしてグループ化される一群の商品。グループレベルの更新スケジュールを保持 |
| 商品（`ShopCommodity`） | 1 つのアイテム ID に関連付き、取引数量、価格倍率、取引可能回数、更新の上書きを記述 |
| 価格属性ソース | アイテム上の `StringIntPair`（通貨 ID → 価格）属性の ID |
| 取引倉庫 | この店との取引に使う倉庫リスト：通貨集計、購入品の受け取り、買い取り元、釣り銭の書き込み |

# ショップ種類

| 種類 | 挙動 |
|------|------|
| **販売（Sell）** | プレイヤーが通貨でショップ商品を購入 |
| **買い取り（Recycle）** | プレイヤーがバックパックのアイテムをショップに売って通貨を得る（「取引機能タグ」でどのアイテムが買い取り対象かを限定可能） |
| **等価交換（Barter）** | 双方が総価値で交換（**今期はプレースホルダー**、取引 API は `NotSupported` を返す） |

3 種類とも「取引倉庫」の設定が必要です。

# タブ構成

Inventory Editor 上部の「**ショップシステム**」タブをクリックします。倉庫システムと対称な 3 カラムレイアウト：

```
左カラム：ショップテンプレート（一覧 + 編集パネル）
中央カラム：ショップ一覧
右カラム：選択したショップの Inspector（商品グループ / 商品の編集を含む）
```

# ショップテンプレート（左カラム）

ショップテンプレートは、設定可能項目の既定値 + カスタム属性フィールドを定義し、ショップ作成のひな型になります（ショップとテンプレートは同じ設定項目ドロワーを共有）。

| フィールド | 説明 |
|------|------|
| 名称 / 色 | テンプレート名 + 一覧の色ドット |
| ショップ種類 | 販売 / 買い取り / 等価交換 |
| 取引倉庫 | 倉庫 ID のリスト（複数選択可） |
| 取引機能タグ | 買い取りのみ有効：これらのタグのいずれかを含むアイテムのみ買い取り。空 = 制限なし |
| フィルタタグ | UI では機能タグボタンとして表示 |
| 「すべて」タブを表示 | UI のタブバーに「すべて」を表示して既定選択するか |
| 数字フォーマット | 参照する数字フォーマット設定の名称 |
| 価格属性ソース | StringIntPair アイテム属性の ID |
| 商品グループ | 商品グループと商品のリスト |
| 属性フィールドリスト | テンプレートのカスタム属性フィールド |

# ショップ Inspector（右カラム）

ショップを選択すると、右カラムに完全な設定が表示されます。フィールドはテンプレートと同じで、さらに：

- **ID**：一意な識別子。空または重複のときハイライト表示。
- **ソーステンプレート**：読み取り専用。
- **名称 / 説明**：`displayNameText` / `descriptionText`（`Text`：プレーンテキストのフォールバック + 任意のローカライズ参照。名称が空のとき `id` に退避）。
- **商品グループ / 商品**：折りたたみ + 検索 + アイテムテンプレート別グループ化ドロップダウン + アイテム ID 検証（存在しないアイテムを参照するとエクスポート検証時にエラー）。

# 価格ソース

価格は商品にハードコードせず、命中したアイテムの属性から読み取ります：

1. アイテムシステムでアイテムに **`StringIntPair` 配列属性**（例：ID「販売価格」）を追加します。各要素 = `通貨ID → 単価`、例：`ゴールド → 100`、`ジェム → 1`。
2. ショップの「価格属性ソース」にその属性 ID（例：「販売価格」）を入力します。
3. ランタイムで `ShopRuntimeManager.GetUnitPrice(shop, commodity)` がその属性を読み取り、各通貨ペアの価格に商品の `priceMultiplier` を掛けて合算し、`通貨ID → 金額` の辞書を返します。

```csharp
Dictionary<string,int> unit  = ShopRuntimeManager.Instance.GetUnitPrice(shop, commodity);
Dictionary<string,int> total = ShopRuntimeManager.Instance.GetTotalPrice(shop, commodity, times);
```

価格ソースがない / アイテムにその属性がない場合は空の辞書を返します（無料 / 無収益とみなす）。複数通貨とは、1 つの商品に複数の通貨価格を同時に付けることです。詳細は [属性システム - StringIntPair](AttributeSystem_JA.md#stringintpair-と価格--通貨) を参照してください。

# 取引倉庫

ショップの `tradeInventoryRefs` は一組の倉庫 ID（順序付き）です。これらは同時に次を担います：

- **通貨集計**：プレイヤーの通貨 = これらの倉庫内の「通貨アイテム」の総保有量（通貨とは、id が通貨 ID に等しいアイテムのこと）。
- **購入品の受け取り**：販売店で購入したアイテムを優先度順にこれらの倉庫へ格納。
- **買い取り元**：買い取り店はこれらの倉庫から買い取られたアイテムを差し引く。
- **釣り銭の書き込み**：取引で発生した通貨を優先度順にこれらの倉庫へ書き込む。

# 商品グループと商品

**商品グループ（`ShopCommodityGroup`）**：name（UI タブ名）、description、グループレベルの更新スケジュール、商品リスト。

**商品（`ShopCommodity`）**：

| フィールド | 説明 |
|------|------|
| `itemId` | 関連するアイテム ID |
| `count` | 1 回の取引で入手 / 買い取りする数量 |
| `priceMultiplier` | 価格倍率（1 = 元の価格。買い取りではよく <1、例：0.5 で半額買い取り） |
| `tradeLimit` | 各更新周期内の取引可能回数（-1 = 無制限） |
| `overrideRefresh` | グループレベルの更新スケジュールを上書きして自身の `refresh` を使うか |
| `refresh` | 商品レベルの更新スケジュール（`overrideRefresh` が true のときのみ有効） |

# 更新スケジュール

更新スケジュール（`ShopRefreshSchedule`）は、「取引可能回数」がどの周期で、どの時計に基づき、どの時刻にリセットされるかを記述します。グループが 1 つ保持し、商品が上書きできます。

| フィールド | 説明 |
|------|------|
| 更新周期 | 更新なし / 毎日 / 毎週 / 毎月 |
| 時計種類 | ゲーム時間 / ローカル時間 / サーバー時間 |
| タイムゾーン ID | IANA / Windows のタイムゾーン識別子（空 = 時計自身のローカルタイムゾーン） |
| 時刻 | 時（0-23）+ 分（0-59） |
| 曜日 | 毎週更新用（0 = 日曜 … 6 = 土曜） |
| 日 | 毎月更新用（1-31。その月の日数を超える場合は最終日を採用） |

「更新なし」のとき `tradeLimit` は生涯の上限になります。

# ランタイム API

```csharp
using Ale.Inventory.Runtime;

var sm = ShopRuntimeManager.Instance;

// クエリ
int owned   = sm.GetOwnedCount(shop, itemId);          // 取引倉庫内の保有量（通貨の確認にも使う）
int left    = sm.GetRemainingTrades(shop, commodity);  // 今周期の残り取引可能回数
int maxBuy  = sm.GetMaxPurchasable(shop, commodity);   // 回数/通貨/容量に制約された最大購入数
int maxRec  = sm.GetMaxRecyclable(shop, commodity);    // 最大買い取り可能数

// 取引（回数 / 通貨 / 容量 に応じて成立回数を自動で下方調整）
ShopTradeResult buy  = sm.Purchase(shopId, commodity, times);   // 販売店：購入
ShopTradeResult sell = sm.Recycle(shopId, commodity, times);    // 買い取り店：買い取り
ShopTradeResult s2   = sm.RecycleItem(shopId, itemId, times);   // 買い取り店：アイテム ID で買い取り

// 取引進捗の変化をリッスン（UI リフレッシュ）
sm.OnShopChanged += shopId => RefreshShopUI(shopId);
```

`ShopTradeResult` は、実際の成立回数 / 価格と失敗理由（通貨不足、容量満杯、回数上限到達、`NotSupported` など）を記述します。

> `CraftingRuntimeManager` と同様に `ShopRuntimeManager` は軽量シングルトンです：商品カタログは登録済みデータベースから来て、プレイヤーごとの取引進捗だけが必要に応じて作成・セーブされます。

# 時間注入

更新に必要な時計は `InventoryRuntimeManager` が統一的に提供します。ゲーム / サーバー時間はゲッターの登録が必要で、未登録の場合はシステムのローカル時間にフォールバックします：

```csharp
// enum のメンバーはソースでは中国語の識別子です。そのまま維持してください。
InventoryRuntimeManager.Instance.RegisterTimeGetter(ShopTimeType.游戏时间,   () => GameClock.Now); // 游戏时间 = ゲーム時間
InventoryRuntimeManager.Instance.RegisterTimeGetter(ShopTimeType.服务器时间, () => NetTime.UtcNow); // 服务器时间 = サーバー時間
```

# セーブとロード

```csharp
// プレイヤーごとの取引進捗を保存
List<ShopRuntimeState> save = ShopRuntimeManager.Instance.GetSaveData();

// 読み込み
ShopRuntimeManager.Instance.LoadSaveData(save);

// すべての進捗をクリア（アカウント切り替え / リスタートなど）
ShopRuntimeManager.Instance.ResetAll();
```

# ショップ UI

ショップ UI は種類ごとに分かれ、基底クラス `UiwShopViewBase`（`Runtime/UI/View/Shop/`）を共有します：

| ビュー | 説明 |
|------|------|
| `UiwSellShopView` | 販売店画面：商品グループタブ + 通貨バー + カート式の購入決済 |
| `UiwRecycleShopView` | 買い取り店画面：バックパックアイテムの買い取り決済 |
| `UiwBarterShopView` | 等価交換画面（プレースホルダー） |

**フィルタ / ソート**：`UiwShopViewBase` は `UiwFilterTabBar`（ショップの「フィルタタグ」で商品をフィルタ）と `UiwSortToolbar`（ソートドロップダウン + 昇順/降順 + 自動整理）に対応し、いずれもリスト基底クラスのフィルタ / ソートパイプラインを再利用します（[UI コンポーネントガイド §7.4](UIComponentGuide_JA.md) を参照）。ソート条件はショップの `sortPriorities` / `sortTiebreakers`（整理ソート、`Shop` / `ShopTemplate` のいずれにも設定可能、テンプレートからショップを作成する際にコピー）から取得します。

**開き方と対象ショップ**：`Open(shopId)` はショップ ID を記録してから開きます。`_shopId`（Inspector「ショップ」）で既定ショップをあらかじめ設定でき、引数なしの `Open()` は `_shopId` からショップを解決して開き、エディタの `autoOpenOnStart` も現在の `_shopId` を使います。`Open(shopId)` または Inspector で変更されるまでその値を使います。（`ShopId` は現在 `protected` プロパティで、backing フィールドは `_shopId`。）

プレハブ作成とコンポーネントのパラメータは [UI コンポーネントガイド](UIComponentGuide_JA.md) を参照してください。
