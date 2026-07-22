#if IS_LOCALIZATION
using UnityEngine;
using UnityEngine.Localization;

namespace InventorySystem.Editor
{
    /// <summary>
    /// 内部辅助 ScriptableObject：为 AttributeFieldDrawer 提供一个可被 SerializedObject 绑定的
    /// LocalizedString 容器，使编辑器能使用 Unity 原生的 LocalizedString 属性控件（表 / 条目选择器）。
    ///
    /// 该对象不作为资产持久化（HideFlags.DontSave），仅在编辑器内存中存活，域重载时自动销毁。
    /// </summary>
    internal class LocalizedStringHolder : ScriptableObject
    {
        public LocalizedString value;
    }
}
#endif
