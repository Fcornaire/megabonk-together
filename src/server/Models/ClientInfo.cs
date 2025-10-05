using System.Net.WebSockets;

namespace MegabonkTogether.Server.Models
{
    class ClientInfo
    {
        public uint Id { get; set; } = 0;
        public WebSocket? Socket { get; set; }
        public string QueueId { get; set; } = "";
    }
}
