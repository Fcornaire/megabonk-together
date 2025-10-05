using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class Introduced : IGameNetworkMessage
    {
        public uint ConnectionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsHost { get; set; }
    }
}
