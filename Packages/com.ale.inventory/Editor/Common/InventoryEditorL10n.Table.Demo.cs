namespace Ale.Inventory.Editor
{
    /// <summary>
    /// Welcome 窗口「预制体生成」区的 Demo 目录（<c>InventoryDemoWizard</c> 的分类名、生成项显示名）
    /// 与生成流程各对话框的英 / 日译表。
    ///
    /// <para>生成项显示名为「中文描述 + 预制体资产名」，故以 <c>"描述 {0}"</c> 为键、
    /// 资产名作参数——资产名恒为英文，不参与翻译。</para>
    /// </summary>
    public static partial class InventoryEditorL10n
    {
        static partial void RegisterDemo()
        {
            // ── 分类（其余六项与系统页签同名，已在框架译表登记）──────────────────
            Add("通用（数据库 / 管理器）",
                "Common (Database / Manager)",
                "共通（データベース / マネージャー）");

            // ── 通用 ──────────────────────────────────────────────────────────────
            Add("数据库 {0}", "Database {0}", "データベース {0}");
            Add("管理器 {0}", "Manager {0}",  "マネージャー {0}");

            // ── 道具系统 ──────────────────────────────────────────────────────────
            Add("简易格子 {0}",     "Simple cell {0}",       "シンプルセル {0}");
            Add("网格格子 {0}",     "Grid cell {0}",         "グリッドセル {0}");
            Add("功能标签 {0}",     "Function tag {0}",      "機能タグ {0}");
            Add("价格货币 {0}",     "Price / currency {0}",  "価格・通貨 {0}");
            Add("列表格子 {0}",     "List cell {0}",         "リストセル {0}");
            Add("道具悬停弹窗 {0}", "Item hover tooltip {0}", "アイテムホバーポップアップ {0}");

            // ── 仓库系统 ──────────────────────────────────────────────────────────
            Add("仓库页签 {0}", "Warehouse tab {0}",   "倉庫タブ {0}");
            Add("过滤按钮 {0}", "Filter button {0}",   "フィルタボタン {0}");
            Add("折叠页签 {0}", "Collapsible tab {0}", "折りたたみタブ {0}");
            Add("列表面板 {0}", "List panel {0}",      "リストパネル {0}");
            Add("网格面板 {0}", "Grid panel {0}",      "グリッドパネル {0}");
            Add("仓库面板 {0}", "Warehouse panel {0}", "倉庫パネル {0}");

            // ── 商店系统 ──────────────────────────────────────────────────────────
            Add("商店组页签 {0}",   "Shop group tab {0}",     "ショップグループタブ {0}");
            Add("数量计数器 {0}",   "Quantity counter {0}",   "数量カウンター {0}");
            Add("商店商品条目 {0}", "Shop product entry {0}", "ショップ商品エントリ {0}");
            Add("商店面板 {0}",     "Shop panel {0}",         "ショップパネル {0}");

            // ── 制作系统 ──────────────────────────────────────────────────────────
            Add("制作消耗行 {0}", "Crafting input row {0}", "クラフト消費行 {0}");
            Add("蓝图条目 {0}",   "Blueprint entry {0}",    "ブループリントエントリ {0}");
            Add("蓝图列表 {0}",   "Blueprint list {0}",     "ブループリント一覧 {0}");
            Add("制作主界面 {0}", "Crafting main view {0}", "クラフトメイン画面 {0}");

            // ── 装备系统（「装备槽 {0}」「槽位列表 {0}」与装备译表共用）───────────
            Add("候选道具格子 {0}", "Candidate item cell {0}",   "候補アイテムセル {0}");
            Add("属性加成条目 {0}", "Attribute bonus entry {0}", "属性ボーナスエントリ {0}");
            Add("候选道具列表 {0}", "Candidate item list {0}",   "候補アイテム一覧 {0}");
            Add("装备组面板 {0}",   "Equipment group panel {0}", "装備グループパネル {0}");
            Add("属性加成面板 {0}", "Attribute bonus panel {0}", "属性ボーナスパネル {0}");
            Add("装备选择面板 {0}", "Equipment select panel {0}", "装備選択パネル {0}");
            Add("装备主界面 {0}",   "Equipment main view {0}",   "装備メイン画面 {0}");

            // ── 技能系统 ──────────────────────────────────────────────────────────
            Add("技能网格条目 {0}", "Skill grid entry {0}",    "スキルグリッドエントリ {0}");
            Add("技能列表条目 {0}", "Skill list entry {0}",    "スキルリストエントリ {0}");
            Add("技能网格列表 {0}", "Skill grid list {0}",     "スキルグリッド一覧 {0}");
            Add("技能顺序列表 {0}", "Skill ordered list {0}",  "スキル順序一覧 {0}");
            Add("技能悬停弹窗 {0}", "Skill hover tooltip {0}", "スキルホバーポップアップ {0}");
            Add("技能主界面 {0}",   "Skill main view {0}",     "スキルメイン画面 {0}");

            // ── 生成流程对话框 ────────────────────────────────────────────────────
            Add("生成完成", "Generation Complete", "生成完了");
            Add("已生成全部资产：\n{0}/\n{1}/\n\n" +
                "将 InventoryManager.prefab 拖入场景，点击 Play 即可验证。",
                "All assets generated:\n{0}/\n{1}/\n\n" +
                "Drag InventoryManager.prefab into the scene and click Play to verify.",
                "すべてのアセットを生成しました：\n{0}/\n{1}/\n\n" +
                "InventoryManager.prefab をシーンにドラッグし、Play をクリックして確認してください。");
            Add("依赖提示", "Dependencies", "依存関係の確認");
            Add("「{0}」依赖以下子项：\n\n{1}\n\n是否一并生成这些依赖？",
                "\"{0}\" depends on the following:\n\n{1}\n\nGenerate these dependencies as well?",
                "「{0}」は以下に依存しています：\n\n{1}\n\nこれらの依存も一緒に生成しますか？");
            Add("一并生成",   "Generate All", "一緒に生成");
            Add("仅生成此项", "This Only",    "この項目のみ");
            Add("覆盖确认", "Confirm Overwrite", "上書きの確認");
            Add("以下资产已存在，将被覆盖：\n\n{0}",
                "The following assets already exist and will be overwritten:\n\n{0}",
                "以下のアセットは既に存在し、上書きされます：\n\n{0}");
            Add("覆盖", "Overwrite", "上書き");
            Add("生成测试资产", "Generating Test Assets", "テストアセットを生成中");
            Add("保存并刷新资产数据库...",
                "Saving and refreshing the asset database...",
                "アセットデータベースを保存・更新中...");
        }
    }
}
