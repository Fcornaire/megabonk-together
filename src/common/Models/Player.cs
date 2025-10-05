using MegabonkTogether.Common.Messages;
using MemoryPack;

namespace MegabonkTogether.Common.Models
{
    [MemoryPackable]
    public partial class Player
    {
        public uint ConnectionId;
        public bool IsHost = false;
        public uint Character = 0;
        public string Skin = "";
        public bool IsReady = false;
        public string Name = "Player";
        public QuantizedVector3 Position = new();
        public AnimatorState AnimatorState { get; set; } = new();
        public MovementState MovementState { get; set; } = new();

        public InventoryInfo Inventory { get; set; } = new();

        public uint Hp = 100;
        public uint MaxHp = 100;
        //public uint Xp = 0;
        public uint Shield = 0;
        public uint MaxShield = 0;

    }
}
