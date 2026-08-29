using UnityEngine;

namespace NakeDev.Player
{
    public partial class PlayerLocomotion2D
    {
        private float _wallSlideEntryTimer;

        private void ApplyCornerCorrection()
        {
            if (_rb.linearVelocity.y <= 0f || _config.CornerCorrectionDistance <= 0f)
                return;

            Bounds bounds = _collider.bounds;
            float checkDistance = Mathf.Max(
                _config.CornerCheckDistance,
                _rb.linearVelocity.y * Time.fixedDeltaTime);
            const float skin = 0.01f;
            Vector2 leftOrigin = new Vector2(bounds.min.x + skin, bounds.max.y + skin);
            Vector2 rightOrigin = new Vector2(bounds.max.x - skin, bounds.max.y + skin);
            bool leftBlocked = Physics2D.Raycast(
                leftOrigin, Vector2.up, checkDistance, _config.GroundLayerMask);
            bool rightBlocked = Physics2D.Raycast(
                rightOrigin, Vector2.up, checkDistance, _config.GroundLayerMask);

            if (leftBlocked == rightBlocked) return;

            float direction = leftBlocked ? 1f : -1f;
            float step = Mathf.Max(0.001f, _config.CornerCorrectionStep);

            for (float distance = step;
                 distance <= _config.CornerCorrectionDistance + 0.0001f;
                 distance += step)
            {
                Vector2 offset = Vector2.right * direction * distance;
                Vector2 blockedOrigin = leftBlocked ? leftOrigin : rightOrigin;

                if (Physics2D.Raycast(
                    blockedOrigin + offset,
                    Vector2.up,
                    checkDistance,
                    _config.GroundLayerMask))
                {
                    continue;
                }

                _rb.position += offset;
                return;
            }
        }

        private void GroundCheck()
        {
            bool wasGrounded = IsGrounded;
            float landingSpeed = Mathf.Max(0f, -_rb.linearVelocity.y);
            Vector2 origin = _groundCheckPoint != null
                ? (Vector2)_groundCheckPoint.position
                : (Vector2)transform.position;

            IsGrounded = Physics2D.OverlapCircle(
                origin,
                _config.GroundCheckRadius,
                _config.GroundLayerMask);

            if (_groundStateInitialized && !wasGrounded && IsGrounded)
                OnLanded?.Invoke(landingSpeed);

            _groundStateInitialized = true;
            if (!IsGrounded) return;

            _coyoteTimer = _config.CoyoteTime;
            _wallJumpBufferTimer = 0f;
            _lastWallDirection = 0;
            _extraJumpsRemaining = _config.MaxExtraJumps;
            _wallJumpsRemaining = _config.MaxWallJumps;
            IsJumping = false;
        }

        private void WallCheck()
        {
            bool wasWallSliding = IsWallSliding;

            if (IsGrounded || _wallJumpControlLockTimer > 0f)
            {
                StopWallSlide();
                return;
            }

            Bounds bounds = _collider.bounds;
            const float skin = 0.01f;
            float verticalOffset = bounds.extents.y * _config.WallCheckVerticalOffset;
            float upperY = bounds.center.y + verticalOffset;
            float lowerY = bounds.center.y - verticalOffset;
            Vector2 upperRightOrigin = new Vector2(bounds.max.x + skin, upperY);
            Vector2 lowerRightOrigin = new Vector2(bounds.max.x + skin, lowerY);
            Vector2 upperLeftOrigin = new Vector2(bounds.min.x - skin, upperY);
            Vector2 lowerLeftOrigin = new Vector2(bounds.min.x - skin, lowerY);

            bool touchingRight =
                Physics2D.Raycast(upperRightOrigin, Vector2.right, _config.WallCheckDistance, _config.GroundLayerMask) &&
                Physics2D.Raycast(lowerRightOrigin, Vector2.right, _config.WallCheckDistance, _config.GroundLayerMask);
            bool touchingLeft =
                Physics2D.Raycast(upperLeftOrigin, Vector2.left, _config.WallCheckDistance, _config.GroundLayerMask) &&
                Physics2D.Raycast(lowerLeftOrigin, Vector2.left, _config.WallCheckDistance, _config.GroundLayerMask);

            float x = _input != null ? _input.MoveInput.x : 0f;
            bool wallOnRight = touchingRight && x > 0.1f;
            bool wallOnLeft = touchingLeft && x < -0.1f;

            if (wallOnRight || wallOnLeft)
            {
                _wallDirection = wallOnRight ? 1 : -1;
                IsWallSliding = _config.WallSlideEnabled;

                if (IsWallSliding && !wasWallSliding)
                {
                    _wallSlideEntryTimer = _config.WallSlideEntryDuration;

                    if (_wallSlideEntryTimer > 0f &&
                        _rb.linearVelocity.y < -_config.WallSlideEntrySpeed)
                    {
                        _rb.linearVelocity = new Vector2(
                            _rb.linearVelocity.x,
                        -_config.WallSlideEntrySpeed);
                    }
                }
                else if (!IsWallSliding)
                {
                    _wallSlideEntryTimer = 0f;
                }

                if (_config.WallJumpEnabled)
                {
                    _lastWallDirection = _wallDirection;
                    _wallJumpBufferTimer = _config.WallJumpBufferTime;
                }
                else
                {
                    _wallJumpBufferTimer = 0f;
                    _lastWallDirection = 0;
                }
            }
            else
            {
                StopWallSlide();
            }
        }

        private void StopWallSlide()
        {
            IsWallSliding = false;
            _wallDirection = 0;
            _wallSlideEntryTimer = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            if (_config == null) return;

            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Vector3 origin = _groundCheckPoint != null
                ? _groundCheckPoint.position
                : transform.position;
            Gizmos.DrawWireSphere(origin, _config.GroundCheckRadius);

            if (_collider == null) return;
            Bounds bounds = _collider.bounds;
            const float skin = 0.01f;
            float verticalOffset = bounds.extents.y * _config.WallCheckVerticalOffset;
            float upperY = bounds.center.y + verticalOffset;
            float lowerY = bounds.center.y - verticalOffset;
            Vector3 upperRightOrigin = new Vector3(bounds.max.x + skin, upperY, 0f);
            Vector3 lowerRightOrigin = new Vector3(bounds.max.x + skin, lowerY, 0f);
            Vector3 upperLeftOrigin = new Vector3(bounds.min.x - skin, upperY, 0f);
            Vector3 lowerLeftOrigin = new Vector3(bounds.min.x - skin, lowerY, 0f);

            Gizmos.color = IsWallSliding ? Color.cyan : Color.yellow;
            Gizmos.DrawLine(upperRightOrigin, upperRightOrigin + Vector3.right * _config.WallCheckDistance);
            Gizmos.DrawLine(lowerRightOrigin, lowerRightOrigin + Vector3.right * _config.WallCheckDistance);
            Gizmos.DrawLine(upperLeftOrigin, upperLeftOrigin + Vector3.left * _config.WallCheckDistance);
            Gizmos.DrawLine(lowerLeftOrigin, lowerLeftOrigin + Vector3.left * _config.WallCheckDistance);
        }
    }
}
