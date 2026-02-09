using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace MegabonkTogether.Scripts
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static readonly ConcurrentQueue<Action> _executionQueue = new();

        public void Update()
        {
            while (_executionQueue.Count > 0)
            {
                if (_executionQueue.TryDequeue(out var action))
                {
                    action();
                }
            }
        }

        public static void Enqueue(Action action)
        {
            _executionQueue.Enqueue(action);
        }
    }
}
