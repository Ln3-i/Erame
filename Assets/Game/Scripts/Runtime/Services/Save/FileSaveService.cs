using Game.Runtime;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Erame.Core;
using UnityEngine;

namespace Game.Runtime.Services{
    public sealed class FileSaveService : ISaveService, ISaveSlotService, IService{
        private const int CurrentVersion = 1;
        private const string TempSuffix = ".tmp";
        private const string BackupSuffix = ".bak";
        private const string MetaSuffix = ".meta";
        private const string ThumbSuffix = ".thumb";

        private readonly Dictionary<string, ISaveable> _map = new();
        private readonly List<ISaveable> _ordered = new();

        private readonly ILogService _log;
        private readonly IEventBus _eventBus;
        private readonly ISaveCrypto _crypto;
        private readonly string _slotDirectory;

        private bool _isSaving = false;

        /// <summary>
        /// 旧版本存档迁移钩子：参数为读到的版本号与解密后的原始字节，
        /// 返回迁移后的原始字节（其头部版本号必须已是 CurrentVersion）；返回 null 表示无法迁移。
        /// </summary>
        public Func<int, byte[], byte[]> RawDataMigrator { get; set; }

        /// <summary>
        /// <paramref name="slotDirectory"/> 为空时使用 Application.persistentDataPath/Saves（测试可传临时目录）。
        /// </summary>
        public FileSaveService(ILogService log, IEventBus eventBus, ISaveCrypto crypto, string slotDirectory = null){
            _log = log;
            _eventBus = eventBus;
            _crypto = crypto ?? throw new ArgumentNullException(nameof(crypto));
            _slotDirectory = slotDirectory;
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

        // ───────────────────────────── 槽位 ─────────────────────────────

        /// <summary>槽位目录：未显式指定时落在 persistentDataPath/Saves。</summary>
        public string SlotDirectory => _slotDirectory ?? Path.Combine(Application.persistentDataPath, "Saves");

        public string GetSlotPath(int slot){
            if (slot < 0) throw new ArgumentOutOfRangeException(nameof(slot), "槽位号不能为负");
            return Path.Combine(SlotDirectory, $"slot_{slot}.sav");
        }

        public async Task<bool> SaveToSlotAsync(int slot, SaveSlotMetadata metadata = null, CancellationToken cancellationToken = default){
            string path = GetSlotPath(slot);

            bool success = await SaveToFileAsync(path, cancellationToken);
            if (!success) return false;

            // 元数据放在存档成功之后写：宁可少一条展示信息，也不要出现“有封面但没存档”
            WriteSlotMetadata(slot, metadata);
            _log.Info($"[Save] 已保存到槽位 {slot}: {path}");
            return true;
        }

        public Task<bool> LoadFromSlotAsync(int slot, CancellationToken cancellationToken = default){
            return LoadFromFileAsync(GetSlotPath(slot), cancellationToken);
        }

        public IReadOnlyList<SaveSlotInfo> ListSlots(int slotCount){
            if (slotCount <= 0) return Array.Empty<SaveSlotInfo>();

            var result = new List<SaveSlotInfo>(slotCount);
            for (int slot = 0; slot < slotCount; slot++){
                string path = GetSlotPath(slot);
                var info = new SaveSlotInfo{
                    Slot = slot,
                    FilePath = path,
                    Exists = File.Exists(path),
                };

                if (info.Exists){
                    try{
                        var file = new FileInfo(path);
                        info.SizeBytes = file.Length;
                        info.LastWriteTimeUtc = file.LastWriteTimeUtc;
                    }
                    catch (Exception ex){
                        _log.Warn($"[Save] 读取槽位 {slot} 文件信息失败: {ex.Message}");
                    }
                }

                ReadSlotMetadata(slot, info);
                info.HasThumbnail = File.Exists(ThumbPath(slot));
                result.Add(info);
            }
            return result;
        }

        public bool DeleteSlot(int slot){
            string path = GetSlotPath(slot);
            bool existed = File.Exists(path);

            TryDelete(path);
            TryDelete(path + TempSuffix);
            TryDelete(path + BackupSuffix);
            TryDelete(MetaPath(slot));
            TryDelete(ThumbPath(slot));

            if (existed) _log.Info($"[Save] 已删除槽位 {slot}");
            return existed;
        }

        public byte[] ReadThumbnail(int slot){
            string path = ThumbPath(slot);
            if (!File.Exists(path)) return null;

            try{ return File.ReadAllBytes(path); }
            catch (Exception ex){
                _log.Warn($"[Save] 读取槽位 {slot} 缩略图失败: {ex.Message}");
                return null;
            }
        }

        private string MetaPath(int slot) => GetSlotPath(slot) + MetaSuffix;

        private string ThumbPath(int slot) => GetSlotPath(slot) + ThumbSuffix;

        /// <summary>
        /// 元数据是明文小文件（key=value，值做 URL 转义），只用于读档界面展示；
        /// 这样做的好处是列槽位不需要解密 GB 级存档，也不会因为加密失败而丢掉整个存档。
        /// </summary>
        private void WriteSlotMetadata(int slot, SaveSlotMetadata metadata){
            try{
                Directory.CreateDirectory(SlotDirectory);

                var lines = new List<string>{
                    "version=" + CurrentVersion.ToString(CultureInfo.InvariantCulture),
                    "savedAt=" + Uri.EscapeDataString(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)),
                };

                if (metadata != null){
                    lines.Add("title=" + Uri.EscapeDataString(metadata.Title ?? string.Empty));
                    lines.Add("summary=" + Uri.EscapeDataString(metadata.Summary ?? string.Empty));
                    lines.Add("playtime=" + metadata.PlayTimeSeconds.ToString("R", CultureInfo.InvariantCulture));

                    string thumbPath = ThumbPath(slot);
                    if (metadata.Thumbnail != null && metadata.Thumbnail.Length > 0){
                        File.WriteAllBytes(thumbPath, metadata.Thumbnail);
                    }
                    else{
                        TryDelete(thumbPath);
                    }
                }

                File.WriteAllLines(MetaPath(slot), lines);
            }
            catch (Exception ex){
                // 元数据失败不影响存档本身，只是读档界面少显示一栏
                _log.Warn($"[Save] 写入槽位 {slot} 元数据失败: {ex.Message}");
            }
        }

        private void ReadSlotMetadata(int slot, SaveSlotInfo info){
            string metaPath = MetaPath(slot);
            if (!File.Exists(metaPath)) return;

            try{
                foreach (var line in File.ReadAllLines(metaPath)){
                    int separator = line.IndexOf('=');
                    if (separator <= 0) continue;

                    string key = line.Substring(0, separator);
                    string value = line.Substring(separator + 1);

                    switch (key){
                        case "title":
                            info.Title = Uri.UnescapeDataString(value);
                            break;
                        case "summary":
                            info.Summary = Uri.UnescapeDataString(value);
                            break;
                        case "playtime":
                            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var playTime)){
                                info.PlayTimeSeconds = playTime;
                            }
                            break;
                        case "savedAt":
                            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var savedAt)){
                                info.SavedAtUtc = savedAt.ToUniversalTime();
                            }
                            break;
                    }
                }
            }
            catch (Exception ex){
                _log.Warn($"[Save] 读取槽位 {slot} 元数据失败: {ex.Message}");
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

            // 槽位目录可能还不存在（首次存档），先确保父目录可用
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

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
