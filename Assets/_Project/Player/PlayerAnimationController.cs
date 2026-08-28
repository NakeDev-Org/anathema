using UnityEngine;
using NakeDev.Core;

namespace NakeDev.Player
{
    /// <summary>
    /// Traduz o estado de locomoção em crossfade (Regra 5 — CONVENTIONS.md). Nenhuma seta de
    /// transição configurada no Animator Controller: os 4 states (Idle/Run/Jump/Fall) ficam
    /// soltos e são disparados por código via AnimatorBrain.
    /// </summary>
    [RequireComponent(typeof(PlayerLocomotion2D))]
    [RequireComponent(typeof(AnimatorBrain))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private AnimatorBrain _animatorBrain;
        [SerializeField] private PlayerLocomotion2D _locomotion;

        [Header("State Names (devem existir como states soltos no Animator Controller)")]
        [SerializeField] private string _idleState = "Idle";
        [SerializeField] private string _runState = "Run";
        [SerializeField] private string _jumpState = "Jump";
        [SerializeField] private string _fallState = "Fall";
        [SerializeField] private float _crossfadeTime = 0.1f;
        
        [Tooltip("Velocidade horizontal mínima para considerar o personagem em Run.")]
        [SerializeField] private float _runSpeedThreshold = 0.1f;

        private int _idleHash;
        private int _runHash;
        private int _jumpHash;
        private int _fallHash;

        private enum AnimState { Idle, Run, Jump, Fall }
        private AnimState? _currentAnimState;

        private void Awake()
        {
            if (_animatorBrain == null)
                _animatorBrain = GetComponent<AnimatorBrain>();
            if (_locomotion == null)
                _locomotion = GetComponent<PlayerLocomotion2D>();

            _idleHash = Animator.StringToHash(_idleState);
            _runHash = Animator.StringToHash(_runState);
            _jumpHash = Animator.StringToHash(_jumpState);
            _fallHash = Animator.StringToHash(_fallState);
        }

        private void Update()
        {
            if (_animatorBrain == null || _locomotion == null) return;

            AnimState desired = DetermineState();
            if (desired == _currentAnimState) return;

            _currentAnimState = desired;
            _animatorBrain.PlayAnimation(HashFor(desired), _crossfadeTime);
        }

        private AnimState DetermineState()
        {
            if (!_locomotion.IsGrounded)
                return _locomotion.Velocity.y > 0f ? AnimState.Jump : AnimState.Fall;

            return Mathf.Abs(_locomotion.Velocity.x) > _runSpeedThreshold ? AnimState.Run : AnimState.Idle;
        }

        private int HashFor(AnimState state)
        {
            switch (state)
            {
                case AnimState.Run: return _runHash;
                case AnimState.Jump: return _jumpHash;
                case AnimState.Fall: return _fallHash;
                default: return _idleHash;
            }
        }
    }
}
