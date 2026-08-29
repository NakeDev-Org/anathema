using UnityEngine;

namespace NakeDev.Player
{
    public partial class PlayerAnimationController
    {
        private void InitializeStateHashes()
        {
            _idleHash = Animator.StringToHash(_idleState);
            _runHash = Animator.StringToHash(_runState);
            _walkHash = Animator.StringToHash(_walkState);
            _jumpHash = Animator.StringToHash(_jumpState);
            _jumpLoopHash = Animator.StringToHash(_jumpLoopState);
            _doubleJumpHash = Animator.StringToHash(_doubleJumpState);
            _fallHash = Animator.StringToHash(_fallState);
            _fallLoopHash = Animator.StringToHash(_fallLoopState);
            _wallSlideHash = Animator.StringToHash(_wallSlideState);
            _wallJumpHash = Animator.StringToHash(_wallJumpState);
            _landingHash = Animator.StringToHash(_landingState);
            _slideStartHash = Animator.StringToHash(_slideStartState);
            _slideLoopHash = Animator.StringToHash(_slideLoopState);
            _slideEndHash = Animator.StringToHash(_slideEndState);
            _aerialDashHash = Animator.StringToHash(_aerialDashState);
        }

        private AnimState DetermineState()
        {
            if (!_locomotion.IsGrounded)
            {
                if (_locomotion.IsAerialDashing)
                    return AnimState.AerialDash;

                if (_locomotion.IsWallSliding)
                    return AnimState.WallSlide;

                if (_locomotion.Velocity.y > 0f)
                {
                    if (_currentAnimState == AnimState.DoubleJump ||
                        _currentAnimState == AnimState.WallJump)
                    {
                        return _currentAnimState.Value;
                    }

                    if ((_currentAnimState == AnimState.Jump ||
                         _currentAnimState == AnimState.JumpLoop) &&
                        _jumpIntroTimer <= 0f)
                    {
                        return AnimState.JumpLoop;
                    }

                    return AnimState.Jump;
                }

                if ((_currentAnimState == AnimState.Fall ||
                     _currentAnimState == AnimState.FallLoop) &&
                    _fallIntroTimer <= 0f)
                {
                    return AnimState.FallLoop;
                }

                return AnimState.Fall;
            }

            if (_locomotion.IsSliding)
            {
                switch (_locomotion.SlidePhase)
                {
                    case GroundSlidePhase.Start: return AnimState.SlideStart;
                    case GroundSlidePhase.Loop: return AnimState.SlideLoop;
                    case GroundSlidePhase.End: return AnimState.SlideEnd;
                }
            }

            if (_locomotion.IsRunning)
                return AnimState.Run;

            return Mathf.Abs(_locomotion.Velocity.x) > 0f
                ? AnimState.Walk
                : AnimState.Idle;
        }

        private int HashFor(AnimState state)
        {
            switch (state)
            {
                case AnimState.Idle: return _idleHash;
                case AnimState.Walk: return _walkHash;
                case AnimState.Run: return _runHash;
                case AnimState.SlideStart: return _slideStartHash;
                case AnimState.SlideLoop: return _slideLoopHash;
                case AnimState.SlideEnd: return _slideEndHash;
                case AnimState.AerialDash: return _aerialDashHash;
                case AnimState.Jump: return _jumpHash;
                case AnimState.JumpLoop: return _jumpLoopHash;
                case AnimState.DoubleJump: return _doubleJumpHash;
                case AnimState.Fall: return _fallHash;
                case AnimState.FallLoop: return _fallLoopHash;
                case AnimState.WallSlide: return _wallSlideHash;
                case AnimState.WallJump: return _wallJumpHash;
                case AnimState.Landing: return _landingHash;
                default: return _idleHash;
            }
        }
    }
}
