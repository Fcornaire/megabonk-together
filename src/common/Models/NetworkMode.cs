namespace MegabonkTogether.Common.Models
{
    public enum NetworkModeType
    {
        Random,
        Friendlies
    }

    public enum Role
    {
        Host,
        Client
    }

    public class NetworkMode
    {
        public NetworkModeType Mode { get; set; }
        public Role Role { get; set; }
        public string RoomCode { get; set; } = "";

    }
}
