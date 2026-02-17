using MemoryPack;

namespace MegabonkTogether.Common.Messages.GameNetworkMessages
{
    [MemoryPackable]
    public partial class AddXp : IGameNetworkMessage
    {
        public int Amount { get; set; }
        public uint OwnerId { get; set; }
        public float LeftOverXp { get; set; }
        public int Xp { get; set; }
    }
}
