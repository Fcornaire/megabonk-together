using Assets.Scripts._Data.Tomes;
using MegabonkTogether.Common.Messages;
using MegabonkTogether.Common.Models;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MegabonkTogether.Scripts.NetPlayer
{
    public class NetPlayersDisplayer : MonoBehaviour
    {
        private List<NetPlayerCard> playerCards;
        private IPlayerManagerService playerManagerService;

        private const int MAX_PLAYERS = 5;
        private const float MIN_CARD_HEIGHT = 150f;
        private const float MAX_CARD_HEIGHT = 250f;
        private const float MAX_CARD_SPACING = 15f;
        private const float MIN_CARD_SPACING = 5f;
        private const float LEFT_MARGIN = 10f;
        private const float START_Y_OFFSET = -15f;
        private const float AVAILABLE_HEIGHT = 500f;

        private void Awake()
        {
            playerCards = new List<NetPlayerCard>();
            playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();
        }

        public void ResetCards()
        {
            ClearAllPlayers();
        }

        public void RemovePlayer(uint playerId)
        {
            var toRemove = playerCards.FirstOrDefault(card => card.Player.ConnectionId == playerId);

            if (toRemove == null)
            {
                Plugin.Log.LogWarning($"Cannot remove player card for player ID {playerId}: card not found");
                return;
            }

            toRemove.Destroy();
            playerCards.Remove(toRemove);
            RescaleAndRepositionAllCards();
        }

        public void Hide()
        {
            foreach (var playerCard in playerCards)
            {
                playerCard.hide();
            }
        }

        public void Show()
        {
            foreach (var playerCard in playerCards)
            {
                playerCard.show();
            }
        }

        public void AddPlayer(Player player)
        {
            if (playerCards.Count >= MAX_PLAYERS)
            {
                Plugin.Log.LogWarning($"Cannot add more players. Maximum of {MAX_PLAYERS} reached.");
                return;
            }

            var character = (ECharacter)player.Character;

            if (Plugin.Instance == null || !Plugin.Instance.CharactersIcon.ContainsKey(character))
            {
                Plugin.Log.LogWarning($"Cannot add player icon for character {character}: icon not found");
                return;
            }

            var sourceIcon = Plugin.Instance.CharactersIcon[character];

            float cardHeight = CalculateCardHeight(playerCards.Count + 1);

            var playerCard = this.gameObject.AddComponent<NetPlayerCard>();

            playerCard.Initialize(player, sourceIcon, UiManager.Instance.encounterWindows.transform, cardHeight);

            var inventory = playerManagerService.GetPlayerInventory(player.ConnectionId);
            if (inventory != null)
            {
                playerCard.SetPlayeInventory(inventory);
            }

            var rectTransform = playerCard.GetRectTransform();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0, 0.5f);
                rectTransform.anchorMax = new Vector2(0, 0.5f);
                rectTransform.pivot = new Vector2(0, 0.5f);

                float yOffset = CalculateYOffset(playerCards.Count, cardHeight);
                rectTransform.anchoredPosition = new Vector2(LEFT_MARGIN, yOffset);
            }

            playerCards.Add(playerCard);

            RescaleAndRepositionAllCards();

            var netPlayer = playerManagerService.GetNetPlayerByNetplayId(player.ConnectionId);
            if (netPlayer != null)
            {
                netPlayer.UpdateMinimapIconColor();
            }
        }

        public void OnUpdate(PlayerUpdate playerUpdate)
        {
            UpdatePlayerInventory(playerUpdate.ConnectionId, playerUpdate.Inventory);
            UpdatePlayerName(playerUpdate.ConnectionId, playerUpdate.Name);
        }

        public Color GetPlayerColor(uint playerId)
        {
            var card = playerCards.FirstOrDefault(c => c.Player.ConnectionId == playerId);
            if (card != null)
            {
                return card.PlayerColor;
            }

            return Color.cyan;
        }

        private void UpdatePlayerName(uint playerId, string newName)
        {
            foreach (var playerCard in playerCards)
            {
                if (playerCard.Player.ConnectionId == playerId)
                {
                    playerCard.UpdatePlayerName(newName);
                }
            }
        }

        private void UpdatePlayerInventory(uint playerId, InventoryInfo inventoryInfo)
        {
            foreach (var playerCard in playerCards)
            {
                if (playerCard.Player.ConnectionId == playerId)
                {
                    foreach (var weapon in inventoryInfo.WeaponInfos)
                    {
                        playerCard.UpdateWeaponLevel((EWeapon)weapon.EWeapon, (int)weapon.Level);
                    }

                    foreach (var tome in inventoryInfo.TomeInfos)
                    {
                        playerCard.UpdateTomeLevel((ETome)tome.ETome, (int)tome.Level);
                    }
                }
            }
        }

        private void ClearAllPlayers()
        {
            for (int i = 0; i < playerCards.Count; i++)
            {
                if (playerCards[i] != null)
                {
                    playerCards[i].Destroy();
                }
            }

            playerCards.Clear();

            Plugin.Log.LogInfo("Cleared all player cards");
        }

        private float CalculateCardSpacing(int playerCount)
        {
            if (playerCount <= 2)
                return MAX_CARD_SPACING;
            if (playerCount >= 6)
                return MIN_CARD_SPACING;

            float t = (playerCount - 2) / 4.0f;
            return Mathf.Lerp(MAX_CARD_SPACING, MIN_CARD_SPACING, t);
        }

        private float CalculateCardHeight(int playerCount)
        {
            if (playerCount <= 0) return MAX_CARD_HEIGHT;

            float spacing = CalculateCardSpacing(playerCount);
            float totalSpacing = (playerCount - 1) * spacing;

            float availableForCards = AVAILABLE_HEIGHT - totalSpacing;

            float cardHeight = availableForCards / playerCount;

            cardHeight = Mathf.Clamp(cardHeight, MIN_CARD_HEIGHT, MAX_CARD_HEIGHT);

            return cardHeight;
        }

        private float CalculateYOffset(int cardIndex, float cardHeight)
        {
            float totalOffset = START_Y_OFFSET;

            float spacing = CalculateCardSpacing(playerCards.Count);

            for (int i = 0; i < cardIndex; i++)
            {
                float actualCardHeight;
                if (i < playerCards.Count)
                {
                    actualCardHeight = playerCards[i].GetActualHeight();
                }
                else
                {
                    actualCardHeight = cardHeight * 1.11f;
                }

                totalOffset -= (actualCardHeight + spacing);
            }

            return totalOffset;
        }

        private void RescaleAndRepositionAllCards()
        {
            if (playerCards.Count == 0) return;

            float cardHeight = CalculateCardHeight(playerCards.Count);

            for (int i = 0; i < playerCards.Count; i++)
            {
                playerCards[i].Resize(cardHeight);

                var cardRect = playerCards[i].GetRectTransform();
                if (cardRect != null)
                {
                    float yOffset = CalculateYOffset(i, cardHeight);
                    cardRect.anchoredPosition = new Vector2(LEFT_MARGIN, yOffset);
                }
            }
        }

        private void OnDestroy()
        {
            ClearAllPlayers();
        }
    }
}
