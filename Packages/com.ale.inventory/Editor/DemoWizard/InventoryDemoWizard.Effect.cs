using Ale.Effect;
using UnityEditor;
using static Ale.Toolkit.Editor.UiPrefabBuilder;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 演示用效果库（toolkit <see cref="EffectDatabase"/>）的生成。
    ///
    /// <para><b>为什么 Demo 需要它</b>：道具的「可使用」在本包里唯一的判据是 <c>Item.onUseEffectRefs</c> 非空
    /// （见 <c>Item</c> 的字段注释），而 1.13.0 起效果定义已外移到 toolkit 的共用效果库。
    /// 没有这份资产的话，Demo 里没有任何道具算「可使用」，右键菜单的「使用」永远不出现，整条使用链路无从验证。</para>
    ///
    /// <para><b>刻意做到最小</b>：只有一条 <see cref="EDurationPolicy.Instant"/> 效果，执行项用 toolkit 内置的
    /// <c>Effect.NoOp</c>（无参、恒返回已施加）。目的仅仅是让 <c>EffectApplier.Apply</c> 成功一次——
    /// 因为 <c>UseItemCore</c> 要「至少一个效果施加成功」才扣减道具（见 <c>InventoryRuntimeManager.Use.cs</c>）。
    /// 真实工程应在 Effect Editor 里配置有实际数值效果的定义（回血、加属性等），那需要角色系统提供属性 Sink，
    /// 不属于本库存 Demo 的范围。</para>
    /// </summary>
    public static partial class InventoryDemoWizard
    {
        /// <summary>演示用「道具使用」效果的 ID。道具的 <c>onUseEffectRefs</c> 按此 id 引用。</summary>
        internal const string KDemoUseEffectId = "演示·道具使用";

        /// <summary>演示用效果库资产路径（与库存数据库同目录）。</summary>
        static string EffectDatabasePath => DataDir + "/EffectDatabase.asset";

        /// <summary>
        /// 生成演示用效果库：一条 Instant 效果，执行项为内置的 <c>Effect.NoOp</c>。
        /// <para>就地覆盖以保住资产 GUID —— 直接 <c>CreateAsset</c> 会先删旧资产、连带换掉 GUID，
        /// 静默打断 <c>InventoryManager.prefab</c> 对它的引用（理由同 <c>SaveDatabaseAsset</c>）。</para>
        /// </summary>
        static void GetOrCreateEffectDatabase()
        {
            EnsureFolder(DataDir);

            var db = ScriptableObject.CreateInstance<EffectDatabase>();

            var entry = new EffectEntry(KDemoUseEffectId, EDurationPolicy.Instant);
            entry.displayText.SetTextValue(0, "演示：道具使用");
            entry.descriptionText.SetTextValue(0,
                "库存 Demo 用的占位效果：不改动任何数值，只保证「使用」判定成功从而扣减 1 个道具。");

            entry.definition.id            = KDemoUseEffectId;
            entry.definition.displayName   = "演示：道具使用";
            entry.definition.durationPolicy = EDurationPolicy.Instant;

            var group = new EffectGroup();
            group.items.Add(new EffectItem("Effect.NoOp"));
            entry.definition.executions.groups.Add(group);

            db.Effects.Add(entry);
            db.NormalizeAll();

            SaveEffectDatabaseAsset(db, EffectDatabasePath);
        }

        /// <summary>就地覆盖保存效果库资产（保住 GUID），与 <c>SaveDatabaseAsset</c> 同一手法。</summary>
        static void SaveEffectDatabaseAsset(EffectDatabase db, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<EffectDatabase>(path);
            if (existing)
            {
                EditorUtility.CopySerialized(db, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(db);   // 临时实例，内容已拷进原资产
            }
            else
                AssetDatabase.CreateAsset(db, path);

            AssetDatabase.SaveAssets();
            Debug.Log("[InventoryDemoWizard] 效果库已保存：" + path);
        }
    }
}
