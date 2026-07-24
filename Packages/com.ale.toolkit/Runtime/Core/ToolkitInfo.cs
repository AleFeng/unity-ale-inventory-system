namespace Ale.Toolkit.Runtime
{
    /// <summary>
    /// 包元信息。供宿主插件在运行时 / 编辑器内做版本检查与日志标注。
    ///
    /// <para>本包为面向 Unity 插件开发的通用底层，不含任何具体业务领域概念：
    /// 自定义属性系统、排序、虚拟滚动列表、编辑器三列框架、编辑器界面多语言，
    /// 以及 TextMeshPro / Localization / Addressables 的可选支持层。</para>
    /// </summary>
    public static class ToolkitInfo
    {
        /// <summary>UPM 包名。</summary>
        public const string PackageName = "com.ale.toolkit";

        /// <summary>包版本，与 <c>package.json</c> 的 <c>version</c> 保持一致。</summary>
        public const string Version = "1.0.0";
    }
}
