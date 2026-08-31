using System;
using UnityEngine;

namespace NakeDev.Game.MiniGames.TimeTrial
{
    [DisallowMultipleComponent]
    public sealed class TimeTrialController : MonoBehaviour
    {
        public bool IsRunning { get; private set; }
        public bool CanFinish { get; private set; }
        public bool HasBestTime { get; private set; }
        public float CurrentTime { get; private set; }
        public float BestTime { get; private set; }

        public event Action OnTrialStarted;
        public event Action<float> OnTimeChanged;
        public event Action<float, bool> OnTrialFinished;
        public event Action OnTrialReset;

        private void Update()
        {
            if (!IsRunning)
                return;

            CurrentTime += Time.deltaTime;
            OnTimeChanged?.Invoke(CurrentTime);
        }

        public void StartTrial()
        {
            CurrentTime = 0f;
            CanFinish = false;
            IsRunning = true;

            OnTimeChanged?.Invoke(CurrentTime);
            OnTrialStarted?.Invoke();
        }

        public void ValidateCheckpoint()
        {
            if (IsRunning)
                CanFinish = true;
        }

        public bool TryFinishTrial()
        {
            if (!IsRunning || !CanFinish)
                return false;

            IsRunning = false;

            bool isNewBestTime = !HasBestTime || CurrentTime < BestTime;

            if (isNewBestTime)
            {
                BestTime = CurrentTime;
                HasBestTime = true;
            }

            OnTrialFinished?.Invoke(CurrentTime, isNewBestTime);
            return true;
        }

        public void ResetTrial()
        {
            IsRunning = false;
            CanFinish = false;
            CurrentTime = 0f;

            OnTimeChanged?.Invoke(CurrentTime);
            OnTrialReset?.Invoke();
        }
    }
}
