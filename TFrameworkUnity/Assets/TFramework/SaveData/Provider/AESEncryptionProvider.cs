using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TFramework.Debug;

namespace TFramework.SaveData
{
    /// <summary>
    /// AES Encryption Provider
    /// AESアルゴリズムを用いてデータの暗号化・復号化を行う
    /// </summary>
    public class AESEncryptionProvider : IEncryptionProvider
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public AESEncryptionProvider(string key, string iv)
        {
            _key = Encoding.UTF8.GetBytes(key.PadRight(16).Substring(0, 16)); // Ensure 16 bytes for AES-128
            _iv = Encoding.UTF8.GetBytes(iv.PadRight(16).Substring(0, 16));
        }

        /// <summary>
        /// データを暗号化する
        /// </summary>
        public byte[] Encrypt(byte[] data)
        {
            if (data == null || data.Length == 0) return data;

            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                }
                return ms.ToArray();
            }
            catch (Exception e)
            {
                TLogger.Error($"[TFramework] Encryption Error: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// データを復号化する
        /// </summary>
        public byte[] Decrypt(byte[] data)
        {
            if (data == null || data.Length == 0) return data;

            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(data);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var resultMs = new MemoryStream();
                cs.CopyTo(resultMs);
                return resultMs.ToArray();
            }
            catch (Exception e)
            {
                TLogger.Error($"[TFramework] Decryption Error: {e.Message}");
                return null; // Decryption failed
            }
        }
    }
}
