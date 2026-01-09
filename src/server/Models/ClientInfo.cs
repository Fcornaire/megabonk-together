using MegabonkTogether.Common.Models;
using System.Net.WebSockets;

namespace MegabonkTogether.Server.Models
{
    public class ClientInfo
    {
        public uint Id { get; set; } = 0;
        public WebSocket? Socket { get; set; }
        public string QueueId { get; set; } = "";
        public Role? Role { get; set; }
        public string Name { get; internal set; }
    }
}
