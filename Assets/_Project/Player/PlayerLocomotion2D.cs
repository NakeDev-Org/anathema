using UnityEngine;
using System;

namespace NakeDev.Player
{
    public enum JumpType { Ground, Extra, Wall } //servirá para manipular a animação correta de pulo

    /// <summary>
    /// Locomoção 2D completa (andar, pular, double jump, wall slide, wall jump) num só
    /// componente, controlada por um único LocomotionConfigSO. Decisão de projeto (Regra 9 —
    /// CONVENTIONS.md): preferimos um sistema coeso e fácil de achar a fragmentar em
    /// componentes "ability" separados — mais simples de ajustar num workflow solo.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerLocomotion2D : MonoBehaviour
    {
        [Tooltip("Dados de tuning (velocidade, pulo, double jump, wall jump, coyote time...). Sem asset atribuído, usa os defaults do próprio LocomotionConfigSO.")]
        [SerializeField] private LocomotionConfigSO _config;

        [Header("Ground Check")]
        [SerializeField] private Transform _groundCheckPoint;

        private Rigidbody2D _rb;
        private Collider2D _collider;
        private IMovementInput _input;
        private float _defaultGravityScale;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private float _wallJumpControlLockTimer;
        private int _wallJumpsRemaining;
        private int _extraJumpsRemaining;

        // -1 = parede à esquerda, +1 = parede à direita, 0 = nenhuma.
        private int _wallDirection;

        public bool IsGrounded { get; private set; }
        public bool IsJumping { get; private set; }
        public bool IsWallSliding { get; private set; }
        public Vector2 Velocity => _rb.linearVelocity;
        public event Action<JumpType> OnJumpPerformed;
        public event Action<float> OnLanded;

        private float _extraJumpCooldownTimer;
        private bool _groundStateInitialized;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _input = GetComponent<IMovementInput>();
            _defaultGravityScale = _rb.gravityScale;

            if (_config == null)
            {
                Debug.LogWarning($"{name}: PlayerLocomotion2D sem LocomotionConfigSO atribuído. Usando um asset temporário em memória com os valores default.", this);
                _config = ScriptableObject.CreateInstance<LocomotionConfigSO>();
            }

            _wallJumpsRemaining = _config.MaxWallJumps;
        }

        private void OnEnable()
        {
            if (_input != null)
                _input.OnJumpPressed += OnJumpPressed;
        }

        private void OnDisable()
        {
            if (_input != null)
                _input.OnJumpPressed -= OnJumpPressed;
        }

        private void OnJumpPressed()
        {
            _jumpBufferTimer = _config.JumpBufferTime;
        }

        private void Update()
        {
            TickTimers();
        }

        private void FixedUpdate()
        {
            // Ground/wall check rodam no mesmo passo de física do consumo do pulo (evita janela
            // de 1 frame entre Update/FixedUpdate onde a posição ainda não migrou e o coyote
            // time/estado de parede é rearmado indevidamente).
            GroundCheck();
            WallCheck();
            ApplyHorizontalMovement();
            ApplyFallGravity();
            TryConsumeJump();
        }

        private void GroundCheck()
        {
            bool wasGrounded = IsGrounded;
            float landingSpeed = Mathf.Max(0f, -_rb.linearVelocity.y);

            Vector2 origin = _groundCheckPoint != null ? (Vector2)_groundCheckPoint.position : (Vector2)transform.position;
            IsGrounded = Physics2D.OverlapCircle(origin, _config.GroundCheckRadius, _config.GroundLayerMask);

            // Evita disparar Landing no primeiro frame caso o player já comece no chão.
            if (_groundStateInitialized && !wasGrounded && IsGrounded)
            {
                OnLanded?.Invoke(landingSpeed);
            }

            _groundStateInitialized = true;

            if (IsGrounded)
            {
                _coyoteTimer = _config.CoyoteTime;
                _extraJumpsRemaining = _config.MaxExtraJumps;
                _wallJumpsRemaining = _config.MaxWallJumps;
                IsJumping = false;
            }
        }

        private void WallCheck()
        {
            // Sem parede no chão (evita "grudar" em paredes enquanto já está apoiado) nem
            // durante o lock de controle logo após um wall jump (senão gruda de novo na hora).
            if (IsGrounded || _wallJumpControlLockTimer > 0f)
            {
                IsWallSliding = false;
                _wallDirection = 0;
                return;
            }

            // Usa os bounds reais do collider porque o Rigidbody pode não estar
            // exatamente no centro dele, especialmente quando existe Collider Offset.
            Bounds bounds = _collider.bounds;

            // Pequena margem para garantir que o raycast comece fora do collider
            // do player, evitando que ele detecte o próprio personagem como parede.
            const float skin = 0.01f;

            // Cada raycast começa na respectiva borda lateral do collider.
            // Antes, ambos começavam em _rb.position, dentro do player, o que
            // poderia causar um falso positivo caso sua layer estivesse no mask.
            Vector2 rightOrigin = new Vector2(bounds.max.x + skin, bounds.center.y);

            Vector2 leftOrigin = new Vector2(bounds.min.x - skin, bounds.center.y);

            // Como os raios agora começam fora do collider, não precisamos somar
            // metade da largura do player. Verificamos somente a distância configurada.
            bool touchingRight = Physics2D.Raycast(rightOrigin, Vector2.right, _config.WallCheckDistance, _config.GroundLayerMask);

            bool touchingLeft = Physics2D.Raycast(leftOrigin, Vector2.left, _config.WallCheckDistance, _config.GroundLayerMask);

            float x = _input != null ? _input.MoveInput.x : 0f;

            // Só "gruda"/desliza se o jogador estiver segurando o input em direção à parede —
            // senão qualquer encostada de raspão prenderia o player nela.
            if (touchingRight && x > 0.1f)
            {
                IsWallSliding = true;
                _wallDirection = 1;
            }
            else if (touchingLeft && x < -0.1f)
            {
                IsWallSliding = true;
                _wallDirection = -1;
            }
            else
            {
                IsWallSliding = false;
                _wallDirection = 0;
            }
        }

