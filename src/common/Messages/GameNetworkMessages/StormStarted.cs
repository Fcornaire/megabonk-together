using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class StormStarted : IGameNetworkMessage
    {
        public float StormOverAtTime { get; set; }
    }
}
