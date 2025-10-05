using Assets.Scripts.Actors.Player;
using MegabonkTogether.Common.Messages;

namespace MegabonkTogether.Extensions
{
    public static class MyPlayerExtensions
    {
        public static AnimatorState GetAnimatorState(this MyPlayer player)
        {
            var state = new AnimatorState();

            if (player == null || player.playerRenderer == null || player.playerRenderer.animator == null)
            {
                return state;
            }

            var animator = player.playerRenderer.animator;


            return new AnimatorState
            {
                IsGrounded = animator.GetBool("grounded"),
                IsMoving = animator.GetBool("moving"),
                IsIdle = animator.GetBool("idle"),
                IsGrinding = animator.GetBool("grinding"),
                IsJumping = animator.GetBool("jumping")
            };
        }
    }
}
