using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Erame.Core;

namespace Erame.Runtime.Services{
    public sealed class FileSaveService : ISaveService, IService{
        private const int CurrentVersion = 1;
        private const string TempSuffix = ".tmp";
        private const string BackupSuffix = ".bak";

        private readonly Dictionary<string, ISaveable> _map = new();
        private readonly List<ISaveable> _ordered = new();

        private readonly ILogService _log;
        private readonly IEventBus _eventBus;
        private readonly ISaveCrypto _crypto;

        private bool _isSaving = false;

        /// <summary>
        /// 旧版本存档迁移钩子：参数为读到的版本号与解密后的原始字节，
        /// 返回迁移后的原始字节（其头部版本号必须已是 CurrentVersion）；返回 null 表示无法迁移。
        /// </summary>
        public Func<int, byte[], byte[]> RawDataMigrator { get; set; }

        public FileSaveService(ILogService log, IEventBus eventBus, ISaveCrypto crypto){
            _log = log;
            _eventBus = eventBus;
            _crypto = crypto ?? throw new ArgumentNullException(nameof(crypto));
        }

        public void Initialize(){
            _log.Info("[Save] 存档服务初始化完成");
        }

        public void Dispose(){
            _map.Clear();
            _ordered.Clear();
            _log.Info("[Save] 存档服务已销毁");
        }

        public void Register(ISaveable saveable){
            if (saveable == null || string.IsNullOrEmpty(saveable.SaveKey)) return;

            if (_map.ContainsKey(saveable.SaveKey)){
                _log.Warn($"[Save] 发现重复的存档 Key: {saveable.SaveKey}");
                return;
            }
            _map[saveable.SaveKey] = saveable;
            _ordered.Add(saveable);
        }

        public void Unregister(ISaveable saveable){
            if (saveable == null) return;
            _map.Remove(saveable.SaveKey);
            _ordered.Remove(saveable);
        }

        public async Task<bool> SaveToFileAsync(string filePath, CancellationToken cancellationToken = default){
            cancellationToken.ThrowIfCancellationRequested();

            if (_isSaving){
                _log.Warn("[Save] 正在保存中，请勿重复操作！");
                return false;
            }
            _isSaving = true;

            try{
                _eventBus.Publish(new SaveProgressEvent(0f, false));
                byte[] rawBytes = BuildSnapshot();

                bool success = await Task.Run(() => {
                    // 取消发生在触碰主存档之前：加密 + 写临时文件，替换前检查令牌
                    cancellationToken.ThrowIfCancellationRequested();

                    _eventBus.Publish(new SaveProgressEvent(0.5f, false));
                    byte[] encryptedBytes = _crypto.Encrypt(rawBytes);
                    WriteFileAtomic(filePath, encryptedBytes, cancellationToken);
                    return true;
                }, cancellationToken);

                if (success){
                    _eventBus.Publish(new SaveProgressEvent(1f, false));
                    _log.Info($"[Save] 异步保存成功: {filePath}");
                }
                return success;
            }
            catch (OperationCanceledException){
                // 取消时主存档未被替换，清理临时文件后向上抛出
                TryDelete(filePath + TempSuffix);
                _log.Warn("[Save] 保存已取消，主存档未被破坏。");
                throw;
            }
            catch (Exception ex){
                _log.Error($"[Save] 存档主线程快照失败: {ex.Message}");
                TryDelete(filePath + TempSuffix);
                return false;
            }
            finally{
                _isSaving = false; // 释放锁
            }
        }

        public async Task<bool> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default){
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(filePath)){
                _log.Warn($"[Save] 找不到存档文件: {filePath}");
                return false;
            }

