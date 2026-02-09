using MemoryPack;

namespace MegabonkTogether.Common.Messages.WsMessages
{
    [MemoryPackable]
    [MemoryPackUnion(0, typeof(MatchmakingServerConnectionStatus))]
    [MemoryPackUnion(1, typeof(MatchInfo))]
    [MemoryPackUnion(2, typeof(ServerConnectionStatus))]
    [MemoryPackUnion(3, typeof(ClientReadyMessage))]
    [MemoryPackUnion(4, typeof(RunStatistics))]
    [MemoryPackUnion(5, typeof(GameStarting))]
    [MemoryPackUnion(6, typeof(GameStartingResponse))]
    [MemoryPackUnion(7, typeof(HostDisconnected))]
    [MemoryPackUnion(8, typeof(ClientDisconnected))]
    public partial interface IWsMessage
    {
    }
}
