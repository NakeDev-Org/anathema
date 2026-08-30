using NakeDev.Attributes;
using UnityEngine;

namespace NakeDev.Player
{
    /// <summary>
    /// Mantém o alvo de câmera (TargetCamera, seguido pelo Cinemachine) sempre à frente do
    /// player conforme a direção que ele está olhando — em vez de um offset fixo que pode
    /// deixar o personagem "colado" numa borda da tela ou até atrás do enquadramento quando
    /// ele vira. Suaviza a troca via SmoothDamp em vez de saltar instantaneamente: um corte
    /// abrupto de câmera a cada vez que o player vira é desconfortável (acessibilidade —
    /// evita o efeito de "chacoalhar" a cada troca de direção rápida).
    /// </summary>
    public class CameraFacingOffset : MonoBehaviour
    {
        [InspectorLine("References")]
        [Tooltip("Se vazio, procura um PlayerFacing2D no pai deste objeto.")]
        [SerializeField] private PlayerFacing2D _facing;

        [InspectorLine("Offset")]
        [Tooltip("Distância horizontal (unidades) entre o player e o alvo de câmera, na direção que ele está olhando.")]
        [Min(0f)]
        [SerializeField] private float _offsetDistance = 2f;

        [Tooltip("Tempo (s) que o offset leva pra suavizar até a posição nova ao virar de direção. 0 = instantâneo.")]
        [Min(0f)]
        [SerializeField] private float _smoothTime = 0.15f;

        [Tooltip("Clamp de conforto: velocidade máxima (unidades/s) que o offset pode se mover, mesmo se um pico de deltaTime (troca de foco da janela, engasgo de frame) tentar empurrar mais rápido que isso. Evita o \"puxão\" de câmera que causa enjoo.")]
        [Min(0.01f)]
        [SerializeField] private float _maxSpeed = 40f;

        private float _velocityX;
        private float _initialLocalY;
        private float _initialLocalZ;

        private void Awake()
        {
            if (_facing == null)
                _facing = GetComponentInParent<PlayerFacing2D>();

            _initialLocalY = transform.localPosition.y;
            _initialLocalZ = transform.localPosition.z;
        }

        // Update (não LateUpdate): precisa terminar antes do CinemachineBrain ler a posição
        // deste objeto no próprio LateUpdate dele.
        private void Update()
        {
            if (_facing == null) return;

            float targetX = _facing.IsFacingRight ? _offsetDistance : -_offsetDistance;
            Vector3 local = transform.localPosition;

            local.x = _smoothTime <= 0f
                ? targetX
                : Mathf.SmoothDamp(local.x, targetX, ref _velocityX, _smoothTime, _maxSpeed);

            local.y = _initialLocalY;
            local.z = _initialLocalZ;
            transform.localPosition = local;
        }
    }
}
