using UnityEngine;

namespace Ale.Toolkit.Runtime
{
    /// <summary>
    /// 覆盖式 UI（弹窗 / 拖拽幽灵 / 下拉窗等）的无状态辅助。
    ///
    /// <para>这类 UI 的共同需求有两个：① 找一个合适的 Canvas 作为父节点；
    /// ② 实例化后把整棵子树强制设置到指定 Layer。后者<b>必须递归</b>——带独立 Canvas 的子级
    /// 也须落到目标 Layer，仅渲染该 Layer 的 UI 摄像机方可显示。</para>
    ///
    /// <para>本类只提供纯函数。「根节点」「是否强制 Layer」「Layer 值」这些**配置**属于宿主组件，
    /// 应作为其序列化字段持有（移动它们会破坏既有预制体 / 场景中已保存的取值）。</para>
    /// </summary>
    public static class CoverUiUtil
    {
        /// <summary>
        /// 将指定对象及其<b>所有子级</b>递归设置到目标 Layer。
        /// <paramref name="go"/> 为空时无操作。
        /// </summary>
        public static void SetLayerRecursively(GameObject go, int layer)
        {
            if (!go) return;
            go.layer = layer;
            var t = go.transform;
            for (int i = 0; i < t.childCount; i++)
                SetLayerRecursively(t.GetChild(i).gameObject, layer);
        }

        /// <summary>查找场景中任意一个 <see cref="Canvas"/> 的 Transform，作为覆盖式 UI 的兜底父节点；无则返回 null。</summary>
        public static Transform FindCanvasTransform()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            return canvas ? canvas.transform : null;
        }

        /// <summary>把 Layer 序号约束到合法范围 0~31。</summary>
        public static int ClampLayer(int layer) => Mathf.Clamp(layer, 0, 31);
    }
}
