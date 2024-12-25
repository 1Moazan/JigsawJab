using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Client
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static readonly ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();
        private static MainThreadDispatcher _instance;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            while (_actions.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }

        public static void RunOnMainThread(Action action)
        {
            if (action == null) return;
            _actions.Enqueue(action);
        }
    }
}