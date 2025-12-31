using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class ChestOpened : IGameNetworkMessage
    {
        public uint ChestId { get; set; }
        public uint OwnerId { get; set; }
    }
}
