using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 功能标签面板：左侧主列表（功能标签行，可拖拽排序）+ 右侧 Inspector（名称、说明、属性字段定义列表）。
    /// 功能标签的顺序会影响「整理列表」中「功能标签」的排序依据。
    /// </summary>
    public class FunctionTagPanel
    {
        // ── 主列表 ReorderableList 状态 ────────────────────────────────────────────
        private ReorderableList         _masterList;
        private List<FunctionTag>       _boundList;
        private int                     _selectedIndex      = -1;
        private int                     _pendingDeleteIndex = -1;
        private IInventoryEditorContext _masterCtx;

        // ── 属性字段定义列表绘制器（实例持有，保持拖拽排序缓存）──────────────────────
        private readonly AttributeDefinitionListDrawer _attrDefsDrawer = new AttributeDefinitionListDrawer();

        // ── 主列表 ────────────────────────────────────────────────────────────────

        public int DrawMasterList(IInventoryEditorContext ctx, int selectedIndex)
        {
            var db   = ctx.Database;
            var list = db.FunctionTags;
            _masterCtx = ctx;

            if (_masterList == null || !ReferenceEquals(_boundList, list))
            {
                _selectedIndex = Mathf.Clamp(selectedIndex, -1, list.Count - 1);
                BuildMasterList(list);
            }
            else
            {
                // 外部同步：当调用方（Tab）重置了索引（如选中了中间列的条目），
                // 以调用方的值为准，避免面板返回旧索引触发错误的 ActivateEntity。
                int clamped = Mathf.Clamp(selectedIndex, -1, list.Count - 1);
                if (_selectedIndex != clamped)
                {
                    _selectedIndex    = clamped;
                    _masterList.index = clamped;
                }
            }

            // ── 标题栏 + 添加按钮 ──────────────────────────────────────────────
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("功能标签", InventoryEditorStyles.Header);
            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                ctx.RecordUndo("添加功能标签");
                list.Add(new FunctionTag("新标签"));
                ctx.MarkDirty();
                _selectedIndex    = list.Count - 1;
                if (_masterList != null) _masterList.index = _selectedIndex;
            }
            EditorGUILayout.EndHorizontal();

            if (_masterList != null)
            {
                _masterList.DoLayoutList();

                // ── 延迟删除（在 DoLayoutList 完成后处理，避免在回调中修改集合）──────
                if (_pendingDeleteIndex >= 0)
                {
                    int di = _pendingDeleteIndex;
                    _pendingDeleteIndex = -1;
                    if (di < list.Count)
                    {
                        ctx.RecordUndo("删除功能标签");
                        list.RemoveAt(di);
                        ctx.MarkDirty();
                        _selectedIndex = Mathf.Clamp(_selectedIndex, -1, list.Count - 1);
                        _masterList.index = _selectedIndex;
                    }
                }
            }

            return _selectedIndex;
        }

        private void BuildMasterList(List<FunctionTag> list)
        {
            _boundList  = list;
            _masterList = new ReorderableList(list, typeof(FunctionTag),
                draggable: true, displayHeader: false,
                displayAddButton: false, displayRemoveButton: false);

            _masterList.elementHeight = 22f;
            _masterList.index         = _selectedIndex;

            _masterList.drawElementBackgroundCallback = (rect, _, active, _) =>
            {
                if (active)
                    InventoryEditorStyles.DrawRowBackground(rect, InventoryEditorStyles.SelectedColor);
            };

            _masterList.drawElementCallback = (rect, index, _, _) =>
            {
                if (index < 0 || index >= list.Count) return;
                var t      = list[index];
                float cy   = rect.y + (rect.height - EditorGUIUtility.singleLineHeight) * 0.5f;
                var delRect   = new Rect(rect.xMax - 22, cy, 20, EditorGUIUtility.singleLineHeight);
                var labelRect = new Rect(rect.x, cy, rect.xMax - 22 - rect.x - 4,
                    EditorGUIUtility.singleLineHeight);
                GUI.Label(labelRect, t.name);
                if (GUI.Button(delRect, "✕", EditorStyles.miniButton))
                    _pendingDeleteIndex = index;
            };

            _masterList.onSelectCallback = rl => _selectedIndex = rl.index;

            _masterList.onReorderCallback = _ =>
            {
                _masterCtx.RecordUndo("调整功能标签顺序");
                _masterCtx.MarkDirty();
            };
        }

        /// <summary>数据库切换或外部重置时调用，清空主列表缓存。</summary>
        public void Invalidate()
        {
            _masterList         = null;
            _boundList          = null;
            _selectedIndex      = -1;
            _pendingDeleteIndex = -1;
            _attrDefsDrawer.Invalidate();
        }

        // ── Inspector ────────────────────────────────────────────────────────────

        public void DrawInspector(IInventoryEditorContext ctx, FunctionTag tag)
        {
            if (tag == null)
            {
                EditorGUILayout.LabelField("请选择或新建一个功能标签。");
                return;
            }

            // ── 基础信息 ─────────────────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            string name = EditorGUILayout.TextField("标签ID", tag.name);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改功能标签");
                tag.name = name;
                ctx.MarkDirty();
            }

            EditorGUILayout.Space(6);

            // ── 功能标签属性（UI 显示配置）──────────────────────────────────────
            EditorGUILayout.LabelField("功能标签属性", InventoryEditorStyles.Header);

            // 名称 / 描述：Text（纯文本 fallback + 原生可搜索本地化选择器）
            AttributeFieldDrawer.Draw(ctx, "名称", tag.displayNameText, null);
            AttributeFieldDrawer.Draw(ctx, "描述", tag.descriptionText, null);

            // 背景图：直接模式 ObjectField / 授权模式原生 AssetReference 选择器
            if (InventoryAssetRefField.DrawSprite("背景图", tag, "tagBg", tag.backgroundSprite, tag.backgroundSpriteAddress,
                    out var newBg, out var newBgAddr))
            {
                ctx.RecordUndo("修改功能标签背景图");
                tag.backgroundSprite        = newBg;
                tag.backgroundSpriteAddress = newBgAddr;
                ctx.MarkDirty();
            }

            EditorGUI.BeginChangeCheck();
            var bgColor   = EditorGUILayout.ColorField("背景颜色", tag.backgroundColor);
            bool hideInUI = EditorGUILayout.Toggle("UI中隐藏", tag.hideInUI);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改功能标签属性");
                tag.backgroundColor = bgColor;
                tag.hideInUI        = hideInUI;
                ctx.MarkDirty();
            }

            EditorGUILayout.Space(6);

            // ── 道具属性字段 ──────────────────────────────────────────────────────
            _attrDefsDrawer.Draw(ctx, tag.attributes, "道具属性字段");
            EditorGUILayout.HelpBox("附加到道具后，会自动添加至道具的「属性字段」列表中", MessageType.None);
        }
    }
}
