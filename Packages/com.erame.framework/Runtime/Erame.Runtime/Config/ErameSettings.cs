using System;
using System.Security.Cryptography;
using System.Text;
using Erame.Core;
using UnityEngine;

namespace Erame.Runtime.Config{
    /// <summary>
    /// 框架运行配置。放在 Assets 中并通过 GameBootstrap 引用即可，无需修改框架源码。
    /// </summary>
    [CreateAssetMenu(fileName = "ErameSettings", menuName = "Erame/框架设置", order = 0)]
    public sealed class ErameSettings : ScriptableObject{
        public const int DevSteamAppId = 480;
        public const int DefaultKeyLength = 32;

        private const string DevSaveKeyPrefKey = "Erame.DevSaveKey";

        [Header("平台")]
        [Tooltip("Steam 应用 ID。发布前务必替换为你在 Steamworks 后台创建的真实 AppID。")]
        [SerializeField] private uint steamAppId = DevSteamAppId;

        [Header("日志")]
        [SerializeField] private LogLevel logLevel = LogLevel.Info;

        [Header("存档")]
        [Tooltip("AES-GCM 存档密钥，UTF-8 字节数必须是 16/24/32。留空时会在开发期自动生成并保存在 PlayerPrefs。")]
        [SerializeField] private string saveKey = string.Empty;

        public uint SteamAppId => steamAppId;
        public LogLevel LogLevel => logLevel;

        /// <summary>未在场景中指定时使用的运行时默认配置（不落盘，仅当前会话有效）。</summary>
        public static ErameSettings Default{
            get{
                if (_default == null){
                    _default = CreateInstance<ErameSettings>();
                    _default.name = "ErameSettings (Default)";
                    _default.hideFlags = HideFlags.DontSave;
                }
                return _default;
            }
        }

        private static ErameSettings _default;

        /// <summary>
        /// 获取存档密钥。优先使用资产中配置的密钥；未配置或长度非法时降级到开发期密钥，
        /// 保证不同会话之间仍能读取同一份存档。
        /// </summary>
        public byte[] GetSaveKey(){
            if (!string.IsNullOrEmpty(saveKey)){
                var bytes = Encoding.UTF8.GetBytes(saveKey);
                if (IsValidKeyLength(bytes.Length)) return bytes;

                Debug.LogWarning($"[Erame] ErameSettings 中的存档密钥长度为 {bytes.Length} 字节，AES 要求 16/24/32 字节，已临时改用开发期密钥。");
            }

            return CreateOrLoadDevKey();
        }

        private static bool IsValidKeyLength(int length){
            return length == 16 || length == 24 || length == 32;
        }

        private static byte[] CreateOrLoadDevKey(){
            var stored = PlayerPrefs.GetString(DevSaveKeyPrefKey, string.Empty);
            if (!string.IsNullOrEmpty(stored)){
                try{
                    var existing = Convert.FromBase64String(stored);
                    if (IsValidKeyLength(existing.Length)) return existing;
                }
                catch (FormatException){
                    // 数据损坏时重新生成
                }
            }

            var key = new byte[DefaultKeyLength];
            using (var rng = RandomNumberGenerator.Create()){
                rng.GetBytes(key);
            }

            PlayerPrefs.SetString(DevSaveKeyPrefKey, Convert.ToBase64String(key));
            PlayerPrefs.Save();

            Debug.LogWarning("[Erame] 未配置存档密钥，已生成开发期密钥并写入 PlayerPrefs。发布前请在 ErameSettings 资产中填写固定密钥。");
            return key;
        }
    }
}
