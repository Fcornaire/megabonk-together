using MemoryPack;
using System.Numerics;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class SpawnedEnemy : IGameNetworkMessage
    {
        public int Name { get; set; }
        public uint Id { get; set; }
        public bool ShouldForce { get; set; }
        public int Flag { get; set; }

        public Vector3 Position { get; set; }
        public int Wave { get; set; }
        public bool CanBeElite { get; set; }
        public uint TargetId { get; set; }
        public float Hp { get; set; }
        public float ExtraSizeMultiplier { get; set; }
        public uint? ReviverId { get; set; }
    }
}
