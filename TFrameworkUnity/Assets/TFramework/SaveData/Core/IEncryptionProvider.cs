namespace TFramework.SaveData
{
    /// <summary>
    /// Encryption Provider Interface
    /// データの暗号化・復号化を行う
    /// </summary>
    public interface IEncryptionProvider
    {
        /// <summary>
        /// データを暗号化する
        /// </summary>
        byte[] Encrypt(byte[] data);

        /// <summary>
        /// データを復号化する
        /// </summary>
        byte[] Decrypt(byte[] data);
    }
}
