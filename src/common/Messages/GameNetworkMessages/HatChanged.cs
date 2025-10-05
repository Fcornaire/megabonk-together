using MemoryPack;

namespace MegabonkTogether.Common.Messages.GameNetworkMessages
{
    [MemoryPackable]
    public partial class HatChanged : IGameNetworkMessage
    {
        public uint OwnerId { get; set; }
        public int EHat { get; set; }
    }
}
