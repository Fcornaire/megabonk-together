using MemoryPack;

namespace MegabonkTogether.Common.Messages.GameNetworkMessages
{
    [MemoryPackable]
    public partial class WeaponToggled : IGameNetworkMessage
    {
        public uint OwnerId { get; set; }
        public bool Enabled { get; set; }
        public int EWeapon { get; set; }
    }
}
