using MegabonkTogether.Common.Messages;
using MegabonkTogether.Common.Messages.WsMessages;
using MegabonkTogether.Scripts;
using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace MegabonkTogether.Services
{
    public interface IWebsocketClientService
    {
        Task<bool> ConnectAndMatchAsync(string serverUrl, uint rdvServerPort, NetworkHandler networkHandler);
        public Task Reset();
        Task SendRunStatistics(int playerCount, string mapName, int stageLevel, List<string> characters);
    }

    internal class WebsocketClientService : IWebsocketClientService
    {
        private ClientWebSocket ws;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private CancellationToken token;
        private readonly IUdpClientService udpClientService;

        private uint connectionId { get; set; } = 0;

        public WebsocketClientService(IUdpClientService udpClientService)
        {
            token = cts.Token;
            this.udpClientService = udpClientService;
        }

        public async Task<bool> ConnectAndMatchAsync(string serverUrl, uint rdvServerPort, NetworkHandler networkHandler)
        {
            if (ws != null && ws.State == WebSocketState.Open)
            {
                Plugin.Log.LogWarning("WebSocket is already connected");
                return false;
            }

            ws = new ClientWebSocket();
            cts = new CancellationTokenSource();
            token = cts.Token;

            var hasInitialized = udpClientService.Initialize();
            if (!hasInitialized)
            {
                Plugin.Log.LogError("Failed to initialize UDP client");
                return false;
            }

            var uri = new System.Uri($"{serverUrl}/ws?random");

            await ws.ConnectAsync(uri, token);
            networkHandler.OnConnectedToMatchMaker();
            Plugin.Log.LogInfo($"Connected to {uri}");

            var msg = await ReceiveMessageAsync();
            if (msg is not MatchmakingServerConnectionStatus statusMsg)
            {
                Plugin.Log.LogError("Unexpected message type received from matchmaking server");
                Plugin.Instance.NetworkHandler.OnNetworkInterrupted("Unexpected message from matchmaking server");
                return false;
            }
            if (!statusMsg.HasJoined)
            {
                Plugin.Log.LogError("Failed to join matchmaking server");
                return false;
            }

            connectionId = statusMsg.ConnectionId;
            Plugin.Log.LogInfo($"connection ID: {connectionId}");

            await SendMessageAsync(new ClientReadyMessage { });

            Plugin.Log.LogInfo("Waiting for match...");
            var matchInfoMsg = await ReceiveMessageAsync();
            if (matchInfoMsg is not MatchInfo matchInfo)
            {
                Plugin.Log.LogError("Unexpected message type received from matchmaking server");
                Plugin.Instance.NetworkHandler.OnNetworkInterrupted("Unexpected message from matchmaking server");
                return false;
            }

            Plugin.Log.LogInfo($"Match found! {matchInfo.Peers.Count()} players");

            var serverUri = new Uri(serverUrl);
            var hostOnly = serverUri.Host;

            return await udpClientService.HandleMatch(matchInfo, connectionId, hostOnly, rdvServerPort);
        }

        public async Task Reset()
        {
            try
            {
                if (ws != null && ws.State == WebSocketState.Open)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Starting P2P", CancellationToken.None);
                    ws.Dispose();
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error while closing WebSocket: {ex.Message}");
            }

            if (!cts.IsCancellationRequested) cts?.Cancel();
            cts?.Dispose();
        }

        public async Task SendRunStatistics(int playerCount, string mapName, int stageLevel, List<string> characters)
        {
            if (ws == null || ws.State != WebSocketState.Open)
            {
                Plugin.Log.LogWarning("WebSocket is not connected, cannot send run statistics");
                return;
            }

            var msg = new RunStatistics
            {
                PlayerCount = playerCount,
                MapName = mapName,
                StageLevel = stageLevel,
                Characters = characters
            };

            await SendMessageAsync(msg);
        }

        private async Task SendMessageAsync(IWsMessage msg)
        {
            var bytes = MemoryPackSerializer.Serialize(msg);
            var segment = new ReadOnlyMemory<byte>(bytes);
            await ws.SendAsync(segment, WebSocketMessageType.Binary, true, cts.Token);
        }

        private async Task<IWsMessage> ReceiveMessageAsync()
        {
            var buffer = new byte[4096];
            var result = await ws.ReceiveAsync(buffer, cts.Token);
            return MemoryPackSerializer.Deserialize<IWsMessage>(buffer.AsSpan(0, result.Count));
        }
    }
}
