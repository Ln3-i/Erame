namespace Erame.Core{
    /// <summary>
    /// 存档加解密契约。实现类可被注入到存档服务，便于替换算法与单元测试。
    /// </summary>
    public interface ISaveCrypto{
        /// <summary>加密明文数据。</summary>
        byte[] Encrypt(byte[] data);

        /// <summary>解密数据。数据损坏、被篡改或格式不符时返回 null。</summary>
        byte[] Decrypt(byte[] data);
    }
}
