using Assets.Scripts.Inventory__Items__Pickups.Items;

namespace MegabonkTogether.Services
{
    public class Tracks
    {
        public uint moneyFlying { get; set; } = 0;
        public uint itemProcs { get; set; } = 0;
        public uint kills { get; set; } = 0;
    }

    public interface ITrackerService
    {

        public void SetCurrentPlayerId(uint playerId);
        public uint? GetCurrentPlayerId();
        public void UnsetCurrentPlayerId();
        public void RegisterTrack();

        public Tracks GetPlayerTrack();
    }

    internal class TrackerService : ITrackerService
    {
        private uint? currentPlayerId;
        private Tracks playerTrack = new();

        public void SetCurrentPlayerId(uint playerId)
        {
            currentPlayerId = playerId;
        }

        public uint? GetCurrentPlayerId()
        {
            return currentPlayerId;
        }

        public void UnsetCurrentPlayerId()
        {
            currentPlayerId = null;
        }

        public void RegisterTrack()
        {
            uint moneyFlying = 1;
            uint kill = 1;
            uint itemProcs = 0;

            var inventory = GameManager.Instance.player.inventory;
            if (inventory.itemInventory.items.Keys.System_Collections_Generic_ICollection_TKey__Contains(EItem.SoulHarvester))
            {
                itemProcs += 1;
            }

            if (inventory.itemInventory.items.Keys.System_Collections_Generic_ICollection_TKey__Contains(EItem.SluttyCannon))
            {
                itemProcs += 1;
            }

            if (inventory.itemInventory.items.Keys.System_Collections_Generic_ICollection_TKey__Contains(EItem.MoldyCheese))
            {
                itemProcs += 1;
            }

            playerTrack.moneyFlying += moneyFlying;
            playerTrack.itemProcs += itemProcs;
            playerTrack.kills += kill;
        }

        public Tracks GetPlayerTrack()
        {
            return playerTrack;
        }
    }
}
