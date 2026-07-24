namespace Ale.Inventory.Editor
{
    /// <summary>制作系统面板与配置绘制器（<c>Editor/CraftingSystem/*.cs</c>、<c>CraftingConfigDrawer</c>）的英 / 日译表。</summary>
    public static partial class InventoryEditorL10n
    {
        static partial void RegisterCrafting()
        {
            // ── 蓝图模板 ──────────────────────────────────────────────────────────
            Add("蓝图模板",   "Blueprint Template",     "ブループリントテンプレート");
            Add("新蓝图模板", "New Blueprint Template", "新規ブループリントテンプレート");
            Add("请选择或新建一个蓝图模板。",
                "Select or create a blueprint template.",
                "ブループリントテンプレートを選択または新規作成してください。");
            Add("此模板下所有蓝图在 UI 列表中按「整理列表」的配置与优先级排序。",
                "All blueprints under this template are sorted in the UI list by the \"sort list\" config and priority.",
                "このテンプレート配下のすべてのブループリントは、UI 一覧で「整理リスト」の設定と優先度に従って並べ替えられます。");

            // ── 蓝图列表列头 ──────────────────────────────────────────────────────
            Add("主分组", "Main Group", "主グループ");
            Add("产出",   "Output",     "産出");

            // ── 制作配置绘制器 ────────────────────────────────────────────────────
            Add("制作参数", "Crafting Params", "クラフトパラメータ");
            Add("制作时间(秒)", "Craft Time (s)", "クラフト時間（秒）");
            Add("制作一次需要的时间，进度条按此推进。",
                "The time one craft takes; the progress bar advances accordingly.",
                "1 回のクラフトに必要な時間。プログレスバーはこれに従って進みます。");
            Add("连续制作次数", "Continuous Craft Count", "連続クラフト回数");
            Add("单次「制作」动作可连续重复的次数上限；与材料决定的可制作次数取小。-1 = 无限。",
                "The max number of times a single \"craft\" action can repeat continuously; the smaller of this and the count allowed by materials is used. -1 = unlimited.",
                "1 回の「クラフト」操作で連続して繰り返せる回数の上限。材料で決まる回数とのうち小さい方が使われます。-1 = 無制限。");
            Add("（-1 = 无限）", "(-1 = unlimited)", "（-1 = 無制限）");
            Add("制作仓库", "Crafting Warehouse", "クラフト倉庫");
            Add("按上下顺序作为优先级：先从第一个仓库消耗材料 / 放置产出，不足时顺延。",
                "Order = priority: materials are consumed / outputs placed from the first warehouse first, spilling over when insufficient.",
                "上下の順序が優先度：最初の倉庫から材料を消費／産出を配置し、不足時は次へ回します。");
            Add("（未配置；消耗材料的来源与产出落点仓库）",
                "(Not configured; the warehouses materials come from and outputs go to)",
                "（未設定。材料の消費元と産出の配置先となる倉庫）");
            Add("属性字段显示", "Attribute Field Display", "属性フィールド表示");
            Add("+ 添加", "+ Add", "+ 追加");
            Add("在蓝图条目 / 详情上显示主产出道具的属性值（形如「Label 值」）。",
                "Show the main output item's attribute value on the blueprint entry / detail (as \"Label value\").",
                "ブループリントの項目／詳細に、主産出アイテムの属性値を表示します（「Label 値」の形式）。");
            Add("由蓝图模板配置，蓝图条目不可修改（仅展示）。",
                "Configured on the blueprint template; not editable on the blueprint entry (display only).",
                "ブループリントテンプレートで設定。ブループリント項目では変更不可（表示のみ）。");
            Add("（无标签）",   "(No label)",              "（ラベルなし）");
            Add("（未选属性）", "(No attribute selected)", "（属性未選択）");
            Add("（无来源模板：为蓝图指定模板后，可在该模板中配置以上项）",
                "(No source template: assign a template to the blueprint, then configure the above on that template)",
                "（元テンプレートなし：ブループリントにテンプレートを割り当てると、そのテンプレートで上記を設定できます）");
            Add("⚠ 来源模板「{0}」不存在",
                "⚠ Source template \"{0}\" does not exist",
                "⚠ 元テンプレート「{0}」が存在しません");
            Add("（已删除）", "(deleted)", "（削除済み）");

            // ── 蓝图 Inspector ────────────────────────────────────────────────────
            Add("请在中间列表选中一个蓝图。",
                "Select a blueprint in the middle list.",
                "中央の一覧からブループリントを選択してください。");
            Add("产出道具列表", "Output Item List", "産出アイテム一覧");
            Add("消耗道具列表", "Input Item List",  "消費アイテム一覧");
            Add("（暂无分组标签；请在左侧「分组标签」中添加）",
                "(No group tags yet; add them in \"Group Tags\" on the left)",
                "（グループタグがありません。左の「グループタグ」で追加してください）");
            Add("主分组标签", "Main Group Tag",       "主グループタグ");
            Add("副分组标签", "Secondary Group Tags", "副グループタグ");
            Add("（未添加）", "(None added)",         "（未追加）");
            Add("（无可添加的分组标签）",
                "(No group tags available to add)",
                "（追加できるグループタグがありません）");
            Add("第 1 项为主产出（用于 UI 显示），其余为副产出；拖拽左侧句柄调整顺序。",
                "The 1st item is the main output (used for UI display); the rest are secondary; drag the left handle to reorder.",
                "1 番目が主産出（UI 表示に使用）、残りは副産出。左のハンドルをドラッグして順序を調整します。");
            Add("★ 主产出", "★ Main Output", "★ 主産出");
            Add("直接输入道具 ID，回车确认；右侧「选择」可从道具列表快捷选择，写入此处。",
                "Type the item ID directly and press Enter; \"Select\" on the right lets you pick from the item list and writes it here.",
                "アイテム ID を直接入力して Enter で確定。右の「選択」でアイテム一覧から選んでここに書き込めます。");
            Add("数量", "Count", "数量");
            Add("制作一次获得的该道具数量。",
                "The amount of this item gained per craft.",
                "1 回のクラフトで得られるこのアイテムの数量。");
            Add("制作一次需要的该道具数量。",
                "The amount of this item required per craft.",
                "1 回のクラフトに必要なこのアイテムの数量。");
        }
    }
}
