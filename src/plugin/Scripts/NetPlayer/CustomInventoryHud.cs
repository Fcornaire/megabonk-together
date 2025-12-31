using Assets.Scripts._Data.Tomes;
using System.Collections.Generic;
using UnityEngine;

namespace MegabonkTogether.Scripts.NetPlayer
{
    public class CustomInventoryHud : MonoBehaviour
    {
        private const int MAX_WEAPONS = 5;
        private const int MAX_TOMES = 5;

        private Transform weaponParent;
        private Transform tomeParent;
        private GameObject itemContainerPrefab;

        private readonly Dictionary<EWeapon, InventoryItemPrefabUI> weaponContainers = [];
        private readonly Dictionary<ETome, InventoryItemPrefabUI> tomeContainers = [];

        private float iconSize;

        public void Initialize(Transform weaponParentTransform, Transform tomeParentTransform, GameObject containerPrefab, float itemIconSize)
        {
            weaponParent = weaponParentTransform;
            tomeParent = tomeParentTransform;
            itemContainerPrefab = containerPrefab;
            iconSize = itemIconSize;
        }

        public void AddWeapon(WeaponData weaponData)
        {
            if (weaponContainers.Count >= MAX_WEAPONS)
            {
                Plugin.Log.LogWarning($"Cannot add more weapons. Maximum of {MAX_WEAPONS} reached.");
                return;
            }

            var container = Object.Instantiate(itemContainerPrefab, weaponParent);
            container.name = $"WeaponContainer_{weaponContainers.Count}";

            var rect = container.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0, 1f);
                rect.anchorMax = new Vector2(0, 1f);
                rect.pivot = new Vector2(0, 1f);
                rect.anchoredPosition = new Vector2(weaponContainers.Count * (iconSize + 5f), 0);
                rect.sizeDelta = new Vector2(iconSize, iconSize);
            }

            var itemUI = container.GetComponent<InventoryItemPrefabUI>();
            if (itemUI != null)
            {
                itemUI.gameObject.SetActive(true);
                itemUI.SetItem(weaponData);
                itemUI.RefreshEnabled(true);

                weaponContainers.Add(weaponData.eWeapon, itemUI);
            }
            else
            {
                Plugin.Log.LogWarning("InventoryItemPrefabUI component not found on container");
            }
        }

        public void AddTome(TomeData tomeData)
        {
            if (tomeContainers.Count >= MAX_TOMES)
            {
                Plugin.Log.LogWarning($"Cannot add more tomes. Maximum of {MAX_TOMES} reached.");
                return;
            }

            var container = Object.Instantiate(itemContainerPrefab, tomeParent);
            container.name = $"TomeContainer_{tomeContainers.Count}";

            var rect = container.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0, 1f);
                rect.anchorMax = new Vector2(0, 1f);
                rect.pivot = new Vector2(0, 1f);
                rect.anchoredPosition = new Vector2(tomeContainers.Count * (iconSize + 5f), 0);
                rect.sizeDelta = new Vector2(iconSize, iconSize);
            }

            var itemUI = container.GetComponent<InventoryItemPrefabUI>();
            if (itemUI != null)
            {
                itemUI.gameObject.SetActive(true);
                itemUI.SetItem(tomeData);
                itemUI.RefreshEnabled(true);

                tomeContainers.Add(tomeData.eTome, itemUI);
            }
            else
            {
                Plugin.Log.LogWarning("InventoryItemPrefabUI component not found on container");
            }
        }

        public void UpdateWeaponLevel(EWeapon weapon, int lvl)
        {
            if (!weaponContainers.ContainsKey(weapon))
            {
                var data = DataManager.Instance.GetWeapon(weapon);
                AddWeapon(data);
            }

            var itemUI = weaponContainers[weapon];

            Il2CppSystem.Collections.Generic.Dictionary<string, string> smartStrings = new();
            smartStrings.Add("level", lvl.ToString());
            itemUI.t_level.text = Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("Game_HUD", "LEVEL", smartStrings);
        }

        public void UpdateTomeLevel(ETome tome, int lvl)
        {
            if (!tomeContainers.ContainsKey(tome))
            {
                var data = DataManager.Instance.GetTome(tome);
                AddTome(data);
            }

            var itemUI = tomeContainers[tome];

            Il2CppSystem.Collections.Generic.Dictionary<string, string> smartStrings = new();
            smartStrings.Add("level", lvl.ToString());
            itemUI.t_level.text = Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("Game_HUD", "LEVEL", smartStrings);
        }

        private void ClearWeapons()
        {
            foreach (var container in weaponContainers)
            {
                if (container.Value != null)
                {
                    Object.Destroy(container.Value);
                }
            }
            weaponContainers.Clear();
        }

        private void ClearTomes()
        {
            foreach (var container in tomeContainers)
            {
                if (container.Value != null)
                {
                    Object.Destroy(container.Value);
                }
            }
            tomeContainers.Clear();
        }

        public void Resize(float newIconSize)
        {
            iconSize = newIconSize;

            int weaponIndex = 0;
            foreach (var kvp in weaponContainers)
            {
                var container = kvp.Value.gameObject;
                var rect = container.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(iconSize, iconSize);
                    rect.anchoredPosition = new Vector2(weaponIndex * (iconSize + 5f), 0);
                }
                weaponIndex++;
            }

            int tomeIndex = 0;
            foreach (var kvp in tomeContainers)
            {
                var container = kvp.Value.gameObject;
                var rect = container.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(iconSize, iconSize);
                    rect.anchoredPosition = new Vector2(tomeIndex * (iconSize + 5f), 0);
                }
                tomeIndex++;
            }
        }

        public void Destroy()
        {
            ClearWeapons();
            ClearTomes();
        }
    }
}
