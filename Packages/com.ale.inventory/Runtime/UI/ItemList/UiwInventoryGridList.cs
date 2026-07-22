using UnityEngine;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>列表滚动方向。决定虚拟滚动的主轴与跨轴（跨轴数量按视口对应边自动计算）。</summary>
    public enum EListScrollDirection
    {
        /// <summary>纵向滚动：主轴向下，跨轴向右；列数 = floor(视口宽 ÷ 格子宽)。</summary>
        纵向,
        /// <summary>横向滚动：主轴向右，跨轴向下；行数 = floor(视口高 ÷ 格子高)。</summary>
        横向,
    }

    /// <summary>
    /// 通用<b>网格</b>虚拟滚动列表（二维布局）。在 <see cref="UiwInventoryListBase{TData,TCell}"/>
    /// 的虚拟滚动引擎之上，提供"多列 / 多行"网格布局策略：手动定位固定尺寸格子（不用 GridLayoutGroup），
    /// 只实例化可见区域内的格子并循环复用。
    ///
    /// <para>滚动方向由 <see cref="scrollDirection"/> 切换：<b>纵向</b>滚动时列数按视口宽自动计算、逐行向下；
    /// <b>横向</b>滚动时行数按视口高自动计算、逐列向右。跨轴数量随视口尺寸变化自动重算并重排。</para>
    ///
    /// <para>各系统按需继承本类、闭合泛型并实现 <see cref="UiwInventoryListBase{TData,TCell}.BindCell"/> /
    /// <see cref="UiwInventoryListBase{TData,TCell}.ClearCell"/>（仓库网格见
    /// <see cref="UiwInventoryItemGridList"/>）。</para>
    /// </summary>
    public abstract class UiwInventoryGridList<TData, TCell> : UiwInventoryListBase<TData, TCell>
        where TCell : Component
    {
        [Header("网格布局")]
        [Tooltip("滚动方向：纵向（列数按视口宽自动算）/ 横向（行数按视口高自动算）。")]
        public EListScrollDirection scrollDirection = EListScrollDirection.纵向;
        [Tooltip("格子间距（x=水平间隔，y=垂直间隔），像素。")]
        public Vector2 spacing = new Vector2(6f, 6f);
        [Tooltip("内容起始内边距（x=左，y=上），像素。")]
        public Vector2 padding = new Vector2(6f, 6f);

        // 格子尺寸（像素），从 cellPrefab 的 RectTransform 读取。
        private float _cellWidth  = 72f;
        private float _cellHeight = 72f;
        // 跨轴数量：纵向=列数，横向=行数。默认 1，避免 RecomputeLayout 之前的除零。
        private int _crossCount = 1;

        private bool  IsVertical  => scrollDirection == EListScrollDirection.纵向;
        private float ColStride   => _cellWidth  + spacing.x;   // 一列的步进（含水平间距）
        private float RowStride   => _cellHeight + spacing.y;   // 一行的步进（含垂直间距）

        protected override void MeasureCell()
        {
            if (!cellPrefab) return;
            var rect = cellPrefab.GetComponent<RectTransform>().rect;
            if (rect.width  > 0f) _cellWidth  = rect.width;
            if (rect.height > 0f) _cellHeight = rect.height;
        }

        protected override void RecomputeLayout(Rect viewport)
        {
            // 跨轴数量 = floor((视口跨轴尺寸 - 起始内边距 + 跨轴间距) / 跨轴步进)。
            int cross;
            if (IsVertical)
                cross = Mathf.FloorToInt((viewport.width  - padding.x + spacing.x) / ColStride);
            else
                cross = Mathf.FloorToInt((viewport.height - padding.y + spacing.y) / RowStride);
            cross = Mathf.Max(1, cross);

            if (cross == _crossCount) return;
            _crossCount = cross;
            RegainAllInstances();   // 跨轴数量变化 → 全部格子需重新定位，作废缓存索引强制重排
        }

        protected override void SetContentSize(int count)
        {
            int lineCount = Mathf.CeilToInt(count / (float)_crossCount);   // 主轴方向的行 / 列数
            var size = content.sizeDelta;
            if (IsVertical) size.y = padding.y + lineCount * RowStride;
            else            size.x = padding.x + lineCount * ColStride;
            content.sizeDelta = size;
        }

        protected override int InstancesNeeded(Rect viewport)
        {
            int visibleLines = IsVertical
                ? Mathf.CeilToInt(viewport.height / RowStride)
                : Mathf.CeilToInt(viewport.width  / ColStride);
            return (visibleLines + 1 + bufferCount * 2) * _crossCount;
        }

        protected override int ComputeFirstIndex(Vector2 contentAnchoredPos)
        {
            // 沿主轴的滚动量（纵向：content 上移 y>0；横向：content 左移 x<0，取 -x）。
            float scroll   = IsVertical ? Mathf.Max(0f, contentAnchoredPos.y) : Mathf.Max(0f, -contentAnchoredPos.x);
            float stride   = IsVertical ? RowStride : ColStride;
            float pad      = IsVertical ? padding.y : padding.x;
            int   firstLine = Mathf.FloorToInt(Mathf.Max(0f, scroll - pad) / stride) - bufferCount;
            return firstLine * _crossCount;
        }

        protected override Vector2 PositionOf(int index)
        {
            int line = index / _crossCount;   // 主轴序号（纵向=第几行 / 横向=第几列）
            int slot = index % _crossCount;   // 跨轴序号
            if (IsVertical)
                return new Vector2(padding.x + slot * ColStride, -(padding.y + line * RowStride));
            return new Vector2(padding.x + line * ColStride, -(padding.y + slot * RowStride));
        }

        protected override void SetupInstanceRect(TCell inst)
        {
            var rt = (RectTransform)inst.transform;
            // 锚点与轴心统一为左上角，通过 anchoredPosition 精确定位固定尺寸格子。
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(_cellWidth, _cellHeight);
        }
    }
}
