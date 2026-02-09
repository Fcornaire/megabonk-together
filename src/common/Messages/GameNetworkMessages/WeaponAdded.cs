using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class WeaponAdded : IGameNetworkMessage
    {
        public int Weapon { get; set; }
        public uint OwnerId { get; set; }

        public IEnumerable<StatModifierModel> Upgrades { get; set; }
    }

    [MemoryPackable]
    public partial class StatModifierModel
    {
        public int StatType { get; set; }
        public float Value { get; set; }
        public int ModificationType { get; set; }
    }
}
