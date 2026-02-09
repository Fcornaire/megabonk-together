using MemoryPack;

namespace MegabonkTogether.Common.Messages.WsMessages
{
    [MemoryPackable]
    public partial class RunStatistics : IWsMessage
    {
        public int PlayerCount { get; set; }
        public string MapName { get; set; }
        public int StageLevel { get; set; }
        public List<string> Characters { get; set; } = new();
    }
}
