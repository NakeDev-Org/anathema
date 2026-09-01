using UnityEngine;

namespace NakeDev.Player
{
    public enum GroundSlidePhase { None, Start, Loop, End }

    public partial class PlayerLocomotion2D
    {
        private Vector2 _standingColliderSize;
        private Vector2 _standingColliderOffset;
        private CapsuleDirection2D _standingColliderDirection;
        private float _slidePhaseTimer;
        private float _slideMinimumTimer;
        private float _slideCooldownTimer;
        private float _slideDirection;
        private float _slideCurrentSpeed;

        public GroundSlidePhase SlidePhase { get; private set; }
        public bool IsSliding => SlidePhase != GroundSlidePhase.None;

        private void InitializeGroundSlide()
        {
            _standingColliderSize = _capsuleCollider.size;
            _standingColliderOffset = _capsuleCollider.offset;
            _standingColliderDirection = _capsuleCollider.direction;
        }

        private void TryStartGroundSlide()
        {
            if (!_config.GroundSlideEnabled || _slideCooldownTimer > 0f || !IsGrounded || IsSliding)
                return;

            float directionSource = Mathf.Abs(_rb.linearVelocity.x) > 0.1f ? _rb.linearVelocity.x : _input.MoveInput.x;
            if (Mathf.Abs(directionSource) <= 0.1f) return;

            _slideDirection = Mathf.Sign(directionSource);
            _slideCurrentSpeed = _config.GroundSlideSpeed;
            _slidePhaseTimer = _config.GroundSlideStartDuration;
            _slideMinimumTimer = _config.GroundSlideMinimumDuration;
            SlidePhase = GroundSlidePhase.Start;
            ApplySlideCollider();
        }

        private void UpdateGroundSlideState()
        {
            if (!IsSliding) return;

            if (!IsGrounded)
            {
                CancelGroundSlide();
            }
            else if (SlidePhase == GroundSlidePhase.Start && _slidePhaseTimer <= 0f)
            {
                SlidePhase = GroundSlidePhase.Loop;
            }
            else if (SlidePhase == GroundSlidePhase.Loop &&
                     _slideMinimumTimer <= 0f &&
                     CanRestoreStandingCollider())
            {
                BeginGroundSlideEnd();
            }
            else if (SlidePhase == GroundSlidePhase.End && _slidePhaseTimer <= 0f)
            {
                CompleteGroundSlide();
            }
        }

        private void BeginGroundSlideEnd()
        {
            //RestoreStandingCollider();
            SlidePhase = GroundSlidePhase.End;
            _slidePhaseTimer = _config.GroundSlideEndDuration;
        }

        private void CompleteGroundSlide()
        {
            if (!CanRestoreStandingCollider()) return;

            RestoreStandingCollider();
            
            SlidePhase = GroundSlidePhase.None;
            _slideCooldownTimer = _config.GroundSlideCooldown;
        }

        private void CancelGroundSlide()
        {
            if (!CanRestoreStandingCollider()) return;

            RestoreStandingCollider();
            SlidePhase = GroundSlidePhase.None;
            _slideCooldownTimer = _config.GroundSlideCooldown;
        }

        private bool TryExitSlideForJump()
        {
            if (!CanRestoreStandingCollider()) return false;

            SlidePhase = GroundSlidePhase.None;
            _slideCooldownTimer = Mathf.Max(
                _slideCooldownTimer,
                _config.GroundSlideCooldown);
            RestoreStandingCollider();
            return true;
        }

        private bool ApplyDashHorizontalMovement()
        {
            if (IsAerialDashing)
            {
                _rb.linearVelocity = new Vector2(
                    _aerialDashDirection * _config.AerialDashSpeed,
                    0f);
                return true;
            }

            if (!IsSliding) return false;

            _rb.linearVelocity = new Vector2(_slideDirection * _slideCurrentSpeed, _rb.linearVelocity.y);

            float targetSpeed = SlidePhase == GroundSlidePhase.End ? 0f : _config.GroundSlideMinimumSpeed;

            _slideCurrentSpeed = Mathf.MoveTowards(_slideCurrentSpeed, targetSpeed, _config.GroundSlideDeceleration * Time.fixedDeltaTime);

            return true;
        }

        private void TickGroundSlideTimers()
        {
            if (_slidePhaseTimer > 0f)
                _slidePhaseTimer -= Time.fixedDeltaTime;
            if (_slideMinimumTimer > 0f)
                _slideMinimumTimer -= Time.fixedDeltaTime;
            if (_slideCooldownTimer > 0f)
                _slideCooldownTimer -= Time.fixedDeltaTime;
        }

        private void ApplySlideCollider()
        {
            float slideHeight = Mathf.Clamp(_config.GroundSlideColliderHeight, _standingColliderSize.x, _standingColliderSize.y); 
            float slideWidth = _config.GroundSlideColliderWidth;

            float heightDifference = _standingColliderSize.y - slideHeight;
            
            _capsuleCollider.direction = CapsuleDirection2D.Horizontal;
            _capsuleCollider.size = new Vector2(slideWidth, slideHeight);
            _capsuleCollider.offset = new Vector2(_standingColliderOffset.x, _standingColliderOffset.y - heightDifference * 0.5f);
        }

        private bool CanRestoreStandingCollider()
        {
            const float clearanceSkin = 0.02f;
            Vector3 scale = transform.lossyScale;
            Vector2 clearanceSize = new Vector2(Mathf.Max(0.01f, _standingColliderSize.x - clearanceSkin * 2f), Mathf.Max(0.01f, _standingColliderSize.y - clearanceSkin * 2f));
            Vector2 worldSize = new Vector2(clearanceSize.x * Mathf.Abs(scale.x), clearanceSize.y * Mathf.Abs(scale.y));
            Vector2 worldCenter = transform.TransformPoint(_standingColliderOffset);

            Collider2D obstruction = Physics2D.OverlapCapsule(worldCenter, worldSize, _standingColliderDirection, transform.eulerAngles.z,_config.GroundLayerMask);
            return obstruction == null;
        }

        private void RestoreStandingCollider()
        {
            _capsuleCollider.direction = _standingColliderDirection;
            _capsuleCollider.size = _standingColliderSize;
            _capsuleCollider.offset = _standingColliderOffset;
        }
    }
}
