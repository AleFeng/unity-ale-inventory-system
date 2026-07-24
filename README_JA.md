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
  <a href="./README.md">中文</a> |
  <a href="./README_EN.md">English</a> |
  日本語
</p>

<p align="center">
  📥
  <a href="#-インストール">インストール</a> |
  <a href="#-クイックスタート">クイックスタート</a> |
  <a href="Packages/com.ale.inventory/README.md">詳細ドキュメント</a>
</p>

# Ale Inventory System - インベントリシステム
Ale Inventory System は `Unity` 向けの**データ駆動インベントリプラグイン**で、**アイテム / 倉庫 / ショップ / クラフト / 装備 / スキル** という 6 つのサブシステムを一つのツールチェーンに統合します。  
1 つの `InventoryDatabase` アセットで、6 つのサブシステムすべての**静的な定義データ**（列挙型、機能タグ、アイテムテンプレート、倉庫、ショップ、ブループリント、装備グループ、スキルなど）を一元管理し、**そのまま使えるランタイム UI 一式**（バックパック / ショップ / クラフト / 装備 / スキル画面）と、それぞれの**ランタイムマネージャー**（所持数、取引の進捗、クラフト成果物、装備中アイテム、習得済みスキル、セーブデータはすべてマネージャーが管理）を備えています。  
**デザイナー向け**の設計です：エディタは常に ScriptableObject のみを対象に動作し、Undo / Redo に完全対応。`JSON` / バイナリは**一方向のエクスポート**フォーマットとしてのみ使われます。テキストコンポーネント（TextMeshPro）、ローカライズ（Unity Localization）、アセット読み込み（Addressables）はいずれも**コンパイルマクロで任意に有効化**でき、パッケージ本体はハード依存を一切持ち込みません。

![スクリーンショット](./Packages/com.ale.inventory/Docs~/Images/image.png)

