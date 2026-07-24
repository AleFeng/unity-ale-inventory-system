namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 运行时组件自定义 Inspector（<c>UiwEquipmentGroupPanelEditor</c> / <c>UiwEquipmentSlotListEditor</c> /
    /// <c>UiwSkillViewEditor</c>）的英 / 日译表。这些面板显示在 Unity 标准 Inspector 中，
    /// 不属于插件的两个编辑器窗口，故单列一张表。
    /// </summary>
    public static partial class InventoryEditorL10n
    {
        static partial void RegisterComponentInspectors()
        {
            // ── 装备组面板 / 槽位列表：布局方式 ───────────────────────────────────
            Add("手动模式", "Manual Mode", "手動モード");
            Add("自动模式", "Auto Mode",   "自動モード");
            Add("在本物体层级下手动摆放槽位列表物体，再在下方逐条指定「槽位列表 ID → 槽位列表」。\n" +
                "槽位列表 ID 须与装备组配置中某槽位列表的 ID 一致。",
                "Place the slot-list objects manually under this object's hierarchy, then map them one by one below as \"slot list ID → slot list\".\n" +
                "Each slot list ID must match the ID of a slot list in the equipment group config.",
                "このオブジェクトの階層下にスロットリストのオブジェクトを手動で配置し、下で「スロットリスト ID → スロットリスト」を 1 件ずつ指定します。\n" +
                "スロットリスト ID は装備グループ設定内のいずれかのスロットリストの ID と一致する必要があります。");
            Add("在本物体层级下手动摆放装备槽物体，再在下方逐条指定「槽位 ID → 装备槽」。\n" +
                "槽位 ID 须与槽位列表配置中某装备槽的 ID 一致。",
                "Place the equipment-slot objects manually under this object's hierarchy, then map them one by one below as \"slot ID → equipment slot\".\n" +
                "Each slot ID must match the ID of an equipment slot in the slot list config.",
                "このオブジェクトの階層下に装備スロットのオブジェクトを手動で配置し、下で「スロット ID → 装備スロット」を 1 件ずつ指定します。\n" +
                "スロット ID はスロットリスト設定内のいずれかの装備スロットの ID と一致する必要があります。");

            // ── 技能视图：按技能来源显示的 ID 字段 ────────────────────────────────
            Add("装备组 ID", "Equipment Group ID", "装備グループ ID");
            Add("从该装备组所有装备槽的已装备道具采集技能。",
                "Collects skills from the items equipped in every slot of this equipment group.",
                "この装備グループの全スロットに装備中のアイテムからスキルを収集します。");
            Add("仓库 ID", "Warehouse ID", "倉庫 ID");
            Add("从该仓库所有道具采集技能。",
                "Collects skills from every item in this warehouse.",
                "この倉庫の全アイテムからスキルを収集します。");
            Add("角色 ID", "Character ID", "キャラクター ID");
            Add("显示该角色（SkillRuntimeManager）当前已学会的技能。",
                "Shows the skills this character has currently learned (from SkillRuntimeManager).",
                "このキャラクターが現在習得しているスキル（SkillRuntimeManager）を表示します。");
        }
    }
}
