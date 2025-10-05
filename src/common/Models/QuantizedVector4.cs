using MemoryPack;

namespace MegabonkTogether.Common.Models
{
    [MemoryPackable]
    public partial class QuantizedVector4
    {
        public short QuantizedX { get; set; }
        public short QuantizedY { get; set; }
        public short QuantizedZ { get; set; }
        public short QuantizedW { get; set; }
    }
}
