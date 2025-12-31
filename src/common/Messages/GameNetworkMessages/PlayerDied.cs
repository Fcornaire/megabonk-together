using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class PlayerDied : IGameNetworkMessage
    {
        public uint PlayerId { get; set; }
    }
}
