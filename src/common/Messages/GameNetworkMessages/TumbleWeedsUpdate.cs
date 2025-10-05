using MegabonkTogether.Common.Models;
using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class TumbleWeedsUpdate : IGameNetworkMessage
    {
        public TumbleWeedModel[] TumbleWeeds { get; set; }
    }
}
