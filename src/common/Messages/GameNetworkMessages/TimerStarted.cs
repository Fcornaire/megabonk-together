using MemoryPack;

namespace MegabonkTogether.Common.Messages.GameNetworkMessages
{
    [MemoryPackable]
    public partial class TimerStarted : IGameNetworkMessage
    {
        public bool IsDungeonTimer { get; set; }
        public uint SenderId { get; set; }
    }
}
