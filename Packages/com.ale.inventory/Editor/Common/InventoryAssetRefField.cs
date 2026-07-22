using UnityEngine;
using UnityEditor;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 配置类<b>固定资源字段</b>（具名字段，如 <c>Skill.icon</c> / <c>FunctionTag.backgroundSprite</c>）的编辑器绘制辅助。
    /// 与属性系统对象字段一致：
    /// <list type="bullet">
    ///   <item>未注入（IS_ADDRESSABLE 关 / 包未装）：绘制普通 <c>ObjectField</c>，写实时引用、地址置空。</item>
    ///   <item>已注入：经 <see cref="AttributeFieldDrawer.AddressableFieldDrawer"/> 绘制原生 AssetReference 可搜索选择器，
    ///   写授权 GUID、实时引用置空（配置不再硬引用资源）。</item>
    /// </list>
    /// core 编辑器程序集对 Addressables 零依赖，具体选择器由受约束的 Addressable 编辑器程序集注入。
    /// </summary>
    public static class InventoryAssetRefField
    {
        /// <summary>
        /// 绘制一个固定 <see cref="Sprite"/> 资源引用字段（GUILayout）。返回是否变更；
        /// 变更时经 <paramref name="newLive"/> / <paramref name="newAddress"/> 输出新值（二者互斥，未选中方为 null）。
        /// 调用方据返回值自行 <c>RecordUndo</c> + 赋值 + <c>MarkDirty</c>。
        /// </summary>
        /// <param name="cacheKey">holder 缓存键对象（通常传所属配置对象），配合 <paramref name="fieldKey"/> 唯一标识该字段。</param>
        /// <param name="fieldKey">字段标识（如 "skillIcon" / "tagBg"）。</param>
        public static bool DrawSprite(string label, object cacheKey, string fieldKey,
            Sprite currentLive, string currentAddress, out Sprite newLive, out string newAddress)
        {
            newLive    = currentLive;
            newAddress = currentAddress;

            var drawer = AttributeFieldDrawer.AddressableFieldDrawer;
            if (drawer != null)
            {
                float h  = drawer.GetGuidHeight(cacheKey, fieldKey, currentAddress);
                var rect = EditorGUILayout.GetControlRect(true, h);
                if (drawer.DrawGuid(rect, cacheKey, fieldKey, currentAddress, typeof(Sprite), label, out string guid))
                {
                    newAddress = guid;
                    newLive    = null;   // 授权模式不保留硬引用
                    return true;
                }
                return false;
            }

            EditorGUI.BeginChangeCheck();
            var picked = (Sprite)EditorGUILayout.ObjectField(label, currentLive, typeof(Sprite), false);
            if (EditorGUI.EndChangeCheck())
            {
                newLive    = picked;
                newAddress = null;       // 直接模式不保留地址
                return true;
            }
            return false;
        }
    }
}
