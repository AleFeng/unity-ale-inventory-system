using static Ale.Toolkit.Editor.ToolkitEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>效果系统面板（<c>Editor/EffectSystem/*.cs</c>）与道具「使用效果」引用列表的英 / 日译表。</summary>
    internal static partial class InventoryEditorL10nTables
    {
        static partial void RegisterEffect()
        {
            // ── 页签 / 名词 ───────────────────────────────────────────────────────
            Add("效果系统",     "Effect System",  "エフェクトシステム");
            Add("效果",         "Effect",         "エフェクト");
            Add("Gameplay 标签", "Gameplay Tags",  "ゲームプレイタグ");
            Add("新效果",       "New Effect",     "新規エフェクト");

            // ── 效果列表（中列）──────────────────────────────────────────────────
            Add("瞬时", "Instant",  "即時");
            Add("持续", "Duration", "持続");
            Add("无限", "Infinite", "無限");
            Add("显示名", "Display Name", "表示名");
            Add("策略",   "Policy",       "ポリシー");
            Add("内容",   "Content",      "内容");
            Add("{0} 修饰 · {1} 执行 · {2} 标签", "{0} mods · {1} execs · {2} tags", "{0} 修飾 · {1} 実行 · {2} タグ");

            // ── 效果 Inspector ────────────────────────────────────────────────────
            Add("请选择或新建一个效果。",
                "Select or create an effect.",
                "エフェクトを選択または新規作成してください。");
            Add("⚠ ID 重复或为空（导出时空 ID 条目将被跳过）",
                "⚠ Duplicate or empty ID (entries with empty ID are skipped on export)",
                "⚠ ID が重複または空です（空 ID のエントリはエクスポート時にスキップされます）");
            Add("效果定义", "Effect Definition", "エフェクト定義");
            Add("定义内的 id / 显示名由上方字段同步；道具以 ID 引用本效果。",
                "The definition's id / display name are synced from the fields above; items reference this effect by ID.",
                "定義内の id / 表示名は上のフィールドから同期されます。アイテムは ID でこのエフェクトを参照します。");
            Add("效果定义编辑暂不可用（无序列化对象）。",
                "Effect definition editing is unavailable (no serialized object).",
                "エフェクト定義の編集は現在利用できません（シリアライズ対象がありません）。");
            Add("效果定义编辑暂不可用。",
                "Effect definition editing is unavailable.",
                "エフェクト定義の編集は現在利用できません。");

            // ── Gameplay 标签面板 ─────────────────────────────────────────────────
            Add("请选择或新建一个 Gameplay 标签。",
                "Select or create a gameplay tag.",
                "ゲームプレイタグを選択または新規作成してください。");
            Add("本库声明的层级标签（如 Status.Buff.Might）。注册数据库时并入 toolkit 标签注册表，供效果的标签字段下拉与「未登记」提示；运行时匹配不依赖此表。",
                "Hierarchical tags declared by this database (e.g. Status.Buff.Might). Merged into the toolkit tag registry when the database is registered, feeding the tag dropdowns and \"unregistered\" hints of effect fields; runtime matching does not depend on it.",
                "このデータベースが宣言する階層タグ（例：Status.Buff.Might）。データベース登録時に toolkit のタグレジストリへ統合され、エフェクトのタグ欄のドロップダウンと「未登録」表示に使われます。ランタイムのマッチングはこの表に依存しません。");
            Add("注释", "Comment", "コメント");
            Add("⚠ 名称不合法：段不能为空，段内不能含空白或 '/'",
                "⚠ Invalid name: segments must not be empty and must not contain whitespace or '/'",
                "⚠ 名前が不正です：セグメントを空にできず、空白や '/' を含められません");
            Add("隐式登记的祖先", "Implicitly registered ancestors", "暗黙的に登録される祖先");

            // ── 道具 / 模板：使用效果引用 ────────────────────────────────────────
            Add("使用时施加的效果", "Effects on Use", "使用時に付与するエフェクト");
            Add("默认使用效果（从模板创建时复制）", "Default Effects on Use (copied on create)", "既定の使用時エフェクト（作成時にコピー）");
            Add("（按序对目标施加；可引用本库效果或其它系统的效果 id）",
                "(Applied to the target in order; may reference effects of this database or effect ids of other systems)",
                "（対象へ順に付与。このデータベースのエフェクト、または他システムのエフェクト id を参照可能）");
            Add("（暂无使用效果）", "(No effects on use)", "（使用時エフェクトはありません）");
            Add("（外部）", " (external)", "（外部）");
            Add("本库未定义；运行时经全局效果注册表（EffectDefinitionRegistry.Default）按 id 解析",
                "Not defined in this database; resolved by id at runtime via the global effect registry (EffectDefinitionRegistry.Default)",
                "このデータベースには未定義。ランタイムにグローバルエフェクトレジストリ（EffectDefinitionRegistry.Default）から id で解決されます");
            Add("添加 id", "Add id", "id を追加");
            Add("（本库无可添加的效果；可在下方输入其它系统的效果 id）",
                "(No effects to add from this database; enter an effect id of another system below)",
                "（このデータベースに追加できるエフェクトがありません。下に他システムのエフェクト id を入力できます）");
        }
    }
}
