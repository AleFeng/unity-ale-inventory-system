using static Ale.Toolkit.Editor.ToolkitEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 效果相关的英 / 日译表（1.13.0 起效果在 toolkit 效果库配置，本表只剩：道具 / 模板的「使用效果」引用列表文案、
    /// legacy 迁移窗口与资产 Inspector 提示）。引用列表本体（目录菜单 / 打开 / 未找到 / 添加 id）的译文由 toolkit 提供。
    /// </summary>
    internal static partial class InventoryEditorL10nTables
    {
        static partial void RegisterEffect()
        {
            // ── 道具 / 模板：使用效果引用 ────────────────────────────────────────
            Add("使用时施加的效果", "Effects on Use", "使用時に付与するエフェクト");
            Add("默认使用效果（从模板创建时复制）", "Default Effects on Use (copied on create)", "既定の使用時エフェクト（作成時にコピー）");
            Add("（按序对目标施加；引用 toolkit 效果库中的效果 id——「+」从目录选择，「打开」跳转到 Effect Editor）",
                "(Applied to the target in order; references effect ids of the toolkit effect library — \"+\" picks from the catalog, \"Open\" jumps to the Effect Editor)",
                "（対象へ順に付与。toolkit エフェクトライブラリのエフェクト id を参照——「+」でカタログから選択、「開く」で Effect Editor へ移動）");

            // ── legacy 迁移（1.12.0 → 1.13.0）────────────────────────────────────
            Add("迁移效果到 Effect Database", "Migrate Effects to Effect Database", "エフェクトを Effect Database へ移行");
            Add("1.13.0 起效果与 Gameplay 标签由 toolkit 效果库（EffectDatabase）承载，所有上层系统共用；库存库只保留道具的效果 id 引用。本工具把 1.12.0 存在库存库里的效果 / 标签逐条搬入目标效果库：定义原样保留（显示名取定义的 displayName，不挂模板）；目标库已有同 id 的效果跳过并报告（不覆盖，条目留在 legacy 字段）；标签按名称去重并入。",
                "Since 1.13.0 effects and gameplay tags live in the toolkit effect library (EffectDatabase) shared by every upper system; the inventory database keeps only the items' effect-id references. This tool moves the effects / tags stored in the inventory database by 1.12.0 into the target effect library one by one: definitions are kept as they are (display name taken from the definition's displayName, no template); effects whose id already exists in the target are skipped and reported (not overwritten, kept in the legacy field); tags are merged with name de-duplication.",
                "1.13.0 以降、エフェクトとゲームプレイタグは全上位システムが共用する toolkit エフェクトライブラリ（EffectDatabase）が保持し、インベントリデータベースはアイテムのエフェクト id 参照のみを保持します。本ツールは 1.12.0 でインベントリデータベースに保存されたエフェクト / タグを対象のエフェクトライブラリへ 1 件ずつ移します：定義はそのまま保持（表示名は定義の displayName、テンプレートなし）。対象に同 id が既にあるエフェクトはスキップして報告（上書きせず legacy フィールドに残す）。タグは名前で重複排除して統合します。");
            Add("来源（库存库）", "Source (inventory database)", "移行元（インベントリデータベース）");
            Add("目标（效果库）", "Target (effect library)", "移行先（エフェクトライブラリ）");
            Add("新建…", "New…", "新規…");
            Add("迁移", "Migrate", "移行");
            Add("结果", "Result", "結果");
            Add("在 Effect Editor 中打开目标库", "Open target in Effect Editor", "移行先を Effect Editor で開く");
            Add("legacy 效果 {0} 条，legacy Gameplay 标签 {1} 条", "{0} legacy effects, {1} legacy gameplay tags", "legacy エフェクト {0} 件、legacy ゲームプレイタグ {1} 件");
            Add("来源库没有 legacy 效果 / 标签数据，无需迁移。", "The source database has no legacy effect / tag data; nothing to migrate.", "移行元に legacy エフェクト / タグはありません。移行は不要です。");
            Add("存在同 id 冲突：请在目标库中重命名 / 删除对应效果后重跑（冲突条目仍留在来源库的 legacy 字段）。",
                "Id conflicts found: rename / delete the corresponding effects in the target and run again (conflicting entries remain in the source's legacy field).",
                "同 id の競合があります。移行先で該当エフェクトを改名 / 削除して再実行してください（競合エントリは移行元の legacy フィールドに残ります）。");
            Add("本资产仍含 1.12.0 存于库存库的效果 / Gameplay 标签（legacy）。1.13.0 起效果由 toolkit 效果库（EffectDatabase）承载，运行时不再读取这些数据；请迁移到效果库。",
                "This asset still holds effects / gameplay tags stored in the inventory database by 1.12.0 (legacy). Since 1.13.0 effects live in the toolkit effect library (EffectDatabase) and this data is no longer read at runtime; please migrate it.",
                "このアセットには 1.12.0 でインベントリデータベースに保存されたエフェクト / ゲームプレイタグ（legacy）が残っています。1.13.0 以降は toolkit エフェクトライブラリ（EffectDatabase）が保持し、ランタイムではこのデータを読みません。移行してください。");
            Add("迁移效果到 Effect Database…", "Migrate Effects to Effect Database…", "エフェクトを Effect Database へ移行…");
        }
    }
}