            try{
                _eventBus.Publish(new SaveProgressEvent(0f, true));

                byte[] rawBytes = await Task.Run(() => ReadAndDecrypt(filePath), cancellationToken);

                // 主存档损坏或被篡改时回退到上一次的备份
                if (rawBytes == null){
                    rawBytes = await Task.Run(() => ReadAndDecrypt(filePath + BackupSuffix), cancellationToken);
                    if (rawBytes != null){
                        _log.Warn("[Save] 主存档读取失败，已回退到备份存档。");
                    }
                }

                if (rawBytes == null){
                    _log.Error("[Save] 存档解密失败！可能文件被篡改或损坏，且没有可用备份。");
                    return false;
                }

                _eventBus.Publish(new SaveProgressEvent(0.4f, true));

                int version;
                using (var versionStream = new MemoryStream(rawBytes))
                using (var versionReader = new SaveReader(versionStream)){
                    version = versionReader.ReadInt();
                }

                if (version != CurrentVersion){
                    byte[] migrated = RawDataMigrator?.Invoke(version, rawBytes);
                    if (migrated == null){
                        _log.Error($"[Save] 存档版本不匹配且未注册迁移逻辑，当前: {CurrentVersion}，文件: {version}");
                        return false;
                    }
                    rawBytes = migrated;
                    _log.Info($"[Save] 存档已从版本 {version} 迁移到 {CurrentVersion}");
                }

                return await ApplyRawDataAsync(rawBytes, cancellationToken);
            }
            catch (OperationCanceledException){
                _log.Warn("[Save] 读档已取消，主存档未被修改。");
                throw;
            }
            catch (Exception ex){
                _log.Error($"[Save] 读档流程异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>在主线程收集所有已注册模块的数据快照。</summary>
        private byte[] BuildSnapshot(){
            byte[] rawBytes;

            using (var memoryStream = new MemoryStream()){
                using (var writer = new SaveWriter(memoryStream)){
                    writer.Write(CurrentVersion);
                    writer.Write(_ordered.Count);

                    for (int i = 0; i < _ordered.Count; i++){
                        var saveable = _ordered[i];
                        writer.Write(saveable.SaveKey);

                        using var blockStream = new MemoryStream(512);
                        using (var blockWriter = new SaveWriter(blockStream)){
                            saveable.Save(blockWriter);
                        }

                        var bytes = blockStream.ToArray();
                        writer.Write(bytes.Length);
                        writer.WriteRawBytes(bytes);

                        float snapshotProgress = ((i + 1f) / _ordered.Count) * 0.4f;
                        _eventBus.Publish(new SaveProgressEvent(snapshotProgress, false));
                    }
                }
                rawBytes = memoryStream.ToArray();
            }

            return rawBytes;
        }

        private async Task<bool> ApplyRawDataAsync(byte[] rawBytes, CancellationToken cancellationToken){
            using var memoryStream = new MemoryStream(rawBytes);
            using var reader = new SaveReader(memoryStream);

            int version = reader.ReadInt();
            if (version != CurrentVersion){
                _log.Error($"[Save] 迁移后的存档版本仍然不匹配，当前: {CurrentVersion}，文件: {version}");
                return false;
            }

            int blockCount = reader.ReadInt();
            if (blockCount <= 0) return true;

            for (int i = 0; i < blockCount; i++){
                cancellationToken.ThrowIfCancellationRequested();

                string key = reader.ReadString();
                int length = reader.ReadInt();
                var bytes = reader.ReadRawBytes(length);

                if (_map.TryGetValue(key, out var saveable)){
                    using var blockStream = new MemoryStream(bytes);
                    using var blockReader = new SaveReader(blockStream);

                    try{
                        saveable.Load(blockReader);
                    }
                    catch (Exception ex){
                        _log.Error($"[Save] 模块 '{key}' 数据恢复失败: {ex.Message}");
                    }
                }

                float currentProgress = 0.4f + ((i + 1f) / blockCount) * 0.6f;
                _eventBus.Publish(new SaveProgressEvent(currentProgress, true));

                await Task.Yield();
            }

            _eventBus.Publish(new SaveProgressEvent(1f, true));
            _log.Info("[Save] 读档完成");
            return true;
        }

        private byte[] ReadAndDecrypt(string filePath){
            if (!File.Exists(filePath)) return null;
            try{
                return _crypto.Decrypt(File.ReadAllBytes(filePath));
            }
            catch{
                return null;
            }
        }

        /// <summary>
        /// 原子写入：先写临时文件，再替换目标，并把上一份存档保留为 .bak。
        /// 替换前检查取消令牌，保证取消时主存档不被破坏。
        /// File.Replace 在部分平台（Android/iOS/网络盘）不可用，此时退化为"备份 + 删除 + 移动"。
        /// </summary>
        private static void WriteFileAtomic(string filePath, byte[] bytes, CancellationToken cancellationToken){
            string tempPath = filePath + TempSuffix;
            string backupPath = filePath + BackupSuffix;

            File.WriteAllBytes(tempPath, bytes);

            // 只在这里检查一次取消：一旦开始替换就不再中断，避免留下被删掉的主存档
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(filePath)){
                File.Move(tempPath, filePath);
                return;
            }

            try{
                File.Replace(tempPath, filePath, backupPath, true);
                return;
            }
            catch (Exception){
                // 继续走跨平台回退路径
            }

            File.Copy(filePath, backupPath, true);
            TryDelete(filePath);
            File.Move(tempPath, filePath);
        }

        private static void TryDelete(string path){
            try{
                if (File.Exists(path)) File.Delete(path);
            }
            catch{
                // 忽略清理错误
            }
        }
    }
}
