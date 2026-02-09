using MegabonkTogether.Common.Messages.WsMessages;
using MemoryPack;
using System.Net.WebSockets;

namespace MegabonkTogether.Common.Extensions
{
    public static class WebSocketsExtensions
    {
        public static async Task SendMessageAsync<T>(this WebSocket webSocket, T message) where T : IWsMessage
        {
            var bytes = MemoryPackSerializer.Serialize<IWsMessage>(message);
            var segment = new ReadOnlyMemory<byte>(bytes);
            await webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }
}
