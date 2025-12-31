using MemoryPack;

namespace MegabonkTogether.Common.Messages.GameNetworkMessages
{
    [MemoryPackable]
    public partial class ItemAdded : IGameNetworkMessage
    {
        public int EItem { get; set; }
        public uint OwnerId { get; set; }
    }
}
