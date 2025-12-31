using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class PickupFollowingPlayer : IGameNetworkMessage
    {
        public uint PickupId { get; set; }
        public uint PlayerId { get; set; }
    }
}
