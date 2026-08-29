using UnityEngine;

namespace NakeDev.Player
{
    public partial class PlayerLocomotion2D
    {
        private float _aerialDashTimer;
        private float _aerialDashDirection;
        private int _aerialDashesRemaining;

        public bool IsAerialDashing { get; private set; }

        private void InitializeDash()
        {
            InitializeGroundSlide();
            _aerialDashesRemaining = _config.MaxAerialDashes;
        }

        private void HandleDashPressed()
        {
            if (IsSliding || IsAerialDashing) return;

            if (IsGrounded)
            {
                if (IsRunning)
                    TryStartGroundSlide();
                return;
            }

            TryStartAerialDash();
        }

        private void TryStartAerialDash()
        {
            if (!_config.AerialDashEnabled || _aerialDashesRemaining <= 0)
                return;

            float directionSource = Mathf.Abs(_input.MoveInput.x) > 0.1f
                ? _input.MoveInput.x
                : _rb.linearVelocity.x;
            if (Mathf.Abs(directionSource) <= 0.1f) return;

            float dashDirection = Mathf.Sign(directionSource);
            if (IsWallSliding && dashDirection == _wallDirection) return;

            _aerialDashDirection = dashDirection;
            _aerialDashTimer = _config.AerialDashDuration;
            _aerialDashesRemaining--;
            IsAerialDashing = true;
            IsWallSliding = false;
            _rb.gravityScale = 0f;
            _rb.linearVelocity = new Vector2(
                _aerialDashDirection * _config.AerialDashSpeed,
                0f);
        }

        private void UpdateDashState()
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
        }

        private void TickDashTimers()
        {
            TickGroundSlideTimers();
            if (_aerialDashTimer > 0f)
                _aerialDashTimer -= Time.deltaTime;
        }
    }
}
