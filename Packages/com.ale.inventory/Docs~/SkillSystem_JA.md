# スキルシステム（Skill System）

- [説明ドキュメント](../README_JA.md) に戻る

スキルシステムは、プレイヤーが使えるスキル（攻撃 / 回復 / バフ / デバフ / 補助など）を設定します。スキルは独立した設定エントリで、ID / 名称 / 説明 / アイコンなどの固定情報を持ち、スキルの**種類 / 効果 / 数値 / 位階**などは、利用側が「カスタム属性フィールド」内で attrId を取り決めて保持し、他のシステムが読み取って使います。スキルは主に**装備系アイテムに付与**され（武器の攻撃スキル、防具の防御スキルなど）、他のアイテム（消耗品の使用スキル、魔法の巻物 / スキルブックなど）にも付与できます。スキルは設定カタログです。ランタイムの「習得済みスキル」状態は `SkillRuntimeManager` が管理し、表示集合は `SkillCollector` がソース別に収集します。

> **識別子について**：`位階`（tier）、`背景框`（背景フレーム）、`名称`（name）、`技能`（スキル参照）と示される attrId の既定値は、コンポーネントに焼き込まれた**実際の既定文字列値**です。変更すると実際の既定を誤って表すため、ここではそのまま維持しています。もちろんプロジェクトで任意の文字列に改名できます。

# 📜 目次

