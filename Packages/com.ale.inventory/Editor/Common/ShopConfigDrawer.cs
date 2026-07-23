using System;
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 商店可配置项的共享 IMGUI 绘制：商店类型、交易仓库、过滤设置、UI 配置（数字格式 + 价格属性来源）、商品组。
    /// 按 <see cref="IShopConfig"/> 工作，供 <see cref="ShopInspectorPanel"/>（商店实例）与
    /// <see cref="ShopTemplatePanel"/>（商店模板）共用，确保两者配置项一致。
    /// </summary>
    public static class ShopConfigDrawer
    {
        private const float CommodityListHeight = 400f;

        // 搜索匹配条目的高亮样式（绿色加粗）。
        private static GUIStyle _matchStyle;
        private static GUIStyle MatchStyle => _matchStyle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = new Color(0.30f, 0.85f, 0.35f) }
        };

        #region 状态与匹配

        /// <summary>商品列表的编辑器内存态（按商品组实例缓存；默认折叠）。</summary>
        private class CommodityListState
        {
            public bool          Expanded;
            public Vector2       Scroll;
            public string        Search = string.Empty;
            public readonly List<int> Matches = new List<int>();
            public int           MatchPtr      = -1;
            public int           ScrollToIndex = -1; // 请求滚动定位到的商品下标

            /// <summary>该组商品列表的拖拽重排状态机。</summary>
            public readonly EditorReorderableDrag Drag = new EditorReorderableDrag("ShopCommoditiesDrag");
        }

        private static readonly Dictionary<ShopCommodityGroup, CommodityListState> CommodityStates
            = new Dictionary<ShopCommodityGroup, CommodityListState>();

        /// <summary>商品组拖拽重排状态机（每帧仅绘制一份商店配置，故静态共享安全）。</summary>
        private static readonly EditorReorderableDrag GroupsDrag = new EditorReorderableDrag("ShopGroupsDrag");

        /// <summary>交易仓库列表拖拽重排状态机（每帧仅绘制一份商店配置，故静态共享安全）。</summary>
        private static readonly EditorReorderableDrag TradeInventoriesDrag
            = new EditorReorderableDrag("ShopTradeInventoriesDrag");

        private static CommodityListState GetState(ShopCommodityGroup group)
        {
            if (!CommodityStates.TryGetValue(group, out var s))
            {
                s = new CommodityListState();
                CommodityStates[group] = s;
            }
            return s;
        }

        /// <summary>重算搜索匹配（按道具 ID 包含搜索词，忽略大小写），并定位到首个匹配。</summary>
        private static void RecomputeMatches(CommodityListState state, ShopCommodityGroup group)
        {
            state.Matches.Clear();
            string term = state.Search?.Trim();
            if (!string.IsNullOrEmpty(term))
                for (int i = 0; i < group.commodities.Count; i++)
                {
                    string id = group.commodities[i].itemId;
                    if (!string.IsNullOrEmpty(id) && id.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                        state.Matches.Add(i);
                }
            state.MatchPtr      = state.Matches.Count > 0 ? 0 : -1;
            state.ScrollToIndex = state.MatchPtr >= 0 ? state.Matches[state.MatchPtr] : -1;
        }

        #endregion

        #region 入口

        /// <summary>按顺序绘制全部共享配置区块。</summary>
        public static void DrawAll(IInventoryEditorContext ctx, IShopConfig cfg)
        {
            DrawShopType(ctx, cfg);
            EditorGUILayout.Space(6);
            DrawTradeInventories(ctx, cfg);
            EditorGUILayout.Space(6);
            DrawTradeTags(ctx, cfg);
            EditorGUILayout.Space(6);
            DrawFilterTags(ctx, cfg);
            EditorGUILayout.Space(6);
            DrawSortSettings(ctx, cfg);
            EditorGUILayout.Space(6);
            DrawUIConfig(ctx, cfg);
            EditorGUILayout.Space(6);
            DrawGroups(ctx, cfg);
        }

        // ── 商店类型 ────────────────────────────────────────────────────────────────

        #endregion

        #region 基础配置

        private static void DrawShopType(IInventoryEditorContext ctx, IShopConfig cfg)
        {
            EditorGUILayout.LabelField("商店类型", InventoryEditorStyles.Header);
            EditorGUI.BeginChangeCheck();
            var newType = (ShopType)EditorGUILayout.EnumPopup("类型", cfg.ShopType);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改商店类型");
                cfg.ShopType = newType;
                ctx.MarkDirty();
            }
        }

        // ── 交易仓库 ──────────────────────────────────────────────────────────────

        private static void DrawTradeInventories(IInventoryEditorContext ctx, IShopConfig cfg)
        {
            InventoryRefListDrawer.Draw(ctx, cfg.TradeInventoryRefs, TradeInventoriesDrag,
                "交易仓库", "交易仓库",
                emptyHint: "（未配置；与本商店交易时使用的玩家仓库）");
        }

        // ── 交易功能标签 ──────────────────────────────────────────────────────────────

        private static void DrawTradeTags(IInventoryEditorContext ctx, IShopConfig cfg)
        {
            EditorGUILayout.LabelField("交易功能标签", InventoryEditorStyles.Header);
            EditorGUILayout.LabelField("仅「回收」生效：只回收含勾选标签的道具；不勾选 = 不限制。", EditorStyles.miniLabel);

            EditorTagToggleList.Draw(ctx, cfg.TradeTagRefs,
                "添加交易功能标签", "移除交易功能标签");
        }

        // ── 过滤设置 ────────────────────────────────────────────────────────────────

        private static void DrawFilterTags(IInventoryEditorContext ctx, IShopConfig cfg)
        {
            EditorGUILayout.LabelField("过滤设置", InventoryEditorStyles.Header);
            EditorGUILayout.LabelField("过滤列表（UI 中以标签按钮形式显示）：", EditorStyles.miniLabel);

            EditorGUI.BeginChangeCheck();
            bool newShowAll = EditorGUILayout.ToggleLeft(
                new GUIContent("全部", "勾选后 UI 页签栏会显示「全部」页签（默认选中、显示全部商品）；" +
                                       "取消后不显示「全部」，默认选中第一个商品组。"),
                cfg.ShowAllFilterTab);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改显示全部页签");
                cfg.ShowAllFilterTab = newShowAll;
                ctx.MarkDirty();
            }

            EditorTagToggleList.Draw(ctx, cfg.FilterTagRefs,
                "添加过滤标签", "移除过滤标签");
        }

        // ── 整理排序 ────────────────────────────────────────────────────────────────

        private static void DrawSortSettings(IInventoryEditorContext ctx, IShopConfig cfg)
        {
            EditorGUILayout.LabelField("整理排序", InventoryEditorStyles.Header);
            EditorGUILayout.LabelField(
                "排序条件（UI 中以下拉菜单显示，玩家选择并可升降序；商店仅按当前选中条件对商品显示排序）：",
                EditorStyles.miniLabel);
            SortSettingsDrawer.Draw(ctx, cfg.SortPriorities, cfg.SortTiebreakers);
        }

        // ── UI 配置（数字格式 + 价格属性来源）───────────────────────────────────────

        private static void DrawUIConfig(IInventoryEditorContext ctx, IShopConfig cfg)
        {
            EditorGUILayout.LabelField("UI 配置", InventoryEditorStyles.Header);

            NumberFormatConfigDrawer.DrawRefPopup(ctx, "数字格式",
                cfg.NumberFormatRef, v => cfg.NumberFormatRef = v);

            // 价格属性来源：枚举所有 StringIntPair 属性 id
            var attrIds  = BuildStringIntPairAttrOptions(ctx.Database);
            var displays = new string[attrIds.Count + 1];
            displays[0]  = "（无）";
            int curIdx   = 0;
            for (int i = 0; i < attrIds.Count; i++)
            {
                displays[i + 1] = attrIds[i];
                if (attrIds[i] == cfg.PriceAttrSource) curIdx = i + 1;
            }

            EditorGUI.BeginChangeCheck();
            int picked = EditorGUILayout.Popup("价格属性来源", curIdx, displays);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改价格属性来源");
                cfg.PriceAttrSource = picked <= 0 ? string.Empty : attrIds[picked - 1];
                ctx.MarkDirty();
            }
            EditorGUILayout.LabelField(
                "（道具上 StringIntPair 类型属性：货币ID→价格）", EditorStyles.miniLabel);
        }

        /// <summary>收集数据库中所有 StringIntPair 类型属性字段 id（来自道具模板与功能标签，去重保序）。</summary>
        private static List<string> BuildStringIntPairAttrOptions(InventoryDatabase db)
        {
            var ids  = new List<string>();
            var seen = new HashSet<string>();
            void Collect(List<AttributeDefinition> defs)
            {
                if (defs == null) return;
                foreach (var def in defs)
                    if (def.type == EFieldType.StringIntPair
                        && !string.IsNullOrEmpty(def.id) && seen.Add(def.id))
                        ids.Add(def.id);
            }
            foreach (var tmpl in db.ItemTemplates) Collect(tmpl.attributes);
            foreach (var tag in db.FunctionTags)   Collect(tag.attributes);
            return ids;
        }

        // ── 商品组 ──────────────────────────────────────────────────────────────────

        #endregion

        #region 商品组与商品

        private static void DrawGroups(IInventoryEditorContext ctx, IShopConfig cfg)
        {
            var groups = cfg.Groups;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("商品组", InventoryEditorStyles.Header);
            if (GUILayout.Button("+ 添加商品组", EditorStyles.miniButton, GUILayout.Width(96)))
            {
                ctx.RecordUndo("添加商品组");
                groups.Add(new ShopCommodityGroup
                {
                    guid = InventoryDatabase.NewShopEntryGuid(),   // 交易进度存档键，创建即分配
                    name = "新商品组",
                });
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            GroupsDrag.BeginFrame();

            int removeGroup = -1;
            for (int gi = 0; gi < groups.Count; gi++)
            {
                var group = groups[gi];

                // 左侧拖拽句柄列 + 右侧商品组内容
                Rect rowRect = EditorGUILayout.BeginHorizontal();
                GroupsDrag.RecordRow(gi, rowRect);
                GroupsDrag.DrawHandleColumn(gi);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                string newName = EditorGUILayout.TextField($"组 {gi} 名称", group.name);
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改商品组名称");
                    group.name = newName;
                    ctx.MarkDirty();
                }
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                    removeGroup = gi;
                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                string newDesc = EditorGUILayout.TextField("描述", group.description);
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改商品组描述");
                    group.description = newDesc;
                    ctx.MarkDirty();
                }

                ShopRefreshScheduleDrawer.Draw(ctx, "组刷新计划", group.refresh);

                DrawCommodities(ctx, group);

                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            GroupsDrag.EndFrame(ctx, groups, "调整商品组顺序");

            if (removeGroup >= 0)
            {
                ctx.RecordUndo("删除商品组");
                groups.RemoveAt(removeGroup);
                ctx.MarkDirty();
            }
        }

        private static void DrawCommodities(IInventoryEditorContext ctx, ShopCommodityGroup group)
        {
            var state = GetState(group);

            // ── 折叠标题行 + 添加按钮（默认折叠）──────────────────────────────────
            EditorGUILayout.BeginHorizontal();
            state.Expanded = EditorGUILayout.Foldout(state.Expanded, $"商品列表（{group.commodities.Count}）", true);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 添加商品", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                ctx.RecordUndo("添加商品");
                group.commodities.Add(new ShopCommodity { guid = InventoryDatabase.NewShopEntryGuid() });
                ctx.MarkDirty();
                state.Expanded      = true;                        // 添加后自动展开
                state.ScrollToIndex = group.commodities.Count - 1; // 定位到新条目（末尾）
                ctx.Repaint();
            }
            EditorGUILayout.EndHorizontal();

            if (!state.Expanded) return;

            // ── 搜索行：按道具 ID 查找 + 1/N 指示 + 上/下切换 ─────────────────────
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("搜索", GUILayout.Width(30));
            EditorGUI.BeginChangeCheck();
            string newSearch = EditorGUILayout.TextField(state.Search ?? string.Empty);
            if (EditorGUI.EndChangeCheck())
            {
                state.Search = newSearch;
                RecomputeMatches(state, group);
                ctx.Repaint();
            }
            if (state.Matches.Count > 0)
            {
                EditorGUILayout.LabelField($"{state.MatchPtr + 1}/{state.Matches.Count}", GUILayout.Width(40));
                if (GUILayout.Button("↑", EditorStyles.miniButtonLeft, GUILayout.Width(24)))
                {
                    state.MatchPtr      = (state.MatchPtr - 1 + state.Matches.Count) % state.Matches.Count;
                    state.ScrollToIndex = state.Matches[state.MatchPtr];
                    ctx.Repaint();
                }
                if (GUILayout.Button("↓", EditorStyles.miniButtonRight, GUILayout.Width(24)))
                {
                    state.MatchPtr      = (state.MatchPtr + 1) % state.Matches.Count;
                    state.ScrollToIndex = state.Matches[state.MatchPtr];
                    ctx.Repaint();
                }
            }
            else if (!string.IsNullOrEmpty(state.Search))
            {
                EditorGUILayout.LabelField("无匹配", EditorStyles.miniLabel, GUILayout.Width(48));
            }
            EditorGUILayout.EndHorizontal();

            int currentMatch = (state.MatchPtr >= 0 && state.MatchPtr < state.Matches.Count)
                ? state.Matches[state.MatchPtr] : -1;

            // ── 固定高度 300 的滚动区（隐藏横向滚动条，避免内容溢出不可见）──────────
            state.Scroll = EditorGUILayout.BeginScrollView(state.Scroll, false, false,
                GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView,
                GUILayout.Height(CommodityListHeight));

            state.Drag.BeginFrame();

            // 收窄标签列，把右侧字段值输入框往左加宽约 40%（绘制后还原，不影响其它区块）。
            float prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Max(84f, prevLabelWidth * 0.6f);

            int removeCommodity = -1;
            float? targetY = null;
            for (int ci = 0; ci < group.commodities.Count; ci++)
            {
                var c = group.commodities[ci];

                // 左侧拖拽句柄列 + 右侧商品内容
                Rect rowRect = EditorGUILayout.BeginHorizontal();
                state.Drag.RecordRow(ci, rowRect);
                state.Drag.DrawHandleColumn(ci);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (ci == currentMatch)
                    EditorGUILayout.LabelField("▶ 搜索匹配", MatchStyle);

                // 道具ID 行：直接输入（无效 ID 红色高亮，回车确认）+ 右侧「选择」下拉（同一字段的快捷设置）+ 删除
                bool invalid = !string.IsNullOrEmpty(c.itemId) && ctx.Database.GetItem(c.itemId) == null;
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                string newItemId = EditorGUILayout.DelayedTextField(
                    new GUIContent("道具ID", "直接输入道具 ID，回车确认；右侧「选择」可按道具模板分组从道具列表快捷选择，写入此处。无对应道具时红色提示且无法导出。"),
                    c.itemId ?? string.Empty,
                    invalid ? InventoryEditorStyles.RedField : EditorStyles.textField);
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改商品道具ID");
                    c.itemId = newItemId;
                    ctx.MarkDirty();
                }
                Rect dropRect = GUILayoutUtility.GetRect(new GUIContent("选择"), EditorStyles.popup, GUILayout.Width(56));
                if (EditorGUI.DropdownButton(dropRect,
                        new GUIContent("选择", "从道具列表快捷选择，结果写入左侧道具ID。"),
                        FocusType.Keyboard, EditorStyles.popup))
                    ShowItemMenu(ctx, c, dropRect);
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                    removeCommodity = ci;
                EditorGUILayout.EndHorizontal();

                if (invalid)
                    EditorGUILayout.LabelField("⚠ 无效道具 ID（导出将被阻止）", InventoryEditorStyles.StatusError);

                // 行 3：数量 / 倍率 / 次数（含 tooltip）
                EditorGUI.BeginChangeCheck();
                int newCount = EditorGUILayout.IntField(
                    new GUIContent("每次购买数量", "每完成一次交易（购买 / 回收）获得或扣除的该道具数量。"), c.count);
                float newMul = EditorGUILayout.FloatField(
                    new GUIContent("价格倍率", "在道具基础价格（价格属性来源）上乘以的倍率。1 = 原价；回收常用 <1，如 0.5 = 半价回收。"), c.priceMultiplier);
                int newLimit = EditorGUILayout.IntField(
                    new GUIContent("可交易次数", "每个刷新周期内该商品可被购买 / 回收的次数。-1 = 无限；刷新周期为「不刷新」时为终身上限。"), c.tradeLimit);
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改商品参数");
                    c.count           = Mathf.Max(1, newCount);
                    c.priceMultiplier = Mathf.Max(0f, newMul);
                    c.tradeLimit      = newLimit < 0 ? -1 : newLimit;
                    ctx.MarkDirty();
                }

                // 行 4：覆盖刷新
                EditorGUI.BeginChangeCheck();
                bool newOverride = EditorGUILayout.Toggle(
                    new GUIContent("覆盖组刷新", "勾选后该商品使用自己的刷新计划，覆盖所属商品组的刷新计划。"), c.overrideRefresh);
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改商品覆盖刷新");
                    c.overrideRefresh = newOverride;
                    ctx.MarkDirty();
                }
                if (c.overrideRefresh)
                    ShopRefreshScheduleDrawer.Draw(ctx, "商品刷新计划", c.refresh);

                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();

                // 仅在 Repaint 阶段 rowRect 才返回有效坐标；Layout 阶段返回占位 rect(y≈0)，
                // 若在此处采集会用 0 覆盖滚动并提前清掉定位请求，导致"滚不动"。
                if (ci == state.ScrollToIndex && Event.current.type == EventType.Repaint)
                    targetY = rowRect.y;
            }

            // 拖拽落点处理 + 插入指示线；重排后索引变化，重算搜索匹配。
            if (state.Drag.EndFrame(ctx, group.commodities, "调整商品顺序"))
                RecomputeMatches(state, group);

            EditorGUIUtility.labelWidth = prevLabelWidth;   // 还原标签列宽度

            EditorGUILayout.EndScrollView();

            // 仅当 Repaint 成功取得目标 Y 时才应用定位（下一帧生效）：将目标商品滚动到视口顶部
            if (state.ScrollToIndex >= 0 && targetY.HasValue)
            {
                state.Scroll.y      = Mathf.Max(0f, targetY.Value);
                state.ScrollToIndex = -1;
                ctx.Repaint();
            }

            if (removeCommodity >= 0)
            {
                ctx.RecordUndo("删除商品");
                group.commodities.RemoveAt(removeCommodity);
                ctx.MarkDirty();
                RecomputeMatches(state, group);
            }
        }

        /// <summary>弹出按「道具模板」分组的道具选择菜单（GenericMenu 以 "模板/道具ID" 形成可折叠子菜单）。</summary>
        private static void ShowItemMenu(IInventoryEditorContext ctx, ShopCommodity c, Rect rect)
        {
            var db   = ctx.Database;
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("（未选择）"), string.IsNullOrEmpty(c.itemId), () =>
            {
                ctx.RecordUndo("修改商品道具");
                c.itemId = string.Empty;
                ctx.MarkDirty();
                ctx.Repaint();
            });
            menu.AddSeparator(string.Empty);

            foreach (var item in db.Items)
            {
                if (string.IsNullOrEmpty(item.id)) continue;
                string group   = string.IsNullOrEmpty(item.templateRef) ? "（无模板）" : item.templateRef;
                string path    = group + "/" + item.id;
                string capture = item.id;
                menu.AddItem(new GUIContent(path), c.itemId == item.id, () =>
                {
                    ctx.RecordUndo("修改商品道具");
                    c.itemId = capture;
                    ctx.MarkDirty();
                    ctx.Repaint();
                });
            }

            menu.DropDown(rect);
        }
        #endregion

    }
}
