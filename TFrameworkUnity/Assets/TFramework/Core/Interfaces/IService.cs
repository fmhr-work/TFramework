using System;

namespace TFramework.Core
{
    /// <summary>
    /// サービスの基本インターフェース
    /// VContainerでの登録に使用するマーカー
    /// </summary>
    public interface IService
    {
    }

    /// <summary>
    /// 破棄可能なサービスのインターフェース
    /// IDisposableを継承し、リソースの解放を保証する
    /// </summary>
    public interface IDisposableService : IService, IDisposable
    {
    }
}
