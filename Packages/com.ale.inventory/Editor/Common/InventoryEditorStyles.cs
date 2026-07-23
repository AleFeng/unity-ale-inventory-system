using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 编辑器窗口共用的 GUIStyle 与颜色缓存。延迟初始化（首次访问时构建），避免在非 GUI 线程或静态构造期出错。
    /// </summary>
    public static class InventoryEditorStyles
    {
        private static bool _initialized;

        private static GUIStyle _header;
        private static GUIStyle _redField;
        private static GUIStyle _statusError;
        private static GUIStyle _placeholder;
        private static GUIStyle _colorDot;
        private static GUIStyle _dangerMiniButton;

        /// <summary>重复/错误高亮使用的红色。</summary>
        public static readonly Color ErrorColor = new Color(0.9f, 0.3f, 0.3f);

        /// <summary>列表选中行背景色。</summary>
        public static readonly Color SelectedColor = new Color(0.24f, 0.48f, 0.90f, 0.35f);

        private static void EnsureInit()
        {
            if (_initialized) return;
            _initialized = true;

            _header = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };

            _redField = new GUIStyle(EditorStyles.textField);
            _redField.normal.textColor = ErrorColor;
            _redField.focused.textColor = ErrorColor;

            _statusError = new GUIStyle(EditorStyles.label);
            _statusError.normal.textColor = ErrorColor;

            _placeholder = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14
            };

            _colorDot = new GUIStyle(EditorStyles.label)
            {
                fontSize  = 13,
                alignment = TextAnchor.MiddleCenter,
                padding   = new RectOffset(0, 0, 0, 0),
            };

            _dangerMiniButton = new GUIStyle(EditorStyles.miniButton)
            {
                normal = { textColor = new Color(0.9f, 0.45f, 0.45f) },
            };
        }

        public static GUIStyle Header { get { EnsureInit(); return _header; } }
        public static GUIStyle RedField { get { EnsureInit(); return _redField; } }
        public static GUIStyle StatusError { get { EnsureInit(); return _statusError; } }
        public static GUIStyle Placeholder { get { EnsureInit(); return _placeholder; } }

        /// <summary>危险操作的小按钮（红字），如各系统页签的「删除X」。此前六处各自每帧 new 一个 GUIStyle。</summary>
        public static GUIStyle DangerMiniButton { get { EnsureInit(); return _dangerMiniButton; } }

        /// <summary>在指定矩形绘制一层半透明背景色（用于选中/高亮行）。</summary>
        public static void DrawRowBackground(Rect rect, Color color)
        {
            EditorGUI.DrawRect(rect, color);
        }

        /// <summary>
        /// 在指定矩形中央绘制一个实心圆点（"●"字符），颜色由 <paramref name="dotColor"/> 指定。
        /// 推荐矩形大小为 14×14。
        /// </summary>
        public static void DrawColorDot(Rect rect, Color dotColor)
        {
            EnsureInit();
            var prev = GUI.color;
            GUI.color = dotColor;
            GUI.Label(rect, "●", _colorDot);
            GUI.color = prev;
        }
    }
}
