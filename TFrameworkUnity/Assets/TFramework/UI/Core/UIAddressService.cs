using System;

namespace TFramework.UI
{
    /// <summary>
    /// UIアドレス制御サービス
    /// </summary>
    internal sealed class UIAddressService
    {
        private readonly System.Collections.Generic.Dictionary<Type, string> _pageAddressMap = new();
        private readonly System.Collections.Generic.Dictionary<Type, string> _dialogAddressMap = new();

        /// <summary>
        /// pageアドレス登録
        /// </summary>
        public void RegisterPageAddress<TPage>(string address) where TPage : UIPageBase
        {
            _pageAddressMap[typeof(TPage)] = address;
        }

        /// <summary>
        /// dialogアドレス登録
        /// </summary>
        public void RegisterDialogAddress<TDialog>(string address) where TDialog : UIDialogBase
        {
            _dialogAddressMap[typeof(TDialog)] = address;
        }

        /// <summary>
        /// pageアドレス取得
        /// </summary>
        public string GetPageAddress<TPage>() where TPage : UIPageBase
        {
            return _pageAddressMap.TryGetValue(typeof(TPage), out string address)
                ? address
                : typeof(TPage).Name;
        }

        /// <summary>
        /// dialogアドレス取得
        /// </summary>
        public string GetDialogAddress<TDialog>()
        {
            return _dialogAddressMap.TryGetValue(typeof(TDialog), out string address)
                ? address
                : typeof(TDialog).Name;
        }
    }
}
