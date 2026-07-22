using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 道具 Inspector（右侧列）：编辑 ID（重复检查高亮）、功能标签（多选）、
    /// 属性值（按来源模板 / 功能标签分组显示，支持折叠）。
    /// </summary>
    public class ItemInspectorPanel
    {
        // 样式缓存
        private GUIStyle _warnStyle;
        private GUIStyle _groupStyle;

        private GUIStyle WarnStyle  => _warnStyle  ??= new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            { normal = { textColor = new Color(0.95f, 0.8f, 0.2f) } };

        private GUIStyle GroupStyle => _groupStyle ??= new GUIStyle(EditorStyles.foldout)
        {
            normal   = { textColor = new Color(0.55f, 0.85f, 1.0f) },
            onNormal = { textColor = new Color(0.55f, 0.85f, 1.0f) },
            focused  = { textColor = new Color(0.55f, 0.85f, 1.0f) },
            onFocused= { textColor = new Color(0.55f, 0.85f, 1.0f) },
            active   = { textColor = new Color(0.55f, 0.85f, 1.0f) },
            onActive = { textColor = new Color(0.55f, 0.85f, 1.0f) },
            fontStyle = FontStyle.Bold,
            fontSize  = 11
        };

        /// <summary>每个分组 key 对应的展开状态。默认展开。</summary>
        private readonly Dictionary<string, bool> _groupFoldouts = new Dictionary<string, bool>();

        private bool GetFoldout(string key)
        {
            if (!_groupFoldouts.TryGetValue(key, out bool v)) return true;
            return v;
        }

        private void SetFoldout(string key, bool value)
        {
            _groupFoldouts[key] = value;
        }

        public void DrawInspector(IInventoryEditorContext ctx, Item item)
        {
            if (item == null)
            {
                EditorGUILayout.LabelField("请在中间列表选择一个道具。");
                return;
            }

            var db = ctx.Database;

            // ── ID ──────────────────────────────────────────────────────────────────
            bool isDup = ctx.DuplicateIds.Contains(
                string.IsNullOrWhiteSpace(item.id) ? string.Empty : item.id);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("ID");
            EditorGUI.BeginChangeCheck();
            string newId = EditorGUILayout.TextField(
                item.id, isDup ? InventoryEditorStyles.RedField : EditorStyles.textField);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改道具 ID");
                item.id = newId;
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();
            if (isDup)
                EditorGUILayout.LabelField("⚠ ID 重复或为空（导出时空 ID 条目将被跳过）",
                    InventoryEditorStyles.StatusError);

            // ── 来源模板（只读，创建后不可更改）────────────────────────────────────
            using (new EditorGUI.DisabledScope(true))
            {
                string tmplDisplay = string.IsNullOrEmpty(item.templateRef)
                    ? "（无）" : item.templateRef;
                EditorGUILayout.TextField("来源模板", tmplDisplay);
            }

            EditorGUILayout.Space(6);

            // ── 功能标签（多选，隐藏已被模板覆盖的标签）────────────────────────────
            EditorGUILayout.LabelField("功能标签", InventoryEditorStyles.Header);

            // 收集模板自身携带的标签（道具层面不可手动切换）
            var templateTagSet = new HashSet<string>();
            if (!string.IsNullOrEmpty(item.templateRef))
            {
                var tmpl = db.GetTemplate(item.templateRef);
                if (tmpl != null)
                    foreach (var t in tmpl.tagRefs)
                        templateTagSet.Add(t);
            }

            bool hasTogglableTags = false;
            foreach (var tag in db.FunctionTags)
            {
                // 已被模板锁定的标签：只读显示
                if (templateTagSet.Contains(tag.name))
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.ToggleLeft($"{tag.name}  （由模板锁定）", true);
                    continue;
                }

                hasTogglableTags = true;
                bool has = item.tagRefs.Contains(tag.name);
                bool now = EditorGUILayout.ToggleLeft(tag.name, has);
                if (now != has)
                {
                    if (now) { ctx.RecordUndo("添加功能标签"); item.AddTag(tag.name, db); }
                    else     { ctx.RecordUndo("移除功能标签"); item.RemoveTag(tag.name, db); }
                    ctx.MarkDirty();
                }
            }

            if (db.FunctionTags.Count == 0 || (!hasTogglableTags && templateTagSet.Count == 0))
                EditorGUILayout.LabelField("（暂无可用功能标签）", EditorStyles.miniLabel);

            EditorGUILayout.Space(6);

            // ── 仓库属性 ──────────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("仓库属性", InventoryEditorStyles.Header);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            float newWeight = EditorGUILayout.FloatField("重量", item.weight);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改道具重量");
                item.weight = Mathf.Max(0f, newWeight);
                ctx.MarkDirty();
            }
            EditorGUILayout.LabelField("（0 = 无重量）", EditorStyles.miniLabel, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            int newStackLimit = EditorGUILayout.IntField("堆叠上限", item.stackLimit);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改道具堆叠上限");
                item.stackLimit = Mathf.Max(0, newStackLimit);
                ctx.MarkDirty();
            }
            EditorGUILayout.LabelField("（0 = 无上限）", EditorStyles.miniLabel, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            bool newHideInInventory = EditorGUILayout.Toggle("仓库中隐藏", item.hideInInventory);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改道具 仓库中隐藏");
                item.hideInInventory = newHideInInventory;
                ctx.MarkDirty();
            }

            EditorGUILayout.Space(6);

            // ── 属性值（按来源分组，可折叠）─────────────────────────────────────────
            EditorGUILayout.LabelField("属性", InventoryEditorStyles.Header);

            // 每次绘制前同步属性结构：修正类型/顺序变更（幂等，不影响 Undo 历史）
            item.RebuildAttributes(db);

            if (item.values.Count == 0)
            {
                EditorGUILayout.LabelField(
                    "⚠  该道具暂无属性字段。请先在左侧「道具模板」中添加自定义属性字段，" +
                    "或为道具关联带属性定义的功能标签。",
                    WarnStyle);
                return;
            }

            var defMap = ItemQueryUtil.BuildDefMap(db, item);
            DrawAttributeGroups(ctx, db, item, defMap);
        }

        // ────────────────────────────────────────────────────────────────────────────

        private void DrawAttributeGroups(
            IInventoryEditorContext ctx, InventoryDatabase db, Item item,
            Dictionary<string, AttributeDefinition> defMap)
        {
            // 提前解析模板，后续多处复用
            ItemTemplate tmpl = string.IsNullOrEmpty(item.templateRef)
                ? null : db.GetTemplate(item.templateRef);

            // ── 1. 全量扫描：记录每个 id 来自哪些来源（不去重，用于冲突检测）───────────
            var allIdSources = new Dictionary<string, List<string>>();

            void TrackSources(List<AttributeDefinition> defs, string displaySrc)
            {
                foreach (var def in defs)
                {
                    if (string.IsNullOrEmpty(def.id)) continue;
                    if (!allIdSources.TryGetValue(def.id, out var list))
                        allIdSources[def.id] = list = new List<string>();
                    if (!list.Contains(displaySrc)) list.Add(displaySrc);
                }
            }

            if (tmpl != null)
            {
                TrackSources(tmpl.attributes, $"模板：{item.templateRef}");
                // 模板锁定的标签：以各自标签名为来源（不合并进模板）
                foreach (var tTagName in tmpl.tagRefs)
                {
                    var tTag = db.GetTag(tTagName);
                    if (tTag != null) TrackSources(tTag.attributes, tTagName);
                }
            }

            foreach (var tagName in item.tagRefs)
            {
                var tag = db.GetTag(tagName);
                if (tag != null) TrackSources(tag.attributes, tagName);
            }

            // ── 2. 冲突警告：id 出现在两个以上来源时提示（后出现的来源已被忽略）────────
            bool hasConflict = false;
            foreach (var kv in allIdSources)
                if (kv.Value.Count > 1) { hasConflict = true; break; }

            if (hasConflict)
            {
                EditorGUILayout.LabelField("⚠ 存在重复 ID（仅首个来源生效，其余来源已被忽略）：", WarnStyle);
                foreach (var kv in allIdSources)
                {
                    if (kv.Value.Count <= 1) continue;
                    EditorGUILayout.LabelField($"    \"{kv.Key}\"  ← {string.Join("、", kv.Value)}", WarnStyle);
                }
                EditorGUILayout.Space(4);
            }

            // ── 3. 建立先到先得的 id → 来源 key 映射（与 RebuildAttributes 规则相同）──
            // 顺序：模板自有 → db.FunctionTags 顺序下的标签（模板锁定 + 道具自身合并排序）
            // → 不在 FunctionTags 中的遗留标签（保底）
            var idSource  = new Dictionary<string, string>();
            var tmplTagSet = new HashSet<string>();
            if (tmpl != null) foreach (var t in tmpl.tagRefs) tmplTagSet.Add(t);

            // 模板自身属性 → "__template__"
            if (tmpl != null)
                foreach (var def in tmpl.attributes)
                    if (!string.IsNullOrEmpty(def.id) && !idSource.ContainsKey(def.id))
                        idSource[def.id] = "__template__";

            // 按 db.FunctionTags 顺序分配标签来源
            var processedTagNames = new HashSet<string>();
            foreach (var ft in db.FunctionTags)
            {
                if (!tmplTagSet.Contains(ft.name) && !item.tagRefs.Contains(ft.name)) continue;
                var ftTag = db.GetTag(ft.name);
                if (ftTag == null) { processedTagNames.Add(ft.name); continue; }
                foreach (var def in ftTag.attributes)
                    if (!string.IsNullOrEmpty(def.id) && !idSource.ContainsKey(def.id))
                        idSource[def.id] = ft.name;
                processedTagNames.Add(ft.name);
            }

            // 保底：不在 FunctionTags 中的模板锁定标签
            if (tmpl != null)
                foreach (var tTagName in tmpl.tagRefs)
                {
                    if (processedTagNames.Contains(tTagName)) continue;
                    var tTag = db.GetTag(tTagName);
                    if (tTag == null) continue;
                    foreach (var def in tTag.attributes)
                        if (!string.IsNullOrEmpty(def.id) && !idSource.ContainsKey(def.id))
                            idSource[def.id] = tTagName;
                    processedTagNames.Add(tTagName);
                }

            // 保底：不在 FunctionTags 中的道具自身标签
            foreach (var tagName in item.tagRefs)
            {
                if (processedTagNames.Contains(tagName)) continue;
                var tag = db.GetTag(tagName);
                if (tag == null) continue;
                foreach (var def in tag.attributes)
                    if (!string.IsNullOrEmpty(def.id) && !idSource.ContainsKey(def.id))
                        idSource[def.id] = tagName;
            }

            // ── 4. 模板自身属性组 ─────────────────────────────────────────────────────
            if (tmpl != null)
            {
                var entries = CollectEntries(item, idSource, "__template__");
                if (entries.Count > 0)
                    DrawGroup(ctx, db, defMap, $"模板：{item.templateRef}", "__template__", entries);
            }

            // ── 5 & 6. 功能标签组：按 db.FunctionTags 顺序，模板锁定与道具自身标签统一排序 ──
            var drawnTagGroups = new HashSet<string>();
            foreach (var ft in db.FunctionTags)
            {
                bool fromTemplate = tmplTagSet.Contains(ft.name);
                bool fromItem     = item.tagRefs.Contains(ft.name);
                if (!fromTemplate && !fromItem) continue;

                var entries = CollectEntries(item, idSource, ft.name);
                if (entries.Count > 0)
                {
                    string label = fromTemplate ? $"{ft.name}  （模板锁定）" : ft.name;
                    DrawGroup(ctx, db, defMap, label, ft.name, entries);
                }
                drawnTagGroups.Add(ft.name);
            }

            // 保底：不在 FunctionTags 中的模板锁定标签组
            if (tmpl != null)
                foreach (var tTagName in tmpl.tagRefs)
                {
                    if (drawnTagGroups.Contains(tTagName)) continue;
                    var entries = CollectEntries(item, idSource, tTagName);
                    if (entries.Count > 0)
                        DrawGroup(ctx, db, defMap, $"{tTagName}  （模板锁定）", tTagName, entries);
                    drawnTagGroups.Add(tTagName);
                }

            // 保底：不在 FunctionTags 中的道具自身标签组
            foreach (var tagName in item.tagRefs)
            {
                if (drawnTagGroups.Contains(tagName)) continue;
                var entries = CollectEntries(item, idSource, tagName);
                if (entries.Count > 0)
                    DrawGroup(ctx, db, defMap, tagName, tagName, entries);
            }

            // ── 7. 孤立条目（来源已被删除的历史属性）────────────────────────────────────
            var orphaned = new List<AttributeEntry>();
            foreach (var e in item.values)
                if (!idSource.ContainsKey(e.id)) orphaned.Add(e);

            if (orphaned.Count > 0)
                DrawGroup(ctx, db, defMap, "其他（来源已删除）", "__orphaned__", orphaned);
        }

        /// <summary>绘制一个可折叠分组。</summary>
        private void DrawGroup(
            IInventoryEditorContext ctx, InventoryDatabase db,
            Dictionary<string, AttributeDefinition> defMap,
            string label, string foldKey, List<AttributeEntry> entries)
        {
            bool open = GetFoldout(foldKey);
            bool newOpen = EditorGUILayout.Foldout(open, label, true, GroupStyle);
            if (newOpen != open) SetFoldout(foldKey, newOpen);

            if (newOpen)
            {
                EditorGUI.indentLevel++;
                foreach (var e in entries)
                    DrawEntry(ctx, db, defMap, e);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(2);
        }

        private static List<AttributeEntry> CollectEntries(
            Item item, Dictionary<string, string> idSource, string source)
        {
            var list = new List<AttributeEntry>();
            foreach (var e in item.values)
                if (idSource.TryGetValue(e.id, out var src) && src == source)
                    list.Add(e);
            return list;
        }

        private static void DrawEntry(
            IInventoryEditorContext ctx, InventoryDatabase db,
            Dictionary<string, AttributeDefinition> defMap, AttributeEntry entry)
        {
            defMap.TryGetValue(entry.id, out var def);

            // 从 def.enumTypeRef 解析枚举类型；def 缺失（如模板锁定标签的属性）时
            // 退回到 value.EnumTypeRef（由 RebuildAttributes 在序列化中持久化）。
            EnumType enumType = null;
            if (def != null && def.type == EFieldType.Enum)
                enumType = db.GetEnumType(def.enumTypeRef);
            if (enumType == null && entry.value.Type == EFieldType.Enum
                && !string.IsNullOrEmpty(entry.value.EnumTypeRef))
                enumType = db.GetEnumType(entry.value.EnumTypeRef);

            AttributeFieldDrawer.Draw(ctx, entry.id, entry.value, enumType);

            // 若该属性为枚举类型且枚举类型定义了自定义属性，
            // 则在字段下方以只读方式展示当前选中枚举项的属性值。
            if (enumType != null && enumType.attributes.Count > 0 && !entry.value.IsArray)
            {
                var selectedEnumItem = enumType.GetItemByValue(entry.value.AsEnumValue);
                if (selectedEnumItem != null && selectedEnumItem.attributeValues.Count > 0)
                {
                    enumType.RebuildItemAttributes();

                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUI.indentLevel++;
                        foreach (var attrDef in enumType.attributes)
                        {
                            AttributeEntry attrEntry = null;
                            foreach (var av in selectedEnumItem.attributeValues)
                                if (av.id == attrDef.id) { attrEntry = av; break; }
                            if (attrEntry == null) continue;

                            var nestedEnumType = attrDef.type == EFieldType.Enum
                                ? db.GetEnumType(attrDef.enumTypeRef) : null;
                            AttributeFieldDrawer.Draw(ctx, attrDef.id, attrEntry.value, nestedEnumType);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
            }
        }
    }
}
