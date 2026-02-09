using MegabonkTogether.Common.Messages;
using MegabonkTogether.Common.Messages.WsMessages;
using MegabonkTogether.Common.Models;
using MegabonkTogether.Configuration;
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
        Task ConnectAndMatchAsync(string serverUrl, uint rdvServerPort, NetworkHandler networkHandler);
        public Task Reset();
        Task SendRunStatistics(int playerCount, string mapName, int stageLevel, List<string> characters);
        Task<bool> SendGameStarting();
    }

    internal class WebsocketClientService(IUdpClientService udpClientService, IPlayerManagerService playerManagerService) : IWebsocketClientService
    {
        private ClientWebSocket ws;
        private CancellationTokenSource cts = null;
        private CancellationToken token;

        private uint connectionId { get; set; } = 0;
        private TaskCompletionSource<GameStartingResponse> gameStartingResponseTcs;
        private string currentServerUrl;
        private uint currentRdvServerPort;
        private bool isMessageLoopRunning = false;


        public async Task ConnectAndMatchAsync(string serverUrl, uint rdvServerPort, NetworkHandler networkHandler)
        {
            if (ws != null && ws.State == WebSocketState.Open)
            {
                Plugin.Log.LogWarning("WebSocket is already connected");
                return;
            }

            ws = new ClientWebSocket();
            cts = new CancellationTokenSource();
            token = cts.Token;

            var hasInitialized = udpClientService.Initialize();
            if (!hasInitialized)
            {
                Plugin.Log.LogError("Failed to initialize UDP client");
                return;
            }

            udpClientService.ResetHandledHost();

            var mode = Plugin.Instance.Mode;

            switch (mode.Mode)
            {
                case NetworkModeType.Random: await ConnectRandomAsync(serverUrl, rdvServerPort, networkHandler); break;
                case NetworkModeType.Friendlies: await ConnectFriendliesAsync(serverUrl, rdvServerPort, networkHandler); break;
                default: throw new InvalidOperationException($"Unknown network mode: {mode.Mode}");
            }
        }

        private async Task ConnectRandomAsync(string serverUrl, uint rdvServerPort, NetworkHandler networkHandler)
        {
            var uri = new System.Uri($"{serverUrl}/ws?random");

            await ws.ConnectAsync(uri, token);
            networkHandler.OnConnectedToMatchMaker();
            Plugin.Log.LogInfo($"Connected to {uri}");

            var msg = await ReceiveMessageAsync();
            if (msg is not MatchmakingServerConnectionStatus statusMsg)
            {
                Plugin.Log.LogError("Unexpected message type received from matchmaking server");
                Plugin.Instance.NetworkHandler.OnNetworkInterrupted("Unexpected message from matchmaking server");
                return;
            }
            if (!statusMsg.HasJoined)
            {
                Plugin.Log.LogError("Failed to join matchmaking server");
                return;
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
                return;
            }

            Plugin.Log.LogInfo($"Match found! {matchInfo.Peers.Count()} players");

            var serverUri = new Uri(serverUrl);
            var hostOnly = serverUri.Host;

            var hasFoundMatch = await udpClientService.HandleMatch(matchInfo, connectionId, hostOnly, rdvServerPort);

            Plugin.Instance.NetworkHandler.OnMatchFound(hasFoundMatch);
        }

        private async Task ConnectFriendliesAsync(string serverUrl, uint rdvServerPort, NetworkHandler networkHandler)
        {
            var role = Plugin.Instance.Mode.Role;
            var code = Plugin.Instance.Mode.RoomCode;
            var uri = new System.Uri($"{serverUrl}/ws?friendlies&role={role}&code={code}&name={ModConfig.PlayerName.Value}");

            await ws.ConnectAsync(uri, token);
            networkHandler.OnConnectedToMatchMaker();

            var msg = await ReceiveMessageAsync();
            if (msg is not MatchmakingServerConnectionStatus statusMsg)
            {
                Plugin.Log.LogError($"Unexpected message type received from matchmaking server");
                Plugin.Instance.NetworkHandler.OnNetworkInterrupted("Unexpected message from matchmaking server");
                return;
            }

            if (!statusMsg.HasJoined)
            {
                Plugin.Log.LogError($"Failed to join matchmaking server : {statusMsg.Message}");
                Plugin.Instance.NetworkHandler.OnNetworkInterrupted(statusMsg.Message);
                return;
            }

            connectionId = statusMsg.ConnectionId;
            Plugin.Instance.Mode.RoomCode = statusMsg.RoomCode;

            currentServerUrl = serverUrl;
            currentRdvServerPort = rdvServerPort;

            Plugin.Log.LogInfo($"Connection ID: {connectionId}");

            StartMessageLoop();

            if (role == Role.Host)
            {
                Plugin.Instance.NetworkHandler.OnMatchFound(true);
            }
        }

        private void StartMessageLoop()
        {
            if (isMessageLoopRunning)
            {
                Plugin.Log.LogWarning("Message loop is already running");
                return;
            }

            isMessageLoopRunning = true;
            _ = MessageLoopAsync();
        }

        private async Task MessageLoopAsync()
        {
            try
            {
                while (!token.IsCancellationRequested && ws.State == WebSocketState.Open)
                {
                    var message = await ReceiveMessageAsync();

                    Plugin.Log.LogInfo($"Received message in loop: {message?.GetType().Name}");

                    switch (message)
                    {
                        case MatchInfo matchInfo:
                            _ = HandleMatchInfo(matchInfo);
                            break;
                        case RunStatistics runStats:
                            break;
                        case GameStartingResponse ack:
                            HandleGameStartingResponse(ack);
                            break;
                        case HostDisconnected hostDisconnected:
                            HandleHostDisconnected(hostDisconnected);
                            break;
                        case ClientDisconnected clientDisconnected:
                            HandleClientDisconnected(clientDisconnected);
                            break;
                        default:
                            Plugin.Log.LogWarning($"Unhandled message type in loop: {message?.GetType().Name}");
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Plugin.Log.LogInfo($"Message loop cancelled");
            }
            catch (WebSocketException ex)
            {
                Plugin.Log.LogError($"WebSocket error in message loop: {ex.Message}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error in message loop: {ex.Message}");
            }
            finally
            {
                isMessageLoopRunning = false;
            }
        }

        private async Task HandleMatchInfo(MatchInfo matchInfo)
        {
            if (udpClientService.HasHandledHost())
            {
                Plugin.Log.LogInfo("Adding missing players...");

                var allPlayers = playerManagerService.GetAllPlayers();
                foreach (var peer in matchInfo.Peers)
                {
                    if (!allPlayers.Any(p => p.ConnectionId == peer.ConnectionId))
                    {
                        playerManagerService.AddPlayer(peer.ConnectionId, peer.IsHost.Value, peer.ConnectionId == connectionId);
                    }
                }

                return;
            }

            Plugin.Log.LogInfo($"Friendlies match found! {matchInfo.Peers.Count()} players");

            var serverUri = new Uri(currentServerUrl);
            var hostOnly = serverUri.Host;

            var hasFoundMatch = await udpClientService.HandleMatch(matchInfo, connectionId, hostOnly, currentRdvServerPort);

            Plugin.Log.LogInfo($"HandleMatchInfo result : {hasFoundMatch}");
            Plugin.Instance.NetworkHandler.OnMatchFound(hasFoundMatch);
        }

        private void HandleGameStartingResponse(GameStartingResponse ack)
        {
            Plugin.Log.LogInfo($"Received GameStartingResponse: {ack.IsSuccess}");

            if (gameStartingResponseTcs != null && !gameStartingResponseTcs.Task.IsCompleted)
            {
                gameStartingResponseTcs.SetResult(ack);
            }
        }

        private void HandleHostDisconnected(HostDisconnected hostDisconnected)
        {
            Plugin.Log.LogWarning($"Host {hostDisconnected.HostConnectionId} disconnected from room");

            if (GameManager.Instance?.player != null)
            {
                return;
            }

            udpClientService.CancelAnyNatIntroduction();
            Plugin.Instance.NetworkHandler.OnNetworkInterrupted("Host has disconnected");

            _ = Task.Run(async () => //TODO: the task might be usesless now (Was preventing a race condition before)
            {
                await Task.Delay(100);
                Plugin.Instance.NetworkHandler.ResetNetworking();
                Plugin.GoToMainMenu();
            });
        }

        private void HandleClientDisconnected(ClientDisconnected clientDisconnected)
        {
            Plugin.Log.LogWarning($"Client {clientDisconnected.ClientConnectionId} disconnected from room");
            playerManagerService.RemovePlayer(clientDisconnected.ClientConnectionId);
            udpClientService.RemovePeer(clientDisconnected.ClientConnectionId);
        }

        public async Task Reset()
        {
            try
            {
                isMessageLoopRunning = false;

                cts?.Cancel();

                if (ws != null && ws.State == WebSocketState.Open)
                {
                    try
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Starting P2P", CancellationToken.None);
                    }
                    catch (WebSocketException wsEx)
                    {
                        Plugin.Log.LogWarning($"WebSocket already closed by remote: {wsEx.Message}");
                    }

                    ws.Dispose();
                    ws = null;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error while closing WebSocket: {ex.Message}");
            }
            finally
            {
                cts?.Dispose();
                cts = null;

                connectionId = 0;
                currentServerUrl = null;
                currentRdvServerPort = 0;
            }
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

        public async Task<bool> SendGameStarting()
        {
            if (ws == null || ws.State != WebSocketState.Open)
            {
                Plugin.Log.LogWarning("WebSocket is not connected, cannot send game starting message");
                return false;
            }

            gameStartingResponseTcs = new TaskCompletionSource<GameStartingResponse>();

            var msg = new GameStarting { ConnectionId = connectionId };

            await SendMessageAsync(msg);

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15));
            var completedTask = await Task.WhenAny(gameStartingResponseTcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                Plugin.Log.LogError("Timeout waiting for GameStartingResponse");
                return false;
            }

            var response = await gameStartingResponseTcs.Task;
            Plugin.Log.LogInfo($"Game starting acknowledged: {response.IsSuccess}");
            return response.IsSuccess;
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
