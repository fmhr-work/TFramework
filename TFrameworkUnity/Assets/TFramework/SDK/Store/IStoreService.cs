using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace TFramework.SDK
{
    /// <summary>
    /// 商品タイプの定義
    /// </summary>
    public enum ProductType
    {
        Consumable,     // 消費型（例：コイン）
        NonConsumable,  // 非消費型（例：広告削除）
        Subscription    // 購読型（サブスクリプション）
    }

    /// <summary>
    /// 商品定義モデル
    /// </summary>
    public class Product
    {
        public string Id { get; }
        public string LocalizedTitle { get; }
        public string LocalizedDescription { get; }
        public string LocalizedPriceString { get; }
        public decimal Price { get; }
        public string Currency { get; }
        public ProductType Type { get; }

        public Product(string id, string title, string description, string priceString, decimal price, string currency, ProductType type)
        {
            Id = id;
            LocalizedTitle = title;
            LocalizedDescription = description;
            LocalizedPriceString = priceString;
            Price = price;
            Currency = currency;
            Type = type;
        }
    }

    /// <summary>
    /// 購入エラーの定義
    /// </summary>
    public class PurchaseError
    {
        public string ProductId { get; }
        public string ErrorMessage { get; }

        public PurchaseError(string productId, string message)
        {
            ProductId = productId;
            ErrorMessage = message;
        }
    }

    /// <summary>
    /// 購入結果
    /// </summary>
    public class PurchaseResult
    {
        public bool IsSuccess { get; }
        public Product Product { get; }
        public string TransactionId { get; }
        public string ErrorMessage { get; }

        private PurchaseResult(bool isSuccess, Product product, string transactionId, string errorMessage)
        {
            IsSuccess = isSuccess;
            Product = product;
            TransactionId = transactionId;
            ErrorMessage = errorMessage;
        }

        public static PurchaseResult Success(Product product, string transactionId)
        {
            return new PurchaseResult(true, product, transactionId, null);
        }

        public static PurchaseResult Failure(Product product, string errorMessage)
        {
            return new PurchaseResult(false, product, null, errorMessage);
        }
    }

    /// <summary>
    /// ストアサービス（アプリ内課金/IAP）のインターフェース
    /// Unity IAP等のラッパーとして機能する
    /// </summary>
    public interface IStoreService
    {
        /// <summary>
        /// インタフェースの初期化が完了しているか
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 提供されている商品リスト
        /// </summary>
        IReadOnlyList<Product> Products { get; }

        /// <summary>
        /// 指定IDの商品情報を取得
        /// </summary>
        /// <param name="productId">商品ID</param>
        /// <returns>商品がある場合はProduct、ない場合はnull</returns>
        Product GetProduct(string productId);

        /// <summary>
        /// ストアサービスの初期化
        /// </summary>
        /// <param name="ct">キャンセレーショントークン</param>
        UniTask InitializeAsync(CancellationToken ct = default);

        /// <summary>
        /// 指定した商品の購入を開始
        /// </summary>
        /// <param name="productId">購入する商品のID</param>
        /// <param name="ct">キャンセレーショントークン</param>
        /// <returns>購入結果</returns>
        UniTask<PurchaseResult> PurchaseAsync(string productId, CancellationToken ct = default);

        /// <summary>
        /// 過去の購入（非消費アイテム）を復元
        /// </summary>
        /// <param name="ct">キャンセレーショントークン</param>
        UniTask RestorePurchasesAsync(CancellationToken ct = default);

        /// <summary>
        /// 購入完了イベント通知（R3 Observable）
        /// </summary>
        Observable<Product> OnPurchaseCompleted { get; }

        /// <summary>
        /// 購入失敗イベント通知（R3 Observable）
        /// </summary>
        Observable<PurchaseError> OnPurchaseFailed { get; }
    }
}
