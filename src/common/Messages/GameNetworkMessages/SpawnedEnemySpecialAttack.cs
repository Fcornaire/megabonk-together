using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class SpawnedEnemySpecialAttack : IGameNetworkMessage
    {
        public uint EnemyId { get; set; }
        public string AttackName { get; set; }
        public uint TargetId { get; set; }
    }
}
