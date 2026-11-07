using Game.Runtime;
using Game.Runtime.Services;
using System;
using NUnit.Framework;
using Erame.Core;

namespace Tests{
    public class AesGcmSaveCryptoTests{
        private static byte[] NewKey(int length){
            var key = new byte[length];
            for (int i = 0; i < length; i++) key[i] = (byte)(i * 7 + 1);
            return key;
        }

        [Test]
        public void RoundTrip_PreservesData(){
            var crypto = new AesGcmSaveCrypto(NewKey(32));
            var data = new byte[100];
            for (int i = 0; i < data.Length; i++) data[i] = (byte)i;

            var decrypted = crypto.Decrypt(crypto.Encrypt(data));

            CollectionAssert.AreEqual(data, decrypted);
        }

        [Test]
        public void RoundTrip_EmptyData_ReturnsEmptyArray(){
            var crypto = new AesGcmSaveCrypto(NewKey(16));
            var decrypted = crypto.Decrypt(crypto.Encrypt(Array.Empty<byte>()));

            Assert.IsNotNull(decrypted);
            Assert.AreEqual(0, decrypted.Length);
        }

        [Test]
        public void Encrypt_OutputLength_IsDataPlusNonceAndTag(){
            var crypto = new AesGcmSaveCrypto(NewKey(32));
            var encrypted = crypto.Encrypt(new byte[50]);

            Assert.AreEqual(50 + 12 + 16, encrypted.Length);
        }

        [Test]
        public void Decrypt_TamperedCiphertext_ReturnsNull(){
            var crypto = new AesGcmSaveCrypto(NewKey(32));
            var encrypted = crypto.Encrypt(new byte[64]);
            encrypted[20] ^= 0xFF; // 翻转密文中的一个比特

            Assert.IsNull(crypto.Decrypt(encrypted));
        }

        [Test]
        public void Decrypt_TamperedTag_ReturnsNull(){
            var crypto = new AesGcmSaveCrypto(NewKey(32));
            var encrypted = crypto.Encrypt(new byte[64]);
            encrypted[encrypted.Length - 1] ^= 0xFF; // 翻转认证标签

            Assert.IsNull(crypto.Decrypt(encrypted));
        }

        [Test]
        public void Decrypt_WithWrongKey_ReturnsNull(){
            var crypto = new AesGcmSaveCrypto(NewKey(32));
            var differentKey = NewKey(32);
            differentKey[0] = 0xAA;

            var encrypted = crypto.Encrypt(new byte[32]);
            var wrongCrypto = new AesGcmSaveCrypto(differentKey);

            Assert.IsNull(wrongCrypto.Decrypt(encrypted));
        }

        [Test]
        public void Decrypt_TooShortInput_ReturnsNull(){
            var crypto = new AesGcmSaveCrypto(NewKey(32));
            Assert.IsNull(crypto.Decrypt(new byte[27])); // 少于 Nonce + Tag
        }

        [Test]
        public void Constructor_InvalidKeyLength_Throws(){
            Assert.Throws<ArgumentNullException>(() => new AesGcmSaveCrypto(null));
            Assert.Throws<ArgumentException>(() => new AesGcmSaveCrypto(new byte[15]));
            Assert.Throws<ArgumentException>(() => new AesGcmSaveCrypto(new byte[33]));
        }

        [Test]
        public void Constructor_CopiesKey_SubsequentMutationHasNoEffect(){
            var key = NewKey(32);
            var crypto = new AesGcmSaveCrypto(key);
            var data = new byte[] { 1, 2, 3 };

            var encrypted = crypto.Encrypt(data);
            key[0] ^= 0xFF; // 调用方事后篡改原数组

            CollectionAssert.AreEqual(data, crypto.Decrypt(encrypted));
        }
    }
}
