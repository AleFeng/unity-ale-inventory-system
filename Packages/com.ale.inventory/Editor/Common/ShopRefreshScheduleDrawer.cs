using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;
using static Ale.Inventory.Editor.InventoryEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// <see cref="ShopRefreshSchedule"/> 的 IMGUI 绘制辅助类。
    /// 绘制：刷新周期 + 刷新时间类型 + 时间点（时:分）+（每周→星期 / 每月→几号）+ 时区。
    /// 供 ShopInspectorPanel 的商品组级与商品级刷新配置共用。
    /// </summary>
    public static class ShopRefreshScheduleDrawer
    {
        // 中文键；显示时经 LocalizedWeekDays() 逐帧翻译（语言切换即时生效）。
        private static readonly string[] WeekDayKeys =
            { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };

        private static string[] LocalizedWeekDays()
        {
            var r = new string[WeekDayKeys.Length];
            for (int i = 0; i < r.Length; i++) r[i] = Tr(WeekDayKeys[i]);
            return r;
        }

        /// <summary>绘制刷新计划。修改时已内部 RecordUndo / MarkDirty。</summary>
        public static void Draw(IInventoryEditorContext ctx, string label, ShopRefreshSchedule schedule)
        {
            if (schedule == null) return;

            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

            EditorGUI.indentLevel++;

            // 刷新周期
            EditorGUI.BeginChangeCheck();
            var newType = (ShopRefreshType)EditorGUILayout.EnumPopup(Tr("刷新周期"), schedule.refreshType);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改刷新周期");
                schedule.refreshType = newType;
                ctx.MarkDirty();
            }

            // 不刷新时无需后续时间配置
            if (schedule.refreshType != ShopRefreshType.Never)
            {
                // 刷新时间类型
                EditorGUI.BeginChangeCheck();
                var newTimeType = (ShopTimeType)EditorGUILayout.EnumPopup(Tr("时间类型"), schedule.timeType);
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改刷新时间类型");
                    schedule.timeType = newTimeType;
                    ctx.MarkDirty();
                }

                // 时间点（时:分，24 小时制）。
                // 用 PrefixLabel 复用标准标签列，使输入框与"时区 ID"等行的字段左对齐；
                // 字段绘制时 indentLevel 归零，避免输入框被二次缩进；
                // 单位用 GUILayout.Label（不受缩进影响、宽度充足）。
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(
                    new GUIContent(Tr("时间点"), Tr("刷新触发的时间点，24 小时制（时 0-23，分 0-59）")));
                int prevIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                EditorGUI.BeginChangeCheck();
                int newHour = EditorGUILayout.IntField(schedule.hour, GUILayout.Width(40));
                GUILayout.Label(Tr("时"), GUILayout.Width(22));
                int newMinute = EditorGUILayout.IntField(schedule.minute, GUILayout.Width(40));
                GUILayout.Label(Tr("分"), GUILayout.Width(22));
                GUILayout.Label("(24h)", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                EditorGUI.indentLevel = prevIndent;
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改刷新时间点");
                    schedule.hour   = Mathf.Clamp(newHour, 0, 23);
                    schedule.minute = Mathf.Clamp(newMinute, 0, 59);
                    ctx.MarkDirty();
                }
                EditorGUILayout.EndHorizontal();

                // 每周 → 星期几
                if (schedule.refreshType == ShopRefreshType.Weekly)
                {
                    EditorGUI.BeginChangeCheck();
                    int newDow = EditorGUILayout.Popup(Tr("星期"), Mathf.Clamp(schedule.dayOfWeek, 0, 6), LocalizedWeekDays());
                    if (EditorGUI.EndChangeCheck())
                    {
                        ctx.RecordUndo("修改刷新星期");
                        schedule.dayOfWeek = newDow;
                        ctx.MarkDirty();
                    }
                }

                // 每月 → 几号
                if (schedule.refreshType == ShopRefreshType.Monthly)
                {
                    EditorGUI.BeginChangeCheck();
                    int newDom = EditorGUILayout.IntField(Tr("几号（1-31）"), schedule.dayOfMonth);
                    if (EditorGUI.EndChangeCheck())
                    {
                        ctx.RecordUndo("修改刷新月日");
                        schedule.dayOfMonth = Mathf.Clamp(newDom, 1, 31);
                        ctx.MarkDirty();
                    }
                }

                // 时区（可选）
                EditorGUI.BeginChangeCheck();
                string newTz = EditorGUILayout.TextField(Tr("时区 ID（可空）"), schedule.timeZoneId ?? string.Empty);
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改刷新时区");
                    schedule.timeZoneId = newTz;
                    ctx.MarkDirty();
                }
            }

            EditorGUI.indentLevel--;
        }
    }
}
