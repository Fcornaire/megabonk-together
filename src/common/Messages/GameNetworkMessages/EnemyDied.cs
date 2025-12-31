using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class EnemyDied : IGameNetworkMessage
    {
        public uint EnemyId { get; set; }

        public uint DiedByOwnerId { get; set; }
    }
}
