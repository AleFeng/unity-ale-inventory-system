using System;
using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 商店实例。携带唯一 <see cref="id"/>、商店类型、交易仓库、过滤/数字格式/价格来源配置，
    /// 以及若干商品组与来自模板的自定义属性值。
    /// 商店是配置目录（商品为配置条目），运行时的每玩家交易进度由 ShopRuntimeManager 另行维护。
    ///
    /// <para>继承 <see cref="AttributeOwner"/> 以复用带缓存的 O(1) <see cref="AttributeOwner.GetEntry"/>
    /// 与 <see cref="AttributeOwner.GetAttributeValue{T}"/> / <see cref="AttributeOwner.SetAttributeValue{T}"/>。</para>
    /// </summary>
    [Serializable]
    public class Shop : AttributeOwner, IShopConfig
    {
        /// <summary>唯一标识。</summary>
        public string id;

        /// <summary>显示名称（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用；为空时回退 <see cref="id"/>）。</summary>
        public AttributeValue displayNameText = new AttributeValue(EFieldType.Text);

        /// <summary>描述（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用；正式游戏配置数据，UI 显示为「描述」）。</summary>
        public AttributeValue descriptionText = new AttributeValue(EFieldType.Text);

        /// <summary>来源商店模板名称（可为空）。</summary>
        public string templateRef;

        /// <summary>商店类型（售卖 / 回收 / 等价交换）。</summary>
        public ShopType shopType = ShopType.Sell;

        /// <summary>
        /// 交易仓库：与本商店交易时使用的仓库条目 ID 列表（可多选）。
        /// 用途：统计玩家货币、购入道具落入、回收道具来源、找零货币写入。
        /// </summary>
        public List<string> tradeInventoryRefs = new List<string>();

        /// <summary>
        /// 交易功能标签列表。仅 Recycle 生效：只有功能标签含其中任一标签的道具才可回收并显示在商品列表中（空 = 不限制）。
        /// </summary>
        public List<string> tradeTagRefs = new List<string>();

        /// <summary>
        /// 过滤标签列表。UI 中以功能标签按钮形式呈现。
        /// </summary>
        public List<string> filterTagRefs = new List<string>();

        /// <summary>
        /// 是否在 UI 页签栏显示"全部"页签。true = 显示并默认选中（显示全部商品）；
        /// false = 不显示，默认选中第一个商品组（无商品组时仍显示全部）。
        /// </summary>
        public bool showAllFilterTab = true;

        /// <summary>引用的数字格式配置名称（对应 InventoryDatabase.NumberFormatConfigs；空 = 不使用）。</summary>
        public string numberFormatRef;

        /// <summary>
        /// 价格属性来源：一个类型为 StringIntPair(货币ID→价格) 的道具属性 ID。
        /// 运行时从命中道具的该属性读取基础价格（空 = 未配置）。
        /// </summary>
        public string priceAttrSource;

        /// <summary>整理列表（UI 中以下拉菜单形式呈现，玩家可选择排序条件及升降序）。</summary>
        public List<SortPriority> sortPriorities = new List<SortPriority>();

        /// <summary>整理优先级（整理列表条件值相同时，依次按此列表比较直至值不同）。</summary>
        public List<SortPriority> sortTiebreakers = new List<SortPriority>();

        /// <summary>商品组列表。</summary>
        public List<ShopCommodityGroup> groups = new List<ShopCommodityGroup>();

        /// <summary>来自模板的自定义属性值。</summary>
        public List<AttributeEntry> values = new List<AttributeEntry>();

        // 实现基类 AttributeOwner 的抽象属性，将 values 列表暴露给基类的懒加载字典缓存。
        protected override List<AttributeEntry> AttributeEntries => values;

        // ── IShopConfig（映射到上述序列化字段，供编辑器共享绘制）────────────────────
        ShopType IShopConfig.ShopType { get => shopType; set => shopType = value; }
        List<string> IShopConfig.TradeInventoryRefs => tradeInventoryRefs;
        List<string> IShopConfig.TradeTagRefs => tradeTagRefs;
        List<string> IShopConfig.FilterTagRefs => filterTagRefs;
        bool IShopConfig.ShowAllFilterTab { get => showAllFilterTab; set => showAllFilterTab = value; }
        string IShopConfig.NumberFormatRef { get => numberFormatRef; set => numberFormatRef = value; }
        string IShopConfig.PriceAttrSource { get => priceAttrSource; set => priceAttrSource = value; }
        List<ShopCommodityGroup> IShopConfig.Groups => groups;
        List<SortPriority> IShopConfig.SortPriorities => sortPriorities;
        List<SortPriority> IShopConfig.SortTiebreakers => sortTiebreakers;

        public Shop()
        {
        }

        public Shop(string newId, string newTemplateRef = null)
        {
            id          = newId;
            templateRef = newTemplateRef;
        }

        /// <summary>
        /// 根据当前模板协调自定义属性值集合：
        /// 为模板新增字段追加默认值条目；移除模板已不存在的字段条目；已存在字段保留现有值
        /// （类型 / 数组形态 / 枚举类型引用变化时重置为新类型默认值）。
        /// </summary>
        public void RebuildAttributes(InventoryDatabase db)
        {
            if (!db) return;

            var template = db.GetShopTemplate(templateRef);
            AttributeSync.Sync(values, template != null ? template.attributes : null);

            // values 已完整重建，使缓存失效；下次 GetEntry 调用时将从最终状态重建字典。
            InvalidateEntryCache();
        }

        /// <summary>深拷贝。</summary>
        public Shop Clone()
        {
            var clone = new Shop(id, templateRef)
            {
                displayNameText       = displayNameText != null ? displayNameText.Clone() : new AttributeValue(EFieldType.Text),
                descriptionText       = descriptionText != null ? descriptionText.Clone() : new AttributeValue(EFieldType.Text),
                shopType              = shopType,
                showAllFilterTab      = showAllFilterTab,
                numberFormatRef       = numberFormatRef,
                priceAttrSource       = priceAttrSource,
                tradeInventoryRefs    = new List<string>(tradeInventoryRefs),
                tradeTagRefs          = new List<string>(tradeTagRefs),
                filterTagRefs         = new List<string>(filterTagRefs),
            };
            foreach (var sp in sortPriorities)
                clone.sortPriorities.Add(sp.Clone());
            foreach (var sp in sortTiebreakers)
                clone.sortTiebreakers.Add(sp.Clone());
            foreach (var g in groups)
                clone.groups.Add(g.Clone());
            foreach (var e in values)
                clone.values.Add(e.Clone());
            return clone;
        }
    }
}
