using UnityEngine;

namespace NakeDev.Player
{
    public partial class PlayerLocomotion2D
    {
        private const float WallProbeSkin = -0.1f;
        private float _wallSlideEntryTimer;
        private float _wallSlideContactGraceTimer;

        private struct WallProbeResult
        {
            public bool Upper;
            public bool Middle;
            public bool Lower;

            public bool HasAll => Upper && Middle && Lower;
            public bool HasAdjacentPair => (Upper && Middle) || (Middle && Lower);
        }

        private void ApplyCornerCorrection()
        {
            if (_rb.linearVelocity.y <= 0f || _config.CornerCorrectionDistance <= 0f)
                return;

            Bounds bounds = _collider.bounds;
            
            float checkDistance = Mathf.Max(_config.CornerCheckDistance, _rb.linearVelocity.y * Time.fixedDeltaTime);
            const float skin = 0.01f;
            
            Vector2 leftOrigin = new Vector2(bounds.min.x + skin, bounds.max.y + skin);
            Vector2 rightOrigin = new Vector2(bounds.max.x - skin, bounds.max.y + skin);

            bool leftBlocked = Physics2D.Raycast(leftOrigin, Vector2.up, checkDistance, _config.WallLayerMask);
            bool rightBlocked = Physics2D.Raycast(rightOrigin, Vector2.up, checkDistance, _config.WallLayerMask);

            if (leftBlocked == rightBlocked) return;

            float direction = leftBlocked ? 1f : -1f;
            float step = Mathf.Max(0.001f, _config.CornerCorrectionStep);

            for (float distance = step;
                 distance <= _config.CornerCorrectionDistance + 0.0001f;
                 distance += step)
            {
                Vector2 offset = Vector2.right * direction * distance;
                Vector2 blockedOrigin = leftBlocked ? leftOrigin : rightOrigin;

                if (Physics2D.Raycast(blockedOrigin + offset, Vector2.up, checkDistance, _config.WallLayerMask))
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
            Vector2 origin = _groundCheckPoint != null ? (Vector2)_groundCheckPoint.position : (Vector2)transform.position;

            bool overlapsGround = Physics2D.OverlapBox(origin, _config.GroundCheckSize, 0f, _config.WallLayerMask);

            // Ignora o overlap enquanto o player ainda está subindo (ex.: no primeiro
            // passo físico após o salto, antes de sair do raio do ground check).
            // Sem isso, IsGrounded oscila por 1 frame e reseta contadores/animação.
            IsGrounded = overlapsGround && _rb.linearVelocity.y <= 0.01f;

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
            WallProbeResult rightProbes = CheckWallProbes(bounds, Vector2.right);
            WallProbeResult leftProbes = CheckWallProbes(bounds, Vector2.left);

            float x = _input != null ? _input.MoveInput.x : 0f;
            int detectedWallDirection = 0;

            if (wasWallSliding)
            {
                bool pressingTowardActiveWall =
                    (_wallDirection > 0 && x > 0.1f) ||
                    (_wallDirection < 0 && x < -0.1f);
                WallProbeResult activeProbes = _wallDirection > 0 ? rightProbes : leftProbes;

                if (pressingTowardActiveWall && activeProbes.HasAll)
                {
                    _wallSlideContactGraceTimer = _config.WallSlideContactGraceTime;
                    detectedWallDirection = _wallDirection;
                }
                else if (pressingTowardActiveWall && activeProbes.HasAdjacentPair && _wallSlideContactGraceTimer > 0f)
                {
                    _wallSlideContactGraceTimer = Mathf.Max(0f, _wallSlideContactGraceTimer - Time.fixedDeltaTime);

                    if (_wallSlideContactGraceTimer > 0f)
                        detectedWallDirection = _wallDirection;
                }
            }
            else
            {
                if (rightProbes.HasAll && x > 0.1f)
                    detectedWallDirection = 1;
                else if (leftProbes.HasAll && x < -0.1f)
                    detectedWallDirection = -1;

                if (detectedWallDirection != 0)
                    _wallSlideContactGraceTimer = _config.WallSlideContactGraceTime;
            }

            if (detectedWallDirection != 0)
            {
                _wallDirection = detectedWallDirection;
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

        private WallProbeResult CheckWallProbes(Bounds bounds, Vector2 direction)
        {
            GetWallProbeOrigins(bounds, direction, out Vector2 upperOrigin, out Vector2 middleOrigin, out Vector2 lowerOrigin);

            float castDistance = _config.WallCheckDistance + WallProbeSkin;

            return new WallProbeResult
            {
                Upper = Physics2D.Raycast(upperOrigin, direction, castDistance, _config.WallLayerMask),
                Middle = Physics2D.Raycast(middleOrigin, direction, castDistance, _config.WallLayerMask),
                Lower = Physics2D.Raycast(lowerOrigin, direction, castDistance, _config.WallLayerMask)
            };
        }

        private void GetWallProbeOrigins(Bounds bounds, Vector2 direction, out Vector2 upperOrigin, out Vector2 middleOrigin, out Vector2 lowerOrigin)
        {
            float verticalOffset = bounds.extents.y * _config.WallCheckVerticalOffset;
            float sideX = direction.x > 0f ? bounds.max.x + WallProbeSkin : bounds.min.x - WallProbeSkin;

            upperOrigin = new Vector2(sideX, bounds.center.y + verticalOffset);
            middleOrigin = new Vector2(sideX, bounds.center.y);
            lowerOrigin = new Vector2(sideX, bounds.center.y - verticalOffset);
        }

        private void StopWallSlide()
        {
            IsWallSliding = false;
            _wallDirection = 0;
            _wallSlideEntryTimer = 0f;
            _wallSlideContactGraceTimer = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            if (_config == null) return;

            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Vector3 origin = _groundCheckPoint != null ? _groundCheckPoint.position : transform.position;
            Gizmos.DrawWireCube(origin, _config.GroundCheckSize);

            Collider2D targetCollider = _collider != null ? _collider : GetComponent<Collider2D>();

            if (targetCollider == null) return;

            Bounds bounds = targetCollider.bounds;
            DrawWallProbes(bounds, Vector2.right);
            DrawWallProbes(bounds, Vector2.left);
        }

        private void DrawWallProbes(Bounds bounds, Vector2 direction)
        {
            GetWallProbeOrigins(bounds, direction, out Vector2 upperOrigin, out Vector2 middleOrigin, out Vector2 lowerOrigin);

            WallProbeResult probes = CheckWallProbes(bounds, direction);

            DrawWallProbe(upperOrigin, direction, probes.Upper);
            DrawWallProbe(middleOrigin, direction, probes.Middle);
            DrawWallProbe(lowerOrigin, direction, probes.Lower);
        }

        private void DrawWallProbe(Vector2 origin, Vector2 direction, bool isTouching)
        {
            Gizmos.color = isTouching ? Color.green : Color.red;
            float castDistance = _config.WallCheckDistance + WallProbeSkin;
            Gizmos.DrawLine(origin, origin + direction * castDistance);
        }
    }
}
