using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Runtime{
    /// <summary>
    /// 存档槽位能力（与 <see cref="ISaveService"/> 分开，避免给只做“存/读一个文件”的实现增加负担）。
    /// 槽位路径、元数据与缩略图由实现决定，业务只需要槽位号。
    /// </summary>
    public interface ISaveSlotService{
        /// <summary>槽位存档的完整路径（不保证文件存在）。</summary>
        string GetSlotPath(int slot);

        /// <summary>扫描前 slotCount 个槽位（0 起）。只读元数据，不解密存档。</summary>
        IReadOnlyList<SaveSlotInfo> ListSlots(int slotCount);

        /// <summary>保存到槽位；成功后写入元数据与缩略图。metadata 可为 null。</summary>
        Task<bool> SaveToSlotAsync(int slot, SaveSlotMetadata metadata = null, CancellationToken cancellationToken = default);

        /// <summary>从槽位读档。</summary>
        Task<bool> LoadFromSlotAsync(int slot, CancellationToken cancellationToken = default);

        /// <summary>删除槽位（存档、备份、元数据、缩略图一并删除）。</summary>
        bool DeleteSlot(int slot);

        /// <summary>读取槽位缩略图字节；没有则返回 null。</summary>
        byte[] ReadThumbnail(int slot);
    }
}
