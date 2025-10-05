using MegabonkTogether.Common.Extensions;
using MegabonkTogether.Common.Messages;
using MegabonkTogether.Common.Messages.WsMessages;
using MegabonkTogether.Server.Models;
using MemoryPack;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace MegabonkTogether.Server.Services
{
    public class WebSocketHandler
    {
        private const int MinPlayers = 2;
        private const int MaxPlayers = 6;
        private const int MinWaitTimeSeconds = 10;
        private const int AdditionalTimePerPlayerSeconds = 3;
        private const int MaxWaitTimeSeconds = 15;
        private const int MatchmakingIntervalMs = 1000;

        private readonly ConcurrentDictionary<uint, ClientInfo> clients = new();
        private readonly ConcurrentDictionary<uint, ClientInfo> randomQueue = new();
        private readonly ILogger<WebSocketHandler> logger;
        private readonly IMetricsService metricsService;
        private readonly IRendezVousServer rendezVousServer;
        private readonly object queueLock = new();

        private DateTime? queueStartTime;
        private DateTime? queueDeadline;

        public WebSocketHandler(ILogger<WebSocketHandler> logger, IMetricsService metricsService, IRendezVousServer rendezVousServer)
        {
            this.logger = logger;
            this.metricsService = metricsService;
            this.rendezVousServer = rendezVousServer;
            StartMatchmakingLoop();
        }

        private void StartMatchmakingLoop()
        {
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(MatchmakingIntervalMs);

                        lock (queueLock)
                        {
                            EnsureQueueIsStillConnected();

                            if (queueDeadline.HasValue && DateTime.UtcNow >= queueDeadline.Value)
                            {
                                if (randomQueue.Count >= MinPlayers)
                                {
                                    logger.LogInformation("Queue timer expired, creating match.");
                                    CreateMatch();
                                }
                                else if (randomQueue.Count > 0)
                                {
                                    logger.LogInformation($"Queue timer expired with {randomQueue.Count} player(s), restarting timer...");
                                    queueStartTime = DateTime.UtcNow;
                                    queueDeadline = queueStartTime.Value.AddSeconds(MinWaitTimeSeconds);
                                }
                                else
                                {
                                    logger.LogInformation("Queue timer expired with no players, stopping timer.");
                                    queueStartTime = null;
                                    queueDeadline = null;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error in matchmaking loop");
                    }
                }
            });
        }

        private void EnsureQueueIsStillConnected()
        {
            var disconnectedClients = randomQueue.Values.Where(c => c.Socket.State != WebSocketState.Open).ToList();
            foreach (var client in disconnectedClients)
            {
                randomQueue.TryRemove(client.Id, out _);
                logger.LogInformation($"Client {client.Id} removed from queue due to disconnection, queue size: {randomQueue.Count}");
            }
        }

        public async Task HandleClientAsync(WebSocket ws, string? remoteAddress, int remoteWSPort,
            string queueId, CancellationToken token)
        {
            var id = ConnectionIdPool.NewId();

            var client = new ClientInfo
            {
                Socket = ws,
                QueueId = queueId,
                Id = id
            };

            clients[id] = client;
            metricsService.ClientConnected(remoteAddress);

            IWsMessage connectedMsg = new MatchmakingServerConnectionStatus
            {
                ConnectionId = id,
                HasJoined = true
            };
            await ws.SendMessageAsync(connectedMsg);

            logger.LogInformation($"Client connected: {id}");

            var buffer = new byte[4096];

            try
            {
                while (ws.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(buffer, token);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    var msg = MemoryPackSerializer.Deserialize<IWsMessage>(buffer.AsSpan(0, result.Count));
                    logger.LogInformation($"Received message from {id}: {msg}");

                    switch (msg)
                    {
                        case ClientReadyMessage readyMsg:
                            logger.LogInformation($"Client {id} ready");

                            if (!string.IsNullOrEmpty(queueId) && queueId == "random")
                            {
                                AddToQueue(client);
                            }
                            break;
                        case RunStatistics runStats:
                            metricsService.RunStarted(runStats.PlayerCount, runStats.MapName, runStats.StageLevel, runStats.Characters);
                            break;
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                logger.LogWarning($"Client operation cancelled: {e.Message}");
            }
            catch (WebSocketException e)
            {
                logger.LogWarning($"Client operation cancelled: {e.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Client error for {id}");
            }
            finally
            {
                logger.LogInformation($"Client disconnected: {id}");
                clients.TryRemove(id, out _);
                rendezVousServer.CleanRelaySession(id);
                metricsService.ClientDisconnected();
                RemoveFromQueue(client);
                ConnectionIdPool.Release(id);

                if (ws.State == WebSocketState.Open)
                {
                    try
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
                    }
                    catch (WebSocketException)
                    {
                        // Socket already closed or in invalid state or something i dunno
                    }
                }

                ws.Dispose();
            }
        }

        private void AddToQueue(ClientInfo client)
        {
            lock (queueLock)
            {
                if (randomQueue.ContainsKey(client.Id))
                {
                    logger.LogWarning($"Client {client.Id} is already in queue");
                    return;
                }

                randomQueue[client.Id] = client;
                var queueCount = randomQueue.Count;

                logger.LogInformation($"Client {client.Id} added to queue, queue size: {queueCount}");

                if (queueStartTime == null)
                {
                    queueStartTime = DateTime.UtcNow;
                    queueDeadline = queueStartTime.Value.AddSeconds(MinWaitTimeSeconds);
                    logger.LogInformation($"Queue timer started, deadline: {queueDeadline}");
                }
                else if (queueCount > MinPlayers && queueDeadline.HasValue)
                {
                    var newDeadline = queueDeadline.Value.AddSeconds(AdditionalTimePerPlayerSeconds);
                    var maxDeadline = queueStartTime.Value.AddSeconds(MaxWaitTimeSeconds);

                    queueDeadline = newDeadline > maxDeadline ? maxDeadline : newDeadline;
                    logger.LogInformation($"Queue deadline extended to: {queueDeadline}");
                }

                EnsureQueueIsStillConnected();

                if (queueCount >= MaxPlayers)
                {
                    logger.LogInformation($"Queue reached max capacity ({MaxPlayers}), creating match...");
                    queueStartTime = null;
                    queueDeadline = null;
                    CreateMatch();
                }
            }
        }

        private void RemoveFromQueue(ClientInfo client)
        {
            lock (queueLock)
            {
                randomQueue.TryRemove(client.Id, out _);

                logger.LogInformation($"Client {client.Id} removed from queue, queue size: {randomQueue.Count}");

                if (randomQueue.Count < MinPlayers)
                {
                    queueStartTime = null;
                    queueDeadline = null;
                    logger.LogInformation("Queue below minimum players, timer reset.");
                }
            }
        }

        private void CreateMatch()
        {
            var playersToMatch = Math.Min(MaxPlayers, randomQueue.Count);
            var matches = randomQueue.Take(playersToMatch).ToList();

            foreach (var kvp in matches)
            {
                randomQueue.TryRemove(kvp.Key, out _);
            }

            var matchedClients = matches.Select(kvp => kvp.Value).ToList();

            if (matchedClients.Count < MinPlayers)
            {
                foreach (var client in matchedClients)
                {
                    randomQueue[client.Id] = client;
                }

                if (matchedClients.Count > 0)
                {
                    queueStartTime = DateTime.UtcNow;
                    queueDeadline = queueStartTime.Value.AddSeconds(MinWaitTimeSeconds);
                    logger.LogInformation($"Not enough players ({matchedClients.Count}), timer restarted and waiting for more players...");
                }
                else
                {
                    queueStartTime = null;
                    queueDeadline = null;
                    logger.LogInformation("No players in queue, timer stopped.");
                }
                return;
            }

            queueStartTime = null;
            queueDeadline = null;

            if (randomQueue.Count > 0)
            {
                queueStartTime = DateTime.UtcNow;
                queueDeadline = queueStartTime.Value.AddSeconds(MinWaitTimeSeconds);
                logger.LogInformation($"Remaining {randomQueue.Count} player(s) in queue, timer started.");
            }

            var host = matchedClients[Random.Shared.Next(matchedClients.Count)];

            logger.LogInformation($"Match created with {matchedClients.Count} players: {string.Join(",", matchedClients.Select(c => c.Id))}, host: {host.Id}");

            IWsMessage msg = new MatchInfo
            {
                Peers = matchedClients.Select(match => new PeerInfo
                {
                    ConnectionId = match.Id,
                    IsHost = match.Id == host.Id,
                    HostConnectionId = host.Id
                }).ToList(),
                Seed = (uint)Random.Shared.Next(),
            };

            foreach (var c in matchedClients)
            {
                if (c.Socket.State == WebSocketState.Open)
                {
                    try
                    {
                        c.Socket.SendMessageAsync(msg).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Failed to send match info to {c.Id}");
                    }
                }
            }
        }
    }
}
