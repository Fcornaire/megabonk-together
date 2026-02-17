using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class InteractableUsed : IGameNetworkMessage
    {
        public uint NetplayId { get; set; }
        public InteractableAction Action { get; set; } = InteractableAction.Used;
        public bool IsPortal { get; set; }
        public bool IsFinalPortal { get; set; }
        public bool IsCryptKey { get; set; }
        public uint OwnerId { get; set; }
        public bool IsMicrowaveAndHaveItem { get; set; }
    }

    public enum InteractableAction
    {
        Used = 0,
        Interact = 1,
        Destroy = 2
    }
}
