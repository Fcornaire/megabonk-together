using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class StartedSwarmEvent : IGameNetworkMessage
    {
        public float Duration;
    }
}
