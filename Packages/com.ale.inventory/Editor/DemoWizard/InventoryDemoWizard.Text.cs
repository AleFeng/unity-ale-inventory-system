using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Ale.Inventory.Runtime;
using Ale.Inventory.Runtime.UI;
using Ale.Toolkit.Runtime.UI;

#if  ATK_TMP
using TMPro;
#endif

namespace Ale.Inventory.Editor
{
    // 类型 Inventory 与命名空间段 Ale.Inventory 同名，此处显式别名消歧义（否则 CS0118）。
    using Inventory = global::Ale.Inventory.Runtime.Inventory;

    /// <summary>
    /// 向导专属的本地化字体事件挂载（ATK_TMP &amp;&amp; ATK_LOCALIZATION）：把欢迎窗中配置的本地化字体引用
    /// 写入根节点的 <see cref="LocalizedFontEvent"/>。ATK_TMP 感知的文本 / 按钮构建已下沉至
    /// <see cref="Ale.Toolkit.Editor.UiTextBuilder"/>。
    /// </summary>
    public static partial class InventoryDemoWizard
    {
        #region ATK_TMP && ATK_LOCALIZATION 字体事件辅助

#if ATK_TMP && ATK_LOCALIZATION
        /// <summary>
        /// 在 <paramref name="root"/> 上挂载 <see cref="LocalizedFontEvent"/>，
        /// 将 WelcomeWindow 中配置的本地化字体引用写入组件，
        /// 然后扫描所有子节点以填充 texts / textEvents 列表并建立双向绑定。
        ///
        /// <para>必须在所有子节点（含 <see cref="LocalizedTextEvent"/>）都已添加后调用，
        /// 否则 <see cref="LocalizedFontEvent.RefreshComponents"/> 扫描结果不完整。</para>
        /// </summary>
        static void AttachFontEvent(GameObject root)
        {
            var fontEvent = root.AddComponent<LocalizedFontEvent>();

            // 将 Ale Toolkit 欢迎窗口全局配置的本地化字体引用写入 LocalizedAssetEvent 基类的
            // AssetReference（基类实际用于驱动本地化的字段）。用 SetReference 直接复制表 / 条目引用，
            // 不走 JsonUtility —— 后者不会触发嵌套的 TableReference / TableEntryReference 结构体的
            // ISerializationCallbackReceiver，会丢失 ReferenceType 导致引用变空。
            var localizedFont = Ale.Toolkit.Editor.ToolkitPrefabFonts.LocalizedFont;
            if (localizedFont != null && !localizedFont.IsEmpty)
                fontEvent.AssetReference.SetReference(
                    localizedFont.TableReference, localizedFont.TableEntryReference);

            fontEvent.RefreshComponents();
        }
#endif

        #endregion
    }
}
