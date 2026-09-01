using UnityEngine;

namespace NakeDev.Game.MiniGames.TimeTrial
{
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public sealed class TimeTrialStartFinishTrigger : MonoBehaviour
    {
        [SerializeField] private TimeTrialController _controller;

        private void Awake()
        {
            if (_controller == null)
                _controller = GetComponentInParent<TimeTrialController>();

            if (_controller == null)
                Debug.LogError("Time Trial Controller não encontrado na hierarquia.", this);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Rigidbody2D playerRigidbody = other.attachedRigidbody;

            if (playerRigidbody == null || !playerRigidbody.CompareTag("Player"))
                return;

            if (_controller == null)
                return;

            if (!_controller.IsRunning)
            {
                _controller.StartTrial();
                return;
            }

            if (_controller.TryFinishTrial())
                _controller.StartTrial();
        }

        private void Reset()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;

            _controller = GetComponentInParent<TimeTrialController>();
        }
    }
}
