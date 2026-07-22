using UnityEngine;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 通用<b>顺序</b>虚拟滚动列表（一维纵向、单列）。在 <see cref="UiwInventoryListBase{TData,TCell}"/>
    /// 的虚拟滚动引擎之上，提供"单列纵向"布局策略：Content 高度 = 条目数 × 行高；实例锚点顶部横向拉伸，
    /// 逐行向下排布，滚动时循环复用。
    ///
    /// <para>各系统按需继承本类、闭合泛型并实现 <see cref="UiwInventoryListBase{TData,TCell}.BindCell"/> /
    /// <see cref="UiwInventoryListBase{TData,TCell}.ClearCell"/>（仓库列表见
    /// <see cref="UiwInventoryItemOrderList"/>）。</para>
    /// </summary>
    public abstract class UiwInventoryOrderList<TData, TCell> : UiwInventoryListBase<TData, TCell>
        where TCell : Component
    {
        // 行高（像素）。应与 cellPrefab 的 RectTransform 高度一致。
        private float _cellHeight = 120f;
        
        /// <summary>
        /// 测量 cellPrefab 的 RectTransform 高度，作为行高。若 prefab 高度为 0，则使用默认值 120。
        /// </summary>
        protected override void MeasureCell()
        {
            if (cellPrefab)
            {
                float h = cellPrefab.GetComponent<RectTransform>().rect.height;
                if (h > 0f) _cellHeight = h;
            }
        }

        /// <summary>
        /// 设置 Content 高度 = 条目数 × 行高。Content 锚点顶部横向拉伸，纵向高度随条目数变化。
        /// </summary>
        /// <param name="count"></param>
        protected override void SetContentSize(int count)
        {
            var size = content.sizeDelta;
            size.y = count * _cellHeight;
            content.sizeDelta = size;
        }
        
        /// <summary>
        /// 计算 可见区域需要的实例数 = 可见高度 / 行高 + 1 + bufferCount * 2。+1 是为了避免滚动到最后一行时出现空白。
        /// </summary>
        /// <param name="viewport"></param>
        /// <returns></returns>
        protected override int InstancesNeeded(Rect viewport)
            => Mathf.CeilToInt(viewport.height / _cellHeight) + 1 + bufferCount * 2;
        
        /// <summary>
        /// 计算 第一个可见条目索引 = Content.anchoredPosition.y / 行高 - bufferCount。Content 向上移动时 anchoredPosition.y > 0（UGUI 坐标）。
        /// </summary>
        /// <param name="contentAnchoredPos"></param>
        /// <returns></returns>
        protected override int ComputeFirstIndex(Vector2 contentAnchoredPos)
        {
            // Content 向上移动时 anchoredPosition.y > 0（UGUI 坐标）。
            float scrollY = Mathf.Max(0f, contentAnchoredPos.y);
            return Mathf.FloorToInt(scrollY / _cellHeight) - bufferCount;
        }
        
        /// <summary>
        /// 计算 第 index 个条目的纵向位置 = -index * 行高。Content 锚点顶部横向拉伸，纵向位置由 anchoredPosition.y 控制。
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected override Vector2 PositionOf(int index) => new Vector2(0f, -(index * _cellHeight));
        
        /// <summary>
        /// 设置 实例 RectTransform：锚点顶部横向拉伸，纵向位置由 anchoredPosition.y 控制，宽度随 Content 变化，高度 = 行高。
        /// </summary>
        /// <param name="inst"></param>
        protected override void SetupInstanceRect(TCell inst)
        {
            var rt = (RectTransform)inst.transform;
            // 锚点顶部左右拉伸，通过 anchoredPosition.y 控制纵向位置、宽度随 content 变化。
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, _cellHeight);
        }
    }
}
