using Ale.Toolkit.Runtime;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Ale.Toolkit.Editor.ToolkitEditorL10n;

namespace Ale.Toolkit.Editor
{
    /// <summary>
    /// 标签编辑面板（泛型基类）：左侧主列表（标签行，可拖拽排序）+ 右侧 Inspector
    /// （标签 ID、UI 呈现配置：显示名 / 描述 / 背景图 / 颜色 / 隐藏，及属性字段定义列表）。
    /// 标签在列表中的顺序即其序号，可用作排序依据（见 <see cref="SortFieldKeys.TagOrder"/>）。
    ///
    /// <para>子类仅需绑定 <see cref="EditorMasterListPanel{TDb,T}.GetList"/>（即宿主库的标签列表）。
    /// schema 内引用的枚举类型经 <see cref="IEnumTypeSource"/>（由宿主库实现）解析。</para>
    /// </summary>
    /// <typeparam name="TDb">宿主数据库类型（须实现 <see cref="IEnumTypeSource"/> 方能解析 schema 内的枚举引用）。</typeparam>
    public abstract class EditorTagPanel<TDb> : EditorMasterListPanel<TDb, Tag>
        where TDb : ScriptableObject
    {
        private readonly AttributeDefinitionListDrawer _attrDefsDrawer = new AttributeDefinitionListDrawer();

        #region 主列表配置

        protected override string Noun => "功能标签";
        protected override string RowLabel(Tag t) => t.name;

        protected override Tag CreateNew(TDb db, List<Tag> list) => new Tag(Tr("新标签"));

        protected override void OnInvalidate() => _attrDefsDrawer.Invalidate();

        #endregion

        // ── Inspector ────────────────────────────────────────────────────────────

        public override void DrawInspector(IEditorDbContext<TDb> ctx, Tag tag)
        {
            if (tag == null)
            {
                EditorGUILayout.LabelField(Tr("请选择或新建一个功能标签。"));
                return;
            }

            // schema 内 Enum 字段引用的枚举类型来源（宿主库即为来源）。
            var src = ctx.Database as IEnumTypeSource;

            // ── 基础信息 ─────────────────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            string name = EditorGUILayout.TextField(Tr("标签ID"), tag.name);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改功能标签");
                tag.name = name;
                ctx.MarkDirty();
            }

            EditorGUILayout.Space(6);

            // ── 功能标签属性（UI 显示配置）──────────────────────────────────────
            EditorGUILayout.LabelField(Tr("功能标签属性"), ToolkitEditorStyles.Header);

            // 名称 / 描述：Text（纯文本 fallback + 原生可搜索本地化选择器）
            AttributeFieldDrawer.Draw(ctx, Tr("名称"), tag.displayNameText, null);
            AttributeFieldDrawer.Draw(ctx, Tr("描述"), tag.descriptionText, null);

            // 背景图：直接模式 ObjectField / 授权模式原生 AssetReference 选择器
            if (EditorAssetRefField.DrawSprite(Tr("背景图"), tag, "tagBg", tag.backgroundSprite, tag.backgroundSpriteAddress,
                    out var newBg, out var newBgAddr))
            {
                ctx.RecordUndo("修改功能标签背景图");
                tag.backgroundSprite        = newBg;
                tag.backgroundSpriteAddress = newBgAddr;
                ctx.MarkDirty();
            }

            EditorGUI.BeginChangeCheck();
            var bgColor   = EditorGUILayout.ColorField(Tr("背景颜色"), tag.backgroundColor);
            bool hideInUI = EditorGUILayout.Toggle(Tr("UI中隐藏"), tag.hideInUI);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改功能标签属性");
                tag.backgroundColor = bgColor;
                tag.hideInUI        = hideInUI;
                ctx.MarkDirty();
            }

            EditorGUILayout.Space(6);

            // ── 属性字段 schema ───────────────────────────────────────────────────
            _attrDefsDrawer.Draw(ctx, src, tag.attributes, Tr("道具属性字段"));
            EditorGUILayout.HelpBox(Tr("附加到道具后，会自动添加至道具的「属性字段」列表中"), MessageType.None);
        }
    }
}
