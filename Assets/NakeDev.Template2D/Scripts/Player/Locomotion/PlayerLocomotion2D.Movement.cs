using UnityEngine;

namespace NakeDev.Player
{
    public partial class PlayerLocomotion2D
    {
        private float _groundedHorizontalMomentum;
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

            TickGroundSlideTimers();
            TickGroundDashTimers();
            TickAerialDashTimers();
        }

        private void ApplyHorizontalMovement()
        {
            float x = _input != null ? _input.MoveInput.x : 0f;

            if (Mathf.Abs(x) > 0.1f) _lastMoveInputX = Mathf.Sign(x);
            
            if (_wallJumpControlLockTimer > 0f)
            {
                UpdateBurstRunEvent(false);
                return;
            }

            if (ApplyDashHorizontalMovement())
            {
                UpdateBurstRunEvent(false);
                return;
            }

            bool isBurstRunActive = IsGrounded && _config.BurstRunEnabled && WantsToBurstRun && Mathf.Abs(x) > 0.01f;
            UpdateBurstRunEvent(isBurstRunActive);

            float currentSpeed = _rb.linearVelocity.x;
            float maximumSpeed;

            if (IsGrounded)
            {
                bool canUseBurst = _config.BurstRunEnabled && WantsToBurstRun;

                maximumSpeed = canUseBurst ? _config.BurstRunSpeed : _config.MoveSpeed;
            }
            else
            {
                bool groundMomentumWasBurst = Mathf.Abs(_groundedHorizontalMomentum) > _config.MoveSpeed;

                bool stillHasBurstSpeed = Mathf.Abs(currentSpeed) > _config.MoveSpeed;

                bool movingInMomentumDirection = Mathf.Sign(currentSpeed) == Mathf.Sign(_groundedHorizontalMomentum);

                bool inputContinuesMomentum = Mathf.Abs(x) > 0.01f && Mathf.Sign(x) == Mathf.Sign(_groundedHorizontalMomentum);

                bool canPreserveGroundMomentum = groundMomentumWasBurst && stillHasBurstSpeed && movingInMomentumDirection && inputContinuesMomentum;

                maximumSpeed = canPreserveGroundMomentum ? Mathf.Min(Mathf.Abs(currentSpeed), Mathf.Abs(_groundedHorizontalMomentum)) : _config.MoveSpeed;
            }

            float targetSpeed = x * maximumSpeed;

            bool sameDirection = Mathf.Abs(currentSpeed) < 0.01f || Mathf.Sign(currentSpeed) == Mathf.Sign(targetSpeed);
            bool isAccelerating = sameDirection && Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed);

            float rate = isAccelerating ? _config.Acceleration : _config.Deceleration;

            float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);

            _rb.linearVelocity = new Vector2(newSpeed, _rb.linearVelocity.y);

            if (IsGrounded)
                _groundedHorizontalMomentum = newSpeed;
        }

        private void UpdateBurstRunEvent(bool isActive)
        {
            if (isActive && !_wasBurstRunActive)
                OnBurstRunStarted?.Invoke();

            _wasBurstRunActive = isActive;
        }

        private void ApplyFallGravity()
        {
            if (IsAerialDashing || IsGroundDashing)
            {
                _rb.gravityScale = 0f;
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
                return;
            }

            float verticalSpeed = Mathf.Max(_rb.linearVelocity.y, -_config.MaxFallSpeed);
            float x = _input != null ? _input.MoveInput.x : 0f;
            bool idleOnGround = 
                IsGrounded && 
                !IsWallSliding && 
                !IsSliding && 
                !IsJumping &&
                !IsGroundDashing &&
                Mathf.Abs(x) < 0.01f 
                && Mathf.Abs(verticalSpeed) < 1f 
                && _wallJumpControlLockTimer <= 0f;

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
