using Assets.Scripts.Inventory__Items__Pickups.Items;
using MegabonkTogether.Helpers;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace MegabonkTogether.Scripts
{
    public class NotificationQueueManager : MonoBehaviour
    {
        private class QueuedNotification
        {
            public (string tableReference, string tableEntryReference) LocalizedName { get; set; }
            public (string tableReference, string tableEntryReference) LocalizedDescription { get; set; }
            public IEnumerable<string> DescriptionArgs { get; set; }
            public RandomSfx Sfx { get; set; }
            public EItem Item { get; set; }
        }

        private readonly ConcurrentQueue<QueuedNotification> notificationQueue = new();
        private bool isProcessing = false;
        private float notificationDuration = 5f;

        public void Initialize()
        {
            var popup = Plugin.Instance.GetAchievementPopup();
            if (popup != null)
            {
                notificationDuration = (popup.moveTime * 2) + popup.stayTime;
            }
        }

        public void EnqueueNotification(
            (string tableReference, string tableEntryReference) localizedName,
            (string tableReference, string tableEntryReference) localizedDescription,
            IEnumerable<string> descriptionArgs,
            RandomSfx sfx = null,
            EItem item = EItem.Key)
        {
            notificationQueue.Enqueue(new QueuedNotification
            {
                LocalizedName = localizedName,
                LocalizedDescription = localizedDescription,
                DescriptionArgs = descriptionArgs,
                Sfx = sfx,
                Item = item
            });

            if (!isProcessing)
            {
                CoroutineRunner.Instance.Run(ProcessNotificationQueue());
            }
        }

        private IEnumerator ProcessNotificationQueue()
        {
            isProcessing = true;

            while (notificationQueue.Count > 0)
            {
                if (notificationQueue.TryDequeue(out var notification))
                {
                    bool success = ShowNotificationInternal(
                        notification.LocalizedName,
                        notification.LocalizedDescription,
                        notification.DescriptionArgs,
                        notification.Sfx,
                        notification.Item
                    );

                    if (success)
                    {
                        yield return new WaitForSeconds(notificationDuration);
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.5f);
                    }
                }
            }

            isProcessing = false;
        }

        private bool ShowNotificationInternal(
            (string tableReference, string tableEntryReference) localizedName,
            (string tableReference, string tableEntryReference) localizedDescription,
            IEnumerable<string> descriptionArgs,
            RandomSfx sfx,
            EItem item)
        {
            try
            {
                if (DataManager.Instance == null || DataManager.Instance.itemData == null)
                {
                    Plugin.Log.LogWarning("DataManager.Instance or itemData is null ?");
                    return false;
                }

                if (!DataManager.Instance.itemData.TryGetValue(item, out var data))
                {
                    Plugin.Log.LogWarning($"Item {item} not found in DataManager.Instance.itemData");
                    return false;
                }

                var achievementData = data.GetUnlockRequirement();
                achievementData.localizedName = new LocalizedString() { TableReference = localizedName.tableReference, TableEntryReference = localizedName.tableEntryReference };
                achievementData.localizedDescription = new LocalizedString()
                {
                    TableReference = localizedDescription.tableReference,
                    TableEntryReference = localizedDescription.tableEntryReference
                };

                var localizationService = Plugin.Services.GetService<ILocalizationService>();
                localizationService.EnqueueNextLocalizedDescription(descriptionArgs);

                Plugin.Instance.GetAchievementPopup().OnAchievementUnlocked(achievementData);
                if (sfx != null)
                {
                    AudioManager.Instance.PlaySfx(sfx.sounds[0]);
                }

                return true;
            }
            catch (System.AccessViolationException ex)
            {
                Plugin.Log.LogError($"AccessViolationException in ShowNotificationInternal: {ex.Message}"); //Idk why this happens
                Plugin.Instance.AchievementPopup = null;
                return false;
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Unexpected error in ShowNotificationInternal: {ex}");
                return false;
            }
        }
    }
}
