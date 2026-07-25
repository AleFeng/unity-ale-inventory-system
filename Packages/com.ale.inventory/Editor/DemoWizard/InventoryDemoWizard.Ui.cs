using Ale.Toolkit.Runtime.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Ale.Inventory.Runtime;
using Ale.Inventory.Runtime.UI;
using static Ale.Toolkit.Editor.UiPrefabBuilder;

#if  IS_TMP
using TMPro;
#endif

namespace Ale.Inventory.Editor
{
    // 类型 Inventory 与命名空间段 Ale.Inventory 同名，此处显式别名消歧义（否则 CS0118）。
    using Inventory = global::Ale.Inventory.Runtime.Inventory;

    /// <summary>
    /// 向导专属的 UI 构建辅助：仍依赖领域数据（Demo 精灵 / 预制体路径）或本地化字体事件的少数几件。
    /// 与领域无关的底层 UGUI 原语已下沉至 <see cref="Ale.Toolkit.Editor.UiPrefabBuilder"/>，
    /// 文本 / 按钮构建已下沉至 <see cref="Ale.Toolkit.Editor.UiTextBuilder"/>。
    /// </summary>
    public static partial class InventoryDemoWizard
    {
        #region 向导专属 UI 构建

        /// <summary>
        /// 在 <paramref name="parent"/> 下建一个方形「IconFrame」（占据布局槽位）内含
        /// 「QualityBackground」（全覆盖品质底图）+「Icon」（四边内缩 4px 的图标）。
        /// 两个 Image 均为白色、保持宽高比、不接收射线（装饰用，不挡点击）。经出参传出两个 Image。
        /// <para>制作蓝图条目（56px）与商店商品条目（44px）共用此结构；此前商店的两张图未显式关闭
        /// raycastTarget（默认接收射线），现随制作侧统一为不接收（不影响外观，仅不再无谓拦截射线）。</para>
        /// </summary>
        static void MakeIconFrame(Transform parent, float size, out Image quality, out Image icon)
        {
            var frameGo = ChildGameObject("IconFrame", parent);
            frameGo.AddComponent<RectTransform>();
            SetLayoutElement(frameGo, minW: size, prefW: size, minH: size, prefH: size);

            var qualityGo = ChildGameObject("QualityBackground", frameGo.transform);
            Stretch(qualityGo.AddComponent<RectTransform>());
            quality = qualityGo.AddComponent<Image>();
            quality.color = Color.white; quality.preserveAspect = true; quality.raycastTarget = false;
            quality.sprite = LoadSprite(SpriteQualityPoor);

            var iconGo = ChildGameObject("Icon", frameGo.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(4f, 4f); iconRt.offsetMax = new Vector2(-4f, -4f);
            icon = iconGo.AddComponent<Image>();
            icon.color = Color.white; icon.preserveAspect = true; icon.raycastTarget = false;
        }

        /// <summary>
        /// 保存 Prefab 到指定路径并销毁临时 GameObject。
        /// <para><b>就地覆盖</b>：路径上已有预制体时 <see cref="PrefabUtility.SaveAsPrefabAsset(GameObject, string, out bool)"/>
        /// 只替换其内容，<c>.meta</c>（即资产 GUID）随路径保留。<b>切勿在此之前删除旧资产</b>——
        /// 先删再建会换掉 GUID，使「单独重生成某个被依赖的预制体」静默打断依赖它的预制体的引用
        /// （生成窗口的依赖对话框只向下遍历依赖、不向上遍历被依赖者，故不会提示）。</para>
        /// </summary>
        static void SavePrefab(GameObject root, string path)
        {
#if IS_TMP && IS_LOCALIZATION
            // 双宏下统一在保存前挂本地化字体事件（此前各 builder 尾部各写一遍；装备类曾漏挂，
            // 收口到此处后自动补齐）。AttachFontEvent 会扫描全部子节点建立字体绑定，故须在层级搭好后调用。
            AttachFontEvent(root);
#endif
            MovePrimaryUiwToTop(root);
            PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
            Object.DestroyImmediate(root);

            if (saved) Debug.Log("[InventoryDemoWizard] 预制体已保存：" + path);
            else       Debug.LogError("[InventoryDemoWizard] 预制体保存失败：" + path);
        }

        /// <summary>
        /// 每个 builder 开头的固定一步：由预制体名解析出目标资产路径（<see cref="Pfb"/>），
        /// 顺带确保其所在文件夹存在，返回该路径。
        /// <para><b>刻意不删除同名旧资产</b>——保住资产 GUID，理由见 <see cref="SavePrefab"/>。
        /// 内容由 <see cref="SavePrefab"/> 整体替换，不会有旧节点残留。</para>
        /// </summary>
        static string BeginPrefab(string prefabName)
        {
            string path = Pfb(prefabName);
            int sep = path.LastIndexOf('/');
            if (sep > 0) EnsureFolder(path.Substring(0, sep));
            return path;
        }

        #endregion
    }
}
