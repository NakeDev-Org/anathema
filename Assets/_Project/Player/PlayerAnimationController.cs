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
        [SerializeField] private string _walkState = "Walk";
        [SerializeField] private string _runState = "Run";
        [SerializeField] private string _jumpState = "Jump";
        [SerializeField] private string _doubleJumpState = "DoubleJump";
        [SerializeField] private string _fallState = "Fall";
        [SerializeField] private string _wallSlideState = "WallSlide";
        [SerializeField] private string _wallJumpState = "WallJump";
        [SerializeField] private string _landingState = "Landing";
        [SerializeField] private float _crossfadeTime = 0.1f;
        
        [Tooltip("Velocidade horizontal mínima para considerar o personagem em Run.")]
        [SerializeField] private float _runSpeedThreshold = 0.1f;

        [Header("Landing")]
        [Tooltip("Tempo durante o qual a animação Landing permanece antes de Idle/Run.")]
        [Min(0f)]
        [SerializeField] private float _landingAnimationDuration = 0.12f;

        [Tooltip("Velocidade mínima de impacto necessária para reproduzir Landing.")]
        [Min(0f)]
        [SerializeField] private float _minimumLandingSpeed = 1f;

        private float _landingAnimationTimer;

        private int _idleHash;
        private int _walkHash;
        private int _runHash;
        private int _jumpHash;
        private int _doubleJumpHash;
        private int _fallHash;
        private int _wallSlideHash;
        private int _wallJumpHash;
        private int _landingHash;

        private enum AnimState { Idle, Run, Walk, Jump, DoubleJump, Fall, WallSlide, WallJump, Landing }
        private AnimState? _currentAnimState;

        private void Awake()
        {
            if (_animatorBrain == null)
                _animatorBrain = GetComponent<AnimatorBrain>();
            if (_locomotion == null)
                _locomotion = GetComponent<PlayerLocomotion2D>();

            _idleHash = Animator.StringToHash(_idleState);
            _runHash = Animator.StringToHash(_runState);
            _walkHash = Animator.StringToHash(_walkState);
            _jumpHash = Animator.StringToHash(_jumpState);
            _doubleJumpHash = Animator.StringToHash(_doubleJumpState);
            _fallHash = Animator.StringToHash(_fallState);
            _wallSlideHash = Animator.StringToHash(_wallSlideState);
            _wallJumpHash = Animator.StringToHash(_wallJumpState);
            _landingHash = Animator.StringToHash(_landingState);
        }

        private void Update()
        {
            if (_animatorBrain == null || _locomotion == null)
                return;

            if (_landingAnimationTimer > 0f)
            {
                _landingAnimationTimer -= Time.deltaTime;

                if (_landingAnimationTimer > 0f)
                    return;
            }

            SetPlayState(DetermineState());
        }

        //Separei do update para poder chamar quando o evento da locomotion for disparada
        private void SetPlayState(AnimState desired, bool forceRestart = false)
        {
            if (!forceRestart && desired == _currentAnimState) return;

            _currentAnimState = desired;
            _animatorBrain.PlayAnimation(HashFor(desired), _crossfadeTime);
        }

        private void OnEnable()
        {
            if (_locomotion == null) return;

            _locomotion.OnJumpPerformed += HandleJumpPerformed;
            _locomotion.OnLanded += HandleLanded;
        }

        private void OnDisable()
        {
            if (_locomotion == null) return;

            _locomotion.OnJumpPerformed -= HandleJumpPerformed;
            _locomotion.OnLanded -= HandleLanded;
        }

        private void HandleLanded(float landingSpeed)
        {
            if (landingSpeed < _minimumLandingSpeed) return;

            _landingAnimationTimer = _landingAnimationDuration;
            SetPlayState(AnimState.Landing, true);
        }

        private void HandleJumpPerformed(JumpType jumpType)
        {
            _landingAnimationTimer = 0f;

            switch (jumpType)
            {
                case JumpType.Ground:
                    SetPlayState(AnimState.Jump, true);
                    break;

                case JumpType.Extra:
                    SetPlayState(AnimState.DoubleJump, true);
                    break;

                case JumpType.Wall:
                    SetPlayState(AnimState.WallJump, true);
                    break;
            }
        }

        private AnimState DetermineState()
        {
            if (!_locomotion.IsGrounded)
            {
                if (_locomotion.IsWallSliding)
                    return AnimState.WallSlide;

                if (_locomotion.Velocity.y > 0f)
                {
                    if (_currentAnimState == AnimState.Jump ||
                        _currentAnimState == AnimState.DoubleJump ||
                        _currentAnimState == AnimState.WallJump)
                    {
                        return _currentAnimState.Value;
                    }

                    return AnimState.Jump;
                }

                return AnimState.Fall;
            }

            if (Mathf.Abs(_locomotion.Velocity.x) > 0f)
            {
                return Mathf.Abs(_locomotion.Velocity.x) > _runSpeedThreshold
                ? AnimState.Run
                : AnimState.Walk;
            }
            else
            {
                return AnimState.Idle;
            }
        }

        private int HashFor(AnimState state)
        {
            switch (state)
            {
                case AnimState.Idle: return _idleHash;
                case AnimState.Walk: return _walkHash;
                case AnimState.Run: return _runHash;
                case AnimState.Jump: return _jumpHash;
                case AnimState.DoubleJump: return _doubleJumpHash;
                case AnimState.Fall: return _fallHash;
                case AnimState.WallSlide: return _wallSlideHash;
                case AnimState.WallJump: return _wallJumpHash;
                case AnimState.Landing: return _landingHash;
                default: return _idleHash;
            }
        }
    }
}
