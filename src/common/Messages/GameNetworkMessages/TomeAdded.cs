using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class TomeAdded : IGameNetworkMessage
    {
        public int Tome { get; set; }
        public uint OwnerId { get; set; }
        public List<StatModifierModel> Upgrades { get; set; }
        public int Rarity { get; set; }
    }
}
