using System;
using InventorySystem.Runtime;
using UnityEngine;

namespace InventorySystem.Editor
{
    /// <summary>
    /// AssetReference 授权字段绘制器抽象。启用 IS_ADDRESSABLE 时，受约束的 Addressable 编辑器程序集
    /// 在 <c>[InitializeOnLoad]</c> 时把实现注入 <see cref="AttributeFieldDrawer.AddressableFieldDrawer"/>；
    /// 未注入（宏未启用 / 包未装）时，对象引用字段回退为普通 <c>ObjectField</c>。
    ///
    /// core 编辑器程序集对 Addressables <b>零依赖</b>，故此抽象只用 core / Unity 基础类型，
    /// 具体的原生 AssetReference 选择器桥接由注入实现完成（参见 <see cref="InventoryExportResolver"/> 同构的注入模式）。
    /// </summary>
    public interface IAddressableAssetFieldDrawer
    {
        /// <summary>该对象引用字段（可能多行，如图集子 Sprite 选择）在 rect 模式下的高度。</summary>
        float GetHeight(AttributeValue value, int index);

        /// <summary>
        /// 在 <paramref name="rect"/> 内绘制原生 AssetReference 可搜索选择器。当前 GUID 取自
        /// <paramref name="value"/> 在 <paramref name="index"/> 处的授权地址（<see cref="AttributeValue.GetObjAddress"/>）。
        /// 用户改变选择时返回 <c>true</c>，并通过 <paramref name="newGuid"/> 输出新的 GUID（子资源为 <c>GUID[子名]</c>）。
        /// </summary>
        /// <param name="objectType">期望的资源类型（用于选择器过滤 / 校验）。</param>
        /// <param name="label">字段标签（可为 null / 空表示无标签）。</param>
        bool Draw(Rect rect, AttributeValue value, int index, Type objectType, string label, out string newGuid);

        /// <summary>
        /// 固定资源引用字段（配置类具名字段，如 <c>Skill.icon</c>）的选择器高度。
        /// 按 (<paramref name="cacheKey"/>, <paramref name="fieldKey"/>) 缓存 holder 状态。
        /// </summary>
        float GetGuidHeight(object cacheKey, string fieldKey, string currentGuid);

        /// <summary>
        /// 在 <paramref name="rect"/> 内绘制固定资源引用字段的原生 AssetReference 选择器（当前值 = <paramref name="currentGuid"/>）。
        /// 用户改选时返回 <c>true</c> 并通过 <paramref name="newGuid"/> 输出新 GUID（子资源为 <c>GUID[子名]</c>）。
        /// </summary>
        bool DrawGuid(Rect rect, object cacheKey, string fieldKey, string currentGuid, Type objectType, string label, out string newGuid);

        /// <summary>
        /// 把拖入的资源对象转换为授权键（<c>GUID</c>；子资源为 <c>GUID[子名]</c>），并按需登记进 Addressable 分组
        /// （与原生 AssetReference 选择器一致，避免出现「引用了未标记为 Addressable 的资源」导致运行时加载失败）。
        /// 供 Sprite 正方形预览等「拖拽差替」入口在 core 侧使用（core 对 Addressables 零依赖，无法自行完成登记）。
        /// <paramref name="obj"/> 为 null 时返回空串。
        /// </summary>
        string ObjectToKey(UnityEngine.Object obj);
    }
}
