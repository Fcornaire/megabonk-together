using MemoryPack;

namespace MegabonkTogether.Common.Models
{
    [MemoryPackable]
    public partial class TumbleWeedModel
    {
        public uint NetplayId { get; set; }
        public QuantizedVector3 Position { get; set; }
    }
}
