using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 通用「过滤页签栏」绘制：工具栏样式的「全部」+ 每个选项一个 Toggle 按钮，互斥单选。
    /// 供道具 / 仓库 / 商店列表面板复用，按各自「模板」列表筛选条目。
    /// </summary>
    public static class EditorFilterTabs
    {
        /// <summary>
        /// 绘制过滤页签栏，返回新的选中项（<c>null</c> = 全部）。
        /// </summary>
        /// <param name="current">当前选中项（<c>null</c> = 全部）。</param>
        /// <param name="options">候选项数据源。</param>
        /// <param name="nameOf">从候选项取显示 / 选中名称（同时作为按钮标签与选中键）。</param>
        public static string Draw<T>(string current, IReadOnlyList<T> options, Func<T, string> nameOf)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            bool allActive = current == null;
            if (GUILayout.Toggle(allActive, "全部", EditorStyles.toolbarButton) && !allActive)
                current = null;

            for (int i = 0; i < options.Count; i++)
            {
                string name   = nameOf(options[i]);
                bool   active = current == name;
                if (GUILayout.Toggle(active, name, EditorStyles.toolbarButton) && !active)
                    current = name;
            }

            EditorGUILayout.EndHorizontal();
            return current;
        }
    }
}
