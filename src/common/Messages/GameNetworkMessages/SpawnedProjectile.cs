using MegabonkTogether.Common.Models;
using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    [MemoryPackUnion(0, typeof(SpawnedProjectile))]
    [MemoryPackUnion(1, typeof(SpawnedAxeProjectile))]
    [MemoryPackUnion(2, typeof(SpawnedBlackHoleProjectile))]
    [MemoryPackUnion(3, typeof(SpawnedCringeSwordProjectile))]
    [MemoryPackUnion(4, typeof(SpawnedFireFieldProjectile))]
    [MemoryPackUnion(5, typeof(SpawnedHeroSwordProjectile))]
    [MemoryPackUnion(6, typeof(SpawnedRocketProjectile))]
    [MemoryPackUnion(7, typeof(SpawnedShotgunProjectile))]
    [MemoryPackUnion(8, typeof(SpawnedDexecutionerProjectile))]
    [MemoryPackUnion(9, typeof(SpawnedRevolverProjectile))]
    [MemoryPackUnion(10, typeof(SpawnedSniperProjectile))]
    public abstract partial class AbstractSpawnedProjectile : IGameNetworkMessage
    {
        public QuantizedVector3 Position { get; set; } = new();
        public uint Id { get; set; }

        public uint OwnerId { get; set; }
        public int Weapon { get; set; }
        public QuantizedVector3 Rotation { get; set; } = new();
    }

    [MemoryPackable]
    public partial class SpawnedProjectile : AbstractSpawnedProjectile
    {

    }

    [MemoryPackable]
    public partial class SpawnedShotgunProjectile : AbstractSpawnedProjectile
    {
        public QuantizedVector3 MuzzlePosition { get; set; } = new();
        public QuantizedVector3 MuzzleRotation { get; set; } = new();
    }

    [MemoryPackable]
    public partial class SpawnedAxeProjectile : AbstractSpawnedProjectile
    {
        public QuantizedVector3 StartPosition { get; set; } = new();
        public QuantizedVector3 DesiredPosition { get; set; } = new();
    }

    [MemoryPackable]
    public partial class SpawnedBlackHoleProjectile : AbstractSpawnedProjectile
    {
        public QuantizedVector3 StartPosition { get; set; } = new();
        public QuantizedVector3 DesiredPosition { get; set; } = new();
        public QuantizedVector3 StartScaleSize { get; set; } = new();
    }

    [MemoryPackable]
    public partial class SpawnedCringeSwordProjectile : AbstractSpawnedProjectile
    {
        public QuantizedVector3 MovingProjectilePosition { get; set; } = new();
        public QuantizedVector4 MovingProjectileRotation { get; set; } = new();
    }

    [MemoryPackable]
    public partial class SpawnedFireFieldProjectile : AbstractSpawnedProjectile
    {
        public float ExpirationTime { get; set; }
    }

    [MemoryPackable]
    public partial class SpawnedHeroSwordProjectile : AbstractSpawnedProjectile
    {
        public QuantizedVector3 MovingProjectilePosition { get; set; } = new();
        public QuantizedVector4 MovingProjectileRotation { get; set; } = new();
    }

    [MemoryPackable]
    public partial class SpawnedRocketProjectile : AbstractSpawnedProjectile
    {
        public QuantizedVector3 RocketPosition { get; set; }
        public QuantizedVector3 RocketRotation { get; set; }
    }

    [MemoryPackable]
    public partial class SpawnedDexecutionerProjectile : AbstractSpawnedProjectile
    {
        public float ProjectileDistance { get; set; }
        public float ForwardOffset { get; set; }
        public float UpOffset { get; set; }

        public QuantizedVector3 AttackDir { get; set; }
        public float Chance { get; set; }

        public bool UseAudio { get; set; }
    }

    [MemoryPackable]
    public partial class SpawnedRevolverProjectile : AbstractSpawnedProjectile
    {
        public QuantizedVector3 MuzzlePosition { get; set; } = new();
        public QuantizedVector3 MuzzleRotation { get; set; } = new();
    }

    [MemoryPackable]
    public partial class SpawnedSniperProjectile : AbstractSpawnedProjectile
    {
        public QuantizedVector3 MuzzlePosition { get; set; } = new();
        public QuantizedVector3 MuzzleRotation { get; set; } = new();
    }
}
