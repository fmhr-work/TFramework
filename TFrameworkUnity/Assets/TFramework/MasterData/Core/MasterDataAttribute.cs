using System;

namespace TFramework.MasterData
{
    /// <summary>
    /// MasterDataであることを示す属性
    /// クラスに付与し、シート名を指定する
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MasterDataAttribute : Attribute
    {
        /// <summary>
        /// 外部データソース（CSV/JSON）のシート名またはファイル名
        /// </summary>
        public string SheetName { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="sheetName">シート名</param>
        public MasterDataAttribute(string sheetName)
        {
            SheetName = sheetName;
        }
    }
}
