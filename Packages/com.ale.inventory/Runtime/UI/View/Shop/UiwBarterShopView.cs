namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 等价交换店视图（占位）。本期不实现交易逻辑：结算按钮始终禁用，点击结算仅提示「暂不支持」。
    /// 待真正实现等价交换时在此补充逻辑。
    /// </summary>
    public class UiwBarterShopView : UiwShopViewBase
    {
        protected override ShopType ExpectedType => ShopType.Barter;

        /// <summary>等价交换暂不支持结算。</summary>
        protected override void Settle() => ShowHint(notSupportedHint);

        /// <summary>占位类型：结算按钮始终禁用。</summary>
        protected override bool CanSettle(int selectedCount) => false;
    }
}
