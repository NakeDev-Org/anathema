using NakeDev.Attributes;
using UnityEngine;

namespace NakeDev.Debugging
{
    public enum FrameRateLockMode
    {
        FPS30,
        FPS60,
        FPS120,
        Unlimited,
        Custom
    }

    /// <summary>
    /// Controla o frame rate do jogo em runtime — base para o futuro "frame locker" completo.
    /// Presets (30/60/120/Sem Limite) cobrem o uso normal; Custom + Range existe para debugar
    /// com frame rate reduzido (até 1 FPS) e observar comportamento frame a frame.
    /// </summary>
    public sealed class FrameRateLockerDebug : MonoBehaviour
    {
        [InspectorLine("Frame Rate")]
        [SerializeField] private FrameRateLockMode _mode = FrameRateLockMode.Unlimited;

        [Tooltip("Usado apenas quando o Mode está em Custom. Vai até 1 FPS para debugar em câmera lenta.")]
        [SerializeField, Range(1, 300)] private int _customFrameRate = 60;

        [InspectorLine("Estado")]
        [ReadOnly, SerializeField] private int _appliedFrameRate;

        private void Awake()
        {
            Apply();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            Apply();
        }

        [Button("Aplicar Frame Rate")]
        public void Apply()
        {
            // vSync sobrescreve Application.targetFrameRate na maioria das plataformas —
            // precisa estar desligado pra qualquer um dos modos (inclusive Unlimited) valer.
            QualitySettings.vSyncCount = 0;

            _appliedFrameRate = ResolveTargetFrameRate();
            Application.targetFrameRate = _appliedFrameRate;
        }

        private int ResolveTargetFrameRate()
        {
            switch (_mode)
            {
                case FrameRateLockMode.FPS30: return 30;
                case FrameRateLockMode.FPS60: return 60;
                case FrameRateLockMode.FPS120: return 120;
                case FrameRateLockMode.Custom: return _customFrameRate;
                case FrameRateLockMode.Unlimited:
                default: return -1;
            }
        }
    }
}
