using UnityEngine;

namespace NakeDev.Player
{
    public partial class PlayerLocomotion2D
    {
        private void OnJumpPressed()
        {
            _jumpBufferTimer = _config.JumpBufferTime;
        }

        private void HandleJumpReleased()
        {
            if (!IsJumping || _rb.linearVelocity.y <= 0f) return;

            _rb.linearVelocity = new Vector2(
                _rb.linearVelocity.x,
                _rb.linearVelocity.y * _config.JumpCutMultiplier);
        }

        private void TryConsumeJump()
        {
            if (_jumpBufferTimer <= 0f) return;
            if (IsAerialDashing) return;
            if (IsSliding && !TryExitSlideForJump()) return;

            bool canWallJump =
                _config.WallJumpEnabled &&
                (IsWallSliding || _wallJumpBufferTimer > 0f) &&
                _wallJumpsRemaining > 0;

            if (canWallJump)
            {
                int jumpWallDirection = _wallDirection != 0
                    ? _wallDirection
                    : _lastWallDirection;
                float pushDirection = -jumpWallDirection;
                _rb.linearVelocity = new Vector2(
                    pushDirection * _config.WallJumpForceX,
                    _config.WallJumpForceY);
                OnJumpPerformed?.Invoke(JumpType.Wall);

                _wallJumpsRemaining--;
                if (_config.ResetExtraJumpsOnWallJump)
                    _extraJumpsRemaining = _config.MaxExtraJumps;

                if (_config.ResetAerialDashesOnWallJump)
                    _aerialDashesRemaining = _config.MaxAerialDashes;

                _extraJumpCooldownTimer = _config.ExtraJumpCooldown;
                _wallJumpControlLockTimer = _config.WallJumpControlLockTime;
                _wallJumpBufferTimer = 0f;
                _lastWallDirection = 0;
                _coyoteTimer = 0f;
                StopWallSlide();
                IsGrounded = false;
                IsJumping = true;
                _jumpBufferTimer = 0f;
                return;
            }

            if (_coyoteTimer > 0f)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _config.JumpForce);
                OnJumpPerformed?.Invoke(JumpType.Ground);
                _extraJumpCooldownTimer = _config.ExtraJumpCooldown;
                _coyoteTimer = 0f;
                IsGrounded = false;
                IsJumping = true;
            }
            else if (_extraJumpsRemaining > 0 && _extraJumpCooldownTimer <= 0f && !IsWallSliding)
            {
                _extraJumpsRemaining--;
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _config.ExtraJumpForce);
                OnJumpPerformed?.Invoke(JumpType.Extra);
                _extraJumpCooldownTimer = _config.ExtraJumpCooldown;
                IsJumping = true;
            }
            else
            {
                return;
            }

            _jumpBufferTimer = 0f;
        }
    }
}
