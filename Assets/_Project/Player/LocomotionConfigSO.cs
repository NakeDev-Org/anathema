using UnityEngine;

namespace NakeDev.Player
{
    /// <summary>
    /// Dados de tuning da locomoção base, num asset à parte (Regra 3 — CONVENTIONS.md).
    /// Permite criar variações (ex.: "Player_Rapido.asset") sem tocar em código, e reaproveitar
    /// o mesmo PlayerLocomotion2D noutro projeto só trocando o asset referenciado.
    /// </summary>
    [CreateAssetMenu(fileName = "LocomotionConfig", menuName = "NakeDev/Player/Locomotion Config")]
    public class LocomotionConfigSO : ScriptableObject
    {
        [Header("Movement")]
        public float MoveSpeed = 6f;
        [Tooltip("Unidades/s² até atingir MoveSpeed.")]
        public float Acceleration = 60f;
        [Tooltip("Unidades/s² até parar quando solta o input (ou troca de direção).")]
        public float Deceleration = 80f;

        [Header("Jump")]
        public float JumpForce = 7f;
        public float FallGravityMultiplier = 2.5f;

        [Header("Double Jump")]
        [Tooltip("Quantos pulos extras no ar, além do pulo do chão. 0 desliga o double jump.")]
        public int MaxExtraJumps = 1;
        [Tooltip("Força vertical do(s) pulo(s) extra(s) no ar. Pode ser diferente do pulo do chão.")]
        public float ExtraJumpForce = 6f;
        [Tooltip("Intervalo mínimo entre um salto e a liberação do próximo pulo extra. 0 permite o pulo extra imediatamente.")]
        [Min(0f)]
        public float ExtraJumpCooldown = 0.15f;

        [Header("Jump Assist (funcional, não estético — Regra 6)")]
        [Tooltip("Tempo (s) após sair da borda em que ainda é possível pular.")]
        public float CoyoteTime = 0.1f;
        [Tooltip("Tempo (s) antes de aterrissar em que um pulo pressionado antecipadamente ainda é aceito.")]
        public float JumpBufferTime = 0.1f;

        [Header("Ground Check")]
        [Tooltip("Raio do OverlapCircle usado para detectar o chão a partir do GroundCheckPoint.")]
        public float GroundCheckRadius = 0.15f;
        [Tooltip("NUNCA deixe em 'Everything': o ponto de checagem fica colado no próprio collider do Player, então uma mask ampla detecta o player nele mesmo e trava IsGrounded em true pra sempre. Restrinja a uma layer dedicada de chão/plataforma. Essa mesma layer também é usada para detectar paredes.")]
        public LayerMask GroundLayerMask;

        [Header("Wall Slide")]
        [Tooltip("Distância do raycast horizontal (a partir da borda do collider) usado pra detectar parede.")]
        public float WallCheckDistance = 0.15f;
        [Tooltip("Velocidade máxima de queda (unidades/s, positivo) enquanto desliza na parede.")]
        public float WallSlideSpeed = 2f;

        [Header("Wall Jump")]
        [Tooltip("Força horizontal do impulso ao pular saindo da parede (na direção oposta a ela).")]
        public float WallJumpForceX = 8f;
        [Tooltip("Força vertical do pulo saindo da parede.")]
        public float WallJumpForceY = 10f;
        [Tooltip("Tempo (s) após o wall jump em que o input horizontal normal fica suspenso, pra não cancelar o impulso na hora.")]
        public float WallJumpControlLockTime = 0.15f;
        [Tooltip("Quantidade máxima de wall jumps antes de tocar o chão novamente. 0 desliga o wall jump.")]
        [Min(0)]
        public int MaxWallJumps = 1;

        [Tooltip("Se habilitado, um wall jump restaura todos os pulos extras no ar.")]
        public bool ResetExtraJumpsOnWallJump = true;
    }
}
