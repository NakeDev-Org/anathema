using NakeDev.Player;
using UnityEngine;

namespace NakeDev.VFX
{
    /// <summary>
    /// Emite estrelas pontuais em resposta a habilidades do player.
    /// A aparência do burst fica configurada no ParticleSystem.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class PlayerStarlightTrail : MonoBehaviour
    {
        [SerializeField] private PlayerLocomotion2D _locomotion;
        [SerializeField, Min(1)] private int _extraJumpBurstCount = 4;
        [SerializeField, Min(1)] private int _wallJumpBurstCount = 3;
        [SerializeField, Min(1)] private int _burstRunBurstCount = 3;

        private ParticleSystem _particles;

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();

            if (_locomotion == null)
                _locomotion = GetComponentInParent<PlayerLocomotion2D>();
        }

        private void OnEnable()
        {
            if (_locomotion != null)
            {
                _locomotion.OnJumpPerformed += HandleJumpPerformed;
                _locomotion.OnBurstRunStarted += HandleBurstRunStarted;
            }
        }

        private void OnDisable()
        {
            if (_locomotion != null)
            {
                _locomotion.OnJumpPerformed -= HandleJumpPerformed;
                _locomotion.OnBurstRunStarted -= HandleBurstRunStarted;
            }

            if (_particles != null)
            {
                _particles.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void HandleJumpPerformed(JumpType jumpType)
        {
            switch (jumpType)
            {
                case JumpType.Extra:
                    _particles.Emit(_extraJumpBurstCount);
                    break;

                case JumpType.Wall:
                    _particles.Emit(_wallJumpBurstCount);
                    break;
            }
        }

        private void HandleBurstRunStarted()
        {
            _particles.Emit(_burstRunBurstCount);
        }

        public void ClearTrail()
        {
            if (_particles == null)
                _particles = GetComponent<ParticleSystem>();

            _particles.Clear(true);
        }

        private void Reset()
        {
            _locomotion = GetComponentInParent<PlayerLocomotion2D>();
        }
    }
}
