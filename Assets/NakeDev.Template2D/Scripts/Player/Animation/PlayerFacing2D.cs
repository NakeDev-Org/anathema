using NakeDev.Attributes;
using UnityEngine;

namespace NakeDev.Player
{
    /// <summary>
    /// Vira o sprite pra esquerda/direita conforme o input de movimento. Responsabilidade
    /// isolada da locomoção (Regra 1 — CONVENTIONS.md): PlayerLocomotion2D não sabe nada
    /// sobre orientação visual.
    /// </summary>
    public class PlayerFacing2D : MonoBehaviour
    {
        [InspectorLine("References")]
        [Tooltip("Referência opcional; se vazia, gira o próprio transform deste GameObject.")]
        [SerializeField] private Transform _visualRoot;

        private IMovementInput _input;
        private PlayerLocomotion2D _locomotion;

        [InspectorLine("Settings")]
        [Tooltip("Posição default.")]
        [SerializeField] private bool _facingRight = true;

        public bool IsFacingRight => _facingRight;

        private void Awake()
        {
            _input = GetComponent<IMovementInput>();
            _locomotion = GetComponent<PlayerLocomotion2D>();
            if (_visualRoot == null)
                _visualRoot = transform;
        }

        private void Update()
        {
            if (_locomotion != null && _locomotion.IsGroundDashing)
            {
                FaceDirection(_locomotion.GroundDashDirection);
                return;
            }
            
            if (_locomotion != null &&_locomotion.IsAerialDashing) 
            {
                FaceDirection(_locomotion.AerialDashDirection);
                return;
            }

            if (_locomotion != null && _locomotion.IsSliding) 
            {
                FaceDirection(_locomotion.SlidingDirection);
                return;
            }

            if (_input == null) return;

            float x = _input.MoveInput.x;
            if (x > 0.01f && !_facingRight)
                Flip();
            else if (x < -0.01f && _facingRight)
                Flip();
        }

        //deixei genérico para usar em outras animações, como ground slide
        private void FaceDirection(float _direction)
        {
            if (Mathf.Abs(_direction) < 0.01f) return;

            bool shouldFaceRight = _direction > 0.1f;
            if (shouldFaceRight && !_facingRight) 
                Flip();
            else if (!shouldFaceRight && _facingRight) 
                Flip();
        }

        private void Flip()
        {
            _facingRight = !_facingRight;
            Vector3 scale = _visualRoot.localScale;
            scale.x *= -1f;
            _visualRoot.localScale = scale;
        }
    }
}
