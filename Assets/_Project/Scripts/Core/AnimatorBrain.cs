using UnityEngine;

namespace NakeDev.Core
{
    /// <summary>
    /// Wrapper de animação 100% crossfade (Regra 5 — CONVENTIONS.md). Nenhum sistema deste
    /// template dispara animação direto no Animator: tudo passa por aqui, sempre via
    /// CrossFadeInFixedTime, nunca por transições configuradas no Animator Controller.
    /// </summary>
    public class AnimatorBrain : MonoBehaviour
    {
        [SerializeField] private Animator[] _animators;

        public Animator[] Animators => _animators;

        public void PlayAnimation(string stateName, float crossfadeTime = 0.1f, int layer = 0)
        {
            if (_animators == null) return;

            for (int i = 0; i < _animators.Length; i++)
            {
                if (_animators[i] != null)
                    _animators[i].CrossFadeInFixedTime(stateName, crossfadeTime, layer);
            }
        }

        public void PlayAnimation(int stateHash, float crossfadeTime = 0.1f, int layer = 0)
        {
            if (_animators == null) return;

            for (int i = 0; i < _animators.Length; i++)
            {
                if (_animators[i] != null)
                    _animators[i].CrossFadeInFixedTime(stateHash, crossfadeTime, layer);
            }
        }

        public void SetFloat(int id, float value)
        {
            if (_animators == null) return;

            for (int i = 0; i < _animators.Length; i++)
            {
                if (_animators[i] != null)
                    _animators[i].SetFloat(id, value);
            }
        }
    }
}
