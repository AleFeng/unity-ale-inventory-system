using System.Collections.Generic;
using Ale.Effect;
using Ale.Effect.Editor;
using Ale.GameplayTags;
using Ale.GameplayTags.Editor;
using Ale.Inventory.Runtime;
using Ale.Toolkit.Editor;
using UnityEditor;
using UnityEngine;
using static Ale.Toolkit.Editor.ToolkitEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 「效果系统」页签（三列，1.12.0）：左=本库声明的 Gameplay 标签目录、中=效果列表（以时长策略充当过滤 / 新建入口）、
    /// 右=效果 Inspector（ID / 显示名 + 内联 toolkit 效果定义绘制器 + 校验摘要）。效果为 toolkit <see cref="EffectDefinition"/>
    /// 原样入库（GAS 式：时长 / 周期 / 叠加 / 标签 / 条件 / 修饰器 / 执行阶段），道具经 <see cref="Item.onUseEffectRefs"/> 引用。
    /// </summary>
    public class EffectSystemTab : EditorThreeColumnTab<EffectDefinition>
    {
        private readonly GameplayTagPanel     _tagPanel       = new GameplayTagPanel();
        private readonly EffectListPanel      _listPanel      = new EffectListPanel();
        private readonly EffectInspectorPanel _inspectorPanel = new EffectInspectorPanel();

        private IEditorMasterListPanel<InventoryDatabase>[] _leftPanels;

        protected override IEditorMasterListPanel<InventoryDatabase>[] LeftPanels
            => _leftPanels ??= new IEditorMasterListPanel<InventoryDatabase>[] { _tagPanel };

        protected override string EntityNoun        => "效果";
        protected override float  DeleteButtonWidth => 68f;

        protected override List<EffectDefinition> EntityList(InventoryDatabase db) => db.Effects;

        protected override EffectDefinition DrawEntityList(IInventoryEditorContext ctx, EffectDefinition displaySelected)
            => _listPanel.DrawList(ctx, displaySelected);

        protected override EffectDefinition ConsumePendingSelect() => _listPanel.ConsumePendingSelect();

        protected override void DrawEntityInspector(IInventoryEditorContext ctx, EffectDefinition entity)
            => _inspectorPanel.DrawInspector(ctx, entity);
    }

    /// <summary>效果「伪模板」：以时长策略充当中列的过滤页签 / 新建入口 / 行色点（效果没有模板机制）。名称经 Tr 翻译后作为键。</summary>
    public sealed class EffectPolicyTemplate
    {
        public readonly string          key;
        public readonly EDurationPolicy policy;
        public readonly Color           color;

        private EffectPolicyTemplate(string key, EDurationPolicy policy, Color color)
        {
            this.key    = key;
            this.policy = policy;
            this.color  = color;
        }

        public string Name => Tr(key);

        public static readonly List<EffectPolicyTemplate> All = new List<EffectPolicyTemplate>
        {
            new EffectPolicyTemplate("瞬时", EDurationPolicy.Instant,     new Color(0.55f, 0.75f, 0.95f)),
            new EffectPolicyTemplate("持续", EDurationPolicy.HasDuration, new Color(0.55f, 0.85f, 0.55f)),
            new EffectPolicyTemplate("无限", EDurationPolicy.Infinite,    new Color(0.92f, 0.72f, 0.40f)),
        };

        public static EffectPolicyTemplate Of(EDurationPolicy policy)
        {
            foreach (var t in All) if (t.policy == policy) return t;
            return All[0];
        }

        public static EffectPolicyTemplate ByName(string name)
        {
            foreach (var t in All) if (t.Name == name) return t;
            return null;
        }
    }

    /// <summary>效果列表面板（中列）：策略过滤 + 搜索 + 按策略新建 / 快速添加，每行 id / 显示名 / 策略 / 内容摘要。</summary>
    public class EffectListPanel : EditorEntityListPanel<EffectDefinition, EffectPolicyTemplate>
    {
        public EffectListPanel() : base("InventoryEffectListDrag") { }

        protected override EInventoryEntityKind Kind => EInventoryEntityKind.Effect;
        protected override string Noun => "效果";

        protected override List<EffectDefinition>     Entities(InventoryDatabase db)  => db.Effects;
        protected override List<EffectPolicyTemplate> Templates(InventoryDatabase db) => EffectPolicyTemplate.All;
        protected override string TemplateName(EffectPolicyTemplate t) => t.Name;
        protected override string TemplateRefOf(EffectDefinition e)    => EffectPolicyTemplate.Of(e.durationPolicy).Name;
        protected override string IdOf(EffectDefinition e)             => e.id;

        protected override Color RowDotColor(InventoryDatabase db, EffectDefinition e)
            => EffectPolicyTemplate.Of(e.durationPolicy).color;

        protected override bool Matches(InventoryDatabase db, EffectDefinition e, string term)
        {
            if (string.IsNullOrEmpty(term)) return true;
            term = term.ToLowerInvariant();
            return (!string.IsNullOrEmpty(e.id) && e.id.ToLowerInvariant().Contains(term))
                || (!string.IsNullOrEmpty(e.displayName) && e.displayName.ToLowerInvariant().Contains(term));
        }

        protected override EffectDefinition AddFromTemplate(IInventoryEditorContext ctx, string templateName)
        {
            var db = ctx.Database;
            var t  = EffectPolicyTemplate.ByName(templateName) ?? EffectPolicyTemplate.All[0];

            ctx.RecordUndo("添加效果");
            var e = new EffectDefinition(GenerateId(db, "effect_", id => db.GetEffect(id) != null), t.policy)
            {
                displayName = Tr("新效果"),
            };
            if (t.policy == EDurationPolicy.HasDuration)
                e.duration = EffectMagnitude.Scalable(1f);   // HasDuration 要求时长 > 0
            e.Normalize();
            db.Effects.Add(e);
            ctx.MarkDirty();
            return e;
        }

        protected override EffectDefinition QuickAdd(IInventoryEditorContext ctx)
        {
            var db = ctx.Database;
            if (db.Effects.Count == 0)
                return AddFromTemplate(ctx, EffectPolicyTemplate.All[0].Name);

            ctx.RecordUndo("快速添加效果");
            var clone = db.Effects[db.Effects.Count - 1].Clone();
            clone.id = GenerateId(db, "effect_", id => db.GetEffect(id) != null);
            clone.Normalize();
            db.Effects.Add(clone);
            ctx.MarkDirty();
            return clone;
        }

        protected override void DrawRowColumns(InventoryDatabase db, EffectDefinition e,
            Rect keyRow, float contentX, float contentRight, float valY, float valH)
        {
            float w     = Mathf.Max(0f, contentRight - contentX);
            float idW   = Mathf.Min(100f, w * 0.28f);
            float polW  = 46f;
            float sumW  = Mathf.Min(130f, w * 0.30f);
            float nameX = contentX + idW + Pad;
            float sumX  = contentRight - sumW;
            float polX  = sumX - Pad - polW;
            float nameW = Mathf.Max(0f, polX - Pad - nameX);

            GUI.Label(new Rect(contentX, keyRow.y, idW,   keyRow.height), "ID",        KeyStyle);
            GUI.Label(new Rect(nameX,    keyRow.y, nameW, keyRow.height), Tr("显示名"), KeyStyle);
            GUI.Label(new Rect(polX,     keyRow.y, polW,  keyRow.height), Tr("策略"),   KeyStyle);
            GUI.Label(new Rect(sumX,     keyRow.y, sumW,  keyRow.height), Tr("内容"),   KeyStyle);

            GUI.Label(new Rect(contentX, valY, idW, valH), string.IsNullOrWhiteSpace(e.id) ? Tr("(空 ID)") : e.id, IdStyle);
            GUI.Label(new Rect(nameX, valY, nameW, valH), string.IsNullOrEmpty(e.displayName) ? "—" : e.displayName, SubStyle);
            GUI.Label(new Rect(polX,  valY, polW,  valH), EffectPolicyTemplate.Of(e.durationPolicy).Name, SubStyle);
            GUI.Label(new Rect(sumX,  valY, sumW,  valH), Summary(e), SubStyle);
        }

        private static string Summary(EffectDefinition d)
        {
            int mods = d.modifiers != null ? d.modifiers.Count : 0;
            int exec = 0;
            if (d.executions?.groups != null)
                foreach (var g in d.executions.groups)
                    if (g?.items != null) exec += g.items.Count;
            int tags = d.assetTags != null && d.assetTags.tags != null ? d.assetTags.tags.Count : 0;
            return Fmt("{0} 修饰 · {1} 执行 · {2} 标签", mods, exec, tags);
        }
    }

    /// <summary>效果 Inspector（右列）：ID（查重）/ 显示名 + 内联 toolkit 效果定义（隐藏定义内的 id / 显示名两行）+ 校验摘要。</summary>
    public class EffectInspectorPanel
    {
        private EffectDefinition _lastExpanded;

        public void DrawInspector(IInventoryEditorContext ctx, EffectDefinition effect)
        {
            if (effect == null)
            {
                EditorGUILayout.LabelField(Tr("请选择或新建一个效果。"));
                return;
            }

            EditorEntityHeader.DrawIdField(ctx, "效果", effect.id,
                ctx.DuplicateIdsOf(EInventoryEntityKind.Effect), v => effect.id = v,
                dupHint: Tr("⚠ ID 重复或为空（导出时空 ID 条目将被跳过）"));

            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.TextField(Tr("显示名"), effect.displayName ?? string.Empty);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改效果显示名");
                effect.displayName = newName;
                ctx.MarkDirty();
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(Tr("效果定义"), ToolkitEditorStyles.Header);
            EditorGUILayout.LabelField(Tr("定义内的 id / 显示名由上方字段同步；道具以 ID 引用本效果。"), EditorStyles.miniLabel);
            DrawInlineDefinition(ctx, effect);

            // 校验摘要（错误阻断导出；警告仅提示）
            var msgs = new List<string>();
            effect.Validate(msgs);
            if (msgs.Count > 0)
            {
                EditorGUILayout.Space(4);
                foreach (var m in msgs)
                {
                    bool warn = EffectDefinition.IsWarning(m);
                    EditorGUILayout.HelpBox(warn ? m.Substring(EffectDefinition.WarningPrefix.Length).Trim() : m,
                        warn ? MessageType.Warning : MessageType.Error);
                }
            }
        }

        private void DrawInlineDefinition(IInventoryEditorContext ctx, EffectDefinition effect)
        {
            var so = ctx.Serialized;
            var db = ctx.Database;
            if (so == null || db == null)
            {
                EditorGUILayout.HelpBox(Tr("效果定义编辑暂不可用（无序列化对象）。"), MessageType.None);
                return;
            }

            so.Update();
            var arr = so.FindProperty("effects");
            int idx = db.Effects.IndexOf(effect);
            if (arr == null || idx < 0 || idx >= arr.arraySize)
            {
                EditorGUILayout.HelpBox(Tr("效果定义编辑暂不可用。"), MessageType.None);
                return;
            }
            var prop = arr.GetArrayElementAtIndex(idx);

            if (!ReferenceEquals(_lastExpanded, effect))
            {
                _lastExpanded   = effect;
                prop.isExpanded = true;
            }

            bool prevShow = EffectDefinitionDrawerHooks.ShowIdentityFields;
            EffectDefinitionDrawerHooks.ShowIdentityFields = false;
            try
            {
                EditorGUILayout.PropertyField(prop, new GUIContent(Tr("效果定义")), true);
            }
            finally
            {
                EffectDefinitionDrawerHooks.ShowIdentityFields = prevShow;
            }
            so.ApplyModifiedProperties();
        }
    }

    /// <summary>
    /// Gameplay 标签面板（效果页左列）：绑定 <see cref="InventoryDatabase.GameplayTags"/>；检视 名称（点分层级，非法红框）/ 注释，
    /// 并列出会被隐式登记的祖先标签。注册数据库时并入 toolkit 标签注册表；运行时匹配不依赖它。
    /// </summary>
    public class GameplayTagPanel : EditorMasterListPanel<GameplayTagDefinition>
    {
        protected override List<GameplayTagDefinition> GetList(InventoryDatabase db) => db.GameplayTags;
        protected override string Noun => "Gameplay 标签";

        protected override string HeaderHelp
            => Tr("本库声明的层级标签（如 Status.Buff.Might）。注册数据库时并入 toolkit 标签注册表，供效果的标签字段下拉与「未登记」提示；运行时匹配不依赖此表。");

        protected override string RowLabel(GameplayTagDefinition item)
            => string.IsNullOrEmpty(item.name) ? Tr("(空 ID)") : item.name;

        protected override GameplayTagDefinition CreateNew(InventoryDatabase db, List<GameplayTagDefinition> list)
        {
            int n = list.Count + 1;
            string name;
            do { name = "Tag.New" + n; n++; } while (Contains(list, name));
            return new GameplayTagDefinition(name);
        }

        private static bool Contains(List<GameplayTagDefinition> list, string name)
        {
            foreach (var t in list) if (t != null && t.name == name) return true;
            return false;
        }

        public override void DrawInspector(IInventoryEditorContext ctx, GameplayTagDefinition tag)
        {
            if (tag == null)
            {
                EditorGUILayout.LabelField(Tr("请选择或新建一个 Gameplay 标签。"));
                return;
            }

            EditorGUILayout.LabelField(Tr("基础信息"), ToolkitEditorStyles.Header);
            bool valid = GameplayTag.IsValidName(tag.name);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(Tr("名称"));
            string name = EditorGUILayout.TextField(tag.name ?? string.Empty,
                valid ? EditorStyles.textField : ToolkitEditorStyles.RedField);
            EditorGUILayout.EndHorizontal();
            string comment = EditorGUILayout.TextField(Tr("注释"), tag.comment ?? string.Empty);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改 Gameplay 标签");
                tag.name    = name;
                tag.comment = comment;
                ctx.MarkDirty();
            }

            if (!valid)
            {
                EditorGUILayout.LabelField(Tr("⚠ 名称不合法：段不能为空，段内不能含空白或 '/'"), ToolkitEditorStyles.StatusError);
                return;
            }

            string norm = GameplayTag.Normalize(tag.name);
            var segs = norm.Split('.');
            if (segs.Length > 1)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField(Tr("隐式登记的祖先"), ToolkitEditorStyles.Header);
                for (int i = segs.Length - 1; i >= 1; i--)
                    EditorGUILayout.LabelField("• " + string.Join(".", segs, 0, i), EditorStyles.miniLabel);
            }
        }
    }

    /// <summary>把数据库声明的 Gameplay 标签并入 toolkit 标签注册表并刷新编辑器标签目录（仅在标签集合变化时执行）。</summary>
    public static class InventoryGameplayTagSync
    {
        private static string _lastSignature;

        public static void Sync(InventoryDatabase db)
        {
            if (!db) return;
            var sb = new System.Text.StringBuilder();
            foreach (var t in db.GameplayTags)
                if (t != null) sb.Append(t.name).Append('\n');
            string sig = sb.ToString();
            if (sig == _lastSignature) return;
            _lastSignature = sig;

            GameplayTagRuntime.Register(db.GameplayTags);
            GameplayTagEditorCatalog.Rebuild();
        }
    }
}
