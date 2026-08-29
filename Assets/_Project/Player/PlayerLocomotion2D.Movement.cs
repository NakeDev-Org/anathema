using UnityEngine;

namespace NakeDev.Player
{
    public partial class PlayerLocomotion2D
    {
        private void TickTimers()
        {
            if (!IsGrounded)
                _coyoteTimer -= Time.deltaTime;
            if (_jumpBufferTimer > 0f)
                _jumpBufferTimer -= Time.deltaTime;
            if (_wallJumpControlLockTimer > 0f)
                _wallJumpControlLockTimer -= Time.deltaTime;
            if (_wallJumpBufferTimer > 0f)
                _wallJumpBufferTimer -= Time.deltaTime;
            if (_extraJumpCooldownTimer > 0f)
                _extraJumpCooldownTimer -= Time.deltaTime;

            TickDashTimers();
        }

        private void ApplyHorizontalMovement()
        {
            if (_wallJumpControlLockTimer > 0f) return;
            if (ApplyDashHorizontalMovement()) return;

            float x = _input != null ? _input.MoveInput.x : 0f;
            float targetSpeed = x * _config.MoveSpeed;
            float currentSpeed = _rb.linearVelocity.x;
            float rate = Mathf.Abs(targetSpeed) > 0.01f
                ? _config.Acceleration
                : _config.Deceleration;
            float newSpeed = Mathf.MoveTowards(
                currentSpeed,
                targetSpeed,
                rate * Time.fixedDeltaTime);

            _rb.linearVelocity = new Vector2(newSpeed, _rb.linearVelocity.y);
        }

        private void ApplyFallGravity()
        {
            if (IsAerialDashing)
            {
                _rb.gravityScale = 0f;
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
                return;
            }

            _rb.gravityScale = _rb.linearVelocity.y < 0f
                ? _defaultGravityScale * _config.FallGravityMultiplier
                : _defaultGravityScale;

            float verticalSpeed = Mathf.Max(_rb.linearVelocity.y, -_config.MaxFallSpeed);

            if (IsWallSliding && verticalSpeed <= 0f)
            {
                verticalSpeed = Mathf.MoveTowards(
                    verticalSpeed,
                    -_config.WallSlideSpeed,
                    _config.WallSlideAcceleration * Time.fixedDeltaTime);
            }

            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, verticalSpeed);
        }
    }
}
