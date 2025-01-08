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

        public void StartTimer(double totalSeconds)
        {
            _endTime = NetworkTime.time + totalSeconds;
            _isTimerRunning = true;
        }

        private void StopTimer()
        {
            _isTimerRunning = false;
            OnTimerCompleted?.Invoke();
        }

        public void ResetTimer()
        {
            _isTimerRunning = false;
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