using NakeDev.Attributes;
using UnityEngine;

namespace NakeDev.Player
{
    /// <summary>
    /// Mantém o alvo de câmera (TargetCamera, seguido pelo Cinemachine) à frente do player
    /// conforme a direção que ele está olhando quando no chão — em vez de um offset fixo que
    /// pode deixar o personagem "colado" numa borda da tela.
    ///
    /// Regras de conforto (nessa ordem de prioridade — Regra 6: funcional, não decorativo):
    /// 1. Amplitude pequena e transição lenta o bastante pra nunca parecer um corte.
    /// 2. Held direction (técnica de Super Mario World): no chão, só troca de lado depois que
    ///    a nova direção se sustenta por <see cref="_directionHoldTime"/> segundos.
    /// 3. Centralização no ar: sem favorecer lado nenhum durante manobras aéreas.
    /// 4. Pausa pós-pouso (<see cref="_landingSettleTime"/>): a câmera NÃO decide lado no
    ///    instante exato em que os pés tocam o chão — isso empilharia o impacto do pouso com
    ///    o início do movimento de câmera, a combinação mais comum de causar desconforto/enjoo.
    ///    Ela mantém a centralização por um instante depois de pousar, e só então recomeça a
    ///    avaliar o lado normalmente.
    /// </summary>
    public class CameraFacingOffset : MonoBehaviour
    {
        [InspectorLine("References")]
        [Tooltip("Se vazio, procura um PlayerFacing2D no pai deste objeto.")]
        [SerializeField] private PlayerFacing2D _facing;

        [Tooltip("Se vazio, procura um PlayerLocomotion2D no pai deste objeto. Usado pra saber se o player está no chão (centraliza a câmera enquanto no ar).")]
        [SerializeField] private PlayerLocomotion2D _locomotion;

        [InspectorLine("Offset")]
        [Tooltip("Distância horizontal (unidades) entre o player e o alvo de câmera, na direção que ele está olhando (só aplicado no chão). Mantenha pequeno — quanto maior, mais rápido a câmera precisa se mover pra cobrir a distância.")]
        [Min(0f)]
        [SerializeField] private float _offsetDistance = 1.6f;

        [Tooltip("Tempo (s) que o offset leva pra suavizar até a posição nova. 0 = instantâneo.")]
        [Min(0f)]
        [SerializeField] private float _smoothTime = 0.6f;

        [Tooltip("Clamp de conforto: velocidade máxima (unidades/s) que o offset pode se mover. É o principal freio contra a sensação de \"corte\" — baixo o bastante pra a troca de lado ser sempre visivelmente gradual.")]
        [Min(0.01f)]
        [SerializeField] private float _maxSpeed = 6f;

        [InspectorLine("Held Direction (evita chacoalhar em trocas rápidas no chão)")]
        [Tooltip("Tempo (s) que a nova direção precisa se sustentar antes da câmera se comprometer com ela, quando o player está no chão.")]
        [Min(0f)]
        [SerializeField] private float _directionHoldTime = 0.25f;

        [InspectorLine("Ar e Pouso")]
        [Tooltip("Enquanto o player não está no chão, a câmera centraliza (offset 0) em vez de favorecer um lado.")]
        [SerializeField] private bool _centerWhileAirborne = true;

        [Tooltip("Tempo (s) após pousar em que a câmera continua centralizada antes de voltar a decidir o lado. Evita somar o impacto do pouso com o início do movimento de câmera no mesmo instante.")]
        [Min(0f)]
        [SerializeField] private float _landingSettleTime = 0.25f;

        private float _velocityX;
        private float _initialLocalY;
        private float _initialLocalZ;

        // Lado que a câmera já assumiu de fato no chão — só muda depois do hold time.
        private bool _committedFacingRight = true;
        private bool _hasPendingDirection;
        private bool _pendingFacingRight;
        private float _pendingTimer;

        private bool _wasGrounded = true;
        private float _landingSettleTimer;

        private void Awake()
        {
            if (_facing == null)
                _facing = GetComponentInParent<PlayerFacing2D>();
            if (_locomotion == null)
                _locomotion = GetComponentInParent<PlayerLocomotion2D>();

            _initialLocalY = transform.localPosition.y;
            _initialLocalZ = transform.localPosition.z;

            if (_facing != null)
                _committedFacingRight = _facing.IsFacingRight;

            _wasGrounded = _locomotion == null || _locomotion.IsGrounded;
        }

        // Update (não LateUpdate): precisa terminar antes do CinemachineBrain ler a posição
        // deste objeto no próprio LateUpdate dele.
        private void Update()
        {
            if (_facing == null) return;

            bool isGrounded = _locomotion == null || _locomotion.IsGrounded;

            // Detecta o instante do pouso e arma a pausa de assentamento.
            if (isGrounded && !_wasGrounded)
                _landingSettleTimer = _landingSettleTime;
            _wasGrounded = isGrounded;

            if (_landingSettleTimer > 0f)
                _landingSettleTimer -= Time.deltaTime;

            bool isAirborne = _centerWhileAirborne && (!isGrounded || _landingSettleTimer > 0f);

            float targetX;
            if (isAirborne)
            {
                targetX = 0f;
                _hasPendingDirection = false; // no ar/assentando, descarta qualquer pendência anterior
            }
            else
            {
                UpdateHeldDirection();
                targetX = _committedFacingRight ? _offsetDistance : -_offsetDistance;
            }

            Vector3 local = transform.localPosition;
            local.x = _smoothTime <= 0f
                ? targetX
                : Mathf.SmoothDamp(local.x, targetX, ref _velocityX, _smoothTime, _maxSpeed);

            local.y = _initialLocalY;
            local.z = _initialLocalZ;
            transform.localPosition = local;
        }

        private void UpdateHeldDirection()
        {
            bool currentFacingRight = _facing.IsFacingRight;

            if (currentFacingRight == _committedFacingRight)
            {
                // Voltou pro lado já comprometido antes do hold completar — cancela a troca pendente.
                _hasPendingDirection = false;
                return;
            }

            if (!_hasPendingDirection || _pendingFacingRight != currentFacingRight)
            {
                _hasPendingDirection = true;
                _pendingFacingRight = currentFacingRight;
                _pendingTimer = 0f;
            }

            _pendingTimer += Time.deltaTime;

            if (_pendingTimer >= _directionHoldTime)
            {
                _committedFacingRight = _pendingFacingRight;
                _hasPendingDirection = false;
            }
        }
    }
}
