using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    public float raycastDistance = 0.1f;
    public LayerMask excludedLayers; // Layers to exclude from ground detection

    public static bool isGrounded;

    void FixedUpdate()
    {
        // Cast rays from all sides of the player to check for ground
        RaycastHit hitFront;
        RaycastHit hitBack;
        RaycastHit hitLeft;
        RaycastHit hitRight;

        Vector3 frontRayOrigin = transform.position + transform.forward * 0.5f;
        Vector3 backRayOrigin = transform.position - transform.forward * 0.5f;
        Vector3 leftRayOrigin = transform.position - transform.right * 0.5f;
        Vector3 rightRayOrigin = transform.position + transform.right * 0.5f;

        // Perform raycasts and check if the hit colliders have walkable surfaces
        bool isFrontHit = Physics.Raycast(frontRayOrigin, Vector3.down, out hitFront, raycastDistance);
        bool isBackHit = Physics.Raycast(backRayOrigin, Vector3.down, out hitBack, raycastDistance);
        bool isLeftHit = Physics.Raycast(leftRayOrigin, Vector3.down, out hitLeft, raycastDistance);
        bool isRightHit = Physics.Raycast(rightRayOrigin, Vector3.down, out hitRight, raycastDistance);

        // Check if the hit colliders are on the excluded layers
        if (isFrontHit && (excludedLayers & (1 << hitFront.collider.gameObject.layer)) != 0)
        {
            isFrontHit = false;
        }

        if (isBackHit && (excludedLayers & (1 << hitBack.collider.gameObject.layer)) != 0)
        {
            isBackHit = false;
        }

        if (isLeftHit && (excludedLayers & (1 << hitLeft.collider.gameObject.layer)) != 0)
        {
            isLeftHit = false;
        }

        if (isRightHit && (excludedLayers & (1 << hitRight.collider.gameObject.layer)) != 0)
        {
            isRightHit = false;
        }

        // Player is grounded if any of the raycasts hits a collider with a walkable surface
        isGrounded = isFrontHit || isBackHit || isLeftHit || isRightHit;

        // If the player is grounded, perform additional actions
        if (isGrounded)
        {
            // Add any additional actions here
        }
    }

    // // Optionally, draw the ground check rays in the Scene view for debugging purposes
    // void OnDrawGizmosSelected()
    // {
    //     Vector3 frontRayOrigin = transform.position + transform.forward * 0.5f;
    //     Vector3 backRayOrigin = transform.position - transform.forward * 0.5f;
    //     Vector3 leftRayOrigin = transform.position - transform.right * 0.5f;
    //     Vector3 rightRayOrigin = transform.position + transform.right * 0.5f;

    //     // Draw the front ray
    //     Gizmos.color = isGrounded ? Color.green : Color.red;
    //     Gizmos.DrawLine(frontRayOrigin, frontRayOrigin + Vector3.down * raycastDistance);

    //     // Draw the back ray
    //     Gizmos.color = isGrounded ? Color.green : Color.red;
    //     Gizmos.DrawLine(backRayOrigin, backRayOrigin + Vector3.down * raycastDistance);

    //     // Draw the left ray
    //     Gizmos.color = isGrounded ? Color.green : Color.red;
    //     Gizmos.DrawLine(leftRayOrigin, leftRayOrigin + Vector3.down * raycastDistance);

    //     // Draw the right ray
    //     Gizmos.color = isGrounded ? Color.green : Color.red;
    //     Gizmos.DrawLine(rightRayOrigin, rightRayOrigin + Vector3.down * raycastDistance);
    // }
}
