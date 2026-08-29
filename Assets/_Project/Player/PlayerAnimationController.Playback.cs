using UnityEngine;

namespace NakeDev.Player
{
    public partial class PlayerAnimationController
    {
        private void Update()
        {
            if (_animatorBrain == null || _locomotion == null)
                return;

            _jumpIntroTimer = Mathf.Max(0f, _jumpIntroTimer - Time.deltaTime);
            _fallIntroTimer = Mathf.Max(0f, _fallIntroTimer - Time.deltaTime);

            if (_landingAnimationTimer > 0f)
            {
                _landingAnimationTimer -= Time.deltaTime;

                if (_landingAnimationTimer > 0f)
                    return;
            }

            SetPlayState(DetermineState());
        }

        private void SetPlayState(AnimState desired, bool forceRestart = false)
        {
            if (!forceRestart && desired == _currentAnimState) return;

            AnimState? previous = _currentAnimState;
            _currentAnimState = desired;

            if (desired == AnimState.Jump &&
                (forceRestart || (previous != AnimState.Jump && previous != AnimState.JumpLoop)))
            {
                _jumpIntroTimer = _jumpIntroDuration;
            }

            if (desired == AnimState.Fall &&
                previous != AnimState.Fall && previous != AnimState.FallLoop)
            {
                _fallIntroTimer = _fallIntroDuration;
            }

            float crossfadeTime = desired == AnimState.JumpLoop || desired == AnimState.FallLoop
                ? _airLoopCrossfadeTime
                : _crossfadeTime;

            _animatorBrain.PlayAnimation(HashFor(desired), crossfadeTime);
        }

        private void HandleLanded(float landingSpeed)
        {
            if (landingSpeed < _minimumLandingSpeed) return;

            _landingAnimationTimer = _landingAnimationDuration;
            SetPlayState(AnimState.Landing, true);
        }

        private void HandleJumpPerformed(JumpType jumpType)
        {
            _landingAnimationTimer = 0f;

            switch (jumpType)
            {
                case JumpType.Ground:
                    SetPlayState(AnimState.Jump, true);
                    break;
                case JumpType.Extra:
                    SetPlayState(AnimState.DoubleJump, true);
                    break;
                case JumpType.Wall:
                    SetPlayState(AnimState.WallJump, true);
                    break;
            }
        }
    }
}
