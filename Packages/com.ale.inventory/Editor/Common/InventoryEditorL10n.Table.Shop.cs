namespace Ale.Inventory.Editor
{
    /// <summary>商店系统面板与配置绘制器（<c>Editor/ShopSystem/*.cs</c>、<c>ShopConfigDrawer</c>、<c>ShopRefreshScheduleDrawer</c>）的英 / 日译表。</summary>
    public static partial class InventoryEditorL10n
    {
        static partial void RegisterShop()
        {
            // ── 商店模板 / Inspector ──────────────────────────────────────────────
            Add("商店模板",   "Shop Template",     "ショップテンプレート");
            Add("新商店模板", "New Shop Template", "新規ショップテンプレート");
            Add("请选择或新建一个商店模板。",
                "Select or create a shop template.",
                "ショップテンプレートを選択または新規作成してください。");
            Add("请在中间列表选中一个商店。",
                "Select a shop in the middle list.",
                "中央の一覧からショップを選択してください。");
            Add("（该商店暂无自定义属性字段；可在左侧「商店模板」中添加）",
                "(This shop has no custom attribute fields yet; add them in \"Shop Templates\" on the left)",
                "（このショップにはカスタム属性フィールドがありません。左の「ショップテンプレート」で追加できます）");

            // ── 商店列表列头 ──────────────────────────────────────────────────────
            Add("类型",   "Type",           "タイプ");
            Add("商品组", "Product Groups", "商品グループ");

            // ── 商店配置绘制器 ────────────────────────────────────────────────────
            Add("商店类型", "Shop Type",       "ショップ種類");
            Add("交易仓库", "Trade Warehouse", "取引倉庫");
            Add("（未配置；与本商店交易时使用的玩家仓库）",
                "(Not configured; the player warehouse used when trading with this shop)",
                "（未設定。このショップとの取引に使うプレイヤー倉庫）");
            Add("交易功能标签", "Trade Function Tags", "取引機能タグ");
            Add("仅「回收」生效：只回收含勾选标签的道具；不勾选 = 不限制。",
                "Only applies to \"buy-back\": only buys back items carrying the checked tags; unchecked = no restriction.",
                "「買い取り」のみ有効：チェックしたタグを持つアイテムのみ買い取ります。未チェック = 制限なし。");
            Add("勾选后 UI 页签栏会显示「全部」页签（默认选中、显示全部商品）；" +
                "取消后不显示「全部」，默认选中第一个商品组。",
                "When checked, the UI tab bar shows an \"All\" tab (selected by default, shows all products); " +
                "when unchecked, \"All\" is hidden and the first product group is selected by default.",
                "チェックすると UI のタブバーに「すべて」タブが表示されます（既定で選択・全商品を表示）。" +
                "チェックを外すと「すべて」は非表示になり、最初の商品グループが既定で選択されます。");
            Add("整理排序", "Sorting", "整理ソート");
            Add("排序条件（UI 中以下拉菜单显示，玩家选择并可升降序；商店仅按当前选中条件对商品显示排序）：",
                "Sort conditions (shown as a dropdown in the UI; players pick one and can toggle asc/desc; the shop sorts products only by the currently selected condition):",
                "ソート条件（UI ではドロップダウンで表示。プレイヤーが選択し昇順・降順を切り替え可能。ショップは現在選択中の条件でのみ商品を並べ替え表示）：");
            Add("价格属性来源", "Price Attribute Source", "価格属性ソース");
            Add("（道具上 StringIntPair 类型属性：货币ID→价格）",
                "(A StringIntPair attribute on the item: currency ID → price)",
                "（アイテムの StringIntPair 型属性：通貨 ID → 価格）");

            // 商品组与商品
            Add("+ 添加商品组", "+ Add Product Group", "+ 商品グループを追加");
            Add("新商品组",     "New Product Group",   "新規商品グループ");
            Add("组 {0} 名称",  "Group {0} name",      "グループ {0} 名");
            Add("组刷新计划",   "Group refresh schedule", "グループ更新スケジュール");
            Add("商品列表（{0}）", "Product list ({0})", "商品一覧（{0}）");
            Add("+ 添加商品",   "+ Add Product",       "+ 商品を追加");
            Add("搜索",         "Search",              "検索");
            Add("无匹配",       "No match",            "一致なし");
            Add("▶ 搜索匹配",   "▶ Search match",      "▶ 検索一致");
            Add("直接输入道具 ID，回车确认；右侧「选择」可按道具模板分组从道具列表快捷选择，写入此处。无对应道具时红色提示且无法导出。",
                "Type the item ID directly and press Enter; \"Select\" on the right lets you pick from the item list grouped by item template and writes it here. Items with no match are highlighted red and cannot be exported.",
                "アイテム ID を直接入力して Enter で確定。右の「選択」でアイテムテンプレート別に分類されたアイテム一覧から選んでここに書き込めます。対応するアイテムがない場合は赤く表示され、エクスポートできません。");
            Add("选择", "Select", "選択");
            Add("从道具列表快捷选择，结果写入左侧道具ID。",
                "Quick-pick from the item list; the result is written to the Item ID on the left.",
                "アイテム一覧から素早く選択し、結果を左のアイテム ID に書き込みます。");
            Add("⚠ 无效道具 ID（导出将被阻止）",
                "⚠ Invalid item ID (export will be blocked)",
                "⚠ 無効なアイテム ID（エクスポートがブロックされます）");
            Add("每次购买数量", "Quantity per purchase", "1 回あたりの数量");
            Add("每完成一次交易（购买 / 回收）获得或扣除的该道具数量。",
                "The amount of this item gained or deducted each time a trade (buy / buy-back) completes.",
                "取引（購入 / 買い取り）が 1 回完了するたびに得られる、または差し引かれるこのアイテムの数量。");
            Add("价格倍率", "Price Multiplier", "価格倍率");
            Add("在道具基础价格（价格属性来源）上乘以的倍率。1 = 原价；回收常用 <1，如 0.5 = 半价回收。",
                "A multiplier applied to the item's base price (price attribute source). 1 = original price; buy-back often uses <1, e.g. 0.5 = half-price buy-back.",
                "アイテムの基本価格（価格属性ソース）に掛ける倍率。1 = 元の価格。買い取りは <1 が一般的で、例えば 0.5 = 半額買い取り。");
            Add("可交易次数", "Trade Count", "取引可能回数");
            Add("每个刷新周期内该商品可被购买 / 回收的次数。-1 = 无限；刷新周期为「不刷新」时为终身上限。",
                "How many times this product can be bought / bought back within each refresh cycle. -1 = unlimited; when the refresh cycle is \"Never\", it's a lifetime cap.",
                "各更新周期内でこの商品を購入 / 買い取りできる回数。-1 = 無制限。更新周期が「更新しない」の場合は生涯上限。");
            Add("覆盖组刷新", "Override group refresh", "グループ更新を上書き");
            Add("勾选后该商品使用自己的刷新计划，覆盖所属商品组的刷新计划。",
                "When checked, this product uses its own refresh schedule, overriding the schedule of its product group.",
                "チェックすると、この商品は独自の更新スケジュールを使用し、所属する商品グループの更新スケジュールを上書きします。");
            Add("商品刷新计划", "Product refresh schedule", "商品更新スケジュール");
            Add("（未选择）", "(Not selected)", "（未選択）");
            Add("（无模板）", "(No template)",  "（テンプレートなし）");

            // ── 刷新计划绘制器 ────────────────────────────────────────────────────
            Add("周日", "Sun", "日");
            Add("周一", "Mon", "月");
            Add("周二", "Tue", "火");
            Add("周三", "Wed", "水");
            Add("周四", "Thu", "木");
            Add("周五", "Fri", "金");
            Add("周六", "Sat", "土");
            Add("刷新周期", "Refresh Cycle", "更新周期");
            Add("时间类型", "Time Type",     "時間タイプ");
            Add("时间点",   "Time",          "時刻");
            Add("刷新触发的时间点，24 小时制（时 0-23，分 0-59）",
                "The time the refresh triggers, 24-hour format (hour 0-23, minute 0-59).",
                "更新がトリガーされる時刻。24 時間制（時 0-23、分 0-59）。");
            Add("时", "h", "時");
            Add("分", "m", "分");
            Add("星期", "Weekday", "曜日");
            Add("几号（1-31）", "Day (1-31)", "日（1-31）");
            Add("时区 ID（可空）", "Time Zone ID (optional)", "タイムゾーン ID（省略可）");
        }
    }
}
