using System;
using System.Security.Cryptography;

namespace Erame.Core{
    /// <summary>
    /// 基于 AES-GCM 的存档加解密实现。
    /// 输出格式：[12 字节 Nonce][密文][16 字节 Tag]。GCM 自带认证，任何比特翻转都会导致解密失败。
    /// </summary>
    public sealed class AesGcmSaveCrypto : ISaveCrypto{
        private const int NonceLength = 12;
        private const int TagLength = 16;

        private readonly byte[] _key;

        public AesGcmSaveCrypto(byte[] key){
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (key.Length != 16 && key.Length != 24 && key.Length != 32){
                throw new ArgumentException("AES Key 必须是 16, 24, 或 32 字节。", nameof(key));
            }
            // 复制一份，避免调用方后续修改数组内容
            _key = new byte[key.Length];
            Buffer.BlockCopy(key, 0, _key, 0, key.Length);
        }

        public byte[] Encrypt(byte[] data){
            if (data == null) throw new ArgumentNullException(nameof(data));

            byte[] nonce = new byte[NonceLength];
            using (var rng = RandomNumberGenerator.Create()){
                rng.GetBytes(nonce);
            }

            byte[] cipher = new byte[data.Length];
            byte[] tag = new byte[TagLength];

            using (var aes = new AesGcm(_key)){
                aes.Encrypt(nonce, data, cipher, tag);
            }

            byte[] result = new byte[NonceLength + cipher.Length + TagLength];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceLength);
            Buffer.BlockCopy(cipher, 0, result, NonceLength, cipher.Length);
            Buffer.BlockCopy(tag, 0, result, NonceLength + cipher.Length, TagLength);
            return result;
        }

        public byte[] Decrypt(byte[] data){
            if (data == null || data.Length < NonceLength + TagLength) return null;

            int cipherLength = data.Length - NonceLength - TagLength;

            byte[] nonce = new byte[NonceLength];
            Buffer.BlockCopy(data, 0, nonce, 0, NonceLength);

            byte[] cipher = new byte[cipherLength];
            Buffer.BlockCopy(data, NonceLength, cipher, 0, cipherLength);

            byte[] tag = new byte[TagLength];
            Buffer.BlockCopy(data, NonceLength + cipherLength, tag, 0, TagLength);

            byte[] plain = new byte[cipherLength];
            try{
                using (var aes = new AesGcm(_key)){
                    aes.Decrypt(nonce, cipher, tag, plain);
                }
                return plain;
            }
            catch (CryptographicException){
                return null; // 认证失败：篡改或损坏
            }
        }
    }
}
