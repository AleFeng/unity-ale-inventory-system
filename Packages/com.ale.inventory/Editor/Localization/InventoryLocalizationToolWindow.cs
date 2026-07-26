#if ATK_LOCALIZATION
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using Ale.Toolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 本地化工具窗口（库存闭合，仅 ATK_LOCALIZATION 编译）。建表 / Key 生成机制全部下沉至
    /// <see cref="EditorLocalizationToolWindow{TDb}"/>；本类只提供菜单入口、本库表集合 GUID 存取、Text 字段来源
    /// （<see cref="InventoryTextFieldCollector"/>），及库存专属的 EditorPrefs 键 / 默认值。
    /// </summary>
    public class InventoryLocalizationToolWindow : EditorLocalizationToolWindow<InventoryDatabase>
    {
        [MenuItem("Tools/Ale Toolkit/Inventory System/Localization/本地化工具窗口", priority = 1100)]
        public static void Open()
        {
            var win = GetWindow<InventoryLocalizationToolWindow>(true, "本地化建表 / Key 生成", true);
            win.minSize = new Vector2(500f, 460f);
            win.Show();
        }

        /// <summary>本库的表集合 GUID 存 / 取 <see cref="InventoryDatabase.LocalizationTableCollectionGuid"/>。</summary>
        protected override string TableCollectionGuid
        {
            get => database ? database.LocalizationTableCollectionGuid : null;
            set { if (database) database.LocalizationTableCollectionGuid = value; }
        }

        /// <summary>本库全部 Text 字段（含语义中文 Key 路径）。</summary>
        protected override IReadOnlyList<TextFieldRef> CollectTextFields(InventoryDatabase db)
            => InventoryTextFieldCollector.Collect(db);

        // 库存专属的 EditorPrefs 键与默认值（沿用 1.7.x 键名，避免升级后丢失用户设置）。
        protected override string PrefKeyFolder => "InventorySystem.Localization.GenFolder";
        protected override string PrefKeyPrefix => "InventorySystem.Localization.TablePrefix";
        protected override string DefaultFolder => "Assets/Localization/InventorySystem";
        protected override string DefaultPrefix => "InventoryStrings";
    }
}
#endif
