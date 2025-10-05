using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class StoppingChargingPylon : IGameNetworkMessage
    {
        public uint PylonNetplayId;
        public uint PlayerChargingId;
    }
}
