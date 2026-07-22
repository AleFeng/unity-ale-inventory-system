#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using UnityEngine;

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// UI 视图公共基类（背包 / 商店 / 制作等）。承载与具体视图无关的通用能力：
    /// <list type="bullet">
    ///   <item>标题文本 <see cref="titleLabel"/>；</item>
    ///   <item>打开（模板方法 <see cref="Open"/>）/ 关闭（<see cref="Close"/>）/ 切换（<see cref="ToggleOpenClose"/>），
    ///         具体「打开逻辑」「取消订阅」「重新打开」下沉到子类 <see cref="Open"/> / <see cref="Unsubscribe"/> / <see cref="Reopen"/>；</item>
    ///   <item>按当前语言解析数字格式 <see cref="ResolveNumberFormatLocale"/>。</item>
    /// </list>
    /// 无参 <c>Open()</c> 为公共模板（仅激活面板），子类覆写实现各自打开逻辑；因参数不同，
    /// 各视图另在子类提供带参 <c>Open(...)</c> 重载——缓存参数到字段后调用无参 <c>Open()</c>。
    /// </summary>
    public abstract class UiwViewBase : MonoBehaviour
    {
        #region 标题
        [Header("标题")]
        [Tooltip("标题文本：显示当前视图名称（仓库 / 商店 / 模板名等）。为空时各视图自行回退。")]
        public InventoryText titleLabel;

        /// <summary>
        /// 组合标题文本（各视图刷新标题时复用）：取显示名 <paramref name="displayName"/>
        /// （由各视图从固定 <c>displayNameText</c> 解析而来），为空时用 <paramref name="id"/> 兜底。
        /// </summary>
        /// <param name="displayName">对象显示名（固定 <c>displayNameText.ResolveText()</c> 的结果）。</param>
        /// <param name="id">对象 ID（显示名为空时的兜底）。</param>
        protected string ResolveTitleText(string displayName, string id)
        {
            return !string.IsNullOrEmpty(displayName) ? displayName : id;
        }
        #endregion

        #region 打开与关闭
        /// <summary>
        /// 打开视图（模板方法）。基类实现只做各视图共有的唯一步骤——激活面板；
        /// 子类覆写本方法：先调用 <c>base.Open()</c> 激活面板，再执行本视图特定的构建 / 订阅 / 刷新。
        /// 带参数的 <c>Open(...)</c> 重载应先把参数缓存到字段，再调用本无参方法。
        /// </summary>
        public virtual void Open()
        {
            gameObject.SetActive(true);
        }

        /// <summary>关闭视图：取消事件订阅并停用面板。</summary>
        public void Close()
        {
            Unsubscribe();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 打开 / 关闭 切换（主要用于挂到按钮 onClick）。已打开（GameObject 激活）则 <see cref="Close"/>；
        /// 否则用上次打开的参数重新打开（<see cref="Reopen"/>）。
        /// <remarks>关闭会停用整个面板，故切换按钮应挂在「常驻激活」的独立物体上。</remarks>
        /// </summary>
        public void ToggleOpenClose()
        {
            if (gameObject.activeSelf) { Close(); return; }
            Reopen();
        }

        /// <summary>取消本视图的所有运行时事件订阅（由 <see cref="Close"/> 调用；子类亦可在 OnDestroy 调用）。</summary>
        protected abstract void Unsubscribe();

        /// <summary>用上次打开时缓存的参数重新打开本视图（供 <see cref="ToggleOpenClose"/>）。</summary>
        protected abstract void Reopen();
        #endregion

        #region 数字格式
        /// <summary>当前语言代码（可在子类覆写以对接本地化）；默认空，命中回退语言。</summary>
        protected virtual string GetCurrentLanguage() => string.Empty;

        /// <summary>
        /// 从 <see cref="NumberFormatConfig"/> 中按当前语言代码解析对应的 <see cref="NumberFormatLocale"/>。
        /// 未命中回退首个；config 为 null 或无语言时返回 null。
        /// </summary>
        protected NumberFormatLocale ResolveNumberFormatLocale(NumberFormatConfig config)
        {
            if (config == null || config.locales == null || config.locales.Count == 0) return null;
            string lang = GetCurrentLanguage();
            foreach (var locale in config.locales)
                if (locale.languageCode == lang) return locale;
            return config.locales[0];
        }
        #endregion
    }
}
