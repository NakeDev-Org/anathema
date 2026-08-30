using UnityEngine;

namespace NakeDev.VFX
{
    /// <summary>
    /// Controla a emissão do rastro estrelado conforme a velocidade do player.
    /// A aparência e a emissão por distância ficam configuradas no ParticleSystem.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class PlayerStarlightTrail : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _body;
        [SerializeField, Min(0f)] private float _minimumSpeed = 2f;

        private ParticleSystem _particles;
        private ParticleSystem.EmissionModule _emission;

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();

            if (_body == null)
                _body = GetComponentInParent<Rigidbody2D>();

            _emission = _particles.emission;
        }

        private void LateUpdate()
        {
            if (_body == null)
            {
                _emission.enabled = false;
                return;
            }

            float minimumSpeedSquared = _minimumSpeed * _minimumSpeed;
            _emission.enabled = _body.linearVelocity.sqrMagnitude >= minimumSpeedSquared;
        }

        private void OnDisable()
        {
            if (_particles == null)
                return;

            _particles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Reset()
        {
            _body = GetComponentInParent<Rigidbody2D>();
        }
    }
}
