using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class StoppingChargingShrine : IGameNetworkMessage
    {
        public uint ShrineNetplayId;
        public uint PlayerChargingId;
    }
}