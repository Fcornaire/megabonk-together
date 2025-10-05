using MemoryPack;

namespace MegabonkTogether.Common.Messages.GameNetworkMessages
{
    [MemoryPackable]
    public partial class WantToStartFollowingPickup : IGameNetworkMessage
    {
        public uint PickupId { get; set; }
        public uint OwnerId { get; set; }
    }
}
