using MegabonkTogether.Common.Messages.WsMessages;
using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class ServerConnectionStatus : IWsMessage
    {
        public bool HasJoined { get; set; }
        public uint ConnectionId { get; set; }
    }
}
