using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 装备系统运行时管理器（非 MonoBehaviour 单例，首次访问自动创建）。
    ///
    /// <para>职责：</para>
    /// <list type="bullet">
    ///   <item>已装备状态：按 装备组 ID → (槽位 ID → 已装备道具 ID) 维护，并提供存档接口</item>
    ///   <item>道具限制匹配：槽位列表的功能标签与枚举约束、装备槽的过滤条件，<b>全部 AND</b> 满足方可装入</item>
    ///   <item>装备 / 卸下 / 交换：与 <see cref="InventoryRuntimeManager"/> 协作在仓库与槽位间搬运道具</item>
    ///   <item>自动找槽：按槽位列表 / 槽位顺序，为某道具找到第一个可装入的空槽</item>
    ///   <item>属性加成汇总：按装备组「装备属性字段列表」跨全部已装备道具求和</item>
    /// </list>
    ///
    /// <para>装备组目录来自已注册数据库（经 <see cref="InventoryDataManager"/> 查询）；仓库读写一律经
    /// <see cref="InventoryRuntimeManager"/>（装备 / 卸下会触发其 OnInventoryChanged，背包 UI 据此刷新）。
    /// 本管理器自身的 <see cref="OnEquipmentChanged"/> 供装备 UI 刷新。</para>
    /// </summary>
    public class EquipmentRuntimeManager : InventorySystemSingleton<EquipmentRuntimeManager>
    {
        /// <summary>装备组 ID → (槽位 ID → 已装备道具 ID)。按需创建；空槽不入字典。</summary>
        private readonly Dictionary<string, Dictionary<string, string>> _equipped
            = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>某装备组的已装备状态发生变化时触发。参数为 groupId。供装备 UI 刷新。</summary>
        public event Action<string> OnEquipmentChanged;

        protected override void Init()
        {
            // 装备组目录来自已注册数据库；槽位初始为空，无需预初始化。
        }

        #region 查询：已装备状态

        /// <summary>获取某装备组某槽位当前已装备的道具 ID；空槽返回 null。</summary>
        public string GetEquipped(string groupId, string slotId)
        {
            if (_equipped.TryGetValue(groupId, out var slots)
                && slots.TryGetValue(slotId, out var itemId)
                && !string.IsNullOrEmpty(itemId))
                return itemId;
            return null;
        }

        /// <summary>某装备组某槽位是否已装备道具。</summary>
        public bool IsSlotOccupied(string groupId, string slotId) => GetEquipped(groupId, slotId) != null;

        private void SetEquipped(string groupId, string slotId, string itemId)
        {
            if (!_equipped.TryGetValue(groupId, out var slots))
                _equipped[groupId] = slots = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(itemId)) slots.Remove(slotId);
            else                              slots[slotId] = itemId;
        }

        #endregion

        #region 限制匹配（全部 AND）

        /// <summary>
        /// 道具是否满足某槽位列表的限制：所列功能标签<b>全部具备</b>，且所列枚举约束<b>全部满足</b>（AND）。
        /// </summary>
        public bool ItemMatchesSlotList(EquipmentSlotList slotList, string itemId)
        {
            if (slotList == null || string.IsNullOrEmpty(itemId)) return false;
            var dm   = InventoryDataManager.Instance;
            var item = dm?.GetItem(itemId);
            if (item == null) return false;

            foreach (var tag in slotList.requiredTags)
            {
                if (string.IsNullOrEmpty(tag)) continue;
                if (!dm.ItemHasTag(itemId, tag)) return false;
            }

            foreach (var c in slotList.enumConstraints)
            {
                if (c == null || string.IsNullOrEmpty(c.enumTypeRef)) continue;
                if (!ItemSatisfiesEnumConstraint(item, c)) return false;
            }
            return true;
        }

        /// <summary>
        /// 道具是否可装入某槽位：先满足所属槽位列表限制，再满足该槽位的全部过滤条件（等值，AND）。
        /// </summary>
        public bool ItemMatchesSlot(EquipmentSlotList slotList, EquipmentSlot slot, string itemId)
        {
            if (slot == null) return false;
            if (!ItemMatchesSlotList(slotList, itemId)) return false;

            var item = InventoryDataManager.Instance?.GetItem(itemId);
            if (item == null) return false;

            foreach (var f in slot.filters)
            {
                if (f == null || string.IsNullOrEmpty(f.attrId)) continue;
                var av = item.GetAttributeValue(f.attrId);
                if (av == null) return false;
                if (!AttributeValuesEqual(av, f.value)) return false;
            }
            return true;
        }

        /// <summary>
        /// 道具是否拥有某枚举约束要求的枚举属性：存在引用该枚举类型的属性且其值在 allowedValues 内；
        /// allowedValues 为空表示「任意值」（仅需具备该枚举类型的属性）。
        /// </summary>
        private static bool ItemSatisfiesEnumConstraint(Item item, EquipmentEnumConstraint c)
        {
            foreach (var entry in item.values)
            {
                var v = entry?.value;
                if (v == null || v.Type != EFieldType.Enum) continue;
                if (v.EnumTypeRef != c.enumTypeRef) continue;
                if (c.allowedValues == null || c.allowedValues.Count == 0) return true;
                if (c.allowedValues.Contains(v.AsEnumValue)) return true;
            }
            return false;
        }

        /// <summary>
        /// 属性值等值比较（按类型；枚举需同枚举类型 + 同值）。
        /// <para>注意：**不可**用 <see cref="AttributeValue.ToComparableNumber"/> 做等值判据——
        /// 它是为「排序」设计的降维投影：向量 / 颜色取的是**模长**（于是 (1,0) 会等于 (0,1)），
        /// 对象引用类与 Text 一律返回 0（于是任意两个资源都相等）。此处逐类型比较真实载荷。</para>
        /// </summary>
        private static bool AttributeValuesEqual(AttributeValue a, AttributeValue b)
        {
            if (a == null || b == null) return false;
            if (a.Type != b.Type) return false;
            switch (a.Type)
            {
                case EFieldType.Int:
                case EFieldType.Bool:
                    return a.AsInt == b.AsInt;
                case EFieldType.Enum:
                    return a.EnumTypeRef == b.EnumTypeRef && a.AsEnumValue == b.AsEnumValue;
                case EFieldType.Float:
                    return Mathf.Approximately(a.AsFloat, b.AsFloat);
                case EFieldType.String:
                    return a.AsString == b.AsString;
                case EFieldType.Text:
                    return a.GetTextValue(0) == b.GetTextValue(0);

                // 向量 / 颜色逐分量比较（Unity 的 Vector*/Color 相等运算符本身带浮点容差）
                case EFieldType.Vector2:    return a.GetVector2(0)    == b.GetVector2(0);
                case EFieldType.Vector3:    return a.GetVector3(0)    == b.GetVector3(0);
                case EFieldType.Vector4:    return a.GetVector4(0)    == b.GetVector4(0);
                case EFieldType.Color:      return a.GetColor(0)      == b.GetColor(0);
                case EFieldType.VectorInt2: return a.GetVector2Int(0) == b.GetVector2Int(0);
                case EFieldType.VectorInt3: return a.GetVector3Int(0) == b.GetVector3Int(0);
                case EFieldType.VectorInt4: return a.GetVector4Int(0) == b.GetVector4Int(0);

                case EFieldType.StringIntPair:
                {
                    var pa = a.GetStringIntPair(0);
                    var pb = b.GetStringIntPair(0);
                    return pa.key == pb.key && pa.value == pb.value;
                }
                case EFieldType.EnumIntPair:
                {
                    var pa = a.GetEnumIntPair(0);
                    var pb = b.GetEnumIntPair(0);
                    return a.EnumTypeRef == b.EnumTypeRef
                           && pa.enumValue == pb.enumValue && pa.value == pb.value;
                }

                default:
                    // 对象引用类（Sprite / Prefab / Material / …）比较引用本身；
                    // 其余无载荷类型（AnimationCurve 等）保持原有的数值近似判据。
                    return a.Type.IsObjectBacked()
                        ? a.GetObject(0) == b.GetObject(0)
                        : Math.Abs(a.ToComparableNumber() - b.ToComparableNumber()) < 1e-6;
            }
        }

        #endregion

        #region 自动找槽

        /// <summary>
        /// 为某道具在装备组中按 槽位列表 / 槽位 顺序找到第一个「空且可装入」的槽位。
        /// 找到返回 true 并输出 slotListId / slotId；否则返回 false。
        /// </summary>
        public bool TryFindEquipSlot(string groupId, string itemId, out string slotListId, out string slotId)
        {
            slotListId = null; slotId = null;
            var group = ResolveGroup(groupId);
            if (group == null || string.IsNullOrEmpty(itemId)) return false;

            foreach (var sl in group.slotLists)
            {
                if (!ItemMatchesSlotList(sl, itemId)) continue;
                foreach (var slot in sl.slots)
                {
                    if (IsSlotOccupied(groupId, slot.id)) continue;
                    if (ItemMatchesSlot(sl, slot, itemId))
                    {
                        slotListId = sl.id; slotId = slot.id;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 自动装备：为道具找到第一个可装入的空槽并装备（从 <paramref name="fromInventoryId"/> 取出 1 个）。
        /// 无可装入<b>空</b>槽位时返回 false（UI 据此提示玩家无法装备）。仅填空槽，不替换已装备道具。
        /// </summary>
        public bool TryAutoEquip(string groupId, string itemId, string fromInventoryId)
        {
            if (!TryFindEquipSlot(groupId, itemId, out var slotListId, out var slotId)) return false;
            return Equip(groupId, slotListId, slotId, itemId, fromInventoryId);
        }

        /// <summary>
        /// 为某道具在装备组中按 槽位列表 / 槽位 顺序找到第一个「<b>已占用</b>且该道具满足其装入限制」的槽位
        /// （用于快速装备的替换回退）。找到返回 true 并输出 slotListId / slotId；否则返回 false。
        /// </summary>
        public bool TryFindReplaceableSlot(string groupId, string itemId, out string slotListId, out string slotId)
        {
            slotListId = null; slotId = null;
            var group = ResolveGroup(groupId);
            if (group == null || string.IsNullOrEmpty(itemId)) return false;

            foreach (var sl in group.slotLists)
            {
                if (!ItemMatchesSlotList(sl, itemId)) continue;
                foreach (var slot in sl.slots)
                {
                    if (!IsSlotOccupied(groupId, slot.id)) continue;   // 只找已占用的槽
                    if (ItemMatchesSlot(sl, slot, itemId))
                    {
                        slotListId = sl.id; slotId = slot.id;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 快速装备（可替换，带首选槽）。装入优先级：
        /// <list type="number">
        ///   <item>首选槽 <paramref name="preferredSlotId"/>（如装备选择面板中当前选中的装备槽）——非空且该道具满足其限制时装入，占用则替换。</item>
        ///   <item>第一个「空且可装入」的槽。</item>
        ///   <item>第一个「已占用且满足限制」的槽（Index0 起）——替换其原道具。</item>
        /// </list>
        /// 替换时原道具经 <see cref="Equip"/> 放回 <paramref name="fromInventoryId"/>（放不下则回滚、不替换）。三者皆不满足时返回 false。
        /// </summary>
        public bool TryAutoEquipOrReplace(string groupId, string itemId, string fromInventoryId,
            string preferredSlotId = null)
        {
            // ① 首选槽（如装备选择面板中当前选中的装备槽）：该道具满足其限制时装入，占用则由 Equip 替换。
            if (!string.IsNullOrEmpty(preferredSlotId)
                && LocateSlot(ResolveGroup(groupId), preferredSlotId, out var pSlotList, out var pSlot)
                && ItemMatchesSlot(pSlotList, pSlot, itemId))
                return Equip(groupId, pSlotList.id, preferredSlotId, itemId, fromInventoryId);

            // ② 优先填入空槽（不打扰已装备的道具）。
            if (TryFindEquipSlot(groupId, itemId, out var slotListId, out var slotId))
                return Equip(groupId, slotListId, slotId, itemId, fromInventoryId);

            // ③ 无空槽：替换第一个满足限制的已占用槽（Index0 起；Equip 内部会把原道具放回来源仓库，放不下则回滚、不替换）。
            if (TryFindReplaceableSlot(groupId, itemId, out slotListId, out slotId))
                return Equip(groupId, slotListId, slotId, itemId, fromInventoryId);

            return false;
        }

        #endregion

        #region 装备 / 卸下 / 交换

        /// <summary>
        /// 将道具装备到指定槽位。先校验限制；从 <paramref name="fromInventoryId"/> 取出 1 个该道具，
        /// 若槽位原有道具则放回该仓库（先移除后放回，净占用不变）。未提供来源仓库时不做仓库搬运
        /// （原有道具将被直接替换丢弃，调用方自负）。成功触发 <see cref="OnEquipmentChanged"/>。
        /// </summary>
        public bool Equip(string groupId, string slotListId, string slotId, string itemId, string fromInventoryId)
        {
            var group = ResolveGroup(groupId);
            if (group == null || string.IsNullOrEmpty(itemId)) return false;

            var slotList = FindSlotList(group, slotListId);
            var slot     = FindSlot(slotList, slotId);
            if (slotList == null || slot == null) return false;

            if (!ItemMatchesSlot(slotList, slot, itemId)) return false;

            var invMgr = InventoryRuntimeManager.Instance;
            if (!string.IsNullOrEmpty(fromInventoryId))
            {
                if (!invMgr || !invMgr.HasItem(fromInventoryId, itemId)) return false;

                string old = GetEquipped(groupId, slotId);
                invMgr.TryRemoveItemById(fromInventoryId, itemId);   // 先取出待装备道具，腾出空间
                if (!string.IsNullOrEmpty(old) && !invMgr.TryAddItem(fromInventoryId, old))
                {
                    // 旧道具放不回（来源仓库已满）：回滚取出的道具，中止装备，避免道具丢失。
                    invMgr.TryAddItem(fromInventoryId, itemId);
                    return false;
                }
            }

            SetEquipped(groupId, slotId, itemId);
            OnEquipmentChanged?.Invoke(groupId);
            return true;
        }

        /// <summary>
        /// 卸下指定槽位的道具并放入 <paramref name="toInventoryId"/>。目标仓库放不下时返回 false（不卸下）。
        /// 未提供目标仓库时仅清空槽位（道具丢弃，调用方自负）。成功触发 <see cref="OnEquipmentChanged"/>。
        /// </summary>
        public bool Unequip(string groupId, string slotId, string toInventoryId)
        {
            string itemId = GetEquipped(groupId, slotId);
            if (string.IsNullOrEmpty(itemId)) return false;

            if (!string.IsNullOrEmpty(toInventoryId))
            {
                var invMgr = InventoryRuntimeManager.Instance;
                if (!invMgr || invMgr.GetFreeSpaceFor(toInventoryId, itemId) < 1) return false;
                invMgr.TryAddItem(toInventoryId, itemId);
            }

            SetEquipped(groupId, slotId, null);
            OnEquipmentChanged?.Invoke(groupId);
            return true;
        }

        /// <summary>
        /// 卸下指定槽位的道具到该装备组配置的「装备仓库」：从列表 Index0 起逐个尝试，放入第一个放得下的仓库。
        /// 已配置装备仓库但均放不下时返回 false（不卸下，避免道具丢失）；
        /// 未配置装备仓库时回退到 <paramref name="fallbackInventoryId"/>；两者皆无（回退仓库为空）时同样返回 false、不卸下
        /// （不会凭空丢弃道具）。成功触发 <see cref="OnEquipmentChanged"/>。
        /// </summary>
        public bool UnequipToConfigured(string groupId, string slotId, string fallbackInventoryId = null)
        {
            string itemId = GetEquipped(groupId, slotId);
            if (string.IsNullOrEmpty(itemId)) return false;

            var group = ResolveGroup(groupId);
            var refs  = EffectiveInventories(group);

            string target;
            if (refs != null && refs.Count > 0)
            {
                // 已配置装备仓库：仅在其中从 Index0 起找第一个放得下的仓库；都放不下则中止。
                target = FindEquipmentInventoryFor(group, itemId);
                if (string.IsNullOrEmpty(target)) return false;
            }
            else
            {
                // 未配置装备仓库：回退到调用方传入的仓库；也没有则不卸下（避免道具被丢弃）。
                target = fallbackInventoryId;
                if (string.IsNullOrEmpty(target)) return false;
            }
            return Unequip(groupId, slotId, target);
        }

        /// <summary>
        /// 在装备组「装备仓库」列表中从 Index0 起找到第一个能放下该道具（自由空间 ≥ 1）的仓库 ID；找不到返回 null。
        /// </summary>
        public string FindEquipmentInventoryFor(string groupId, string itemId)
            => FindEquipmentInventoryFor(ResolveGroup(groupId), itemId);

        /// <summary>
        /// 某装备组「有效的装备仓库」列表（装备系统 / UI 可交互的仓库）：
        /// 优先用装备组自身配置；自身为空时回退到其来源模板的配置（模板承载默认值）。空列表表示两者都未配置。
        /// </summary>
        public IReadOnlyList<string> GetEquipmentInventories(string groupId)
        {
            var refs = EffectiveInventories(ResolveGroup(groupId));
            return refs != null ? refs : Array.Empty<string>();
        }

        /// <summary>某仓库是否属于该装备组的「有效装备仓库」列表。</summary>
        public bool IsEquipmentInventory(string groupId, string inventoryId)
        {
            if (string.IsNullOrEmpty(inventoryId)) return false;
            var refs = EffectiveInventories(ResolveGroup(groupId));
            return refs != null && refs.Contains(inventoryId);
        }

        private static string FindEquipmentInventoryFor(EquipmentGroup group, string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            var refs = EffectiveInventories(group);
            if (refs == null) return null;
            var invMgr = InventoryRuntimeManager.Instance;
            if (!invMgr) return null;
            foreach (var invId in refs)
            {
                if (string.IsNullOrEmpty(invId)) continue;
                if (invMgr.GetFreeSpaceFor(invId, itemId) >= 1) return invId;
            }
            return null;
        }

        /// <summary>
        /// 装备组的「有效装备仓库」：装备组自身配置非空则用之；否则回退到来源模板的配置。
        /// （装备组从模板深拷贝创建；此处兼容装备组自身未单独配置、仅在模板层配置的情形。）
        /// </summary>
        private static List<string> EffectiveInventories(EquipmentGroup group)
        {
            if (group == null) return null;
            if (group.equipmentInventoryRefs != null && group.equipmentInventoryRefs.Count > 0)
                return group.equipmentInventoryRefs;
            var tmpl = InventoryDataManager.Instance?.GetEquipmentGroupTemplate(group.templateRef);
            return tmpl != null ? tmpl.equipmentInventoryRefs : null;
        }

        /// <summary>
        /// 交换同一装备组内两个槽位上的道具。交换后两者需各自满足目标槽位限制，否则中止并返回 false。
        /// 用于装备槽之间拖拽换位。成功触发 <see cref="OnEquipmentChanged"/>。
        /// </summary>
        public bool SwapSlots(string groupId, string slotIdA, string slotIdB)
        {
            var group = ResolveGroup(groupId);
            if (group == null || string.IsNullOrEmpty(slotIdA) || slotIdA == slotIdB) return false;
            if (!LocateSlot(group, slotIdA, out var slA, out var slotA)) return false;
            if (!LocateSlot(group, slotIdB, out var slB, out var slotB)) return false;

            string itemA = GetEquipped(groupId, slotIdA);
            string itemB = GetEquipped(groupId, slotIdB);

            if (!string.IsNullOrEmpty(itemB) && !ItemMatchesSlot(slA, slotA, itemB)) return false;
            if (!string.IsNullOrEmpty(itemA) && !ItemMatchesSlot(slB, slotB, itemA)) return false;

            SetEquipped(groupId, slotIdA, itemB);
            SetEquipped(groupId, slotIdB, itemA);
            OnEquipmentChanged?.Invoke(groupId);
            return true;
        }

        /// <summary>
        /// 将某装备槽的道具卸下并精确放入某仓库的指定<b>空</b>格（用于从装备槽拖到背包空格）。
        /// 目标格必须存在且为空，否则返回 false（不卸下）。成功触发 <see cref="OnEquipmentChanged"/>。
        /// </summary>
        public bool UnequipToSlot(string groupId, string equipSlotId, string toInventoryId, string toSlotId)
        {
            string itemId = GetEquipped(groupId, equipSlotId);
            if (string.IsNullOrEmpty(itemId)) return false;

            var invMgr = InventoryRuntimeManager.Instance;
            if (!invMgr) return false;

            var target = invMgr.GetSlot(toInventoryId, toSlotId);
            if (target == null || !string.IsNullOrEmpty(target.itemId)) return false;   // 目标格须存在且为空

            if (!invMgr.SetSlotContent(toInventoryId, toSlotId, itemId, 1)) return false;
            SetEquipped(groupId, equipSlotId, null);
            OnEquipmentChanged?.Invoke(groupId);
            return true;
        }

        /// <summary>
        /// 用某仓库指定格中的道具与某装备槽<b>交换</b>（用于从装备槽拖到背包中有道具的格）：
        /// 该格道具须能装入目标装备槽，否则返回 false（不改动）。成功后该格道具装入装备槽，
        /// 原已装备道具落回<b>同一格</b>（实现位置交换）。该格若堆叠多个，仅取 1 个装备，其余保留、原道具另找空位。
        /// 成功触发 <see cref="OnEquipmentChanged"/>。
        /// </summary>
        public bool EquipFromSlotSwap(string groupId, string equipSlotId, string fromInventoryId, string fromSlotId)
        {
            var group = ResolveGroup(groupId);
            if (!LocateSlot(group, equipSlotId, out var slotList, out var slot)) return false;

            var invMgr = InventoryRuntimeManager.Instance;
            if (!invMgr) return false;

            var src = invMgr.GetSlot(fromInventoryId, fromSlotId);
            if (src == null || string.IsNullOrEmpty(src.itemId)) return false;   // 该格须有道具

            string newItem = src.itemId;                        // 待装备道具（悬停格中的道具）
            if (!ItemMatchesSlot(slotList, slot, newItem)) return false;   // 不可装入 → 不改动（调用方取消拖拽）

            string oldItem = GetEquipped(groupId, equipSlotId); // 原已装备道具（拖拽源装备槽）

            if (src.count <= 1)
            {
                // 纯位置交换：该格 ← 原已装备道具（oldItem 为空则等价于将该格道具移入装备槽）。
                invMgr.SetSlotContent(fromInventoryId, fromSlotId, oldItem, string.IsNullOrEmpty(oldItem) ? 0 : 1);
            }
            else
            {
                // 该格堆叠多个：仅取 1 个装备，其余保留在原格；原已装备道具无法与剩余堆叠同格，另找空位放入。
                invMgr.SetSlotContent(fromInventoryId, fromSlotId, newItem, src.count - 1);
                if (!string.IsNullOrEmpty(oldItem) && !invMgr.TryAddItem(fromInventoryId, oldItem))
                {
                    invMgr.SetSlotContent(fromInventoryId, fromSlotId, newItem, src.count);   // 放不下 → 回滚，中止
                    return false;
                }
            }

            SetEquipped(groupId, equipSlotId, newItem);
            OnEquipmentChanged?.Invoke(groupId);
            return true;
        }

        #endregion

        #region 属性加成

        /// <summary>
        /// 计算 装备组的 总属性加成：对「装备属性字段列表」每一条，跨全部已装备道具汇总。
        /// 顺序与 <see cref="EquipmentGroup.attributeDisplays"/> 一致；UI 可按 <see cref="EquipmentBonus.GroupTag"/> 分组显示。
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
        public List<EquipmentBonus> GetTotalBonuses(string groupId)
        {
            var result = new List<EquipmentBonus>();
            var group  = ResolveGroup(groupId);
            if (group == null) return result;

            var dm = InventoryDataManager.Instance;
            _equipped.TryGetValue(groupId, out var slots);

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

        /// <summary>收集某属性字段在装备组全部已装备道具上的属性值（跳过空槽 / 缺失道具 / 缺失字段）。</summary>
        private static List<AttributeValue> CollectEquippedValues(
            Dictionary<string, string> slots, InventoryDataManager dm, string attrId)
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

        #region 存档

        /// <summary>获取全部装备组已装备状态的深拷贝（由游戏层 SaveManager 序列化）。</summary>
        public List<RuntimeEquipmentState> GetSaveData()
        {
            var result = new List<RuntimeEquipmentState>(_equipped.Count);
            foreach (var kv in _equipped)
            {
                var st = new RuntimeEquipmentState(kv.Key);
                foreach (var s in kv.Value)
                    if (!string.IsNullOrEmpty(s.Value))
                        st.slots.Add(new EquippedSlotEntry(s.Key, s.Value));
                result.Add(st);
            }
            return result;
        }

        /// <summary>从存档数据恢复已装备状态（覆盖当前内存状态）。</summary>
        public void LoadSaveData(List<RuntimeEquipmentState> data)
        {
            _equipped.Clear();
            if (data == null) return;
            foreach (var st in data)
            {
                if (st == null || string.IsNullOrEmpty(st.groupId)) continue;
                var slots = new Dictionary<string, string>();
                foreach (var e in st.slots)
                    if (e != null && !string.IsNullOrEmpty(e.slotId) && !string.IsNullOrEmpty(e.itemId))
                        slots[e.slotId] = e.itemId;
                _equipped[st.groupId] = slots;
            }
        }

        /// <summary>清空全部已装备状态（如开始新游戏）。</summary>
        public void ResetAll() => _equipped.Clear();

        #endregion

        #region 内部辅助

        private static EquipmentGroup ResolveGroup(string groupId)
            => string.IsNullOrEmpty(groupId) ? null : InventoryDataManager.Instance?.GetEquipmentGroup(groupId);

        private static EquipmentSlotList FindSlotList(EquipmentGroup group, string slotListId)
        {
            if (group == null) return null;
            foreach (var sl in group.slotLists)
                if (sl.id == slotListId) return sl;
            return null;
        }

        private static EquipmentSlot FindSlot(EquipmentSlotList slotList, string slotId)
        {
            if (slotList == null) return null;
            foreach (var s in slotList.slots)
                if (s.id == slotId) return s;
            return null;
        }

        /// <summary>在装备组中按槽位 ID 定位其所属槽位列表与槽位。</summary>
        private static bool LocateSlot(EquipmentGroup group, string slotId,
            out EquipmentSlotList outSlotList, out EquipmentSlot outSlot)
        {
            outSlotList = null; outSlot = null;
            if (group == null || string.IsNullOrEmpty(slotId)) return false;
            foreach (var sl in group.slotLists)
                foreach (var s in sl.slots)
                    if (s.id == slotId) { outSlotList = sl; outSlot = s; return true; }
            return false;
        }

        #endregion
    }
}
