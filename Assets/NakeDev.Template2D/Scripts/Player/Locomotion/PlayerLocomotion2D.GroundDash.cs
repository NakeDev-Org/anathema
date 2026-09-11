using UnityEngine;

namespace NakeDev.Player
{
    public partial class PlayerLocomotion2D
    {
        private float _groundDashTimer;
        private float _groundDashCooldownTimer;
        private float _groundDashDirection;
        public bool IsGroundDashing { get; private set; }
        public float GroundDashDirection => _groundDashDirection;

        private void InitializeDash()
        {
            InitializeGroundSlide();
            _aerialDashesRemaining = _config.MaxAerialDashes;
        }

        private void TryStartGroundDash()
        {
            if (!_config.GroundDashEnabled || !IsGrounded || IsSliding || IsAerialDashing || IsGroundDashing || _groundDashCooldownTimer > 0f)
                return;

            //float directionSource = Mathf.Abs(_input.MoveInput.x) > 0.1f ? _input.MoveInput.x : _rb.linearVelocity.x;
            float directionSource = Mathf.Abs(_input.MoveInput.x) > 0.1f ? _input.MoveInput.x : _lastMoveInputX;
            if (Mathf.Abs(directionSource) <= 0.1f) return;

            float dashDirection = Mathf.Sign(directionSource);
            if (IsWallSliding && dashDirection == _wallDirection) return;

            _groundDashDirection = dashDirection;
            _groundDashTimer = _config.GroundDashDuration;
            IsGroundDashing = true;
            StopWallSlide();
            _rb.gravityScale = 0f;
            _rb.linearVelocity = new Vector2(_groundDashDirection * _config.GroundDashSpeed, 0f);
        }

        private void UpdateGroundDashState()
        {
            if (IsGroundDashing && _groundDashTimer <= 0f) 
                EndGroundDash();
        }

        private void EndGroundDash()
        {
            if (!IsGroundDashing) return;

            IsGroundDashing = false;
            _groundDashTimer = 0f;
            _groundDashCooldownTimer = Mathf.Max(0f, _config.GroundDashCooldown);
            _rb.gravityScale = _defaultGravityScale;
        }

        private void TickGroundDashTimers()
        {
            if (_groundDashTimer > 0f)
                _groundDashTimer -= Time.fixedDeltaTime;

            if (_groundDashCooldownTimer > 0f)
            {
                _groundDashCooldownTimer = Mathf.Max(0f, _groundDashCooldownTimer - Time.fixedDeltaTime);
            }
        }
    }
}
