using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class ProjectileDone : IGameNetworkMessage
    {
        public uint ProjectileId { get; set; }
        public uint SenderConnectionId { get; set; }
    }
}
