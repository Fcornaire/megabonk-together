using MemoryPack;

namespace MegabonkTogether.Common.Models
{
    [MemoryPackable]
    public partial class EnemyModel
    {
        public uint Id { get; set; }

        public float Hp { get; set; }

        public QuantizedVector3 Position { get; set; } = new();

        public QuantizedRotation Yaw { get; set; } = new();
    }

}
