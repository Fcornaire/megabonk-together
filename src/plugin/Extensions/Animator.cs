using MegabonkTogether.Common.Messages;
using UnityEngine;

namespace MegabonkTogether.Extensions
{
    public static class AnimatorExtensions
    {
        public static void UpdateAnimator(this Animator animator, AnimatorState state)
        {
            animator.SetBool("grounded", state.IsGrounded);
            animator.SetBool("moving", state.IsMoving);
            animator.SetBool("idle", state.IsIdle);
            animator.SetBool("grinding", state.IsGrinding);
            animator.SetBool("jumping", state.IsJumping);
        }
    }
}