        private void TickTimers()
        {
            if (!IsGrounded)
                _coyoteTimer -= Time.deltaTime;

            if (_jumpBufferTimer > 0f)
                _jumpBufferTimer -= Time.deltaTime;

            if (_wallJumpControlLockTimer > 0f)
                _wallJumpControlLockTimer -= Time.deltaTime;

            if (_extraJumpCooldownTimer > 0f)
                _extraJumpCooldownTimer -= Time.deltaTime;
        }

        private void ApplyHorizontalMovement()
        {
            // Durante o lock pós-wall-jump, deixa o impulso agir sem o input horizontal cancelá-lo.
            if (_wallJumpControlLockTimer > 0f) return;

            float x = _input != null ? _input.MoveInput.x : 0f;
            float targetSpeed = x * _config.MoveSpeed;
            float currentSpeed = _rb.linearVelocity.x;

            // Desacelera mais forte que acelera quando não há input (ou trocando de direção),
            // e nunca ultrapassa targetSpeed nesse passo (evita overshoot/oscilação).
            float rate = Mathf.Abs(targetSpeed) > 0.01f ? _config.Acceleration : _config.Deceleration;
            float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);

            _rb.linearVelocity = new Vector2(newSpeed, _rb.linearVelocity.y);
        }

        private void ApplyFallGravity()
        {
            _rb.gravityScale = _rb.linearVelocity.y < 0f ? _defaultGravityScale * _config.FallGravityMultiplier : _defaultGravityScale;

            // Limita a velocidade de queda enquanto desliza na parede (só afeta quando já caindo
            // mais rápido que o limite — não interfere num pulo ainda subindo contra a parede).
            if (IsWallSliding && _rb.linearVelocity.y < -_config.WallSlideSpeed)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -_config.WallSlideSpeed);
            }
        }

        private void TryConsumeJump()
        {
            if (_jumpBufferTimer <= 0f) return;

            if (IsWallSliding && _wallJumpsRemaining > 0)
            {
                // Pulo saindo da parede, na direção oposta a ela.
                float pushDirection = -_wallDirection;
                _rb.linearVelocity = new Vector2(pushDirection * _config.WallJumpForceX, _config.WallJumpForceY);
                OnJumpPerformed?.Invoke(JumpType.Wall);

                _wallJumpsRemaining--;

                if (_config.ResetExtraJumpsOnWallJump)
                    _extraJumpsRemaining = _config.MaxExtraJumps;

                _extraJumpCooldownTimer = _config.ExtraJumpCooldown;
                _wallJumpControlLockTimer = _config.WallJumpControlLockTime;
                _coyoteTimer = 0f;
                IsWallSliding = false;
                _wallDirection = 0;
                IsGrounded = false;
                IsJumping = true;
                _jumpBufferTimer = 0f;
                return;
            }

            bool canPrimaryJump = _coyoteTimer > 0f;

            if (canPrimaryJump)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _config.JumpForce);
                OnJumpPerformed?.Invoke(JumpType.Ground); //dispara uma notificação do evento, informando o tipo de pulo

                _extraJumpCooldownTimer = _config.ExtraJumpCooldown;

                _coyoteTimer = 0f;
                // Marca como não-grounded na hora: a física só vai afastar o collider do chão
                // no próximo passo, então sem isso o GroundCheck deste mesmo frame rearmaria
                // o coyote time (e recarregaria os pulos extra) antes do player sair do chão de fato.
                IsGrounded = false;
                IsJumping = true;
            }
            else if (_extraJumpsRemaining > 0 && _extraJumpCooldownTimer <= 0f)
            {
                _extraJumpsRemaining--;
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _config.ExtraJumpForce);
                OnJumpPerformed?.Invoke(JumpType.Extra);
                _extraJumpCooldownTimer = _config.ExtraJumpCooldown;
                IsJumping = true;
            }
            else
            {
                return; // nada pra consumir: nem pulo primário, nem parede, nem pulo extra disponível.
            }

            _jumpBufferTimer = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            if (_config == null) return;

            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Vector3 origin = _groundCheckPoint != null ? _groundCheckPoint.position : transform.position;
            Gizmos.DrawWireSphere(origin, _config.GroundCheckRadius);

            if (_collider == null) return;
            float halfWidth = _collider.bounds.extents.x;
            float castDistance = halfWidth + _config.WallCheckDistance;
            Gizmos.color = IsWallSliding ? Color.cyan : Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * castDistance);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.left * castDistance);
        }
    }
}
