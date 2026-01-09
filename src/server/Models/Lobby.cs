namespace MegabonkTogether.Server.Models
{
    public class Lobby
    {
        public uint Seed { get; set; }
        public string RoomCode { get; set; } = "";
        public ClientInfo Host { get; set; } = new();
        public ICollection<ClientInfo> Clients { get; set; } = new List<ClientInfo>();
        public bool GameStarted { get; set; } = false;
    }
}
