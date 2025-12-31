using MemoryPack;

namespace MegabonkTogether.Common.Models
{
    [MemoryPackable]
    public partial class QuantizedVector2
    {
        public short QuantizedX { get; set; }
        public short QuantizedY { get; set; }
    }
}
