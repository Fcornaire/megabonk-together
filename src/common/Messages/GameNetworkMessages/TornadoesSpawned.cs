using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class TornadoesSpawned : IGameNetworkMessage
    {
        public int Amount { get; set; }
    }
}
