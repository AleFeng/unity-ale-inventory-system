using System;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 仓库自动整理的单条优先级规则。<see cref="field"/> 指定排序依据的字段 ID，
    /// 特殊值 <c>"__id__"</c> 表示按道具 ID 排序；<see cref="ascending"/> 控制升/降序。
    /// </summary>
    [Serializable]
    public class SortPriority
    {
        /// <summary>
        /// 排序字段。特殊值 "__id__" 表示按道具 ID 排序；
        /// 其余值为道具模板或功能标签中 AttributeDefinition 的 id。
        /// </summary>
        public string field;

        /// <summary>true = 升序；false = 降序。</summary>
        public bool ascending;

        public SortPriority()
        {
        }

        public SortPriority(string fieldArg, bool ascArg = false)
        {
            field     = fieldArg;
            ascending = ascArg;
        }

        public SortPriority Clone() => new SortPriority(field, ascending);
    }
}
