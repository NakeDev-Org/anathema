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
        [Tooltip("Velocidade horizontal mínima para considerar o player correndo.")]
        [Min(0f)]
        public float RunSpeedThreshold = 12f;

        [Header("Jump")]
        public float JumpForce = 7f;
        public float FallGravityMultiplier = 2.5f;

        [Header("Fall")]
        [Tooltip("Velocidade vertical máxima de queda, em unidades por segundo. Use um valor positivo.")]
        [Min(0.1f)]
        public float MaxFallSpeed = 20f;

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

        [Header("Ground Slide")]
        public bool GroundSlideEnabled = true;
        [Min(0f)]
        public float GroundSlideSpeed = 20f;
        [Tooltip("Duração da fase inicial do ground slide.")]
        [Min(0f)]
        public float GroundSlideStartDuration = 0.27f;
        [Tooltip("Duração mínima total antes que o ground slide possa terminar.")]
        [Min(0f)]
        public float GroundSlideMinimumDuration = 0.7f;
        [Tooltip("Duração da fase de encerramento do ground slide.")]
        [Min(0f)]
        public float GroundSlideEndDuration = 0.27f;
        [Min(0f)]
        public float GroundSlideDeceleration = 18f;
        [Tooltip("Velocidade mínima mantida durante Start/Loop, inclusive sob obstáculos longos.")]
        [Min(0f)]
        public float GroundSlideMinimumSpeed = 8f;
        [Min(0f)]
        public float GroundSlideCooldown = 0.2f;
        [Tooltip("Altura do CapsuleCollider2D durante o ground slide. A largura é respeitada como mínimo.")]
        [Min(0.1f)]
        public float GroundSlideColliderHeight = 2.1f;

        [Header("Aerial Dash")]
        public bool AerialDashEnabled = true;
        [Tooltip("Quantidade de aerial dashes disponível antes de tocar o chão.")]
        [Min(0)]
        public int MaxAerialDashes = 1;
        [Min(0f)]
        public float AerialDashSpeed = 22f;
        [Min(0.01f)]
        public float AerialDashDuration = 0.18f;

        [Header("Ground Check")]
        [Tooltip("Raio do OverlapCircle usado para detectar o chão a partir do GroundCheckPoint.")]
        public float GroundCheckRadius = 0.15f;
        [Tooltip("NUNCA deixe em 'Everything': o ponto de checagem fica colado no próprio collider do Player, então uma mask ampla detecta o player nele mesmo e trava IsGrounded em true pra sempre. Restrinja a uma layer dedicada de chão/plataforma. Essa mesma layer também é usada para detectar paredes.")]
        public LayerMask GroundLayerMask;

        [Header("Corner Correction")]
        [Tooltip("Distância horizontal máxima usada para desviar o player de uma quina do teto durante a subida. 0 desliga a correção.")]
        [Min(0f)]
        public float CornerCorrectionDistance = 0.2f;
        [Tooltip("Tamanho de cada tentativa horizontal ao procurar espaço livre ao lado da quina.")]
        [Min(0.001f)]
        public float CornerCorrectionStep = 0.02f;
        [Tooltip("Distância projetada acima do collider para antecipar o contato com o teto.")]
        [Min(0.001f)]
        public float CornerCheckDistance = 0.05f;

        [Header("Wall Slide")]
        [Tooltip("Permite que o personagem deslize lentamente enquanto segura o direcional contra uma parede.")]
        public bool WallSlideEnabled = true;
        [Tooltip("Distância do raycast horizontal (a partir da borda do collider) usado pra detectar parede.")]
        public float WallCheckDistance = 0.15f;
        [Tooltip("Posição vertical dos dois raycasts de parede, relativa à metade da altura do collider. 0 junta ambos no centro; 1 aproxima dos extremos.")]
        [Range(0.1f, 0.9f)]
        public float WallCheckVerticalOffset = 0.5f;
        [Tooltip("Velocidade máxima de queda (unidades/s, positivo) enquanto desliza na parede.")]
        public float WallSlideSpeed = 2f;
        [Tooltip("Tempo inicial do wall slide em que a queda usa WallSlideEntrySpeed.")]
        [Min(0f)]
        public float WallSlideEntryDuration = 0.08f;
        [Tooltip("Velocidade de queda durante a entrada do wall slide. Use um valor positivo.")]
        [Min(0f)]
        public float WallSlideEntrySpeed = 0.25f;
        [Tooltip("Aceleração vertical usada para atingir a velocidade de wall slide.")]
        [Min(0f)]
        public float WallSlideAcceleration = 20f;

        [Header("Wall Jump")]
        [Tooltip("Permite executar wall jump ao tocar ou sair recentemente de uma parede.")]
        public bool WallJumpEnabled = true;
        [Tooltip("Força horizontal do impulso ao pular saindo da parede (na direção oposta a ela).")]
        public float WallJumpForceX = 8f;
        [Tooltip("Força vertical do pulo saindo da parede.")]
        public float WallJumpForceY = 10f;
        [Tooltip("Tempo (s) após o wall jump em que o input horizontal normal fica suspenso, pra não cancelar o impulso na hora.")]
        public float WallJumpControlLockTime = 0.15f;
        [Tooltip("Tempo após sair do wall slide em que o wall jump ainda é permitido.")]
        [Min(0f)]
        public float WallJumpBufferTime = 0.12f;
        [Tooltip("Quantidade máxima de wall jumps antes de tocar o chão novamente. 0 desliga o wall jump.")]
        [Min(0)]
        public int MaxWallJumps = 1;

        [Tooltip("Se habilitado, um wall jump restaura todos os pulos extras no ar.")]
        public bool ResetExtraJumpsOnWallJump = true;
        [Tooltip("Se habilitado, um wall jump restaura todos os aerial dashes.")]
        public bool ResetAerialDashesOnWallJump = true;
    }
}
