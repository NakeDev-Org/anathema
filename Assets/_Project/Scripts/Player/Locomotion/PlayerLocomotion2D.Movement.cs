using UnityEngine;

namespace NakeDev.Player
{
    public partial class PlayerLocomotion2D
    {
        private void TickTimers()
        {
            if (!IsGrounded)
                _coyoteTimer -= Time.fixedDeltaTime;
            if (_jumpBufferTimer > 0f)
                _jumpBufferTimer -= Time.fixedDeltaTime;
            if (_wallJumpControlLockTimer > 0f)
                _wallJumpControlLockTimer -= Time.fixedDeltaTime;
            if (_wallJumpBufferTimer > 0f)
                _wallJumpBufferTimer -= Time.fixedDeltaTime;
            if (_extraJumpCooldownTimer > 0f)
                _extraJumpCooldownTimer -= Time.fixedDeltaTime;
            if (IsWallSliding && _rb.linearVelocity.y <= 0f && _wallSlideEntryTimer > 0f)
                _wallSlideEntryTimer -= Time.fixedDeltaTime;

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

            float verticalSpeed = Mathf.Max(_rb.linearVelocity.y, -_config.MaxFallSpeed);
            float x = _input != null ? _input.MoveInput.x : 0f;
            bool idleOnGround = IsGrounded && !IsWallSliding && !IsSliding && !IsJumping &&
                Mathf.Abs(x) < 0.01f && Mathf.Abs(verticalSpeed) < 1f && _wallJumpControlLockTimer <= 0f;

            if (IsWallSliding && verticalSpeed <= 0f)
            {
                _rb.gravityScale = 0f;
                float targetSpeed = _wallSlideEntryTimer > 0f
                    ? -_config.WallSlideEntrySpeed
                    : -_config.WallSlideSpeed;
                verticalSpeed = Mathf.MoveTowards(
                    verticalSpeed,
                    targetSpeed,
                    _config.WallSlideAcceleration * Time.fixedDeltaTime);
            }
            else if (idleOnGround)
            {
                // Sem fricção (PlayerNoFriction), gravidade sozinha em piso inclinado
                // injeta velocidade tangencial a cada passo físico e o player escorrega
                // mesmo parado. Zerar a gravidade enquanto parado no chão elimina isso.
                _rb.gravityScale = 0f;
                verticalSpeed = 0f;
            }
            else
            {
                _rb.gravityScale = verticalSpeed < 0f
                    ? _defaultGravityScale * _config.FallGravityMultiplier
                    : _defaultGravityScale;
            }

            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, verticalSpeed);
        }
    }
}
