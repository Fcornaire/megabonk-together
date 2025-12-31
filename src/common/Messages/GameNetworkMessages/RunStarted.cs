using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class RunStarted : IGameNetworkMessage
    {
        public int MapData { get; set; }
        public string StageData { get; set; }
        public int MapTierIndex { get; set; }
        public int MusicTrackIndex { get; set; }
        public string ChallengeName { get; set; }
    }
}
