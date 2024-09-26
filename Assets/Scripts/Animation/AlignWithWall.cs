using UnityEngine;

public class AlignWithWall : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Get the cover normal from the player controller
        Vector3 coverNormal = animator.GetComponentInParent<CoverManager>().coverNormal;

        // Calculate the rotation needed to align with the wall
        Quaternion targetRotation = Quaternion.LookRotation(-coverNormal, Vector3.up);

        // Set the animator's rotation to the target rotation
        animator.transform.rotation = targetRotation;
    }
}