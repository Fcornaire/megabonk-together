using MegabonkTogether.Common.Messages.WsMessages;
using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class MatchmakingServerConnectionStatus : IWsMessage
    {
        public bool HasJoined { get; set; }
        public uint ConnectionId { get; set; }
        public string RoomCode { get; set; }
        public string Message { get; set; }
    }
}
