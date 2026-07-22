using System;
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 装备组共享可配置项绘制器：槽位列表（嵌套：道具限制 + 装备槽 + 槽过滤条件）+ 装备属性字段列表。
    /// 由装备组 Inspector 与装备组模板 Inspector 复用（均实现 <see cref="IEquipmentConfig"/>），
    /// 使两者配置项一致。每个使用者各持一个实例（持有拖拽状态；同帧只绘制其一，互不冲突）。
    /// </summary>
    public class EquipmentConfigDrawer
    {
        // 装备仓库（顶层，单实例）的拖拽重排。
        private readonly EditorReorderableDrag _equipInventoriesDrag = new EditorReorderableDrag("EquipInventoriesDrag");

        // 槽位列表（顶层，单实例）的拖拽重排。
        private readonly EditorReorderableDrag _slotListsDrag = new EditorReorderableDrag("EquipSlotListsDrag");

        // 装备属性字段列表（顶层，单实例）的拖拽重排。
        private readonly EditorReorderableDrag _attrDisplaysDrag = new EditorReorderableDrag("EquipAttrDisplaysDrag");

        // 嵌套子列表（功能标签 / 枚举约束）按稳定路径键各自持有独立拖拽状态（同帧出现多份）。
        private readonly Dictionary<string, EditorReorderableDrag> _dragMap
            = new Dictionary<string, EditorReorderableDrag>();

        // 每个槽位列表条目「详细配置」折叠状态（按对象引用记忆，默认折叠）。
        private readonly Dictionary<EquipmentSlotList, bool> _slotListFoldouts
            = new Dictionary<EquipmentSlotList, bool>();

        private EditorReorderableDrag GetDrag(string key)
        {
            if (!_dragMap.TryGetValue(key, out var d))
                _dragMap[key] = d = new EditorReorderableDrag("EquipConfig/" + key);
            return d;
        }

        /// <summary>绘制装备组的全部共享可配置项：装备仓库 + 槽位列表 + 装备属性字段列表。</summary>
        public void Draw(IInventoryEditorContext ctx, IEquipmentConfig cfg)
        {
            InventoryRefListDrawer.Draw(ctx, cfg.EquipmentInventoryRefs, _equipInventoriesDrag,
                "装备仓库", "装备仓库",
                hint: "装备系统 / 装备 UI 可直接交互的仓库；卸下装备时从上到下（Index0 起）找第一个放得下的仓库。",
                emptyHint: "（未配置）");
            EditorGUILayout.Space(6);
            DrawSlotLists(ctx, cfg);
            EditorGUILayout.Space(6);
            DrawAttributeDisplays(ctx, cfg);
            EditorGUILayout.Space(6);
            DrawSortSettings(ctx, cfg);
        }

        // ── 整理排序 ──────────────────────────────────────────────────────────────
        // 应用于可装备道具候选列表（UiwEquipmentCandidateList）的显示排序，组与模板共享此绘制。

        private void DrawSortSettings(IInventoryEditorContext ctx, IEquipmentConfig cfg)
        {
            EditorGUILayout.LabelField("整理排序", InventoryEditorStyles.Header);
            EditorGUILayout.LabelField(
                "可装备道具候选列表按此排序（候选列表有排序栏时玩家可选并升降序，否则以首条为默认排序）：",
                EditorStyles.miniLabel);
            SortSettingsDrawer.Draw(ctx, cfg.SortPriorities, cfg.SortTiebreakers);
        }

        // ── 槽位列表 ──────────────────────────────────────────────────────────────

        private void DrawSlotLists(IInventoryEditorContext ctx, IEquipmentConfig cfg)
        {
            var slotLists = cfg.SlotLists;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("槽位列表", InventoryEditorStyles.Header);
            if (GUILayout.Button("+ 添加槽位列表", EditorStyles.miniButton, GUILayout.Width(96)))
            {
                ctx.RecordUndo("添加槽位列表");
                slotLists.Add(new EquipmentSlotList(GenerateSlotListId(cfg), "新槽位列表"));
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            _slotListsDrag.BeginFrame();
            int removeSlotList = -1;
            for (int i = 0; i < slotLists.Count; i++)
            {
                var sl = slotLists[i];

                Rect rowRect = EditorGUILayout.BeginHorizontal();
                _slotListsDrag.RecordRow(i, rowRect);
                GUILayout.Space(EditorReorderableDrag.HandleWidth);   // 预留左侧拖拽句柄列

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // 标题行（拖拽句柄与本行对齐）：标题 + 删除
                Rect firstRowRect = EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"槽位列表 {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                    removeSlotList = i;
                EditorGUILayout.EndHorizontal();

                // ID（始终可见）
                EditorGUI.BeginChangeCheck();
                string slId = EditorGUILayout.TextField("ID", sl.id);
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改槽位列表 ID");
                    sl.id = slId;
                    ctx.MarkDirty();
                }

                // 详细配置（默认折叠）：名称 / 描述 / 道具限制 / 装备槽
                bool expanded = _slotListFoldouts.TryGetValue(sl, out var e) && e;
                bool toggled  = EditorGUILayout.Foldout(expanded, "详细配置", true);
                if (toggled != expanded)
                {
                    _slotListFoldouts[sl] = toggled;
                    ctx.Repaint();
                }

                if (expanded)
                {
                    EditorGUI.BeginChangeCheck();
                    string slName = EditorGUILayout.TextField("名称", sl.displayName);
                    string slDesc = EditorGUILayout.TextField("描述", sl.description);
                    if (EditorGUI.EndChangeCheck())
                    {
                        ctx.RecordUndo("修改槽位列表");
                        sl.displayName = slName;
                        sl.description = slDesc;
                        ctx.MarkDirty();
                    }

                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("道具限制（功能标签与枚举约束需全部满足）", EditorStyles.miniLabel);
                    DrawRequiredTags(ctx, sl, i);
                    DrawEnumConstraints(ctx, sl, i);

                    EditorGUILayout.Space(2);
                    DrawSlots(ctx, cfg, sl);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                // 拖拽句柄与条目标题行对齐绘制。
                var handleRect = new Rect(rowRect.x, firstRowRect.y,
                    EditorReorderableDrag.HandleWidth, EditorGUIUtility.singleLineHeight);
                _slotListsDrag.DrawHandle(handleRect, i);

                EditorGUILayout.Space(2);
            }
            _slotListsDrag.EndFrame(ctx, slotLists, "调整槽位列表顺序",
                EditorReorderableDrag.HandleWidth, 22f);

            if (removeSlotList >= 0)
            {
                ctx.RecordUndo("删除槽位列表");
                slotLists.RemoveAt(removeSlotList);
                ctx.MarkDirty();
            }
        }

        // ── 道具限制：功能标签 ────────────────────────────────────────────────────────

        private void DrawRequiredTags(IInventoryEditorContext ctx, EquipmentSlotList sl, int slIndex)
        {
            var db = ctx.Database;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("功能标签", EditorStyles.boldLabel);
            if (GUILayout.Button("+", GUILayout.Width(24)))
                ShowAddTagMenu(ctx, sl);
            EditorGUILayout.EndHorizontal();

            if (sl.requiredTags.Count == 0)
            {
                EditorGUILayout.LabelField("（未限制）", EditorStyles.miniLabel);
                return;
            }

            var drag = GetDrag($"sl{slIndex}/tags");
            drag.BeginFrame();
            int removeIndex = -1;
            for (int j = 0; j < sl.requiredTags.Count; j++)
            {
                var ft       = db.GetTag(sl.requiredTags[j]);
                bool exists  = ft != null;
                string ftName = exists && ft.displayNameText != null ? ft.displayNameText.GetTextValue(0) : null;
                string label = exists
                    ? (string.IsNullOrEmpty(ftName) ? ft.name : ftName)
                    : sl.requiredTags[j] + "（已删除）";

                Rect rowRect = EditorGUILayout.BeginHorizontal();
                drag.RecordRow(j, rowRect);
                GUILayout.Space(EditorReorderableDrag.HandleWidth);
                EditorGUILayout.LabelField(label,
                    exists ? EditorStyles.label : InventoryEditorStyles.StatusError);
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                    removeIndex = j;
                EditorGUILayout.EndHorizontal();

                var handleRect = new Rect(rowRect.x,
                    rowRect.y + (rowRect.height - EditorGUIUtility.singleLineHeight) * 0.5f,
                    EditorReorderableDrag.HandleWidth, EditorGUIUtility.singleLineHeight);
                drag.DrawHandle(handleRect, j);
            }
            drag.EndFrame(ctx, sl.requiredTags, "调整功能标签顺序");

            if (removeIndex >= 0)
            {
                ctx.RecordUndo("移除功能标签");
                sl.requiredTags.RemoveAt(removeIndex);
                ctx.MarkDirty();
            }
        }

        private static void ShowAddTagMenu(IInventoryEditorContext ctx, EquipmentSlotList sl)
        {
            var db   = ctx.Database;
            var menu = new GenericMenu();
            bool any = false;
            foreach (var ft in db.FunctionTags)
            {
                if (string.IsNullOrEmpty(ft.name) || sl.requiredTags.Contains(ft.name)) continue;
                any = true;
                string name  = ft.name;
                string ftName = ft.displayNameText != null ? ft.displayNameText.GetTextValue(0) : null;
                string label = string.IsNullOrEmpty(ftName) ? ft.name : ftName;
                menu.AddItem(new GUIContent(label), false, () =>
                {
                    ctx.RecordUndo("添加功能标签");
                    sl.requiredTags.Add(name);
                    ctx.MarkDirty();
                    ctx.Repaint();
                });
            }
            if (!any)
                menu.AddDisabledItem(new GUIContent("（无可添加的功能标签）"));
            menu.ShowAsContext();
        }

        // ── 道具限制：枚举约束 ────────────────────────────────────────────────────────

        private void DrawEnumConstraints(IInventoryEditorContext ctx, EquipmentSlotList sl, int slIndex)
        {
            var db = ctx.Database;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("枚举约束", EditorStyles.boldLabel);
            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                ctx.RecordUndo("添加枚举约束");
                sl.enumConstraints.Add(new EquipmentEnumConstraint());
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            if (sl.enumConstraints.Count == 0)
            {
                EditorGUILayout.LabelField("（未限制）", EditorStyles.miniLabel);
                return;
            }

            // 枚举类型名称列表（含「（无）」占位）
            var enumNames = new List<string> { "（无）" };
            foreach (var et in db.EnumTypes) enumNames.Add(et.name);

            var drag = GetDrag($"sl{slIndex}/enums");
            drag.BeginFrame();
            int removeIndex = -1;
            for (int k = 0; k < sl.enumConstraints.Count; k++)
            {
                var c = sl.enumConstraints[k];

                Rect rowRect = EditorGUILayout.BeginHorizontal();
                drag.RecordRow(k, rowRect);
                GUILayout.Space(EditorReorderableDrag.HandleWidth);

                int curIdx = 0;
                for (int t = 0; t < db.EnumTypes.Count; t++)
                    if (db.EnumTypes[t].name == c.enumTypeRef) { curIdx = t + 1; break; }

                EditorGUI.BeginChangeCheck();
                int picked = EditorGUILayout.Popup(curIdx, enumNames.ToArray(), GUILayout.Width(120));
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改枚举类型");
                    c.enumTypeRef = picked <= 0 ? string.Empty : db.EnumTypes[picked - 1].name;
                    c.allowedValues.Clear();
                    ctx.MarkDirty();
                }

                var et2     = db.GetEnumType(c.enumTypeRef);
                string summary = BuildEnumValueSummary(et2, c.allowedValues);
                using (new EditorGUI.DisabledScope(et2 == null))
                {
                    if (GUILayout.Button(summary, EditorStyles.popup))
                        ShowEnumValuesMenu(ctx, c, et2);
                }

                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                    removeIndex = k;
                EditorGUILayout.EndHorizontal();

                var handleRect = new Rect(rowRect.x,
                    rowRect.y + (rowRect.height - EditorGUIUtility.singleLineHeight) * 0.5f,
                    EditorReorderableDrag.HandleWidth, EditorGUIUtility.singleLineHeight);
                drag.DrawHandle(handleRect, k);
            }
            drag.EndFrame(ctx, sl.enumConstraints, "调整枚举约束顺序");

            if (removeIndex >= 0)
            {
                ctx.RecordUndo("移除枚举约束");
                sl.enumConstraints.RemoveAt(removeIndex);
                ctx.MarkDirty();
            }
        }

        private static string BuildEnumValueSummary(EnumType et, List<int> values)
        {
            if (et == null) return "（先选枚举类型）";
            if (values == null || values.Count == 0) return "（任意值）";
            var names = new List<string>();
            foreach (var v in values)
            {
                var item = et.GetItemByValue(v);
                if (item != null) names.Add(item.name);
            }
            return names.Count == 0 ? "（任意值）" : string.Join(", ", names);
        }

        private static void ShowEnumValuesMenu(IInventoryEditorContext ctx, EquipmentEnumConstraint c, EnumType et)
        {
            var menu = new GenericMenu();
            foreach (var item in et.items)
            {
                int    val      = item.value;
                bool   selected = c.allowedValues.Contains(val);
                string name     = item.name;
                menu.AddItem(new GUIContent(name), selected, () =>
                {
                    ctx.RecordUndo("修改允许枚举值");
                    if (c.allowedValues.Contains(val)) c.allowedValues.Remove(val);
                    else                               c.allowedValues.Add(val);
                    ctx.MarkDirty();
                    ctx.Repaint();
                });
            }
            menu.ShowAsContext();
        }

        // ── 装备槽 + 槽过滤条件 ───────────────────────────────────────────────────────

        private static void DrawSlots(IInventoryEditorContext ctx, IEquipmentConfig cfg, EquipmentSlotList sl)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("装备槽", EditorStyles.boldLabel);
            if (GUILayout.Button("+ 添加装备槽", EditorStyles.miniButton, GUILayout.Width(88)))
            {
                ctx.RecordUndo("添加装备槽");
                sl.slots.Add(new EquipmentSlot(GenerateSlotId(cfg), "新槽位"));
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            int removeSlot = -1;
            for (int s = 0; s < sl.slots.Count; s++)
            {
                var slot = sl.slots[s];

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"装备槽 {s + 1}", EditorStyles.miniBoldLabel);
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                    removeSlot = s;
                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                string sId   = EditorGUILayout.TextField("ID", slot.id);
                string sName = EditorGUILayout.TextField("名称", slot.displayName);
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改装备槽");
                    slot.id          = sId;
                    slot.displayName = sName;
                    ctx.MarkDirty();
                }

                DrawSlotFilters(ctx, slot);

                EditorGUILayout.EndVertical();
            }

            if (removeSlot >= 0)
            {
                ctx.RecordUndo("删除装备槽");
                sl.slots.RemoveAt(removeSlot);
                ctx.MarkDirty();
            }
        }

        private static void DrawSlotFilters(IInventoryEditorContext ctx, EquipmentSlot slot)
        {
            var db = ctx.Database;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("槽位过滤条件（需全部满足）", EditorStyles.miniLabel);
            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                ctx.RecordUndo("添加过滤条件");
                slot.filters.Add(new EquipmentSlotFilter());
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            int removeIndex = -1;
            for (int f = 0; f < slot.filters.Count; f++)
            {
                var filter = slot.filters[f];

                // 属性ID 行：输入框 + 「选择」下拉 + 删除
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                string newAttrId = EditorGUILayout.TextField("属性ID", filter.attrId ?? string.Empty);
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改过滤属性ID");
                    filter.attrId = newAttrId;
                    ctx.MarkDirty();
                }
                Rect dropRect = GUILayoutUtility.GetRect(new GUIContent("选择"), EditorStyles.popup, GUILayout.Width(56));
                if (EditorGUI.DropdownButton(dropRect, new GUIContent("选择"), FocusType.Keyboard, EditorStyles.popup))
                {
                    var capture = filter;
                    ShowAttrIdMenu(ctx, capture.attrId, id => capture.attrId = id, dropRect);
                }
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                    removeIndex = f;
                EditorGUILayout.EndHorizontal();

                // 期望值：按属性定义解析类型；类型不匹配时重置为该定义的默认值
                var def = InventoryRuntimeManager.FindAttrDef(filter.attrId, db);
                if (def == null)
                {
                    if (!string.IsNullOrEmpty(filter.attrId))
                        EditorGUILayout.LabelField("⚠ 未找到属性定义（无法编辑期望值）", InventoryEditorStyles.StatusError);
                    continue;
                }

                bool mismatch = filter.value == null
                             || filter.value.Type    != def.type
                             || filter.value.IsArray != def.isArray
                             || (def.type == EFieldType.Enum && filter.value.EnumTypeRef != def.enumTypeRef);
                if (mismatch)
                {
                    filter.value = def.CreateValue();
                    ctx.MarkDirty();
                }

                var enumType = def.type == EFieldType.Enum ? db.GetEnumType(def.enumTypeRef) : null;
                AttributeFieldDrawer.Draw(ctx, "期望值", filter.value, enumType);
            }

            if (removeIndex >= 0)
            {
                ctx.RecordUndo("删除过滤条件");
                slot.filters.RemoveAt(removeIndex);
                ctx.MarkDirty();
            }
        }

        // ── 装备属性字段列表 ──────────────────────────────────────────────────────────

        private void DrawAttributeDisplays(IInventoryEditorContext ctx, IEquipmentConfig cfg)
        {
            var db   = ctx.Database;
            var list = cfg.AttributeDisplays;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("装备属性字段", InventoryEditorStyles.Header);
            if (GUILayout.Button("+ 添加", EditorStyles.miniButton, GUILayout.Width(64)))
            {
                ctx.RecordUndo("添加装备属性字段");
                list.Add(new EquipmentAttributeDisplay());
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("指定道具属性作为装备组总属性加成；拖拽左侧句柄调整顺序。", EditorStyles.miniLabel);

            // 分组标签下拉数据
            var groupIds      = new List<string>();
            var groupDisplays = new List<string> { "（无）" };
            foreach (var gt in db.EquipmentGroupTags)
            {
                groupIds.Add(gt.id);
                groupDisplays.Add(gt.PlainName());
            }

            _attrDisplaysDrag.BeginFrame();
            int removeIndex = -1;
            for (int i = 0; i < list.Count; i++)
            {
                var ad = list[i];

                Rect rowRect = EditorGUILayout.BeginHorizontal();
                _attrDisplaysDrag.RecordRow(i, rowRect);
                GUILayout.Space(EditorReorderableDrag.HandleWidth);   // 预留左侧拖拽句柄列

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // 属性ID 行（拖拽句柄与本行对齐）：输入框 + 「选择」下拉 + 删除
                Rect firstRowRect = EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                string newAttrId = EditorGUILayout.TextField("属性ID", ad.attrId ?? string.Empty);
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改属性ID");
                    ad.attrId = newAttrId;
                    ctx.MarkDirty();
                }
                Rect dropRect = GUILayoutUtility.GetRect(new GUIContent("选择"), EditorStyles.popup, GUILayout.Width(56));
                if (EditorGUI.DropdownButton(dropRect, new GUIContent("选择"), FocusType.Keyboard, EditorStyles.popup))
                {
                    var capture = ad;
                    ShowAttrIdMenu(ctx, capture.attrId, id => capture.attrId = id, dropRect);
                }
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                    removeIndex = i;
                EditorGUILayout.EndHorizontal();

                // 分组标签下拉
                int curIdx = 0;
                for (int g = 0; g < groupIds.Count; g++)
                    if (groupIds[g] == ad.groupTag) { curIdx = g + 1; break; }
                EditorGUI.BeginChangeCheck();
                int picked = EditorGUILayout.Popup("分组标签", curIdx, groupDisplays.ToArray());
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改属性字段分组标签");
                    ad.groupTag = picked <= 0 ? string.Empty : groupIds[picked - 1];
                    ctx.MarkDirty();
                }

                // 显示名覆盖（可选）：Text 类型（纯文本 fallback + 可选本地化引用），复用统一属性绘制器。
                ad.NormalizeLabel();
                AttributeFieldDrawer.Draw(ctx, "显示名（可选）", ad.label, null);

                // EnumIntPair 专属：指定枚举项上用作“各枚举 Key 显示名”的属性字段（String / LocalizedString）。
                DrawEnumLabelAttrIdField(ctx, db, ad);

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                // 拖拽句柄与条目首行（属性ID 行）对齐绘制，而非在整块多行条目中纵向居中。
                var handleRect = new Rect(rowRect.x, firstRowRect.y,
                    EditorReorderableDrag.HandleWidth, EditorGUIUtility.singleLineHeight);
                _attrDisplaysDrag.DrawHandle(handleRect, i);

                EditorGUILayout.Space(2);
            }
            _attrDisplaysDrag.EndFrame(ctx, list, "调整装备属性字段顺序",
                EditorReorderableDrag.HandleWidth, 22f);

            if (removeIndex >= 0)
            {
                ctx.RecordUndo("删除装备属性字段");
                list.RemoveAt(removeIndex);
                ctx.MarkDirty();
            }
        }

        // ── 辅助 ──────────────────────────────────────────────────────────────────

        /// <summary>弹出按来源（道具模板 / 功能标签）分组的属性字段 ID 选择菜单，结果经 setter 写回。</summary>
        private static void ShowAttrIdMenu(IInventoryEditorContext ctx, string current,
            Action<string> setter, Rect rect)
        {
            var db   = ctx.Database;
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("（清空）"), string.IsNullOrEmpty(current),
                () => Apply(ctx, () => setter(string.Empty)));
            menu.AddSeparator(string.Empty);

            var seen = new HashSet<string>();
            foreach (var tmpl in db.ItemTemplates)
                foreach (var d in tmpl.attributes)
                {
                    if (string.IsNullOrEmpty(d.id)) continue;
                    string path = (string.IsNullOrEmpty(tmpl.name) ? "(模板)" : tmpl.name) + "/" + d.id;
                    if (!seen.Add(path)) continue;
                    string id = d.id;
                    menu.AddItem(new GUIContent(path), current == id, () => Apply(ctx, () => setter(id)));
                }
            foreach (var tag in db.FunctionTags)
                foreach (var d in tag.attributes)
                {
                    if (string.IsNullOrEmpty(d.id)) continue;
                    string path = "功能标签/" + (string.IsNullOrEmpty(tag.name) ? "(标签)" : tag.name) + "/" + d.id;
                    if (!seen.Add(path)) continue;
                    string id = d.id;
                    menu.AddItem(new GUIContent(path), current == id, () => Apply(ctx, () => setter(id)));
                }

            menu.DropDown(rect);
        }

        private static void Apply(IInventoryEditorContext ctx, Action mutate)
        {
            ctx.RecordUndo("修改属性字段ID");
            mutate();
            ctx.MarkDirty();
            ctx.Repaint();
        }

        /// <summary>
        /// 仅当所选属性字段为 <see cref="EFieldType.EnumIntPair"/> 时，绘制「显示名来源（枚举字段）」下拉：
        /// 从该枚举类型的自定义属性字段（String / LocalizedString）中选一个作为各枚举 Key 的显示名来源；
        /// 「（枚举项名称）」表示回退使用枚举项自身名称。
        /// </summary>
        private static void DrawEnumLabelAttrIdField(IInventoryEditorContext ctx, InventoryDatabase db,
            EquipmentAttributeDisplay ad)
        {
            var def = InventoryRuntimeManager.FindAttrDef(ad.attrId, db);
            if (def == null || def.type != EFieldType.EnumIntPair) return;

            // 候选：该枚举类型下 String / Text 类型的字段 ID（首项为回退占位）。
            var labels = new List<string> { "（枚举项名称）" };
            var ids    = new List<string> { string.Empty };
            var et     = db.GetEnumType(def.enumTypeRef);
            if (et != null)
                foreach (var d in et.attributes)
                {
                    if (string.IsNullOrEmpty(d.id)) continue;
                    if (d.type != EFieldType.String && d.type != EFieldType.Text) continue;
                    labels.Add($"{d.id}（{(d.type == EFieldType.String ? "字符串" : "文本")}）");
                    ids.Add(d.id);
                }

            int cur = ids.IndexOf(ad.enumLabelAttrId ?? string.Empty);
            if (cur < 0)
            {
                // 已配置的字段在当前枚举类型中不存在（枚举类型变更 / 字段被删）：追加一个提示项保留原值显示。
                labels.Add($"{ad.enumLabelAttrId}（已失效）");
                ids.Add(ad.enumLabelAttrId);
                cur = ids.Count - 1;
            }

            EditorGUI.BeginChangeCheck();
            int picked = EditorGUILayout.Popup("显示名来源（枚举字段）", cur, labels.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改枚举显示名字段");
                ad.enumLabelAttrId = picked <= 0 ? string.Empty : ids[picked];
                ctx.MarkDirty();
            }
        }

        private static string GenerateSlotListId(IEquipmentConfig cfg)
        {
            int n = cfg.SlotLists.Count + 1;
            string id;
            bool Exists(string candidate)
            {
                foreach (var sl in cfg.SlotLists)
                    if (sl.id == candidate) return true;
                return false;
            }
            do { id = "slotlist_" + n; n++; } while (Exists(id));
            return id;
        }

        private static string GenerateSlotId(IEquipmentConfig cfg)
        {
            int total = 1;
            foreach (var sl in cfg.SlotLists) total += sl.slots.Count;
            string id;
            bool Exists(string candidate)
            {
                foreach (var sl in cfg.SlotLists)
                    foreach (var slot in sl.slots)
                        if (slot.id == candidate) return true;
                return false;
            }
            do { id = "slot_" + total; total++; } while (Exists(id));
            return id;
        }
    }
}
