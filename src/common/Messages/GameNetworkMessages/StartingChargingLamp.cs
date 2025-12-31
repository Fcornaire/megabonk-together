using MemoryPack;

namespace MegabonkTogether.Common.Messages.GameNetworkMessages
{
    [MemoryPackable]
    public partial class StartingChargingLamp : IGameNetworkMessage
    {
        public uint LampNetplayId { get; set; }
        public uint PlayerChargingId { get; set; }
    }
}
