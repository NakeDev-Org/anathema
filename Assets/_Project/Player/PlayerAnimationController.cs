using NakeDev.Core;
using UnityEngine;

namespace NakeDev.Player
{
    /// <summary>
    /// Traduz o estado da locomoção em animações disparadas exclusivamente por crossfade
    /// via AnimatorBrain, sem transições configuradas no Animator Controller.
    /// </summary>
    [RequireComponent(typeof(PlayerLocomotion2D))]
    [RequireComponent(typeof(AnimatorBrain))]
    public partial class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private AnimatorBrain _animatorBrain;
        [SerializeField] private PlayerLocomotion2D _locomotion;

        [Header("State Names (devem existir como states soltos no Animator Controller)")]
        [SerializeField] private string _idleState = "Idle";
        [SerializeField] private string _walkState = "Walk";
        [SerializeField] private string _runState = "Run";
        [SerializeField] private string _jumpState = "Jump";
        [SerializeField] private string _jumpLoopState = "JumpLoop";
        [SerializeField] private string _doubleJumpState = "DoubleJump";
        [SerializeField] private string _fallState = "Fall";
        [SerializeField] private string _fallLoopState = "FallLoop";
        [SerializeField] private string _wallSlideState = "WallSlide";
        [SerializeField] private string _wallJumpState = "WallJump";
        [SerializeField] private string _landingState = "Landing";
        [SerializeField] private string _slideStartState = "SlideStart";
        [SerializeField] private string _slideLoopState = "Slide";
        [SerializeField] private string _slideEndState = "SlideEnd";
        [SerializeField] private string _aerialDashState = "AerialDash";
        [SerializeField] private float _crossfadeTime = 0.1f;

        [Header("Air Animation Phases")]
        [Tooltip("Tempo da introdução do pulo antes de entrar no loop.")]
        [Min(0f)]
        [SerializeField] private float _jumpIntroDuration = 0.24f;

        [Tooltip("Tempo da introdução da queda antes de entrar no loop.")]
        [Min(0f)]
        [SerializeField] private float _fallIntroDuration = 0.52f;

        [Tooltip("Crossfade usado somente ao trocar das introduções aéreas para seus loops.")]
        [Min(0f)]
        [SerializeField] private float _airLoopCrossfadeTime = 0.25f;

        [Header("Landing")]
        [Tooltip("Tempo durante o qual a animação Landing permanece antes de Idle/Run.")]
        [Min(0f)]
        [SerializeField] private float _landingAnimationDuration = 0.12f;

        [Tooltip("Velocidade mínima de impacto necessária para reproduzir Landing.")]
        [Min(0f)]
        [SerializeField] private float _minimumLandingSpeed = 1f;

        private float _landingAnimationTimer;
        private float _jumpIntroTimer;
        private float _fallIntroTimer;

        private int _idleHash;
        private int _walkHash;
        private int _runHash;
        private int _jumpHash;
        private int _jumpLoopHash;
        private int _doubleJumpHash;
        private int _fallHash;
        private int _fallLoopHash;
        private int _wallSlideHash;
        private int _wallJumpHash;
        private int _landingHash;
        private int _slideStartHash;
        private int _slideLoopHash;
        private int _slideEndHash;
        private int _aerialDashHash;

        private enum AnimState
        {
            Idle,
            Run,
            Walk,
            SlideStart,
            SlideLoop,
            SlideEnd,
            AerialDash,
            Jump,
            JumpLoop,
            DoubleJump,
            Fall,
            FallLoop,
            WallSlide,
            WallJump,
            Landing
        }

        private AnimState? _currentAnimState;

        private void Awake()
        {
            if (_animatorBrain == null)
                _animatorBrain = GetComponent<AnimatorBrain>();
            if (_locomotion == null)
                _locomotion = GetComponent<PlayerLocomotion2D>();

            InitializeStateHashes();
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
    }
}
