using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class FinalBossOrbDestroyed : IGameNetworkMessage
    {
        public uint OrbId { get; set; }
        public uint SenderId { get; set; }
    }
}
