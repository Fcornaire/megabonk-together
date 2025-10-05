using MegabonkTogether.Common.Models;

namespace MegabonkTogether.Extensions
{
    internal static class PlayerInventoryExtensions
    {
        public static InventoryInfo ToInventoryInfos(this PlayerInventory playerInventory)
        {
            var inventoryInfo = new InventoryInfo();
            foreach (var weaponKey in playerInventory.weaponInventory.weapons.Keys)
            {
                var weaponInfo = new WeaponInfo
                {
                    EWeapon = (uint)weaponKey,
                    Level = (uint)playerInventory.weaponInventory.weapons[weaponKey].level
                };
                inventoryInfo.WeaponInfos.Add(weaponInfo);
            }

            foreach (var tomeKey in playerInventory.tomeInventory.tomeLevels.Keys)
            {
                var tomeInfo = new TomeInfo
                {
                    ETome = (uint)tomeKey,
                    Level = (uint)playerInventory.tomeInventory.tomeLevels[tomeKey]
                };
                inventoryInfo.TomeInfos.Add(tomeInfo);
            }
            return inventoryInfo;
        }
    }
}
