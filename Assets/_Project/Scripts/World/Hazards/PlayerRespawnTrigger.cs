using UnityEngine;
using NakeDev.VFX;

namespace NakeDev.World
{
    public class PlayerRespawnTrigger : MonoBehaviour
    {
        [SerializeField] private Transform _destination;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            Rigidbody2D playerRigidbody = other.attachedRigidbody;

            if (playerRigidbody == null)
                return;

            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
            playerRigidbody.position = _destination.position;
        }

    }
}
