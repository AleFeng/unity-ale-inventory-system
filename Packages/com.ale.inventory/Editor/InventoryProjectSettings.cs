using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 仓库系统的<b>项目级</b>编辑器设置（版本控制友好）。经 <see cref="ScriptableSingleton{T}"/> 持久化到
    /// <c>ProjectSettings/AleInventorySettings.asset</c>（随仓库提交，用 Unity 原生序列化，数据库引用按 GUID 存）。
    ///
    /// <para>目前承载「创建新数据文件」使用的<b>数据模板</b>——该选择通常全项目一致，故入库共享，
    /// 分发到别的工程时各自维护自己的一份。相对地，<b>每人自定</b>的偏好（启动自动显示、上次打开的数据库路径）
    /// 仍存 <see cref="EditorPrefs"/>，不进本文件。</para>
    /// </summary>
    [FilePath("ProjectSettings/AleInventorySettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class InventoryProjectSettings : ScriptableSingleton<InventoryProjectSettings>
    {
        [SerializeField, Tooltip("创建新数据文件时使用的模板数据库（留空则使用默认数据）。")]
        internal InventoryDatabase templateDatabase;

        /// <summary>写回 <c>ProjectSettings/AleInventorySettings.asset</c>（文本序列化，便于版本管理 diff）。</summary>
        public void SaveSettings() => Save(true);
    }
}
