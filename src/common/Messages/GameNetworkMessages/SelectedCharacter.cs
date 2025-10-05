using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class SelectedCharacter : IGameNetworkMessage
    {
        public uint ConnectionId { get; set; }
        public uint Character { get; set; }
        public string Skin { get; set; } = "";
    }
}
