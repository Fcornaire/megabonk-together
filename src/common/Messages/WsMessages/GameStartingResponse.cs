using MemoryPack;

namespace MegabonkTogether.Common.Messages.WsMessages
{
    [MemoryPackable]
    public partial class GameStartingResponse : IWsMessage
    {
        public bool IsSuccess { get; set; }
    }
}
