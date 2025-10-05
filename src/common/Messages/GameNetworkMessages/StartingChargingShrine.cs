using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class StartingChargingShrine : IGameNetworkMessage
    {
        public uint ShrineNetplayId;
        public uint PlayerChargingId;
    }
}
