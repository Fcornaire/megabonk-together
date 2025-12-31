using Assets.Scripts._Data.MapsAndStages;

namespace MegabonkTogether.Extensions
{
    internal static class EMapExtensions
    {
        public static string GetMapName(this EMap map)
        {
            return map switch
            {
                EMap.Graveyard => "Graveyard",
                EMap.Desert => "Desert",
                EMap.Forest => "Forest",
                EMap.Hell => "Hell",
                EMap.None => "None",
                _ => "Unknown"
            };
        }
    }
}
