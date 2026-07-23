using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 配置模板基类。承载六大系统模板（道具 <see cref="ItemTemplate"/> / 仓库 <see cref="InventoryTemplate"/> /
    /// 商店 <see cref="ShopTemplate"/> / 蓝图 <see cref="CraftingBlueprintTemplate"/> /
    /// 装备组 <see cref="EquipmentGroupTemplate"/> / 技能 <see cref="SkillTemplate"/>）共享的三项：
    /// 模板名称、列表色点、以及属性字段定义（schema）。
    ///
    /// <para>各系统的实体依 <see cref="attributes"/> 由 <see cref="AttributeSync.Sync"/> 协调自身的属性值集合。</para>
    ///
    /// <para><b>序列化：</b>Unity 会把基类的序列化字段并入子类（与 <see cref="GroupTag"/> 同法）。
    /// 资产 / JSON 均按字段名存取，字段名未变即既有数据不受影响。</para>
    /// </summary>
    [Serializable]
    public abstract class ConfigTemplateBase
    {
        /// <summary>模板名称（同时作为各实体 templateRef 的引用键）。</summary>
        public string name;

        /// <summary>模板标识颜色（用于列表中的圆形色点，便于快速区分来源）。</summary>
        public Color color = Color.gray;

        /// <summary>模板所定义的属性字段（schema）。</summary>
        public List<AttributeDefinition> attributes = new List<AttributeDefinition>();

        protected ConfigTemplateBase()
        {
        }

        protected ConfigTemplateBase(string newName)
        {
            name = newName;
        }

        /// <summary>把本模板的公共字段（名称 / 色点 / 属性字段深拷贝）写入 <paramref name="dest"/>，供子类 Clone 复用。</summary>
        protected void CopyTo(ConfigTemplateBase dest)
        {
            dest.name  = name;
            dest.color = color;
            dest.attributes.Clear();
            foreach (var attr in attributes)
                dest.attributes.Add(attr.Clone());
        }
    }
}
