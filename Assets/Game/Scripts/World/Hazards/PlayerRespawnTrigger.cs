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
            Collider2D playerCollider = other.GetComponent<Collider2D>();

            if (playerRigidbody == null)
                return;

            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
            playerCollider.enabled = false;
            playerRigidbody.position = _destination.position;
            playerCollider.enabled = true;
        }

    }
}
