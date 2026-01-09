using MemoryPack;

namespace MegabonkTogether.Common.Messages.WsMessages
{
    [MemoryPackable]
    public partial class ClientDisconnected : IWsMessage
    {
        public string RoomCode { get; set; }
        public uint ClientConnectionId { get; set; }
    }
}
