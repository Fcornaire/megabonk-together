using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class EnemyExploder : IGameNetworkMessage
    {
        public uint EnemyId { get; set; }
        public uint SenderId { get; set; }
    }
}
