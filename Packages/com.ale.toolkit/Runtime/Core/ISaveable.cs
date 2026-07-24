using System.Collections.Generic;

namespace Ale.Toolkit.Runtime
{
    /// <summary>
    /// 持有需要存档的运行时状态的管理器（非泛型部分）。
    /// 供游戏层在「开新游戏」「读档前清场」时统一遍历重置，无需知道各系统的存档数据类型。
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// 清空本系统的全部运行时状态，回到初始态（如开始新游戏）。
        /// <para><b>不</b>触发本系统的变更事件——批量替换状态后由调用方自行刷新 / 重开界面。</para>
        /// </summary>
        void ResetAll();
    }

    /// <summary>
    /// 持有需要存档的运行时状态的管理器。每个持有状态的运行时管理器各实现一份，
    /// 由游戏层的 SaveManager 统一调用。
    ///
    /// <para><b>本接口的意义在于把语义契约钉在一处</b>——否则它只会以「与某某 Manager 一致」的
    /// 散落注释存在，各处各写一遍、极易漂移。实现方必须满足：</para>
    /// <list type="number">
    ///   <item><see cref="GetSaveData"/> 返回<b>深拷贝</b>：调用方持有并序列化它的期间，
    ///         继续操作运行时状态不得改变已取出的快照。</item>
    ///   <item><see cref="LoadSaveData"/> 为<b>覆盖语义</b>而非合并：先丢弃当前内存状态，再按存档重建。
    ///         存档中没有、但内存里存在的条目<b>不得残留</b>。
    ///         （若某系统存在「按容量预分配空槽」之类的概念，清空后应先重建空骨架再叠加存档——语义仍是覆盖。）</item>
    ///   <item><see cref="LoadSaveData"/> 接受 <c>null</c> 与含空 key 的脏条目，跳过而非抛异常。</item>
    ///   <item>三个方法都<b>不</b>触发本系统的变更事件。</item>
    /// </list>
    /// </summary>
    /// <typeparam name="TState">本系统的存档数据类型（每系统一种，故不做进一步抽象）。</typeparam>
    public interface ISaveable<TState> : ISaveable
    {
        /// <summary>获取全部运行时状态的深拷贝（由游戏层 SaveManager 序列化）。</summary>
        List<TState> GetSaveData();

        /// <summary>从存档数据恢复运行时状态（覆盖当前内存状态，契约见接口说明）。</summary>
        void LoadSaveData(List<TState> data);
    }
}