- [コアコンセプト](#コアコンセプト)
- [タブ構成](#タブ構成)
- [グループタグ](#グループタグ)
- [スキルテンプレート](#スキルテンプレート)
- [スキル（中央カラム）](#スキル中央カラム)
- [スキル Inspector（右カラム）](#スキル-inspector右カラム)
- [アイテム ↔ スキルの関連付け](#アイテム--スキルの関連付け)
- [位階（Enum）駆動の表示](#位階enum駆動の表示)
- [スキルソース](#スキルソース)
- [ランタイム API](#ランタイム-api)
- [スキル UI](#スキル-ui)

# コアコンセプト

| 概念 | 説明 |
|------|------|
| グループタグ（`SkillGroupTag`） | **スキル**をグループ化し、ランタイム UI でグルーピングタブによるフィルタを容易にする（例：攻撃 / 回復 / 強化 / 弱体 / 補助）。基本情報のみを保持し、属性フィールドは持たない |
| スキルテンプレート（`SkillTemplate`） | スキル作成のひな型：カスタム属性フィールド（スキーマ）+ 一組の「スキル既定情報」（名称 / 説明 / アイコン / グループタグ）を定義。分類フィルタにも使う |
| スキル（`Skill`） | スキル設定エントリ：固定情報（ID / 名称 / 説明 / アイコン）+ テンプレート由来のカスタム属性値（種類 / 効果 / 数値 / 位階などを保持） |
| アイテムのスキル参照 | アイテムがその「スキル参照属性フィールド」の 1 つ（String、配列可）にスキル ID を格納し、スキルをそのアイテムに**付与**する |
| 位階（Enum 属性） | スキル上の Enum 型カスタム属性。その列挙項目が「名称 / 背景フレーム」などの属性を持ち、エントリの背景フレームと Tooltip の位階名表示を駆動する |

> `Skill` は `AttributeOwner` を継承（`Item` / `EnumItem` と同源）するため、`GetEntry` / `GetAttributeValue<T>` で attrId により カスタム属性（String / Text / 数値 / 列挙など）を読み取れます。

# タブ構成

Inventory Editor 上部の「**スキルシステム**」タブをクリックします。クラフト / 装備システムと対称な 3 カラムレイアウト：

```
左カラム：サブタブ【グループタグ / スキルテンプレート】+ 一覧 + 編集パネル
中央カラム：スキル一覧
右カラム：コンテキスト Inspector（グループタグ / スキルテンプレート / スキル）
```

左カラムのサブタブで「グループタグ」または「スキルテンプレート」を切り替えます。左カラムのエントリを選択すると右カラムにそのエントリの編集パネルが表示され、中央カラムのスキルを選択すると右カラムにスキル Inspector が表示されます。

# グループタグ

基本情報のみを保持し、**属性フィールドは持ちません**。ランタイム UI でグルーピングタブによりスキルをフィルタするために使います。

| フィールド | 説明 |
|------|------|
| ID | 一意な識別子（スキルはこの ID でグループタグを参照） |
| 名称 | `Text`（プレーンテキストのフォールバック + 任意のローカライズ参照。空のとき ID に退避） |
| 説明 | `Text`（プレーンテキストのフォールバック + 任意のローカライズ参照） |
| 色 | 一覧の色ドット |

> ランタイムのグルーピングタブはグループタグの**表示名**をフィルタトークンとして使うため、各グループタグの表示名は互いに異なるべきです。

# スキルテンプレート

**カスタム属性フィールド（スキーマ）を定義**し、一組の「スキル既定情報」を保持して、スキル作成のひな型になり、同時に分類フィルタにも使います。

| フィールド | 説明 |
|------|------|
| 名称 / 色 | テンプレート名（スキルの `templateRef` 参照キー）+ 一覧の色ドット |
| スキル既定情報 | 名称 / ローカライズ名 / 説明 / ローカライズ説明 / アイコン / 主グループタグ / 副グループタグ |
| カスタム属性フィールド | テンプレート定義の属性フィールドスキーマ（スキルはこれに基づいて属性値を調整。各フィールドの `defaultValue` がスキル属性の初期値） |

> スキルとテンプレートは `ISkillConfig`（名称 / 説明 / アイコン / ローカライズ / グループタグ）を共有し、エディタは同じ描画（`SkillConfigDrawer`）を再利用します。
>
> **テンプレートからスキルを作成するとき、テンプレートの「スキル既定情報」**（名称 / 説明 / アイコン / グループタグ）を初期値としてコピーし、属性スキーマの `defaultValue` に従ってカスタム属性値を初期化します。以降、スキルは**独立して編集可能**です（テンプレートとは連動しなくなる）。

# スキル（中央カラム）

中央カラムはスキル一覧（選択した左カラムのテンプレートでフィルタ + 上部検索バーで ID / 名称フィルタ）です。「**テンプレートから追加**」はテンプレートから作成（既定情報をコピー + スキーマに従って属性値を初期化）し、ID は自動生成されます。「**クイック追加**」は末尾を複製します。行をクリックして選択すると右カラムにスキル Inspector が表示され、「スキルを削除」は右カラム上部にあります。各行の左にはドラッグハンドルがあり、スキル順序を並べ替えられます。ID が重複すると行内が赤くハイライトされ、下部のステータスバーに「⚠ スキル重複 ID」と表示されます。

# スキル Inspector（右カラム）

| フィールド | 説明 |
|------|------|
| ID | 一意な識別子（重複するとエクスポート検証でエラー。アイテムはこの ID でスキルを参照） |
| 名称 / 説明 | `Text`（プレーンテキストのフォールバック + 任意のローカライズ参照。名称が空のとき ID に退避。説明は詳細ポップアップに表示） |
| アイコン | `Sprite`（スキルエントリ / ポップアップに表示） |
| ソーステンプレート | 読み取り専用、カスタム属性フィールドを決定 |
| 主グループタグ / 副グループタグ | 主グループ単一選択 + 副グループ複数選択（グループタグ ID を参照） |
| カスタム属性値 | テンプレート定義由来のカスタム属性値（スキルの種類 / 効果 / 数値 / 位階などがここに保持される） |

> スキルの種類 / 効果 / 数値などには**固定フィールドがありません** —— 完全にテンプレートで定義するカスタム属性フィールド（例：`スキル種類` Enum、`効果` String、`ダメージ` Int、`位階` (tier) Enum など）次第で、スキルに値を入れ、他のシステム / UI が attrId で読み取ります。

# アイテム ↔ スキルの関連付け

スキルは**アイテムのカスタム属性フィールドの 1 つ**を通じてアイテムに付与されます：

- アイテムテンプレート（または機能タグ）に **String 型**の属性フィールド（例：`技能` /「スキル」）を定義し、**配列**形式にチェックすると**1 アイテムに複数スキル**を持たせられます。
- アイテムにスキル ID を入力します（配列なら複数）。
- ランタイム UI（`Equipment` / `Inventory` ソース）はコンポーネント上でそのフィールドの attrId（`skillRefAttrId`、既定 `技能`）を設定し、収集時にそのアイテム属性のすべての非空文字列を読み取り、1 つずつ `InventoryDataManager.GetSkill` でスキルに解決します。解決できない ID はスキップします。

> 収集の入口 `SkillCollector` は `EFieldType.String`（スカラーまたは配列）のスキル参照フィールドのみ認識します。同じスキルが複数のアイテム / スロットに参照されても 1 回だけ表示されます（参照で重複排除、順序保持）。

# 位階（Enum）駆動の表示

スキルには **Enum 型**の「位階」属性フィールド（attrId 既定 `位階`）を設定できます。その列挙項目（**アイテムシステムの「列挙型」**で定義、`EnumItem` も `AttributeOwner`）は、**名称**、**説明**、**背景フレーム（Sprite）**などのカスタム属性フィールドを持てます。UI はこれに基づいてレンダリングします（**アイテム UI の品質背景を参考**にし、解決チェーンは完全に同一）：

```
skill.GetEntry(rankAttrId).value → 列挙値 + 列挙型参照
  → InventoryDataManager.GetEnumType(参照).GetItemByValue(列挙値)     // 列挙項目 EnumItem
  → enumItem.GetAttributeValue<Sprite>(backgroundAttrId)   // スキルエントリの「位階背景フレーム」
  → enumItem.GetAttributeValue<string>(nameAttrId)         // Tooltip の「位階名称」（String / Text どちらも可）
```

- **スキルエントリ**（`UiwSkillEntry`）：位階列挙項目の「背景フレーム」Sprite でエントリ背景フレームを表示。コンポーネントに `rankAttrId`（既定 `位階`）+ `rankBackgroundAttrId`（既定 `背景框`）を設定。
- **スキル Tooltip**（`UiwSkillTooltip`）：位階列挙項目の「名称」を表示。コンポーネントに `rankAttrId` + `rankNameAttrId`（既定 `名称`）を設定。
- 全工程で UI 層の解決であり、新たなデータ API は不要。位階データがない / 列挙項目が解決できないときは関連表示が自動的に隠れます（null 安全）。

# スキルソース

ランタイムのスキル UI のスキル集合は 4 種類のソースから来ます（`ESkillSource`、`UiwSkillView` で切り替え。そのカスタム Inspector はソースに応じて**対応する ID フィールドのみ表示**）：

| ソース | 収集内容 | 必要な設定 |
|------|---------|--------|
| `InventoryDatabase` | データベースのすべてのスキル（スキルブック / 図鑑） | — |
| `Equipment` | ある装備グループの全装備スロットの装備中アイテムが参照するスキル | 装備グループ ID + スキル参照属性 `skillRefAttrId` |
| `Inventory` | ある倉庫の全アイテムが参照するスキル | 倉庫 ID + スキル参照属性 `skillRefAttrId` |
| `Character` | あるキャラクターが現在習得しているスキル | キャラクター ID（`SkillRuntimeManager` を参照） |

収集は一律 `SkillCollector.Collect(source, configId, skillRefAttrId)` を通し、結果は重複排除・順序保持されます。

# ランタイム API

`SkillRuntimeManager` は軽量シングルトン（初回アクセスで自動作成、`EquipmentRuntimeManager` に倣う）で、**キャラクター ID → 習得済みスキル ID リスト**（学習順を保持、重複排除）として複数キャラクターの習得済みスキルを管理し、セーブできます。スキル定義は `InventoryDataManager` でクエリし、`SkillCollector` は状態を持たないソース収集ツールです。

```csharp
using System.Collections.Generic;
using Ale.Inventory.Runtime;

// ── 表示するスキル集合を収集（4 種のソース。重複排除、順序保持）──
var all      = SkillCollector.Collect(ESkillSource.InventoryDatabase, null, null);
var equipped = SkillCollector.Collect(ESkillSource.Equipment, "equip_player", "技能"); // 装備グループの装備中アイテムのスキル
var invSk    = SkillCollector.Collect(ESkillSource.Inventory, "バックパック", "技能");   // 倉庫アイテムのスキル
var learned  = SkillCollector.Collect(ESkillSource.Character, "hero_01", null);        // キャラクターの習得スキル

// ── キャラクターの習得スキル（複数キャラクター、セーブ可能）──
var sk = SkillRuntimeManager.Instance;
sk.Learn("hero_01", "skill_fireball");                       // 習得（習得済みなら無視）、変化があったか返す
bool has = sk.HasLearned("hero_01", "skill_fireball");
sk.Forget("hero_01", "skill_fireball");                      // 忘却
IReadOnlyList<string> ids = sk.GetLearnedSkillIds("hero_01"); // 習得スキル ID（読み取り専用）
List<Skill> skills        = sk.GetLearnedSkills("hero_01");   // スキルオブジェクトに解決
sk.ClearLearned("hero_01");                                  // あるキャラクターをクリア

// イベント + セーブ
sk.OnLearnedChanged += characterId => { /* スキル UI をリフレッシュ */ };
var save = sk.GetSaveData();     // List<RuntimeLearnedSkillState>、ゲーム層の SaveManager がシリアライズ
sk.LoadSaveData(save);           // 読み込みで復元
sk.ResetAll();                   // すべてのキャラクターをクリア（新規ゲーム開始など）

// ── スキル定義をクエリ + カスタム属性を読む ──
Skill skill    = InventoryDataManager.Instance.GetSkill("skill_fireball");
string effect  = skill.GetAttributeValue<string>("効果");   // String / Text
int    damage  = skill.GetAttributeValue<int>("ダメージ");  // 数値型
```

- **`SkillRuntimeManager`**：可変状態である「習得済みスキル」のみを管理し、それ以外（名称 / 属性など）はスキル定義から読み取ります。`Learn` / `Forget` / `ClearLearned` は変化時に `OnLearnedChanged(characterId)` を発火して UI をリフレッシュします。セーブ単位は `RuntimeLearnedSkillState`（キャラクター ID + スキル ID リスト）です。
- **`SkillCollector`**：静的ツールで、ランタイム状態を持ちません。`Equipment` は装備グループの各スロットの装備中アイテムを走査し、`Inventory` は倉庫の各アイテムを走査して、アイテムの `skillRefAttrId`（String / 配列）を読んでスキル ID を解決します。`Character` は `SkillRuntimeManager.GetLearnedSkills` を読み、`InventoryDatabase` はすべてのスキルを取得します。

# スキル UI

スキル UI は `Runtime/UI/`（アセンブリ `Ale.Inventory.UI`）にあります：

| コンポーネント | 説明 |
|------|------|
| `UiwSkillView` | スキルメイン画面（`UiwViewBase` を継承）：タイトル + 検索バー + **主 / 副グルーピングタブ**（各々「すべて」あり、各々横スクロール可能な `UiwFilterTabBar` を再利用）+ グリッド / 順序の 2 表示モード切り替え + ホバー詳細ポップアップ。`Open()` はシリアライズされたソース設定で開き、`Open(source, configId)` はソースを切り替えて開く。ソースに応じて `OnEquipmentChanged` / `OnInventoryChanged` / `OnLearnedChanged` を購読して自動リフレッシュ（`InventoryDatabase` は静的データなので購読しない） |
| `UiwSkillGridList` / `UiwSkillOrderList` | スキルリスト（仮想スクロール）：それぞれ汎用の `UiwInventoryGridList` / `UiwInventoryOrderList` を継承。`SetSkills` はスキルを `UiwSkillEntry` にバインドしてプール再利用し、グリッドはビューポート幅から自動で列数を決め、順序は単一列、いずれも可視領域のみ描画。ビューは各インスタンスを 1 つずつ持ち、切り替えボタンでいずれかを表示 |
| `UiwSkillEntry` | スキルエントリ（グリッド / 順序共用）：アイコン + 名称 + **位階背景フレーム** + 任意の説明 / カスタム属性フィールド行。ホバーで `UiwSkillTooltip` により詳細をポップ |
| `UiwSkillTooltip` | スキルホバーポップアップ（`ISkillTooltip` を実装）：アイコン + 名称 + **位階名称** + 説明 + コンポーネント上で設定したカスタムフィールド（`customFieldKeys`、Array で複数可）。`UiwItemTooltip` のフェード / カーソル位置 / キューを再利用。**プレハブは `InventoryRuntimeManager` の `skillTooltipPrefab` に設定し、マネージャーがグローバルに 1 回インスタンス化**（親ノードは `tooltipParent` を再利用）、`ShowSkillTooltip` / `HideSkillTooltip` で呼び出し |
| `UiwSkillText` / `SkillRankUtil` | 共有の解決ヘルパー：名称 / 説明 / カスタムフィールドのテキスト解決（ローカライズ優先）、位階列挙項目の解決。エントリと Tooltip で共用 |

## 表示モードとフィルタ

- **表示モード**：ビューの切り替えボタンが**グリッドリスト**（`UiwSkillGridList`）と**順序リスト**（`UiwSkillOrderList`）を切り替え（2 インスタンスを重ね、いずれかを有効化。どちらも `ScrollRect` 内の仮想スクロールで可視領域のみ描画）。切り替えボタンがないときは設定済みのリストを自動採用。
- **主 / 副グルーピングタブ**（2 つの AND フィルタ条件）：**主タブ**はスキルの主グループタグ、**副タブ**はスキルの副グループタグでフィルタし、**両方を満たす**スキルのみ表示（例：主「戦士」、副「攻撃」→ 戦士の攻撃スキルを表示）。各タブバーは先頭に「すべて」を選択可能（`showAllTab`。「すべて」を選ぶとその条件はフィルタしない）。
  - **タブはスキルが実際に使うタグからのみ生成**：主 / 副タブは、現在のソースのスキルが実際に設定している主 / 副グループタグを取得（データベースのグループタグ順、重複排除）し、スキルのない空タグタブが出ないようにする。
  - **横スクロール可能**：各タブバーは横方向の `ScrollRect`（`Clamped`）—— タグの総幅が収まるときはスクロールせず、画面範囲を超えると横方向にドラッグ / スクロール可能。
- **検索**：スキル名称 / ID でフィルタ。**「すべて」タブを有効にしているとき、検索を入力すると主 / 副グルーピングタブを両方「すべて」に切り替えて**からフィルタ。「すべて」が無効のときは、現在選択中の主 / 副グルーピングタブの範囲内で検索。

## カスタム属性フィールド（Tooltip）

Tooltip は固定フィールド（名称 / 説明 / アイコン）と位階名のほかに、スキルの**カスタム属性フィールド**も表示できます：コンポーネントに `customFieldKeys`（`string[]`、複数キー可）を設定すると、各非空値が 1 行を生成します。`String` は文字列を取り、`Text` はローカライズテキストを取り（取れなければプレーンテキストに退避）、その他の型は汎用表示文字列（`AttributeValue.ToDisplayString()`）を取ります。スキルエントリ（詳細行）も同様に対応します。

## カスタム Inspector

`UiwSkillView` にはカスタム Inspector（`UiwSkillViewEditor`）があります：現在の `source` に応じて、**そのソースが設定を必要とする ID フィールドのみ表示**（`Equipment`→装備グループ ID + `skillRefAttrId`、`Inventory`→倉庫 ID + `skillRefAttrId`、`Character`→キャラクター ID、`InventoryDatabase`→なし）し、無関係なフィールドを隠します。

## プレハブのワンクリック生成

**ウェルカムウィンドウ**の「テストツール-プレハブ生成」で、「スキルシステム」カテゴリはスキルプレハブを個別または一括で生成できます：`PF_UiwSkillCell`（グリッドエントリ）/ `PF_UiwSkillDetail`（リストエントリ）/ `PF_UiwSkillGridList` / `PF_UiwSkillOrderList` / `PF_UiwSkillTooltip` / `PF_UiwSkillView`。`InventoryManager` プレハブはスキルメイン画面をインスタンス化し、スキル Tooltip プレハブをマネージャーに設定します。プレハブ作成と共通コンポーネントは [UI コンポーネントガイド](UIComponentGuide_JA.md) を参照してください。

> ヒント：スキルエントリのプレハブに、既定で無効の「位階背景フレーム」`Image`（`rankBackground` に接続）を付けて初めて位階に応じた背景フレームが表示されます。スキル Tooltip は `InventoryRuntimeManager.skillTooltipPrefab` に設定して初めてホバー時にポップアップします。`Equipment` / `Inventory` ソースはアイテムに `skillRefAttrId` でスキル参照属性を設定しておく必要があり、`Character` ソースは先に `SkillRuntimeManager.Learn` でキャラクターにスキルを習得させる必要があります。
