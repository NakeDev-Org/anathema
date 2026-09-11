using UnityEngine;

namespace NakeDev.Player
{
    public partial class PlayerLocomotion2D
    {
        private float _aerialDashTimer;
        private float _aerialDashDirection;
        private int _aerialDashesRemaining;
        public bool IsAerialDashing { get; private set; }
        public float AerialDashDirection => _aerialDashDirection;

        private void InitializeAerialDash()
        {
            InitializeGroundSlide();
            _aerialDashesRemaining = _config.MaxAerialDashes;
        }

        private void HandleDashPressed()
        {
            if (IsSliding || IsAerialDashing || IsGroundDashing) return;

            if (IsGrounded)
                TryStartGroundDash();
            else
                TryStartAerialDash();
        }

        private void TryStartAerialDash()
        {
            if (!_config.AerialDashEnabled || _aerialDashesRemaining <= 0 || IsGrounded || IsAerialDashing)
                return;

            //float directionSource = Mathf.Abs(_input.MoveInput.x) > 0.1f ? _input.MoveInput.x : _rb.linearVelocity.x;
            float directionSource = Mathf.Abs(_input.MoveInput.x) > 0.1f ? _input.MoveInput.x : _lastMoveInputX;
            if (Mathf.Abs(directionSource) <= 0.1f) return;

            float dashDirection = Mathf.Sign(directionSource);
            if (IsWallSliding && dashDirection == _wallDirection) return;

            _aerialDashDirection = dashDirection;
            _aerialDashTimer = _config.AerialDashDuration;
            _aerialDashesRemaining--;
            IsAerialDashing = true;
            StopWallSlide();
            _rb.gravityScale = 0f;
            _rb.linearVelocity = new Vector2(_aerialDashDirection * _config.AerialDashSpeed, 0f);
            //ApplyDashCollider();
        }

        private void UpdateAerialDashState()
        {
            UpdateGroundSlideState();

            if (IsGrounded)
            {
                if (IsAerialDashing)
                    EndAerialDash();
                _aerialDashesRemaining = _config.MaxAerialDashes;
            }
            else if (IsAerialDashing && _aerialDashTimer <= 0f)
            {
                EndAerialDash();
            }
        }

        private void EndAerialDash()
        {
            IsAerialDashing = false;
            _rb.gravityScale = _defaultGravityScale;
            RestoreStandingCollider();
        }

        private void TickAerialDashTimers()
        {
            if (_aerialDashTimer > 0f)
                _aerialDashTimer -= Time.fixedDeltaTime;
        }

        //Vou manter o ApplyDashCollider comentado por enquanto, pois ele está causando bug com passagens baixas
        //e acho que acabou funcionando como uma correção para o sprite e não da mecânica do game em si.
        //Faz sentido termos uma mudança no collider quando é o ground slide, mas no dash acho que não deveria ter.
        /*
        private void ApplyDashCollider()
        {
            //float dashHeight = Mathf.Clamp(_config.DashSlideColliderHeigth, _standingColliderSize.x, _standingColliderSize.y); 
            float dashHeight = _config.DashSlideColliderHeigth; 
            float dashWidth = _config.DashSlideColliderWidth;

            float heightDifference = _standingColliderSize.y - dashHeight;
            
            //_capsuleCollider.direction = CapsuleDirection2D.Horizontal;
            _capsuleCollider.size = new Vector2(dashWidth, dashHeight);
            _capsuleCollider.offset = new Vector2(_standingColliderOffset.x, _standingColliderOffset.y - heightDifference * 0.5f);
        }
        */
    }
}