## 📜 目次
- [Ale Inventory System - インベントリシステム](#ale-inventory-system---インベントリシステム)
  - [📜 目次](#-目次)
  - [概要](#概要)
    - [主な特徴](#主な特徴)
    - [6 つのサブシステム](#6-つのサブシステム)
  - [💻 動作環境](#-動作環境)
  - [📦 インストール](#-インストール)
    - [UPM を使う（推奨）](#upm-を使う推奨)
    - [デモ Sample のインポート（任意）](#デモ-sample-のインポート任意)
    - [その他の方法](#その他の方法)
  - [🚀 クイックスタート](#-クイックスタート)
    - [1. データファイルを作成](#1-データファイルを作成)
    - [2. エディタを開いて設定](#2-エディタを開いて設定)
    - [3. エクスポート（任意）](#3-エクスポート任意)
    - [4. ランタイムのセットアップ](#4-ランタイムのセットアップ)
    - [5. ワンクリック Demo](#5-ワンクリック-demo)
  - [🖥️ ウェルカムウィンドウ](#️-ウェルカムウィンドウ)
  - [🧩 オプションのマクロ](#-オプションのマクロ)
  - [📖 ドキュメント](#-ドキュメント)
  - [📁 ディレクトリ構成](#-ディレクトリ構成)
  - [📋 ToDo](#-todo)
  - [📄 ライセンス](#-ライセンス)

## 概要
ほとんどのゲームは「アイテム + バックパック + ショップ + クラフト + 装備 + スキル」というデータ体系を必要としますが、これらのシステムは散り散りで互いに密結合になりがちで、毎回作り直すコストは高くつきます。Ale Inventory System はそれらを**一つのデータアセット**と**一つのエディタ**の下にまとめます：

1. **一元的な設定** —— 1 つの `InventoryDatabase` が 6 つのサブシステムすべての静的定義を保持します。エディタは「上部のシステムタブ + 3 カラムレイアウト（定義設定 / エントリ一覧 / 詳細 Inspector）」で、テンプレートフィルタ、検索、ドラッグ並べ替え、キーボード操作、リアルタイムの ID 重複チェックに対応します。
2. **柔軟な属性** —— アイテムや各設定エントリのフィールドは、**柔軟な属性システム**（Bool / Int / Float / String / Text / Vector / Color / Enum / Sprite / Prefab / AudioClip / AnimationCurve… いずれも配列形式に対応）で表現されます。機能タグ単位でまとめて追加・削除でき、コードを触らずにデータ構造を拡張できます。
3. **ランタイムはすぐ使える** —— 各サブシステムには軽量なランタイムマネージャーと仮想スクロール UI コンポーネントが付属し、検索、追加・削除、整理、取引、クラフト、装備、スキル習得、セーブ / ロードのいずれにも既製の API が用意されています。
4. **ハード依存ゼロ** —— TextMeshPro / Localization / Addressables はすべてコンパイルマクロで任意に有効化でき、無効のままでもプラグインは問題なく動作します。

![スクリーンショット](./Packages/com.ale.inventory/Docs~/Images/image-1.png)

### 主な特徴
| 特徴 | 説明 |
| --- | --- |
| 単一アセットで一元管理 | 1 つの `InventoryDatabase` が 6 サブシステムの静的データをすべて集約。エディタは ScriptableObject のみを対象に動作し、Undo / Redo に完全対応。 |
| 柔軟な属性システム | 20 種類以上のフィールド型（いずれも配列形式あり）：Bool / Int / Float / String / **Text**（プレーンテキストのフォールバック + 任意のローカライズ参照）/ Vector2〜4 / VectorInt / Color / Enum / StringIntPair / EnumIntPair / Sprite / Texture / Prefab / Material / AudioClip / AnimationClip / AnimationCurve / PhysicsMaterial。 |
| カスタム列挙 + 機能タグ | 列挙値はシステムが自動採番し、再利用されず、ドラッグで並べ替え可能。機能タグは一組の属性フィールドを定義し、タグの増減でアイテム側の対応フィールドも自動で増減、タグはテンプレートに固定できます。 |
| 6 サブシステム一体化 | アイテム / 倉庫 / ショップ / クラフト / 装備 / スキルが同じデータと属性システムを共有し、エントリ同士が相互参照します（例：装備アイテムに紐づくスキル、アイテム属性から取得するショップ価格）。 |
| 統一された仮想スクロール UI | グリッドも順序リストも仮想スクロール（オブジェクトプール + 可視セルのみ描画）。差分による増分リフレッシュ、生成レート制限（`spawnPerSecond`）、セルごとのフェードインで大量のエントリでも快適です。 |
| ランタイムマネージャー | `InventoryDataManager`（クエリ）に加え、倉庫 / ショップ / クラフト / 装備 / スキルそれぞれの専用ランタイムマネージャー。装備・スキルの状態やショップの進捗はいずれもセーブ可能です。 |
| 一方向エクスポート | `InventoryDtoMapper` → JSON / バイナリ。**データベースの設定データを全て網羅**（6 サブシステムの 20 リスト）。オブジェクト参照は AssetGUID として保持され、Addressables 経由で非同期読み込みも可能です。 |
| 3 つのオプションマクロ | TextMeshPro（`IS_TMP`）/ Unity Localization（`IS_LOCALIZATION`）/ Unity Addressables（`IS_ADDRESSABLE`）。いずれもウェルカムウィンドウからワンクリックで切り替え可能（対応パッケージの導入有無も検出）。パッケージ本体はハード依存ゼロ。 |
| ローカライズツール | `InventoryDatabase` 向けの多言語テーブルをワンクリックで生成 / 関連付けし、データベース内のすべての `Text` フィールドを走査してキーを自動生成、エントリへ書き戻します（プログレスバー + ログ + キャンセル対応）。 |
| ウェルカムウィンドウのウィザード | データ作成、エディタ / ツールウィンドウの起動、マクロ切り替え、そして「完全に動作するサンプルをワンクリック生成」（データベース + 全 UI プレハブ + マネージャー）を一箇所にまとめた入口です。 |
| エディタ UI の 3 言語対応 | ウェルカムウィンドウから **中文 / English / 日本語** をワンクリックで切り替え。ウェルカムウィンドウと `Inventory Editor` 設定エディタ（6 サブシステムの全パネル）が一括で切り替わります。選択は永続化され、ランタイムのコンテンツローカライズとは無関係です。 |

### 6 つのサブシステム
| サブシステム | 設定する内容 | ランタイムマネージャー |
| --- | --- | --- |
| **アイテム** | 列挙型、機能タグ、アイテムテンプレート、アイテム + 柔軟な属性 | `InventoryDataManager`（クエリ） |
| **倉庫** | 倉庫テンプレート、倉庫、容量 / 重量 / タグ制限、整理ソート | `InventoryRuntimeManager`（スロット状態 + セーブ） |
| **ショップ** | ショップテンプレート、ショップ、商品グループ、価格ソース、更新スケジュール | `ShopRuntimeManager`（取引 + 進捗セーブ） |
| **クラフト** | グループタグ、ブループリントテンプレート、ブループリント（レシピ）、クラフト倉庫 | `CraftingRuntimeManager`（消費 → 産出） |
| **装備** | グループタグ、装備グループテンプレート、装備グループ（スロットリスト / 装備スロット / アイテム制限 / 属性ボーナス） | `EquipmentRuntimeManager`（装備 / 解除 + ボーナス + セーブ） |
| **スキル** | グループタグ、スキルテンプレート、スキル（種類 / 効果 / 数値 / 位階をカスタム属性で表現） | `SkillRuntimeManager`（習得状態 + セーブ）+ `SkillCollector`（4 ソース収集） |

> 各サブシステムの詳しい設定とランタイムの説明は[ドキュメント](#-ドキュメント)を参照してください。

## 💻 動作環境
- `Unity 2022.3` 以降（`package.json` が宣言する最低バージョン。本リポジトリは `Unity 6000.3` で開発・保守しています）。
- コアプラグインは純粋な C# で、**ハード依存を一切持ち込みません**。TextMeshPro / Unity Localization / Unity Addressables はすべてコンパイルマクロで**任意**に有効化できます（[オプションのマクロ](#-オプションのマクロ)を参照）。
- `IS_TMP` が無効の場合、UI のテキストコンポーネントは `UnityEngine.UI.Text` にフォールバックし、プラグインは通常どおり動作します。

## 📦 インストール
### UPM を使う（推奨）
`Window > Package Manager` → 左上の `+` → `Install package from git URL...` → 次を貼り付け：

```
https://github.com/AleFeng/unity-ale-inventory-system.git?path=/Packages/com.ale.inventory
```

これで `main` の最新コミットが入ります。**バージョンを固定するには、URL の末尾に `#<tag>` を付けます**（必ず `?path=` の後ろに）：

```
https://github.com/AleFeng/unity-ale-inventory-system.git?path=/Packages/com.ale.inventory#1.7.0
```

利用可能なタグは [Releases](https://github.com/AleFeng/unity-ale-inventory-system/releases) を参照してください。

### デモ Sample のインポート（任意）
インストール後、Package Manager でパッケージを選択 → `Samples` → **Inventory System Demo**（`InventoryDatabase` アセット + マネージャープレハブ + UI サンプルシーン）をインポートすれば、そのまま Play で体験できます。あるいは[ウェルカムウィンドウ](#️-ウェルカムウィンドウ)の「ワンクリック生成」ウィザードで、その場で一式を生成することもできます。

### その他の方法
リポジトリをダウンロードし、`Packages/com.ale.inventory` フォルダをまるごとプロジェクトの **`Packages/` ディレクトリ**（`Assets/` ではありません）にコピーする方法もあります。Unity が自動的にローカルパッケージとして認識します。

インストールが完了すると、メニューバーに **`Tools → Inventory System`** が現れ、Unity セッションで初めてプロジェクトを開いたときには**ウェルカムウィンドウ**も自動で表示されます。

## 🚀 クイックスタート
以下は最短で使い始める手順です。**サブシステムの詳しい設定と API については[ドキュメント](#-ドキュメント)を参照してください**。

### 1. データファイルを作成
```
Project パネルで右クリック > Create > Inventory System > Inventory Database
```
（またはウェルカムウィンドウの「新規データファイルを作成」から。先にウェルカムウィンドウで「データテンプレート」を設定しておくと、作成時にそこから全データをディープコピーできます。）

### 2. エディタを開いて設定
- `.asset` を選択し、Inspector 上部の「Inventory Editor で編集」をクリック。または、メニューの `Tools > Inventory System > Inventory Editor`。
- エディタは**上部のシステムタブ + 3 カラムレイアウト**（左：定義設定 / 中：エントリ一覧 / 右：詳細 Inspector）です。「アイテム / 倉庫 / ショップ / クラフト / 装備 / スキル」の各タブを順に設定します。中央のエントリ一覧はテンプレートフィルタ、検索、ドラッグ並べ替え、↑ / ↓ キーボード操作に対応します。

### 3. エクスポート（任意）
ツールバーの「JSON エクスポート」または「バイナリエクスポート」を使います（空でない ID 重複がある間はボタンが無効。ID が空白のエントリはエクスポート時に自動でスキップされます）。エディタは常に ScriptableObject 上で動作し、エクスポートは一方向のフォーマットです。

> **1.6.0（フォーマット v6）** より、エクスポートはデータベースの設定データを**すべて**網羅します —— 6 サブシステムの 20 リスト全部です。それ以前はアイテムシステムの 4 項目のみで、残りは黙って捨てられていました。

### 4. ランタイムのセットアップ
シーンに GameObject を作成し、`InventoryRuntimeManager` コンポーネントを追加して、`.asset` を `databases` 配列にドラッグします。ゲーム開始時にデータベースが自動登録され、各倉庫が空の状態で初期化されます。

```csharp
using Ale.Inventory.Runtime;

// 静的データのクエリ
Item item = InventoryDataManager.Instance.GetItem("sword_01");

// ランタイムで倉庫を操作
InventoryRuntimeManager.Instance.TryAddItem("backpack", "sword_01", 1);
bool has = InventoryRuntimeManager.Instance.HasItem("backpack", "sword_01");

// セーブ / ロード
var saveData = InventoryRuntimeManager.Instance.GetSaveData();
InventoryRuntimeManager.Instance.LoadSaveData(saveData);

// ニューゲーム：ランタイム状態をすべてクリア
InventoryRuntimeManager.Instance.ResetAll();
```

### 5. ワンクリック Demo
**ウェルカムウィンドウ**で「テストツール-プレハブ生成 → すべて生成」を展開すると、完全に動作するサンプルをワンクリックで生成できます（データベース + 全 UI プレハブ + バックパック / ショップ / クラフト / 装備 / スキル画面 + マネージャー）。

## 🖥️ ウェルカムウィンドウ
プラグインの統一入口パネルで、「データ作成 / エディタ起動 / ドキュメント表示 / サンプル生成 / マクロ切り替え」といったよく使う操作を集約しています。Unity セッションで最初の一度は自動的に表示され、いつでも手動で開けます：

```
Tools > Inventory System > Welcome Window
```

![スクリーンショット](./Packages/com.ale.inventory/Docs~/Images/image-1.png)

ヘッダーのサブタイトル下に「**中文 / English / 日本語**」の 3 ボタンが中央揃えで並び、**ウェルカムウィンドウと `Inventory Editor` 設定エディタ**の UI 言語を切り替えられます（選択は `EditorPrefs` で永続化されセッションをまたいで保持。エディタ UI の文言のみに影響し、ランタイムのコンテンツローカライズとは無関係です）。

ヘッダーの下は上から順に 5 つの領域に分かれています：**多言語設定**（「列挙値」チェック：列挙ドロップダウンの表示名も言語に合わせて切り替えるか。既定は未チェック）、**クイック操作**（データ作成 / 各エディタ・ツールウィンドウの起動 / サンプルプレハブのワンクリック生成）、**データテンプレート**（新規作成のひな型となる `InventoryDatabase` を指定）、**プラグインサポート**（下記 3 つのオプションマクロをワンクリックで切り替え）、**起動時に自動表示**（セッションごとに自動で開くかどうか）。

## 🧩 オプションのマクロ
3 つのマクロはいずれも**ウェルカムウィンドウ**の「プラグインサポート」領域からワンクリックで切り替えでき、対応パッケージの導入有無もリアルタイムに検出します（未導入のマクロにチェックを入れると確認ダイアログが表示されます）：

| 切り替え | マクロ | 効果 |
| --- | --- | --- |
| TextMeshPro | `IS_TMP` | 有効にすると UI テキストコンポーネントが `TMP_Text` を使用し、無効時は `UnityEngine.UI.Text` を使用。「デフォルトフォント」を設定してウィザード生成プレハブに適用できます。 |
| Unity Localization | `IS_LOCALIZATION` | 有効にすると `Text` フィールドにローカライズ参照（テーブル + エントリ）を持たせられます。「ローカライズツールウィンドウ」と組み合わせてテーブル作成 / キー生成をワンクリックで行い、多言語に対応します。 |
| Unity Addressables | `IS_ADDRESSABLE` | 有効にするとランタイムアセットが Addressables 経由でオンデマンドに非同期読み込みされ、参照カウントで自動アンロード。エクスポート時には参照されたアセットが自動で登録されます。 |

> マクロを切り替えたら、反映のために Unity の再コンパイルを待ってください。

## 📖 ドキュメント
この README は概要とクイックスタートです。**完全な利用ガイド**——各サブシステムの設定詳細、ランタイム API、柔軟な属性のリファレンス、UI コンポーネント、アーキテクチャの説明など——はパッケージ内ドキュメントにあります：

👉 **[Packages/com.ale.inventory/README.md](Packages/com.ale.inventory/README.md)**

サブシステムとリファレンスのドキュメント（`Packages/com.ale.inventory/Docs~/` 以下）：

- [アイテムシステム](Packages/com.ale.inventory/Docs~/ItemSystem_JA.md) — 列挙型 / 機能タグ / アイテムテンプレート / アイテム / 柔軟な属性
- [倉庫システム](Packages/com.ale.inventory/Docs~/WarehouseSystem_JA.md) — 倉庫テンプレート / 倉庫 / 整理ソート / ランタイム API / セーブデータ
- [ショップシステム](Packages/com.ale.inventory/Docs~/ShopSystem_JA.md) — ショップ種類 / 価格ソース / 商品グループ / 更新スケジュール / 取引 API
- [クラフトシステム](Packages/com.ale.inventory/Docs~/CraftingSystem_JA.md) — グループタグ / ブループリントテンプレート / ブループリントのレシピ / クラフト倉庫 / クラフト API
- [装備システム](Packages/com.ale.inventory/Docs~/EquipmentSystem_JA.md) — グループタグ / 装備グループテンプレート / スロットリスト / 装備スロット / アイテム制限 / 属性ボーナス / 装備 API
- [スキルシステム](Packages/com.ale.inventory/Docs~/SkillSystem_JA.md) — グループタグ / スキルテンプレート / スキル / アイテムのスキル参照 / 位階の列挙 / 4 つのソース / 習得スキル API
- [属性システム](Packages/com.ale.inventory/Docs~/AttributeSystem_JA.md) — フィールド型リファレンス、`AttributeValue` の取得 / 表示 / ソート比較
- [UI コンポーネントガイド](Packages/com.ale.inventory/Docs~/UIComponentGuide_JA.md) — UI コンポーネント、プレハブ作成、機能マクロ、デモウィザード
- [アーキテクチャ](Packages/com.ale.inventory/Docs~/Architecture_JA.md) — 設計目標、データフロー、エディタ・ランタイムのアーキテクチャ、拡張ガイド

## 📁 ディレクトリ構成
```
Packages/com.ale.inventory/          ← パッケージルート
├── package.json  CHANGELOG.md  LICENSE.md  README.md   ← 詳細な利用ドキュメント
├── Runtime/
│   ├── Data/            データモデル（Item / Inventory / Shop / Crafting* / AttributeValue など）
│   ├── Manager/         DataManager / 倉庫 / ショップ / クラフト / 装備 / スキルのランタイムマネージャー + SkillCollector
│   ├── Serialization/   DTO + JSON / バイナリのシリアライズ
│   ├── Assets/          アセット読み込みの抽象化（直接読み込み）
│   ├── Addressables/    Addressables アセット読み込みサポート
│   ├── Localization/    TMP テキスト / フォントのローカライズイベント
│   └── UI/              ランタイム UI コンポーネント（Item / ItemList / Tab / Tool / View / Common）
├── Editor/
│   ├── ItemSystem/ InventorySystem/ ShopSystem/ CraftingSystem/ EquipmentSystem/ SkillSystem/   ← 6 つのシステムパネル
│   ├── Common/         共通の属性 / 設定ドロワー + ツールウィンドウ基底クラス
│   ├── Addressables/   Addressables アセット参照の移行ツールウィンドウ
│   ├── Localization/   ローカライズツールウィンドウ（テーブル作成 / キー生成）
│   ├── Create/         データファイル作成メニュー
│   └── DemoWizard/     テストデータとプレハブのワンクリック生成
├── Docs~/              詳細ドキュメント
└── Samples~/Demo/      デモ Sample（データベース + マネージャープレハブ + UI サンプルシーン）
```

## 📋 ToDo
- ショップ「等価交換」種類の完全な実装（現状はプレースホルダー）。
- サンプルシーンとランタイムのユースケースの追加。

## 📄 ライセンス
本プロジェクトは [MIT License](LICENSE) の下で公開されており、商用・非商用を問わず自由に利用できます。
