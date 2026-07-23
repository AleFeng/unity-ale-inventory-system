using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// UI 共用工具方法集合（静态）。用于沉淀各处 UI 通用的辅助 / 工具方法，避免重复代码；后续可按主题分区扩充。
    /// </summary>
    public static class UIUtility
    {
        #region 悬停弹窗定位
        // 依据弹窗所在 Canvas 的 RenderMode 选择「屏幕坐标 → 世界坐标」换算：
        //   Overlay                        → 无定位相机（世界坐标即屏幕像素，直接落位）
        //   ScreenSpaceCamera / WorldSpace → Canvas.worldCamera（缺省回退 Camera.main）

        // GetWorldCorners 复用缓冲，避免每次定位产生 GC 分配（主线程同步调用，可安全共用）。
        private static readonly Vector3[] Corners = new Vector3[4];

        // 缓存「弹窗 RectTransform → 其根 Canvas」，避免每次定位都执行 GetComponentInParent（逐级向上遍历，开销较大）。
        // 只缓存这一步查找；renderMode / worldCamera / Camera.main 仍每次实时读取（开销极小），故相机变化时不会用到过期值。
        // 缓存的 Canvas 被销毁（如弹窗被改挂到别的 Canvas 下）时会自动重新查找。
        // 缓存的 Canvas 被销毁（如弹窗被改挂到别的 Canvas 下）时会自动重新查找；
        // 但**键**（RectTransform）不会自己消失——弹窗 / 格子销毁后条目仍留在表里，
        // 持有已销毁对象的托管包装，条目数只增不减。故在回填前按阈值清理死键。
        private static readonly Dictionary<RectTransform, Canvas> RootCanvasCache = new Dictionary<RectTransform, Canvas>();

        // 达到该条目数才做一次死键清扫（清扫是 O(n)，不必每次回填都做）。
        private const int CacheSweepThreshold = 64;

        // 清扫复用缓冲：避免每次清扫都分配（主线程同步调用，可安全共用）。
        private static readonly List<RectTransform> DeadKeys = new List<RectTransform>();

        /// <summary>条目数超阈值时，移除键已被销毁的缓存条目。</summary>
        private static void PruneCanvasCache()
        {
            if (RootCanvasCache.Count < CacheSweepThreshold) return;

            DeadKeys.Clear();
            foreach (var kv in RootCanvasCache)
                if (!kv.Key) DeadKeys.Add(kv.Key);

            for (int i = 0; i < DeadKeys.Count; i++)
                RootCanvasCache.Remove(DeadKeys[i]);
            DeadKeys.Clear();
        }

        /// <summary>
        /// 将 <paramref name="rt"/> 定位到光标处（<paramref name="screenPos"/> + <paramref name="cursorOffset"/> 像素偏移），
        /// 再夹取回屏幕内。换算所需的相机由 <paramref name="rt"/> 自身或父级的 Canvas 渲染模式决定。
        /// <paramref name="ignoreCache"/> 为 true 时忽略缓存、强制重新查找根 Canvas 并回填（默认 false，优先用缓存）。
        /// </summary>
        public static void PositionAtCursor(RectTransform rt, Vector2 screenPos, Vector2 cursorOffset, bool ignoreCache = false)
        {
            if (!rt) return;

            var cam = ResolveCanvasCamera(rt, ignoreCache);
            Vector2 target = screenPos + cursorOffset;

            if (!cam)
            {
                // Overlay：世界坐标即屏幕像素，直接落位（保持原 z 所在的画布平面）。
                var p = rt.position;
                rt.position = new Vector3(target.x, target.y, p.z);
            }
            else if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rt, target, cam, out var world))
            {
                // ScreenSpaceCamera / WorldSpace：把屏幕点换算到 rt 所在平面的世界点。
                rt.position = world;
            }

            ClampToScreen(rt, cam);
        }

        /// <summary>
        /// 取 <paramref name="c"/> 自身或父级最近 Canvas 的<b>根</b> Canvas，未找到返回 null。
        /// <para>子 Canvas 继承根 Canvas 的渲染模式与相机，涉及坐标换算 / 层级挂载时一律以根为准。
        /// 拖拽残影挂载、指针坐标换算等三处此前各写了一遍这两行。</para>
        /// <para><b>不带缓存</b>：<c>GetComponentInParent</c> 会逐级向上遍历，请勿在每帧路径上调用；
        /// 每帧需要定位相机时用 <see cref="ResolveCanvasCamera"/>（带缓存）。</para>
        /// </summary>
        public static Canvas ResolveRootCanvas(Component c)
        {
            if (!c) return null;
            var canvas = c.GetComponentInParent<Canvas>();
            return canvas ? (canvas.rootCanvas ? canvas.rootCanvas : canvas) : null;
        }

        /// <summary>
        /// 取 <paramref name="rt"/> 自身或父级最近 Canvas 的定位相机：
        /// Overlay 返回 null；ScreenSpaceCamera / WorldSpace 返回 Canvas.worldCamera（缺省回退 <see cref="Camera.main"/>）。
        /// <paramref name="ignoreCache"/> 为 true 时忽略缓存、强制重新查找根 Canvas 并回填（默认 false）。
        /// </summary>
        public static Camera ResolveCanvasCamera(RectTransform rt, bool ignoreCache = false)
        {
            if (!rt) return null;

            // 命中且未被销毁 → 直接用；未缓存 / 已销毁 / 弹窗改换父级 / 显式忽略缓存 → 重新查找根 Canvas 并回填缓存。
            if (ignoreCache || !RootCanvasCache.TryGetValue(rt, out var root) || !root)
            {
                root = ResolveRootCanvas(rt);
                PruneCanvasCache();
                RootCanvasCache[rt] = root;
            }
            if (!root) return null;

            switch (root.renderMode)
            {
                case RenderMode.ScreenSpaceCamera:
                case RenderMode.WorldSpace:
                    return root.worldCamera ? root.worldCamera : Camera.main;
                default: // ScreenSpaceOverlay
                    return null;
            }
        }

        /// <summary>清空「RectTransform → 根 Canvas」缓存。一般无需手动调用；弹窗大规模销毁 / 重建后可用于释放残留条目。</summary>
        public static void ClearCanvasCache() => RootCanvasCache.Clear();

        /// <summary>
        /// 任一侧超出屏幕时，把弹窗整体移回并使该侧紧贴边界。越界量在屏幕像素空间计算，
        /// 再按定位相机换算回世界位移，因此对三种 RenderMode 均成立。
        /// </summary>
        private static void ClampToScreen(RectTransform rt, Camera cam)
        {
            if (!rt) return;

            rt.GetWorldCorners(Corners); // 世界坐标四角：0=左下 1=左上 2=右上 3=右下
            Vector2 bl = RectTransformUtility.WorldToScreenPoint(cam, Corners[0]);
            Vector2 tr = RectTransformUtility.WorldToScreenPoint(cam, Corners[2]);
            float minX = Mathf.Min(bl.x, tr.x), maxX = Mathf.Max(bl.x, tr.x);
            float minY = Mathf.Min(bl.y, tr.y), maxY = Mathf.Max(bl.y, tr.y);

            float w = Screen.width, h = Screen.height;
            float dx = 0f, dy = 0f;
            if (maxX > w)       dx = w - maxX;   // 右越界 → 左移贴右边界
            if (minX + dx < 0f) dx = -minX;      // 左越界（或比屏幕更宽）→ 贴左边界优先
            if (maxY > h)       dy = h - maxY;   // 上越界 → 下移贴上边界
            if (minY + dy < 0f) dy = -minY;      // 下越界（或比屏幕更高）→ 贴下边界优先
            if (dx == 0f && dy == 0f) return;

            if (!cam)
            {
                // Overlay：屏幕像素位移即世界位移。
                rt.position += new Vector3(dx, dy, 0f);
                return;
            }

            // ScreenSpaceCamera / WorldSpace：把当前锚点的屏幕位置加上位移后，换算回世界点。
            Vector2 curScreen = RectTransformUtility.WorldToScreenPoint(cam, rt.position);
            Vector2 newScreen = curScreen + new Vector2(dx, dy);
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rt, newScreen, cam, out var world))
                rt.position = world;
        }

        #endregion
    }
}
