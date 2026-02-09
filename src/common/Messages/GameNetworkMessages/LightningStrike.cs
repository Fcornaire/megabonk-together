using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class LightningStrike : IGameNetworkMessage
    {
        public uint EnemyId { get; set; }
        public int Bounces { get; set; }
        public float Damage { get; set; }
        public int DamageEffect { get; set; }
        public int DamageBlockedByArmor { get; set; }
        public string DamageSource { get; set; } = "";
        public bool DamageIsCrit { get; set; }
        public float DamageProcCoefficient { get; set; }
        public int DamageElement { get; set; }
        public int DamageFlags { get; set; }
        public float DamageKnockback { get; set; }
        public float BounceRange { get; set; }
        public float BounceProcCoefficient { get; set; }
        public uint OwnerId { get; set; }
    }
}
