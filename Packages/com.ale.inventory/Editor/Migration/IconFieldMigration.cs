using Ale.Inventory.Runtime;
using Ale.Toolkit.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 一次性迁移（Q1 三段式的第二段）：把 <see cref="Skill"/> / <see cref="SkillTemplate"/> 的旧
    /// <c>icon</c>+<c>iconAddress</c>、<see cref="Tag"/> 的旧 <c>backgroundSprite</c>+<c>backgroundSpriteAddress</c>
    /// 拷入新的对象类属性值（<c>iconValue</c> / <c>backgroundSpriteValue</c>），供随后删除旧字段前把数据落到新字段。
    ///
    /// <para>幂等：仅在新字段尚为空时拷贝。运行后自动 <see cref="EditorUtility.SetDirty"/> + 保存所有 InventoryDatabase 资产，
    /// 确保新字段落盘。<b>本文件在旧字段删除（Q1c）时一并删除。</b></para>
    /// </summary>
    public static class IconFieldMigration
    {
        [MenuItem("Tools/Ale Toolkit/Inventory System/迁移/图标字段迁移到属性系统", priority = 2000)]
        public static void Migrate()
        {
            var guids = AssetDatabase.FindAssets("t:" + nameof(InventoryDatabase));
            int dbCount = 0, fieldCount = 0;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var db = AssetDatabase.LoadAssetAtPath<InventoryDatabase>(path);
                if (!db) continue;

                int n = MigrateDatabase(db);
                if (n > 0)
                {
                    EditorUtility.SetDirty(db);
                    fieldCount += n;
                    dbCount++;
                }
            }
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("图标字段迁移",
                $"已迁移 {dbCount} 个数据库、共 {fieldCount} 处图标字段到属性系统（iconValue / backgroundSpriteValue），并已保存。\n\n" +
                "确认无误后即可删除旧字段（Q1 第三段）。", "知道了");
        }

        private static int MigrateDatabase(InventoryDatabase db)
        {
            int n = 0;
            foreach (var s in db.Skills)
                if (s != null && MigrateSprite(s.icon, s.iconAddress, s.iconValue)) n++;
            foreach (var t in db.SkillTemplates)
                if (t != null && MigrateSprite(t.icon, t.iconAddress, t.iconValue)) n++;
            foreach (var ft in db.FunctionTags)
                if (ft != null && MigrateSprite(ft.backgroundSprite, ft.backgroundSpriteAddress, ft.backgroundSpriteValue)) n++;
            return n;
        }

        /// <summary>把旧「直接引用 + 授权地址」拷入新 Sprite 属性值（仅在新值为空时，幂等）。返回是否发生拷贝。</summary>
        private static bool MigrateSprite(Sprite oldSprite, string oldAddress, AttributeValue target)
        {
            if (target == null || target.Type != EFieldType.Sprite) return false;          // 非 Sprite 型（异常）不误写
            if (target.GetObject(0) != null || !string.IsNullOrEmpty(target.GetObjAddress(0)))
                return false;                                                                // 新值已有数据：幂等跳过

            bool changed = false;
            if (oldSprite)                          { target.SetObject(0, oldSprite);   changed = true; }
            if (!string.IsNullOrEmpty(oldAddress))  { target.SetObjAddress(0, oldAddress); changed = true; }
            return changed;
        }
    }
}
