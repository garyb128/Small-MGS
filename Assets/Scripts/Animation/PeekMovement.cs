using Cinemachine;
using UnityEngine;

public class PeekMovement : StateMachineBehaviour
{
    PlayerController playerController;
    CoverManager coverManager;
    CharacterController characterController;
    Vector3 originalPosition;
    float peekDistance = 0.25f; // Set this to the maximum distance you want the player to move

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        playerController = animator.GetComponentInParent<PlayerController>();
        coverManager = animator.GetComponentInParent<CoverManager>();
        characterController = animator.GetComponentInParent<CharacterController>();

        originalPosition = characterController.transform.position;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Vector3 coverNormal = coverManager.coverNormal;

        float leftPeekMovement = animator.GetFloat("LeftTrigger");
        float rightPeekMovement = animator.GetFloat("RightTrigger");

        bool isPeeking = animator.GetBool("LeftPeek") || animator.GetBool("RightPeek");

        // If the player is not peeking, move back to the original position
        if (!isPeeking && characterController.transform.position != originalPosition)
        {
            Vector3 directionToOriginal = (originalPosition - characterController.transform.position).normalized;
            float distanceToOriginal = Vector3.Distance(characterController.transform.position, originalPosition);

            float moveSpeed = 5f; // Adjust this value to control the speed of the movement
            characterController.Move(directionToOriginal * distanceToOriginal * moveSpeed * Time.deltaTime);
        }

        // Check if the player is in the middle of an animation
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1)
        {
            // If the player is in the middle of an animation, do not move the player
            return;
        }

        if (coverNormal == Vector3.left) //MOVING INTO COVER FROM BELOW
        {
            if (animator.GetBool("LeftPeek"))
            {
                Vector3 targetPosition = originalPosition + Vector3.forward * peekDistance;
                Vector3 newPosition = Vector3.Lerp(originalPosition, targetPosition, leftPeekMovement);

                // Calculate the movement vector and clamp it to the maximum peek distance
                Vector3 movement = newPosition - characterController.transform.position;
                movement = Vector3.ClampMagnitude(movement, peekDistance);
                characterController.Move(movement);
            }
            if (animator.GetBool("RightPeek"))
            {
                Vector3 targetPosition = originalPosition + Vector3.back * peekDistance;
                Vector3 newPosition = Vector3.Lerp(originalPosition, targetPosition, rightPeekMovement);

                // Calculate the movement vector and clamp it to the maximum peek distance
                Vector3 movement = newPosition - characterController.transform.position;
                movement = Vector3.ClampMagnitude(movement, peekDistance);
                characterController.Move(movement);
            }
        }
        else if(coverNormal == Vector3.right) //MOVING INTO COVER FROM ABOVE
        {
            if (animator.GetBool("LeftPeek"))
            {
                Vector3 targetPosition = originalPosition + Vector3.back * peekDistance;
                Vector3 newPosition = Vector3.Lerp(originalPosition, targetPosition, leftPeekMovement);

                // Calculate the movement vector and clamp it to the maximum peek distance
                Vector3 movement = newPosition - characterController.transform.position;
                movement = Vector3.ClampMagnitude(movement, peekDistance);
                characterController.Move(movement);
            }
            if (animator.GetBool("RightPeek"))
            {
                Vector3 targetPosition = originalPosition + Vector3.forward * peekDistance;
                Vector3 newPosition = Vector3.Lerp(originalPosition, targetPosition, rightPeekMovement);

                // Calculate the movement vector and clamp it to the maximum peek distance
                Vector3 movement = newPosition - characterController.transform.position;
                movement = Vector3.ClampMagnitude(movement, peekDistance);
                characterController.Move(movement);
            }
        }
        else if (coverNormal == Vector3.back) //MOVING INTO COVER FROM THE RIGHTSIDE
        {
            if (animator.GetBool("LeftPeek"))
            {
                Vector3 targetPosition = originalPosition + Vector3.left * peekDistance;
                Vector3 newPosition = Vector3.Lerp(originalPosition, targetPosition, leftPeekMovement);

                // Calculate the movement vector and clamp it to the maximum peek distance
                Vector3 movement = newPosition - characterController.transform.position;
                movement = Vector3.ClampMagnitude(movement, peekDistance);
                characterController.Move(movement);
            }
            if (animator.GetBool("RightPeek"))
            {
                Vector3 targetPosition = originalPosition + Vector3.right * peekDistance;
                Vector3 newPosition = Vector3.Lerp(originalPosition, targetPosition, rightPeekMovement);

                // Calculate the movement vector and clamp it to the maximum peek distance
                Vector3 movement = newPosition - characterController.transform.position;
                movement = Vector3.ClampMagnitude(movement, peekDistance);
                characterController.Move(movement);
            }
        }
        else if(coverNormal == Vector3.forward) //MOVING INTO COVER FROM THE LEFTSIDE
        {
            if (animator.GetBool("LeftPeek"))
            {
                Vector3 targetPosition = originalPosition + Vector3.right * peekDistance;
                Vector3 newPosition = Vector3.Lerp(originalPosition, targetPosition, leftPeekMovement);

                // Calculate the movement vector and clamp it to the maximum peek distance
                Vector3 movement = newPosition - characterController.transform.position;
                movement = Vector3.ClampMagnitude(movement, peekDistance);
                characterController.Move(movement);
            }
            if (animator.GetBool("RightPeek"))
            {
                Vector3 targetPosition = originalPosition + Vector3.left * peekDistance;
                Vector3 newPosition = Vector3.Lerp(originalPosition, targetPosition, rightPeekMovement);

                // Calculate the movement vector and clamp it to the maximum peek distance
                Vector3 movement = newPosition - characterController.transform.position;
                movement = Vector3.ClampMagnitude(movement, peekDistance);
                characterController.Move(movement);
            }
        }
        
    }
}
