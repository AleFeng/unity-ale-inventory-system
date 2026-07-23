using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 装备组总属性加成计算器（静态、无状态）。给定一个装备组配置与其「槽位 → 道具 ID」的已装备映射，
    /// 按装备组的 <see cref="EquipmentGroup.attributeDisplays"/> 逐条跨全部已装备道具汇总，产出
    /// <see cref="EquipmentBonus"/> 列表。
    ///
    /// <para>此前这段逻辑（约 190 行）挂在 <see cref="EquipmentRuntimeManager"/> 上，但除了「读取已装备槽位」
    /// 之外零实例状态依赖；已装备映射改为入参后即可独立。<see cref="EquipmentRuntimeManager.GetTotalBonuses"/>
    /// 保留为薄封装：解析装备组、取出该组已装备槽位后转调 <see cref="Compute"/>。</para>
    ///
    /// <para>记录方式随源属性 <see cref="AttributeValue.Type"/> 而不同：</para>
    /// <list type="bullet">
    ///   <item><see cref="EFieldType.EnumIntPair"/>：按枚举 Key 拆分——每个枚举 Key 一条加成，整数值累加进
    ///     <see cref="EquipmentBonus.Total"/>；显示名经 <see cref="EquipmentAttributeDisplay.enumLabelAttrId"/>
    ///     从枚举项属性解析（回退枚举项名称）。<b>无法解析到实际枚举项的 Key 不显示。</b></item>
    ///   <item><see cref="EFieldType.StringIntPair"/>：按字符串 Key 拆分，整数值累加。<b>空字符串 Key 不显示。</b></item>
    ///   <item>其它数组类型：按元素索引拆分——每个索引位置一条加成，各道具同索引位置累加。</item>
    ///   <item>标量类型：汇总为一条（跨全部已装备道具按 <see cref="AttributeValue.ToComparableNumber"/> 求和）。</item>
    /// </list>
    /// </summary>
    public static class EquipmentBonusCalculator
    {
        #region 计算入口

        /// <summary>
        /// 计算装备组的总属性加成。顺序与 <see cref="EquipmentGroup.attributeDisplays"/> 一致；
        /// UI 可按 <see cref="EquipmentBonus.GroupTag"/> 分组显示。
        /// </summary>
        /// <param name="group">装备组配置。为 null 时返回空列表。</param>
        /// <param name="slots">该装备组的「槽位 ID → 已装备道具 ID」映射；为 null / 空表示未装备任何道具。</param>
        public static List<EquipmentBonus> Compute(EquipmentGroup group, IReadOnlyDictionary<string, string> slots)
        {
            var result = new List<EquipmentBonus>();
            if (group == null) return result;

            var dm = InventoryDataManager.Instance;

            foreach (var ad in group.attributeDisplays)
            {
                if (ad == null || string.IsNullOrEmpty(ad.attrId)) continue;

                // 收集本属性字段在全部已装备道具上的属性值；无任何值（如未装备任何道具）则不产出条目，
                // 避免出现"字段名: 0"幻影行——整体的空状态由 UI（UiwEquipmentBonusPanel）统一提示。
                var values = CollectEquippedValues(slots, dm, ad.attrId);
                if (values.Count == 0) continue;

                // 探测代表类型（同一 attrId 各道具类型一致，取首个即可）。
                var sample = values[0];

                if (sample.Type == EFieldType.EnumIntPair)
                    AddEnumIntPairBonuses(result, ad, values, dm);
                else if (sample.Type == EFieldType.StringIntPair)
                    AddStringIntPairBonuses(result, ad, values);
                else if (sample.IsArray)
                    AddArrayBonuses(result, ad, values);
                else
                    AddScalarBonus(result, ad, values);
            }
            return result;
        }
        #endregion

        #region 各类型加成

        /// <summary>收集某属性字段在装备组全部已装备道具上的属性值（跳过空槽 / 缺失道具 / 缺失字段）。</summary>
        private static List<AttributeValue> CollectEquippedValues(
            IReadOnlyDictionary<string, string> slots, InventoryDataManager dm, string attrId)
        {
            var list = new List<AttributeValue>();
            if (slots == null) return list;
            foreach (var kv in slots)
            {
                if (string.IsNullOrEmpty(kv.Value)) continue;
                var av = dm?.GetItem(kv.Value)?.GetAttributeValue(attrId);
                if (av != null) list.Add(av);
            }
            return list;
        }

        /// <summary>标量属性：跨全部提供该字段的已装备道具求和，汇总为一条加成。</summary>
        private static void AddScalarBonus(List<EquipmentBonus> result,
            EquipmentAttributeDisplay ad, List<AttributeValue> values)
        {
            double total = 0.0;
            foreach (var av in values) total += av.ToComparableNumber();

            result.Add(new EquipmentBonus
            {
                AttrId   = ad.attrId,
                GroupTag = ad.groupTag,
                Label    = ad.ResolveLabel(ad.attrId),
                Total    = total,
            });
        }

        /// <summary>
        /// EnumIntPair 属性：按枚举 Key 汇总整数值，每个 Key 拆分为一条加成，输出顺序遵循枚举项定义顺序。
        /// <b>无法解析到实际枚举项的 Key 不显示</b>（枚举类型缺失、或枚举项已被删除等）。
        /// </summary>
        private static void AddEnumIntPairBonuses(List<EquipmentBonus> result,
            EquipmentAttributeDisplay ad, List<AttributeValue> values, InventoryDataManager dm)
        {
            var totals      = new Dictionary<int, double>();
            string enumRef  = null;
            foreach (var av in values)
            {
                if (string.IsNullOrEmpty(enumRef)) enumRef = av.EnumTypeRef;
                int n = av.Count;
                for (int i = 0; i < n; i++)
                {
                    var (key, val) = av.GetEnumIntPair(i);
                    totals.TryGetValue(key, out var cur);
                    totals[key] = cur + val;
                }
            }
            if (totals.Count == 0) return;

            // 枚举类型无法解析：所有 Key 都取不到枚举项 → 整条都不显示。
            var enumType = dm?.GetEnumType(enumRef);
            if (enumType == null) return;

            // 仅输出能解析到实际枚举项的 Key（按枚举项定义顺序）；解析不到的 Key 直接跳过、不显示。
            foreach (var item in enumType.items)
                if (totals.TryGetValue(item.value, out var t))
                    result.Add(BuildEnumBonus(ad, enumRef, item, item.value, t));
        }

        /// <summary>构建一条枚举 Key 加成：显示名优先取枚举项的 <see cref="EquipmentAttributeDisplay.enumLabelAttrId"/> 字段，回退枚举项名称。</summary>
        private static EquipmentBonus BuildEnumBonus(EquipmentAttributeDisplay ad,
            string enumRef, EnumItem enumItem, int key, double total)
        {
            string label = null;
            if (enumItem != null && !string.IsNullOrEmpty(ad.enumLabelAttrId))
                label = enumItem.GetAttributeValue<string>(ad.enumLabelAttrId); // String / LocalizedString 皆可解析
            if (string.IsNullOrEmpty(label))
                label = enumItem != null ? enumItem.name : key.ToString();

            return new EquipmentBonus
            {
                AttrId      = ad.attrId,
                GroupTag    = ad.groupTag,
                Label       = label,
                Total       = total,
                EnumTypeRef = enumRef,
                EnumValue   = key,
            };
        }

        /// <summary>
        /// StringIntPair 属性：按字符串 Key 汇总整数值，每个 Key 拆分为一条加成（保持首次出现顺序）。
        /// <b>空字符串 Key 不显示</b>（取不到 String）。
        /// </summary>
        private static void AddStringIntPairBonuses(List<EquipmentBonus> result,
            EquipmentAttributeDisplay ad, List<AttributeValue> values)
        {
            var order  = new List<string>();
            var totals = new Dictionary<string, double>();
            foreach (var av in values)
            {
                int n = av.Count;
                for (int i = 0; i < n; i++)
                {
                    var (key, val) = av.GetStringIntPair(i);
                    if (string.IsNullOrEmpty(key)) continue;   // 取不到 String：无 Key，不显示
                    if (!totals.ContainsKey(key)) { totals[key] = 0.0; order.Add(key); }
                    totals[key] += val;
                }
            }
            foreach (var key in order)
                result.Add(new EquipmentBonus
                {
                    AttrId   = ad.attrId,
                    GroupTag = ad.groupTag,
                    Label    = key,
                    Total    = totals[key],
                });
        }

        /// <summary>其它数组类型：按元素索引拆分——各道具同一索引位置累加，每个索引一条加成。</summary>
        private static void AddArrayBonuses(List<EquipmentBonus> result,
            EquipmentAttributeDisplay ad, List<AttributeValue> values)
        {
            var totals = new List<double>();
            foreach (var av in values)
            {
                int n = av.Count;
                for (int i = 0; i < n; i++)
                {
                    double v = av.ElementToComparableNumber(i);
                    if (i < totals.Count) totals[i] += v;
                    else                  totals.Add(v);
                }
            }
            string baseLabel = ad.ResolveLabel(ad.attrId);
            for (int i = 0; i < totals.Count; i++)
                result.Add(new EquipmentBonus
                {
                    AttrId   = ad.attrId,
                    GroupTag = ad.groupTag,
                    Label    = $"{baseLabel} {i + 1}",
                    Total    = totals[i],
                });
        }
        #endregion

    }
}
