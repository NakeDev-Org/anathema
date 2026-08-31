using NakeDev.Attributes;
using System;
using UnityEngine;

namespace NakeDev.Player
{
    public enum JumpType { Ground, Extra, Wall }

    /// <summary>
    /// Locomoção 2D completa num único componente parcial, controlada por um único
    /// LocomotionConfigSO. Cada arquivo parcial agrupa uma responsabilidade da locomoção.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public partial class PlayerLocomotion2D : MonoBehaviour
    {
        [InspectorLine("Config")]
        [Tooltip("Dados de tuning (velocidade, pulo, double jump, wall jump, coyote time...). Sem asset atribuído, usa os defaults do próprio LocomotionConfigSO.")]
        [SerializeField] private LocomotionConfigSO _config;

        [InspectorLine("Ground Check")]
        [SerializeField] private Transform _groundCheckPoint;

        private Rigidbody2D _rb;
        private Collider2D _collider;
        private CapsuleCollider2D _capsuleCollider;
        private IMovementInput _input;
        private float _defaultGravityScale;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private float _wallJumpControlLockTimer;
        private float _wallJumpBufferTimer;
        private int _wallJumpsRemaining;
        private int _extraJumpsRemaining;
        private int _wallDirection;
        private int _lastWallDirection;
        private float _extraJumpCooldownTimer;
        private bool _groundStateInitialized;
        private bool _runToggled;

        public bool IsGrounded { get; private set; }
        public bool IsJumping { get; private set; }
        public bool IsWallSliding { get; private set; }
        public bool IsRunning =>
            IsGrounded &&
            !IsSliding &&
            !IsAerialDashing &&
            WantsToRun &&
            Mathf.Abs(Velocity.x) >= _config.RunSpeedThreshold;
        public Vector2 Velocity => _rb.linearVelocity;

        public event Action<JumpType> OnJumpPerformed;
        public event Action<float> OnLanded;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _capsuleCollider = GetComponent<CapsuleCollider2D>();
            _collider = _capsuleCollider;
            _input = GetComponent<IMovementInput>();
            _defaultGravityScale = _rb.gravityScale;

            if (_config == null)
            {
                Debug.LogWarning($"{name}: PlayerLocomotion2D sem LocomotionConfigSO atribuído. Usando um asset temporário em memória com os valores default.", this);
                _config = ScriptableObject.CreateInstance<LocomotionConfigSO>();
            }

            _wallJumpsRemaining = _config.MaxWallJumps;
            InitializeDash();
        }

        private bool WantsToRun
        {
            get
            {
                if (_input == null)
                    return false;

                return _config.Mode == LocomotionConfigSO.InputMode.Toggle
                    ? _runToggled
                    : _input.IsRunHeld;
            }
        }

        private void HandleRunPressed()
        {
            if (_config.Mode != LocomotionConfigSO.InputMode.Toggle)
                return;

            _runToggled = !_runToggled;
        }

        private void OnEnable()
        {
            if (_input == null) return;
            _input.OnJumpPressed += OnJumpPressed;
            _input.OnDashPressed += HandleDashPressed;
            _input.OnRunPressed += HandleRunPressed;
        }

        private void OnDisable()
        {
            _runToggled = false;

            if (_input == null) return;
            _input.OnJumpPressed -= OnJumpPressed;
            _input.OnDashPressed -= HandleDashPressed;
            _input.OnRunPressed -= HandleRunPressed;
        }

        private void FixedUpdate()
        {
            TickTimers();
            GroundCheck();
            UpdateDashState();
            WallCheck();
            ApplyCornerCorrection();
            ApplyHorizontalMovement();
            ApplyFallGravity();
            TryConsumeJump();
        }
    }
}
