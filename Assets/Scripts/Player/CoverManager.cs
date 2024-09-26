using System.Collections;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class CoverManager : MonoBehaviour
{
    PlayerController playerController;

    //Cover stuff
    public LayerMask wallLayerMask;
    float wallRaycastCheckDistance = 0.3f;
    float closeToEdgeThreshold = 0.7f;
    internal bool closeToLeftEdge, closeToRightEdge;
    [SerializeField] Transform coverDetectionTransform;
    [SerializeField] Transform vaultDetectionTransform;
    [SerializeField] internal Vector3 coverNormal;//Normal of the cover the player is in
    Bounds colliderBounds;//Bounds of the cover collider the player is on

    //Cover camera stuff
    [SerializeField] GameObject coverCamera;
    [SerializeField] CinemachineVirtualCamera coverVirtualCamera;
    Vector3 originalPosition; // Set this to the new position of the camera   
    float cameraDistance = 6f; // Set this to control how far the camera moves

    // Start is called before the first frame update
    void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        CoverCameraHandler();
    }

    public void CoverCheck()
    {
        // Check if there's a wall in front of the player based on the velocity vector
        RaycastHit coverCheck;
        RaycastHit vaultCheck;

        Vector3 playerVelocity = playerController.velocity.normalized;

        // Perform raycasts and check if the hit colliders have walkable surfaces
        bool isWallHit = Physics.Raycast(coverDetectionTransform.position, playerVelocity, out coverCheck, wallRaycastCheckDistance, wallLayerMask);
        bool isVaultHit = Physics.Raycast(vaultDetectionTransform.position, playerVelocity, out vaultCheck, wallRaycastCheckDistance, wallLayerMask);

        // Calculate the alignment based on the normal of the surface hit
        coverNormal = coverCheck.normal;

        // Check if the player is facing the wall
        float dotProduct = Vector3.Dot(transform.forward, -coverNormal);

        if (isWallHit && Mathf.Abs(dotProduct) > 0.9) // Adjust this threshold as needed
        {
            // Get the collider of the wall
            BoxCollider wallCollider = (BoxCollider)coverCheck.collider;

            // Get the bounds of the collider
            colliderBounds = wallCollider.bounds;

            // Calculate the center points of the cover
            (Vector3 leftCenter, Vector3 rightCenter) = CalculateCoverPoints(colliderBounds, coverNormal);

            //Find the length of the cover
            float coverLength = Vector3.Distance(leftCenter, rightCenter);

            //If length of cover is less than 1, cover is too small
            if (coverLength < 1f)
            {
                return;
            }

            if (playerController.currentPlayerState == PlayerController.PlayerState.Standing)
            {
                playerController.currentPlayerState = PlayerController.PlayerState.Cover;
                playerController.Cover();

            }
            else if (playerController.currentPlayerState == PlayerController.PlayerState.Crouching || playerController.currentPlayerState == PlayerController.PlayerState.Crawling)
            {
                playerController.currentPlayerState = PlayerController.PlayerState.CoverCrouch;
                playerController.CoverCrouch();
            }

            if (!playerController.inCover)
            {
                // Align player's rotation with the surface normal
                transform.rotation = Quaternion.LookRotation(-coverNormal, Vector3.up);

                float distanceToRight = Vector3.Distance(transform.position, rightCenter);
                float distanceToLeft = Vector3.Distance(transform.position, leftCenter);

                if (coverLength > 1.5f)
                {
                    //Check if the player is near the edge of the cover
                    if (distanceToLeft < closeToEdgeThreshold || distanceToRight < closeToEdgeThreshold)
                    {
                        // Calculate a direction that is perpendicular to the surfaceNormal and the up vector
                        Vector3 pushDirection = Vector3.Cross(coverNormal, Vector3.up).normalized;

                        // Push the player slightly along this direction
                        float pushDistance = 0.4f; // Adjust this value as needed
                        playerController.characterController.Move(pushDirection * pushDistance);
                    }
                }
                else
                {
                    // Calculate the center point of the cover
                    Vector3 coverCenter = (leftCenter + rightCenter) / 2;

                    // Calculate the direction and distance to the cover center
                    Vector3 directionToCoverCenter = (coverCenter - transform.position).normalized;
                    float distanceToCoverCenter = Vector3.Distance(transform.position, coverCenter);

                    // Move the player to the cover center
                    playerController.characterController.Move(directionToCoverCenter * distanceToCoverCenter);
                }

                playerController.inCover = true;
            }
        }
    }

    public void InCoverHandler()
    {
        float leftTrigger = GameInputManager.Instance.player.GetAxis("Peek Left");
        float rightTrigger = GameInputManager.Instance.player.GetAxis("Peek Right");

        (Vector3 leftCenter, Vector3 rightCenter) = CalculateCoverPoints(colliderBounds, coverNormal);

        Vector3 closestPointToLeft = colliderBounds.ClosestPoint(leftCenter);
        Vector3 closestPointToRight = colliderBounds.ClosestPoint(rightCenter);

        float leftDistance = Vector3.Distance(transform.position, closestPointToLeft);
        float rightDistance = Vector3.Distance(transform.position, closestPointToRight);

        Debug.Log($"Left distance: {leftDistance}, Right distance: {rightDistance}, Close to left edge: {closeToLeftEdge}, Close to right edge: {closeToRightEdge}");

        closeToLeftEdge = leftDistance <= closeToEdgeThreshold;//USE THESE TO DECIDE WHEN TO CHANGE THE CAMERA
        closeToRightEdge = rightDistance <= closeToEdgeThreshold;
        
        if (leftDistance <= closeToEdgeThreshold || rightDistance <= closeToEdgeThreshold)
        {
            playerController.canPeek = true;
        }
        else
        {
            playerController.canPeek = false;
        }


        //MOVING INTO COVER FROM BELOW
        if (coverNormal == Vector3.left)
        {
            if (playerController.movementInput.y <= 0f)
            {
                playerController.ResetCover();
            }


            //Invert left trigger value
            leftTrigger = -leftTrigger;

            //Allows players to move along cover with triggers
            if (Mathf.Abs(leftTrigger) > 0)
            {
                playerController.movementInput.x = leftTrigger;
            }
            if (Mathf.Abs(rightTrigger) > 0)
            {
                playerController.movementInput.x = rightTrigger;
            }

            if (playerController.canPeek)
            {
                if (playerController.movementInput.y > 0.5f)
                {
                    if (closeToRightEdge)
                    {
                        if (playerController.movementInput.x > 0 || Mathf.Abs(rightTrigger) > 0)
                        {
                            playerController.movementInput.x = 0;
                        }
                    }
                    else if (closeToLeftEdge)
                    {
                        if (playerController.movementInput.x < 0 || Mathf.Abs(leftTrigger) > 0)
                        {
                            playerController.movementInput.x = 0;
                        }
                    }
                    else if (closeToLeftEdge && closeToRightEdge && Mathf.Abs(playerController.movementInput.x) > 0)
                    {
                        playerController.movementInput.x = 0;
                    }
                }
            }


        }

        //MOVING INTO COVER FROM ABOVE
        else if (coverNormal == Vector3.right)
        {
            if (playerController.movementInput.y >= 0f)
            {
                playerController.ResetCover();
            }

            //Invert right trigger value
            leftTrigger = -leftTrigger;

            //Allows players to move along cover with triggers
            if (Mathf.Abs(leftTrigger) > 0)
            {
                playerController.movementInput.x = leftTrigger;
            }
            if (Mathf.Abs(rightTrigger) > 0)
            {
                playerController.movementInput.x = rightTrigger;
            }

            if (playerController.canPeek)
            {
                if (playerController.movementInput.y < -0.5f)
                {
                    if (closeToRightEdge)
                    {
                        if (playerController.movementInput.x < 0 || Mathf.Abs(leftTrigger) > 0)
                        {
                            playerController.movementInput.x = 0;
                        }
                    }
                    else if (closeToLeftEdge)
                    {
                        if (playerController.movementInput.x > 0 || Mathf.Abs(rightTrigger) > 0)
                        {
                            playerController.movementInput.x = 0;
                        }
                    }
                    else if (closeToLeftEdge && closeToRightEdge && Mathf.Abs(playerController.movementInput.x) > 0)
                    {
                        playerController.movementInput.x = 0;
                    }
                }
            }


        }

        //MOVING INTO COVER FROM THE  LEFTSIDE
        else if (coverNormal == Vector3.back)
        {
            if (playerController.movementInput.x >= 0)
            {
                playerController.ResetCover();
            }

            //Invert left trigger value
            leftTrigger = -leftTrigger;

            //Allows players to move along cover with triggers
            if (Mathf.Abs(leftTrigger) > 0)
            {
                playerController.movementInput.y = leftTrigger;
            }
            if (Mathf.Abs(rightTrigger) > 0)
            {
                playerController.movementInput.y = rightTrigger;
            }

            if (playerController.canPeek)
            {
                if (playerController.movementInput.x < -0.5f)
                {
                    if (closeToRightEdge)
                    {
                        if (playerController.movementInput.y > 0 || Mathf.Abs(rightTrigger) > 0)
                        {
                            playerController.movementInput.y = 0;
                        }
                    }
                    else if (closeToLeftEdge)
                    {
                        if (playerController.movementInput.y < 0 || Mathf.Abs(leftTrigger) > 0)
                        {
                            playerController.movementInput.y = 0;
                        }
                    }
                    else if (closeToLeftEdge && closeToRightEdge && Mathf.Abs(playerController.movementInput.y) > 0)
                    {
                        playerController.movementInput.y = 0;
                    }
                }
                else
                {
                    playerController.ResetCover();
                }
            }
        }

        //MOVING INTO COVER FROM THE RIGHTSIDE
        else if (coverNormal == Vector3.forward)
        {
            if (playerController.movementInput.x <= 0f)
            {
                playerController.ResetCover();
            }

            //Invert right trigger value
            rightTrigger = -rightTrigger;

            //Allows players to move along cover with triggers
            if (Mathf.Abs(leftTrigger) > 0)
            {
                playerController.movementInput.y = leftTrigger;
            }
            if (Mathf.Abs(rightTrigger) > 0)
            {
                playerController.movementInput.y = rightTrigger;
            }

            if (playerController.canPeek)
            {
                if (playerController.movementInput.x > 0.5f)
                {
                    if (closeToRightEdge)
                    {
                        if (playerController.movementInput.y < 0 || Mathf.Abs(rightTrigger) > 0)
                        {
                            playerController.movementInput.y = 0;
                        }
                    }
                    else if (closeToLeftEdge)
                    {
                        if (playerController.movementInput.y > 0 || Mathf.Abs(leftTrigger) > 0)
                        {
                            playerController.movementInput.y = 0;
                        }
                    }
                    else if (closeToLeftEdge && closeToRightEdge && Mathf.Abs(playerController.movementInput.y) > 0)
                    {
                        playerController.movementInput.y = 0;
                    }
                }
            }

        }
    }

    public (Vector3 leftCenter, Vector3 rightCenter) CalculateCoverPoints(Bounds bounds, Vector3 surfaceNormal)
    {
        // Initialize the corners of the collider
        Vector3 topLeft = Vector3.zero;
        Vector3 topRight = Vector3.zero;
        Vector3 bottomLeft = Vector3.zero;
        Vector3 bottomRight = Vector3.zero;

        if (surfaceNormal == Vector3.left) // Moving up into cover
        {
            topLeft = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
            bottomLeft = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
            topRight = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
            bottomRight = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        }
        else if (surfaceNormal == Vector3.right) // Moving down into cover
        {
            topLeft = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);
            bottomLeft = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
            topRight = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
            bottomRight = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        }
        else if (surfaceNormal == Vector3.back) // Moving left into cover
        {
            topLeft = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
            bottomLeft = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
            topRight = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
            bottomRight = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
        }
        else if (surfaceNormal == Vector3.forward) // Moving right into cover
        {
            topLeft = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
            bottomLeft = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
            topRight = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);
            bottomRight = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        }

        // Calculate the center points
        Vector3 rightCenter = (topLeft + bottomLeft) / 2f;
        Vector3 leftCenter = (topRight + bottomRight) / 2f;

        return (leftCenter, rightCenter);
    }

    void CoverCameraHandler()
    {
        bool isPeeking = playerController.animator.GetBool("LeftPeek") || playerController.animator.GetBool("RightPeek");

        if (!closeToLeftEdge && !closeToRightEdge)   
        {
            //Turn main camera on & turn cover camera off
            playerController.mainCamera.gameObject.SetActive(true);
            playerController.mainVirtualCamera.gameObject.SetActive(true);
            coverCamera.SetActive(false);
            coverVirtualCamera.gameObject.SetActive(false);
            originalPosition = coverVirtualCamera.transform.position;
        }
        else if (closeToLeftEdge || closeToRightEdge)
        {
            //Turn cover camera on & turn main camera off
            playerController.mainCamera.gameObject.SetActive(false);
            playerController.mainVirtualCamera.gameObject.SetActive(false);
            coverCamera.SetActive(true);
            coverVirtualCamera.gameObject.SetActive(true);

            // Get input for cover camera
            float cameraX = GameInputManager.Instance.player.GetAxisRaw("CoverCameraX") * cameraDistance;
            float cameraY = GameInputManager.Instance.player.GetAxisRaw("CoverCameraY") * cameraDistance;

            // Create a new offset based on the input
            Vector3 newOffset = originalPosition + new Vector3(0, cameraY, -cameraX);
            float leanOffset = 8f;
            
            if(isPeeking)
            {
                if (coverNormal == Vector3.left) // Moving up into cover
                {
                    if (closeToLeftEdge)
                    {
                        newOffset = originalPosition + new Vector3(0, cameraY, -cameraX + leanOffset);
                    }
                    else
                    {
                        newOffset = originalPosition + new Vector3(0, cameraY, -cameraX - leanOffset);
                    }

                }
                else if (coverNormal == Vector3.right) // Moving down into cover
                {
                    if (closeToLeftEdge)
                    {
                        newOffset = originalPosition + new Vector3(0, cameraY, -cameraX - leanOffset);
                    }
                    else
                    {
                        newOffset = originalPosition + new Vector3(0, cameraY, -cameraX + leanOffset);
                    }
                }
                else if (coverNormal == Vector3.back) // Moving left into cover
                {
                    if (closeToLeftEdge)
                    {
                        newOffset = originalPosition + new Vector3(-leanOffset, cameraY, -cameraX);
                    }
                    else
                    {
                        newOffset = originalPosition + new Vector3(leanOffset, cameraY, -cameraX);
                    }
                }
                else if (coverNormal == Vector3.forward) // Moving right into cover
                {
                    if (closeToLeftEdge)
                    {
                        newOffset = originalPosition + new Vector3(leanOffset, cameraY, -cameraX);
                    }
                    else
                    {
                        newOffset = originalPosition + new Vector3(-leanOffset, cameraY, -cameraX);
                    }
                }

            }

            // Set the camera's position to the player's position plus the newOffset
            coverVirtualCamera.transform.position = Vector3.Lerp(originalPosition, newOffset,  0.1f);
        }
    }
}
