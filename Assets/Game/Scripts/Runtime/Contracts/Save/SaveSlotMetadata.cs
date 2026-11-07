namespace Game.Runtime{
    /// <summary>
    /// 存档槽位的展示信息：读档界面对着一排槽位，只需要标题/时间/时长/缩略图，
    /// 不应该为此解密整份存档。框架把这份信息写成独立的小文件（元数据），
    /// 缩略图另存，因此 ListSlots 可以做到只读几 KB。
    /// </summary>
    public sealed class SaveSlotMetadata{
        /// <summary>槽位标题（存档点名称、章节名等）。</summary>
        public string Title = string.Empty;

        /// <summary>副标题/摘要（可选）。</summary>
        public string Summary = string.Empty;

        /// <summary>游戏内已游玩时长（秒）。</summary>
        public double PlayTimeSeconds;

        /// <summary>缩略图原始字节（png/jpg 均可）；为 null 表示不写缩略图。</summary>
        public byte[] Thumbnail;
    }
}
