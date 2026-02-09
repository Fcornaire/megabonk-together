using MemoryPack;

namespace MegabonkTogether.Common.Models
{
    [MemoryPackable]
    public partial class QuantizedRotation
    {
        public ushort QuantizedYaw { get; set; }
    }
}
