using System;
using Mirror;
using UnityEngine;

namespace Networking
{
    public class NetworkTimer : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnTimerUpdated))]
        private double _endTime;

        public Action OnTimerCompleted;

        private bool _isTimerRunning;

        public event Action<double> OnTimerTick;

        private void Update()
        {
            if (_isTimerRunning && isServer)
            {
                double remainingTime = _endTime - NetworkTime.time;

                if (remainingTime <= 0)
                {
                    remainingTime = 0;
                    StopTimer();
                }
                else
                {
                    OnTimerUpdated(0, remainingTime);
                }
            }
        }

        public void StartTimer(double totalSeconds, Action onComplete = null)
        {
            if (!isServer)
            {
                Debug.LogError("StartTimer can only be called on the server.");
                return;
            }

            _endTime = NetworkTime.time + totalSeconds;
            _isTimerRunning = true;
            OnTimerCompleted = onComplete;
        }

        private void StopTimer()
        {
            if (!isServer)
                return;

            _isTimerRunning = false;
            OnTimerCompleted?.Invoke();
            OnTimerCompleted = null;
        }

        private void OnTimerUpdated(double oldValue, double newValue)
        {
            double remainingTime = Math.Max(0, _endTime - NetworkTime.time);
            OnTimerTick?.Invoke(remainingTime);
        }

        public double GetRemainingTime()
        {
            return Math.Max(0, _endTime - NetworkTime.time);
        }
    }
}