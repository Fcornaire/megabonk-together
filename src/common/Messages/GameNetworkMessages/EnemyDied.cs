using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class EnemyDied : IGameNetworkMessage
    {
        public uint EnemyId { get; set; }

        public uint DiedByOwnerId { get; set; }
        public float DamageProcCoefficient { get; set; }
        public string DamageSource { get; set; }
    }
}
