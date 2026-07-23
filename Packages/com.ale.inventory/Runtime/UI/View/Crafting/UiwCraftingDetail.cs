#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using System.Collections;
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 制作蓝图详情面板（MonoBehaviour）。自上而下显示：主产出（图标/名称/描述）、副产出图标（悬停弹详情）、
    /// 消耗道具列表（图标/名称/需求/持有）、可制作次数 + 主产出持有量、制作次数选择（+/- 与输入框）、
    /// 制作 / 停止按钮与进度条。制作循环由本组件协程驱动，逐次调用 <see cref="CraftingRuntimeManager.CraftOnce(CraftingBlueprint)"/>。
    /// </summary>
    public class UiwCraftingDetail : MonoBehaviour
    {
        [Header("主产出")]
        [Tooltip("主产出道具的详情显示（复用 UiwInventoryItemDetail：图标 / 名称 / 描述）。")]
        public UiwInventoryItemDetail mainOutputDetail;

        [Header("副产出")]
        [Tooltip("副产出图标容器。可空。")]
        public Transform secondaryOutputContainer;
        [Tooltip("副产出图标 Prefab（UiwInventoryItemSimple，自动启用悬停弹窗）。可空。")]
        public UiwInventoryItemSimple secondaryOutputPrefab;

        [Header("消耗道具")]
        [Tooltip("消耗道具行容器。")]
        public Transform inputContainer;
        [Tooltip("消耗道具行 Prefab（UiwCraftingInputCell）。")]
        public UiwCraftingInputCell inputCellPrefab;

        [Header("产出信息")]
        [Tooltip("可制作次数文本。")]
        public InventoryText craftableCountText;
        [Tooltip("可制作次数格式：{0}=次数。")]
        public string        craftableFormat = "可制作：{0}";
        [Tooltip("主产出持有量文本。")]
        public InventoryText ownedCountText;
        [Tooltip("主产出持有量格式：{0}=数量。")]
        public string        ownedFormat = "持有：{0}";

        [Header("制作次数")]
        [Tooltip("数字计数器组件（+/- 调整制作次数，含长按连发与可选输入框）。")]
        public UiwNumberCounter counter;

        [Header("制作操作")]
        [Tooltip("制作 / 停止 按钮。")]
        public Button        craftButton;
        [Tooltip("制作按钮上的文本。可空。")]
        public InventoryText craftButtonLabel;
        [Tooltip("未制作时按钮文本。")]
        public string        craftText = "制作";
        [Tooltip("制作中按钮文本。")]
        public string        stopText  = "停止制作";
        [Tooltip("制作进度条（Filled 类型 Image，按 fillAmount 推进）。可空。")]
        public Image         progressFill;

        // ── 运行时状态 ────────────────────────────────────────────────────────────
        private CraftingBlueprint  _craftingBlueprint;
        private NumberFormatLocale _numberFormatLocale;
        private int       _count;        // 选择 / 剩余的制作次数
        private int       _maxCraftable; // 当前可制作最大次数
        private bool      _crafting;
        private Coroutine _craftRoutine;

        private readonly UiwWidgetPool<UiwInventoryItemSimple> _secondaryPool = new UiwWidgetPool<UiwInventoryItemSimple>();
        private readonly UiwWidgetPool<UiwCraftingInputCell>   _inputPool     = new UiwWidgetPool<UiwCraftingInputCell>();

        private void Awake()
        {
            if (craftButton) craftButton.onClick.AddListener(OnCraftButton);
            if (counter)     counter.OnValueChanged += OnCounterChanged;
        }

        private void OnDestroy()
        {
            if (counter) counter.OnValueChanged -= OnCounterChanged;
        }

        private void OnDisable() => StopCrafting();

        /// <summary>计数器值变化（用户调整制作次数，仅空闲可交互时触发）：记录到本地 _count。</summary>
        private void OnCounterChanged(int value) => _count = value;

        #region 绑定 / 刷新

        /// <summary>绑定蓝图并刷新全部显示（停止任何进行中的制作，制作次数重置为 1）。</summary>
        public void Bind(CraftingBlueprint bp, NumberFormatLocale fmt)
        {
            StopCrafting();
            _craftingBlueprint  = bp;
            _numberFormatLocale = fmt;

            RenderMainOutput();
            RenderSecondaryOutputs();
            RenderInputs();
            RecomputeCraftable();
            _count = _maxCraftable > 0 ? 1 : 0;
            UpdateUI();
        }

        /// <summary>外部刷新（仓库内容变化时）。重算可制作 / 持有 / 消耗显示；非制作中时校正制作次数。</summary>
        public void Refresh()
        {
            if (_craftingBlueprint == null) return;
            RecomputeCraftable();
            RenderInputs();
            UpdateUI();   // UpdateUI → SyncCounter 内部按空闲 / 制作中钳制并刷新计数器
        }

        #endregion

        #region 渲染

        private void RenderMainOutput()
        {
            if (!mainOutputDetail) return;
            mainOutputDetail.numberFormat = _numberFormatLocale;
            var main = _craftingBlueprint != null ? _craftingBlueprint.PrimaryOutput : null;
            if (main != null && !string.IsNullOrEmpty(main.itemId))
                mainOutputDetail.SetSlot(null, new RuntimeItemSlot(null, main.itemId, main.count));
            else
                mainOutputDetail.SetEmpty();
        }

        private void RenderSecondaryOutputs()
        {
            if (!secondaryOutputContainer || !secondaryOutputPrefab) return;

            _secondaryPool.Configure(secondaryOutputPrefab, secondaryOutputContainer);
            _secondaryPool.Begin();
            if (_craftingBlueprint != null && _craftingBlueprint.outputs != null)
                for (int i = 1; i < _craftingBlueprint.outputs.Count; i++)   // index 0 = 主产出
                {
                    var o = _craftingBlueprint.outputs[i];
                    if (o == null || string.IsNullOrEmpty(o.itemId)) continue;
                    var cell = _secondaryPool.Next();
                    if (!cell) break;
                    cell.numberFormat      = _numberFormatLocale;
                    cell.showDetailTooltip = true;
                    cell.SetItem(o.itemId, o.count);
                }
            int count = _secondaryPool.ActiveCount;
            _secondaryPool.End();

            secondaryOutputContainer.gameObject.SetActive(count > 0);
        }

        private void RenderInputs()
        {
            if (!inputContainer || !inputCellPrefab) return;

            _inputPool.Configure(inputCellPrefab, inputContainer);
            _inputPool.Begin();
            if (_craftingBlueprint != null && _craftingBlueprint.inputs != null)
                foreach (var input in _craftingBlueprint.inputs)
                {
                    if (input == null || string.IsNullOrEmpty(input.itemId)) continue;
                    var cell = _inputPool.Next();
                    if (!cell) break;
                    cell.numberFormat = _numberFormatLocale;
                    cell.Bind(_craftingBlueprint, input);
                }
            // 多余的材料格保持显示但置空（与原实现一致：格位不消失，只是没有内容）。
            _inputPool.End(c => c.SetEmpty());
        }

        #endregion

        #region 制作次数 / 可制作

        private void RecomputeCraftable()
            => _maxCraftable = (_craftingBlueprint != null && CraftingRuntimeManager.Instance != null)
                ? CraftingRuntimeManager.Instance.GetMaxCraftable(_craftingBlueprint) : 0;

        /// <summary>
        /// 把当前 制作次数 / 范围 / 可交互状态同步到数字计数器：
        /// 空闲态范围 [可制作>0?1:0, 可制作] 并回读钳制后的值；制作中仅显示剩余次数（不钳制）、禁用调整。
        /// </summary>
        private void SyncCounter()
        {
            if (!counter) return;
            if (_crafting)
            {
                counter.SetRange(0, Mathf.Max(_maxCraftable, _count));
                counter.SetValue(_count, notify: false);
                counter.SetInteractable(false);
            }
            else
            {
                counter.Configure(0, _maxCraftable, _count, notify: false);
                _count = counter.Value;   // 回读钳制后的值，保持同步
                counter.SetInteractable(_maxCraftable > 0);
            }
        }

        #endregion

        #region 制作循环

        private void OnCraftButton()
        {
            if (_crafting)
            {
                StopCrafting();
                RecomputeCraftable();
                UpdateUI();
                return;
            }
            StartCrafting();
        }

        private void StartCrafting()
        {
            if (_craftingBlueprint == null || CraftingRuntimeManager.Instance == null) return;
            RecomputeCraftable();
            if (_maxCraftable <= 0 || _count <= 0) return;

            _crafting     = true;
            UpdateUI();
            _craftRoutine = StartCoroutine(CraftRoutine());
        }

        private void StopCrafting()
        {
            _crafting = false;
            if (_craftRoutine != null) { StopCoroutine(_craftRoutine); _craftRoutine = null; }
            SetProgress(0f);
        }

        private IEnumerator CraftRoutine()
        {
            var mgr = CraftingRuntimeManager.Instance;
            while (_count > 0 && mgr != null && mgr.CanCraftOnce(_craftingBlueprint))
            {
                float dur = Mathf.Max(0f, _craftingBlueprint.craftTime);
                float t   = 0f;
                while (t < dur)
                {
                    if (!_crafting) yield break;
                    t += Time.deltaTime;
                    SetProgress(Mathf.Clamp01(t / dur));
                    yield return null;
                }
                SetProgress(1f);

                if (!mgr.CraftOnce(_craftingBlueprint)) break;
                _count = Mathf.Max(0, _count - 1);

                RecomputeCraftable();
                RenderInputs();
                UpdateOwned();
                SyncCounter();
                SetProgress(0f);

                if (dur <= 0f) yield return null;   // 即时制作也每帧让一次，避免长批次卡死
            }

            StopCrafting();
            RecomputeCraftable();
            UpdateUI();
        }

        private void SetProgress(float v)
        {
            if (progressFill) progressFill.fillAmount = Mathf.Clamp01(v);
        }

        #endregion

        #region UI 更新

        private void UpdateUI()
        {
            if (craftableCountText) craftableCountText.text = string.Format(craftableFormat, _maxCraftable);
            UpdateOwned();
            SyncCounter();
            if (craftButtonLabel) craftButtonLabel.text = _crafting ? stopText : craftText;
            if (craftButton)      craftButton.interactable = _crafting || _maxCraftable > 0;
        }

        private void UpdateOwned()
        {
            if (!ownedCountText) return;
            var main  = _craftingBlueprint != null ? _craftingBlueprint.PrimaryOutput : null;
            int owned = (main != null && CraftingRuntimeManager.Instance != null)
                ? CraftingRuntimeManager.Instance.GetOwnedAcross(_craftingBlueprint, main.itemId) : 0;
            ownedCountText.text = string.Format(ownedFormat, owned);
        }

        #endregion
    }
}
