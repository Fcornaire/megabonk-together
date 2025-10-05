using MegabonkTogether.Common.Models;
using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class TumbleWeedSpawned : IGameNetworkMessage
    {
        public uint NetplayId { get; set; }
        public QuantizedVector3 Position { get; set; }
        public QuantizedVector3 Velocity { get; set; }
    }
}
