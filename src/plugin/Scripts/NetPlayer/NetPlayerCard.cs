using Assets.Scripts._Data.Tomes;
using MegabonkTogether.Common.Models;
using MegabonkTogether.Helpers;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MegabonkTogether.Scripts.NetPlayer
{
    public class NetPlayerCard : MonoBehaviour
    {
        private GameObject cardContainer;
        private RawImage Icon { get; set; }
        private GameObject iconBorder;
        private ECharacter Character { get; set; }
        public Player Player { get; private set; }
        private PlayerInventory Inventory { get; set; }
        public Color PlayerColor { get; private set; }

        private DisplayBar healthBar;
        //private DisplayBar xpBar;
        private DisplayBar shieldBar;
        private CustomInventoryHud customInventoryHud;
        private TextMeshProUGUI playerNameText;

        private float previousHp = 0f;
        //private float previousXp = 0f;
        private float previousShield = 0f;

        private const float HEALTH_BAR_WIDTH_RATIO = 3.5f;
        private const float INVENTORY_ICON_SIZE_RATIO = 0.65f;
        private const float ICON_SIZE_RATIO = 0.40f;
        private const float HEALTH_BAR_HEIGHT_RATIO = 0.15f;
        private const float NAME_TEXT_HEIGHT_RATIO = 0.15f;
        private const float SPACING = 0.02f;
        private const float BORDER_THICKNESS = 3f;

        private float GetIconSize(float cardHeight)
        {
            return cardHeight * ICON_SIZE_RATIO;
        }

        private float GetInventoryIconSize(float cardHeight)
        {
            float iconSize = GetIconSize(cardHeight);
            return iconSize * INVENTORY_ICON_SIZE_RATIO;
        }

        private float GetHealthBarHeight(float cardHeight)
        {
            return cardHeight * HEALTH_BAR_HEIGHT_RATIO;
        }

        private float GetHealthBarWidth(float cardHeight)
        {
            float iconSize = GetIconSize(cardHeight);
            return iconSize * HEALTH_BAR_WIDTH_RATIO;
        }

        private float GetNameTextHeight(float cardHeight)
        {
            return cardHeight * NAME_TEXT_HEIGHT_RATIO;
        }

        private float GetSpacing(float cardHeight)
        {
            return cardHeight * SPACING;
        }

        public void Initialize(Player player, RawImage iconTemplate, Transform parent, float cardHeight)
        {
            Player = player;
            Character = (ECharacter)player.Character;
            PlayerColor = GeneratePlayerColor(player.ConnectionId);

            float iconSize = GetIconSize(cardHeight);
            float healthBarWidth = GetHealthBarWidth(cardHeight);
            float healthBarHeight = GetHealthBarHeight(cardHeight);
            float spacing = GetSpacing(cardHeight);
            float nameTextHeight = GetNameTextHeight(cardHeight);
            float inventoryIconSize = GetInventoryIconSize(cardHeight);

            cardContainer = new GameObject($"PlayerCard_{Player.ConnectionId}_{Character}");
            cardContainer.transform.SetParent(parent, false);
            var containerRect = cardContainer.AddComponent<RectTransform>();

            iconBorder = new GameObject("IconBorder");
            iconBorder.transform.SetParent(cardContainer.transform, false);
            var borderRect = iconBorder.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0, 1f);
            borderRect.anchorMax = new Vector2(0, 1f);
            borderRect.pivot = new Vector2(0, 1f);
            borderRect.anchoredPosition = Vector2.zero;
            borderRect.sizeDelta = new Vector2(iconSize, iconSize);

            var borderImage = iconBorder.AddComponent<Image>();
            borderImage.color = PlayerColor;

            Icon = Object.Instantiate(iconTemplate, iconBorder.transform);
            Icon.name = $"Icon_{Player.ConnectionId}_{Character}";
            var iconRect = Icon.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = Vector2.zero;
                iconRect.sizeDelta = new Vector2(iconSize - BORDER_THICKNESS * 2, iconSize - BORDER_THICKNESS * 2);
            }

            var nameTextObj = new GameObject("PlayerNameText");
            nameTextObj.transform.SetParent(cardContainer.transform, false);
            var nameTextRect = nameTextObj.AddComponent<RectTransform>();
            nameTextRect.anchorMin = new Vector2(0, 1f);
            nameTextRect.anchorMax = new Vector2(0, 1f);
            nameTextRect.pivot = new Vector2(0, 1f);
            nameTextRect.anchoredPosition = new Vector2(0, -iconSize - spacing);
            nameTextRect.sizeDelta = new Vector2(iconSize, nameTextHeight);

            playerNameText = nameTextObj.AddComponent<TextMeshProUGUI>();
            playerNameText.text = player.Name;
            playerNameText.alignment = TextAlignmentOptions.Center;
            playerNameText.fontSize = nameTextHeight * 0.8f;
            playerNameText.enableAutoSizing = true;
            playerNameText.color = Color.white;
            playerNameText.fontStyle = FontStyles.Bold;
            playerNameText.overflowMode = TextOverflowModes.Overflow;

            var healthBarObj = new GameObject("HealthBar");
            healthBarObj.transform.SetParent(cardContainer.transform, false);
            healthBar = healthBarObj.AddComponent<DisplayBar>();
            healthBar.Initialize(
                cardContainer.transform,
                new Vector2(0, -iconSize - spacing - nameTextHeight - spacing),
                iconSize,
                healthBarHeight,
                new Color(1f, 0f, 0f, 1f)
            );

            var shieldBarObj = new GameObject("XpBar");
            shieldBarObj.transform.SetParent(cardContainer.transform, false);
            shieldBar = shieldBarObj.AddComponent<DisplayBar>();
            shieldBar.Initialize(
                cardContainer.transform,
                new Vector2(iconSize + spacing, -iconSize - spacing - nameTextHeight - spacing),
                iconSize,
                healthBarHeight,
                new Color(0f, 1f, 1f, 1f)
            );

            var weaponParentObj = new GameObject("WeaponParent");
            weaponParentObj.transform.SetParent(cardContainer.transform, false);
            var weaponParentRect = weaponParentObj.AddComponent<RectTransform>();
            weaponParentRect.anchorMin = new Vector2(0, 1f);
            weaponParentRect.anchorMax = new Vector2(0, 1f);
            weaponParentRect.pivot = new Vector2(0, 1f);
            weaponParentRect.anchoredPosition = new Vector2(iconSize + spacing, 0);
            weaponParentRect.sizeDelta = new Vector2(healthBarWidth, inventoryIconSize);

            var tomeParentObj = new GameObject("TomeParent");
            tomeParentObj.transform.SetParent(cardContainer.transform, false);
            var tomeParentRect = tomeParentObj.AddComponent<RectTransform>();
            tomeParentRect.anchorMin = new Vector2(0, 1f);
            tomeParentRect.anchorMax = new Vector2(0, 1f);
            tomeParentRect.pivot = new Vector2(0, 1f);
            tomeParentRect.anchoredPosition = new Vector2(iconSize + spacing, -(inventoryIconSize + spacing));
            tomeParentRect.sizeDelta = new Vector2(healthBarWidth, inventoryIconSize);

            var originalInventoryHud = Il2CppFindHelper.FindAllGameObjects()
                .FirstOrDefault(go => go.GetComponent<InventoryHud>() != null)?.GetComponent<InventoryHud>();

            if (originalInventoryHud != null && originalInventoryHud.itemContainerPrefab != null)
            {
                var inventoryHudObj = new GameObject("CustomInventoryHud");
                inventoryHudObj.transform.SetParent(cardContainer.transform, false);
                customInventoryHud = inventoryHudObj.AddComponent<CustomInventoryHud>();
                customInventoryHud.Initialize(
                    weaponParentObj.transform,
                    tomeParentObj.transform,
                    originalInventoryHud.itemContainerPrefab,
                    inventoryIconSize
                );

                foreach (var item in player.Inventory.WeaponInfos)
                {
                    customInventoryHud.AddWeapon(DataManager.Instance.GetWeapon((EWeapon)item.EWeapon));
                }

                foreach (var item in player.Inventory.TomeInfos)
                {
                    customInventoryHud.AddTome(DataManager.Instance.GetTome((ETome)item.ETome));
                }
            }
            else
            {
                Plugin.Log.LogWarning("Could not find original InventoryHud or itemContainerPrefab");
            }

            float totalHeight = iconSize + spacing + nameTextHeight + spacing + healthBarHeight;
            containerRect.sizeDelta = new Vector2(iconSize + spacing + healthBarWidth, totalHeight);

            //Plugin.Log.LogInfo($"NetPlayerCard initialized: cardHeight={cardHeight}, iconSize={iconSize}, totalHeight={totalHeight}");
        }

        public void SetPlayeInventory(PlayerInventory inventory)
        {
            Inventory = inventory;
            UpdateDisplayBars(true);
        }

        public void Resize(float cardHeight)
        {
            if (cardContainer == null) return;

            float iconSize = GetIconSize(cardHeight);
            float healthBarWidth = GetHealthBarWidth(cardHeight);
            float healthBarHeight = GetHealthBarHeight(cardHeight);
            float spacing = GetSpacing(cardHeight);
            float nameTextHeight = GetNameTextHeight(cardHeight);
            float inventoryIconSize = GetInventoryIconSize(cardHeight);

            if (iconBorder != null)
            {
                var borderRect = iconBorder.GetComponent<RectTransform>();
                borderRect.sizeDelta = new Vector2(iconSize, iconSize);
            }

            var iconRect = Icon.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(iconSize - BORDER_THICKNESS * 2, iconSize - BORDER_THICKNESS * 2);

            if (playerNameText != null)
            {
                var nameRect = playerNameText.GetComponent<RectTransform>();
                nameRect.anchoredPosition = new Vector2(0, -iconSize - spacing);
                nameRect.sizeDelta = new Vector2(iconSize, nameTextHeight);
                playerNameText.fontSize = nameTextHeight * 0.8f;
            }

            if (healthBar != null)
            {
                var healthBarRect = healthBar.GetRectTransform();
                healthBarRect?.anchoredPosition = new Vector2(0, -iconSize - spacing - nameTextHeight - spacing);
                healthBar.Resize(iconSize, healthBarHeight);
            }

            if (shieldBar != null)
            {
                var shieldBarRect = shieldBar.GetRectTransform();
                shieldBarRect?.anchoredPosition = new Vector2(iconSize + spacing, -iconSize - spacing - nameTextHeight - spacing);
                shieldBar.Resize(iconSize, healthBarHeight);
            }

            if (customInventoryHud != null)
            {
                customInventoryHud.Resize(inventoryIconSize);

                var weaponParent = cardContainer.transform.Find("WeaponParent");
                if (weaponParent != null)
                {
                    var weaponRect = weaponParent.GetComponent<RectTransform>();
                    weaponRect.anchoredPosition = new Vector2(iconSize + spacing, 0);
                    weaponRect.sizeDelta = new Vector2(healthBarWidth, inventoryIconSize);
                }

                var tomeParent = cardContainer.transform.Find("TomeParent");
                if (tomeParent != null)
                {
                    var tomeRect = tomeParent.GetComponent<RectTransform>();
                    tomeRect.anchoredPosition = new Vector2(iconSize + spacing, -(inventoryIconSize + spacing));
                    tomeRect.sizeDelta = new Vector2(healthBarWidth, inventoryIconSize);
                }
            }

            float totalHeight = iconSize + spacing + nameTextHeight + spacing + healthBarHeight;
            var containerRect = cardContainer.GetComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(iconSize + spacing + healthBarWidth, totalHeight);

            //Plugin.Log.LogInfo($"SetCardHeight: cardHeight={cardHeight}, iconSize={iconSize}, inventoryIconSize={inventoryIconSize}, totalHeight={totalHeight}");
        }

        public void UpdateDisplayBars(bool isImmediate)
        {
            if (Inventory == null || healthBar == null) return;

            int currentHP = Inventory.playerHealth.hp;
            int maxHP = Inventory.playerHealth.maxHp;

            //int currentXp = Inventory.playerXp.xp;
            //int maxXp = XpUtility.XpTotalNextLevel(currentXp);

            float currentShield = Inventory.playerHealth.shield;
            float maxShield = Inventory.playerHealth.maxShield;

            if (isImmediate)
            {
                previousHp = currentHP;
                previousShield = currentShield;
                healthBar.UpdateBarImmediate(currentHP, maxHP);
                shieldBar.UpdateBarImmediate(currentShield, maxShield);
            }
            else
            {
                healthBar.UpdateBar(currentHP, maxHP);
                shieldBar.UpdateBar(currentShield, maxShield);
            }
        }

        public void UpdateWeaponLevel(EWeapon eWeapon, int newLevel)
        {
            if (customInventoryHud != null)
            {
                customInventoryHud.UpdateWeaponLevel(eWeapon, newLevel);
            }
        }

        public void UpdateTomeLevel(ETome eTome, int newLevel)
        {
            if (customInventoryHud != null)
            {
                customInventoryHud.UpdateTomeLevel(eTome, newLevel);
            }
        }

        private void Update()
        {
            UpdateDisplayBars(false);

            if (Inventory == null) return;

            if (previousHp < Inventory.playerHealth.hp)
            {
                healthBar.PulseColor(Color.green, 0.3f);
                healthBar.Shake(1f, 0.1f);
            }

            if (previousHp > Inventory.playerHealth.hp)
            {
                healthBar.PulseColor(Color.black, 0.5f);
                healthBar.Shake(5f, 0.5f);
            }

            //if (previousXp < Inventory.playerXp.xp)
            //{
            //    xpBar.Shake(1f, 0.2f);
            //}

            if (previousShield < Inventory.playerHealth.shield)
            {
                shieldBar.Shake(1f, 0.2f);
            }

            previousHp = Inventory.playerHealth.hp;
            previousShield = Inventory.playerHealth.shield;
        }

        public RectTransform GetRectTransform()
        {
            return cardContainer.GetComponent<RectTransform>();
        }

        public float GetActualHeight()
        {
            if (cardContainer == null) return 0f;
            var rect = cardContainer.GetComponent<RectTransform>();
            return rect != null ? rect.sizeDelta.y : 0f;
        }

        public void Destroy()
        {
            if (healthBar != null)
            {
                healthBar.Destroy();
            }

            if (shieldBar != null)
            {
                shieldBar.Destroy();
            }

            if (customInventoryHud != null)
            {
                customInventoryHud.Destroy();
            }

            if (iconBorder != null)
            {
                Object.Destroy(iconBorder);
            }

            if (cardContainer != null)
            {
                Object.Destroy(cardContainer);
            }
        }

        public void UpdatePlayerName(string newName)
        {
            if (playerNameText != null)
            {
                playerNameText.text = newName;
            }
        }

        public void hide()
        {
            if (cardContainer != null)
            {
                cardContainer.SetActive(false);
            }
        }

        public void show()
        {
            if (cardContainer != null)
            {
                cardContainer.SetActive(true);
            }
        }

        private Color GeneratePlayerColor(uint connectionId)
        {
            Color[] distinctColors =
            [
                new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.5f),
                new Color(Color.white.r, Color.white.g, Color.white.b, 0.5f),
                new Color(Color.clear.r, Color.green.g, Color.green.b, 0.5f),
                new Color(Color.magenta.r, Color.clear.g, Color.clear.b, 0.5f),
                new Color(Color.blue.r, Color.blue.g, Color.blue.b, 0.5f),
                new Color(Color.gray.r, Color.gray.g, Color.gray.b, 0.5f),
            ];

            return distinctColors[connectionId % distinctColors.Length];
        }
    }
}
