using __NS__.Runtime;
using System;

namespace __NS__.Runtime.Services.Utils{
    /// <summary>
    /// 兼容门面：内部委托给注入的 <see cref="ISaveCrypto"/> 实现（默认使用 AES-GCM）。
    /// 新代码请通过构造函数注入 <see cref="ISaveCrypto"/>，不要直接使用本类。
    /// </summary>
    public static class SaveCrypto{
        private static ISaveCrypto _impl;

        /// <summary>
        /// 由业务模块在启动时注入密钥，内部创建 AES-GCM 实现。
        /// 密钥必须是 16, 24, 或 32 字节。
        /// </summary>
        [Obsolete("请改用注入的 ISaveCrypto 实现，本方法仅用于兼容旧代码。")]
        public static void Initialize(byte[] key){
            _impl = new AesGcmSaveCrypto(key);
        }

        /// <summary>加密。数据格式由具体实现决定（默认 AES-GCM）。</summary>
        [Obsolete("请改用注入的 ISaveCrypto 实现，本方法仅用于兼容旧代码。")]
        public static byte[] Encrypt(byte[] data){
            if (_impl == null) throw new InvalidOperationException("SaveCrypto 尚未初始化，请先调用 Initialize 注入密钥。");
            return _impl.Encrypt(data);
        }

        /// <summary>解密。数据损坏或格式不符时返回 null。</summary>
        [Obsolete("请改用注入的 ISaveCrypto 实现，本方法仅用于兼容旧代码。")]
        public static byte[] Decrypt(byte[] data){
            if (_impl == null) throw new InvalidOperationException("SaveCrypto 尚未初始化。");
            return _impl.Decrypt(data);
        }
    }
}
