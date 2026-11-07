using System;

namespace Game.Runtime{
    /// <summary>一次槽位扫描的结果（不需要解密存档即可读取）。</summary>
    public sealed class SaveSlotInfo{
        public int Slot { get; set; }

        public string FilePath { get; set; }

        /// <summary>槽位存档文件是否存在。</summary>
        public bool Exists { get; set; }

        public long SizeBytes { get; set; }

        public DateTime LastWriteTimeUtc { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public double PlayTimeSeconds { get; set; }

        /// <summary>存档写入时间（来自元数据）；无元数据时为 null。</summary>
        public DateTime? SavedAtUtc { get; set; }

        /// <summary>是否有缩略图文件。</summary>
        public bool HasThumbnail { get; set; }
    }
}
