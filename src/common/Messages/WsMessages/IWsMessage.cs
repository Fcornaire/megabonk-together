using MemoryPack;

namespace MegabonkTogether.Common.Messages.WsMessages
{
    [MemoryPackable]
    [MemoryPackUnion(0, typeof(MatchmakingServerConnectionStatus))]
    [MemoryPackUnion(1, typeof(MatchInfo))]
    [MemoryPackUnion(2, typeof(ServerConnectionStatus))]
    [MemoryPackUnion(3, typeof(ClientReadyMessage))]
    [MemoryPackUnion(4, typeof(RunStatistics))]
    public partial interface IWsMessage
    {
    }
}
