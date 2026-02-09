using System.Collections.Generic;
using UnityEngine;

namespace MegabonkTogether.Helpers
{
    public class DistanceThrottler(float mediumDistanceUpdateInterval = 1f)
    {
        private readonly Dictionary<int, float> lastUpdateTimes = [];
        private readonly Dictionary<int, bool> rendererDisabledState = [];
        private readonly float mediumDistanceUpdateInterval = mediumDistanceUpdateInterval;

        public bool ShouldUpdate(GameObject gameObject, int instanceId, bool isServer = false)
        {
            DistanceToPlayer distance = Plugin.GetDistanceToPlayer(gameObject.transform.position);
            return ShouldUpdate(gameObject, instanceId, distance, isServer);
        }

        private bool ShouldUpdate(GameObject gameObject, int instanceId, DistanceToPlayer distance, bool isServer = false)
        {
            switch (distance)
            {
                case DistanceToPlayer.Far:
                    SetRendererEnabled(gameObject, false, instanceId);
                    return isServer;

                case DistanceToPlayer.Medium:
                    SetRendererEnabled(gameObject, true, instanceId);

                    if (isServer)
                    {
                        return true;
                    }

                    float currentTime = Time.time;
                    if (!lastUpdateTimes.TryGetValue(instanceId, out float lastTime) ||
                        currentTime - lastTime >= mediumDistanceUpdateInterval)
                    {
                        lastUpdateTimes[instanceId] = currentTime;
                        return true;
                    }

                    return false;

                case DistanceToPlayer.Close:
                default:
                    SetRendererEnabled(gameObject, true, instanceId);
                    return true;
            }
        }

        private void SetRendererEnabled(GameObject gameObject, bool enabled, int instanceId)
        {
            bool wasDisabled = rendererDisabledState.TryGetValue(instanceId, out bool state) && state;

            if (!enabled && !wasDisabled)
            {
                var renderer = gameObject.GetComponentInChildren<Renderer>();

                if (renderer != null)
                {
                    renderer.enabled = false;
                    rendererDisabledState[instanceId] = true;
                }
            }
            else if (enabled && wasDisabled)
            {
                var renderer = gameObject.GetComponentInChildren<Renderer>();

                if (renderer != null)
                {
                    renderer.enabled = true;
                    rendererDisabledState[instanceId] = false;
                }
            }
        }
        public void Cleanup(int instanceId)
        {
            lastUpdateTimes.Remove(instanceId);
            rendererDisabledState.Remove(instanceId);
        }

        public void ClearAll()
        {
            lastUpdateTimes.Clear();
            rendererDisabledState.Clear();
        }
    }
}
