using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class StartingChargingPylon : IGameNetworkMessage
    {
        public uint PylonNetplayId;
        public uint PlayerChargingId;
    }
}
