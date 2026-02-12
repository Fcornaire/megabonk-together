using MemoryPack;

namespace MegabonkTogether.Common.Messages.GameNetworkMessages
{
    [MemoryPackable]
    public partial class EncounterClosed : IGameNetworkMessage
    {
        public uint OwnerId { get; set; }
    }
}
