using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class PlayerDisconnected : IGameNetworkMessage
    {
        public uint ConnectionId { get; set; }
    }
}
