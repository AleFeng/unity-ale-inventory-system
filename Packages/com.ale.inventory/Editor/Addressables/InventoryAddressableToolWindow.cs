using System.Collections.Generic;
using Ale.Inventory.Runtime;
using Ale.Toolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// Addressable 工具窗口（库存闭合，仅 IS_ADDRESSABLE 编译）。绘制 / 迁移机制全部下沉至
    /// <see cref="EditorAddressableToolWindow{TDb}"/>；本类只提供菜单入口、欢迎窗注入，及本库属性系统之外的固定
    /// Sprite 资源字段（<c>Skill.icon</c> / <c>SkillTemplate.icon</c> / <c>Tag.backgroundSprite</c>）。
    /// </summary>
    [InitializeOnLoad]
    public class InventoryAddressableToolWindow : EditorAddressableToolWindow<InventoryDatabase>
    {
        // 注入钩子：让 core 欢迎窗口（对 Addressables 零依赖、看不见本类型）得以打开本窗口。
        // 仅在本程序集参与编译（IS_ADDRESSABLE 启用）时执行，故宏关闭时欢迎窗口对应按钮自动隐藏。
        static InventoryAddressableToolWindow()
        {
            InventoryWelcomeWindow.OpenAddressableMigration = Open;
        }

        [MenuItem("Tools/Inventory System/Addressable/Addressable工具窗口", priority = 200)]
        public static void Open()
        {
            var win = GetWindow<InventoryAddressableToolWindow>(true, "资源引用迁移（Addressables）", true);
            win.minSize = new Vector2(460f, 420f);
            win.Show();
        }

        /// <summary>本库属性系统之外的固定 Sprite 资源字段：技能 / 技能模板图标、功能标签背景图。</summary>
        protected override IEnumerable<AddressableFixedField> FixedFields(InventoryDatabase db)
        {
            foreach (var s in db.Skills)
            {
                var c = s;
                yield return new AddressableFixedField(() => c.icon, v => c.icon = v,
                    () => c.iconAddress, v => c.iconAddress = v, $"技能「{c.id}」图标");
            }
            foreach (var t in db.SkillTemplates)
            {
                var c = t;
                yield return new AddressableFixedField(() => c.icon, v => c.icon = v,
                    () => c.iconAddress, v => c.iconAddress = v, $"技能模板「{c.name}」图标");
            }
            foreach (var ft in db.FunctionTags)
            {
                var c = ft;
                yield return new AddressableFixedField(() => c.backgroundSprite, v => c.backgroundSprite = v,
                    () => c.backgroundSpriteAddress, v => c.backgroundSpriteAddress = v, $"功能标签「{c.name}」背景图");
            }
        }
    }
}
