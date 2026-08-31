using TMPro;
using UnityEngine;

namespace NakeDev.Game.MiniGames.TimeTrial
{
    [DisallowMultipleComponent]
    public sealed class TimeTrialHUD : MonoBehaviour
    {
        [SerializeField] private TimeTrialController _controller;
        [SerializeField] private TMP_Text _currentTimeText;
        [SerializeField] private TMP_Text _bestTimeText;

        private void Awake()
        {
            if (_controller == null)
                _controller = GetComponentInParent<TimeTrialController>();
        }

        private void OnEnable()
        {
            if (_controller == null)
                return;

            _controller.OnTimeChanged += HandleTimeChanged;
            _controller.OnTrialFinished += HandleTrialFinished;
            _controller.OnTrialReset += Refresh;

            Refresh();
        }

        private void OnDisable()
        {
            if (_controller == null)
                return;

            _controller.OnTimeChanged -= HandleTimeChanged;
            _controller.OnTrialFinished -= HandleTrialFinished;
            _controller.OnTrialReset -= Refresh;
        }

        private void HandleTimeChanged(float currentTime)
        {
            if (_currentTimeText != null)
                _currentTimeText.text = $"TEMPO\n{FormatTime(currentTime)}";
        }

        private void HandleTrialFinished(float _, bool __)
        {
            Refresh();
        }

        private void Refresh()
        {
            HandleTimeChanged(_controller.CurrentTime);

            if (_bestTimeText == null)
                return;

            string bestTime = _controller.HasBestTime
                ? FormatTime(_controller.BestTime)
                : "--:--.---";

            _bestTimeText.text = $"MELHOR\n{bestTime}";
        }

        private static string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            int milliseconds = Mathf.FloorToInt(time * 1000f) % 1000;

            return $"{minutes:00}:{seconds:00}.{milliseconds:000}";
        }
    }
}
