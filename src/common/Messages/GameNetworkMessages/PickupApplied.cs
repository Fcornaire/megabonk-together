using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class PickupApplied : IGameNetworkMessage
    {
        public uint PickupId { get; set; }
        public uint OwnerId { get; set; }
    }
}
