using MemoryPack;

namespace MegabonkTogether.Common.Models
{
    [MemoryPackable]
    public partial class InventoryInfo
    {
        public ICollection<WeaponInfo> WeaponInfos { get; set; } = new List<WeaponInfo>();
        public ICollection<TomeInfo> TomeInfos { get; set; } = new List<TomeInfo>();
    }

    [MemoryPackable]
    public partial class WeaponInfo
    {
        public uint EWeapon;
        public uint Level;
    }

    [MemoryPackable]
    public partial class TomeInfo
    {
        public uint ETome;
        public uint Level;
    }
}
