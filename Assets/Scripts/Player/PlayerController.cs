using System;
using System.Collections;
using System.Numerics;
using Rewired; // Library for handling input
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;
using UnityEngine.Rendering;
using Unity.VisualScripting.Dependencies.Sqlite;
using Cinemachine;

public class PlayerController : MonoBehaviour
{
    // Enumeration to represent different player states
    internal enum PlayerState
    {
        Standing,
        Crouching,
        Crawling,
        Cover,
        CoverCrouch,
        Peeking
    }
    
    // Serialized field to store the current player state
    [SerializeField] internal PlayerState currentPlayerState;
    // Input references
    [Header("Input References")]
    float ySpeed = 0f; // Initial vertical speed
    float terminalVelocity = 9.6f; // Unity units per second (adjust as needed)
    float decayFactor = 0.995f; // Adjust this value (close to 1) for gradual decay
    bool isFalling;
    float rotationSpeed = 720f; //Rotation speed

    // Speed-related variables
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float crouchSpeed = 2.5f;
    [SerializeField] float crawlingSpeed = 1.25f;
    [SerializeField] float wallFlattenSpeed = 0.7f;
    [SerializeField] float wallFlattenCrouchSpeed = 0.5f;
    float originalSpeed;

    // Height-related variables
    float originalHeight;
    float crouchHeight = 1.3f; // Adjust this value as needed

    //Cover related stuff
    CoverManager coverManager;
    [HideInInspector] internal bool inCover;
    [SerializeField] internal bool canPeek;

    // Character controller and movement input
    [HideInInspector]internal CharacterController characterController;
    float originalRadius;
    float originalStepOffset;
    [HideInInspector] internal Vector2 movementInput; // Movement vector
    [HideInInspector] internal Vector3 velocity;//Movement velocity

    // Camera reference
    public Camera mainCamera; // Reference to the main camera
    public CinemachineVirtualCamera mainVirtualCamera;
    [SerializeField] Camera firstPersonCamera;
    [SerializeField] CinemachineVirtualCamera firstPersonVirtualCamera;
    [SerializeField] bool firstPerson;
    float FPSensitivity = 1f;
    float FPRotationSpeed = 5f; // Speed of rotation interpolation
    float xRotation = 0f;
    float leanMultiplier = 1f;
    float maxLeanAmount = 1f;
    Vector3 initialPosition;//Initial position of player, stored before going into FP
    Vector3 initialCameraPosition;//Initial position of camera
    Quaternion initialCameraRotation;//Initial rotation of camera

    // Flag to control player movement
    bool canMove = true;
    bool disableInput;

    // Animator for character animations
    [HideInInspector] public Animator animator;

    void Start()
    {
        // Get references to necessary components
        characterController = GetComponentInChildren<CharacterController>();
        originalHeight = characterController.height;
        originalStepOffset = characterController.stepOffset;
        originalRadius = characterController.radius;
        mainCamera = Camera.main;
        initialCameraPosition = firstPersonCamera.transform.localPosition;
        initialCameraRotation = firstPersonCamera.transform.localRotation;
        animator = GetComponentInChildren<Animator>();
        originalSpeed = moveSpeed;
        coverManager = GetComponent<CoverManager>();
    }

    void OnEnable()
    {
        // Subscribe to the GameManager events
        GameManager.OnMissionCompleteEvent += DisablePlayerMovement;
        GameManager.OnGameOverEvent += DisablePlayerMovement;

        //Default player state is standing when the game starts
        currentPlayerState = PlayerState.Standing;
    }

    void OnDisable()
    {
        // Unsubscribe from the GameManager events
        GameManager.OnMissionCompleteEvent -= DisablePlayerMovement;
        GameManager.OnGameOverEvent -= DisablePlayerMovement;
    }

    void Update()
    {
        if (!disableInput)
        {
            //Get input from Rewired
            movementInput.x = GameInputManager.Instance.player.GetAxisRaw("Move Horizontal");
            movementInput.y = GameInputManager.Instance.player.GetAxisRaw("Move Vertical");
        }

        //Adjust the character controller's step offset based on whether it's grounded or not
        if (GroundCheck.isGrounded)
        {
            if (isFalling)
            {
                StartCoroutine(FallDelay());
            }

            ySpeed = -0.5f;
        }
        else
        {
            if (ySpeed < -4f)
            {
                if (!isFalling)
                {
                    movementInput = Vector2.zero;
                    disableInput = true;
                    characterController.stepOffset = 0;
                    characterController.radius = 0.1f;
                    animator.SetTrigger("IsFalling");
                    isFalling = true;
                }

            }
        }

        if (inCover)
        {
            coverManager.InCoverHandler();
        }

        if (!firstPerson)
        {
            //Move the player based on input
            MovePlayer(movementInput);
        }
        if (firstPerson)
        {
            //Move the player based on input
            FirstPerson();
        }

        //Handle player state changes (standing, crouching, crawling, etc.)
        HandlePlayerState();

        //Pause or unpause the game based on input
        if (GameManager.Instance.currentGameState != GameManager.GameState.Paused && GameInputManager.Instance.player.GetButtonDown("Pause"))
        {
            GameManager.Instance.OnPause();
        }
        else if (GameManager.Instance.currentGameState == GameManager.GameState.Paused && GameInputManager.Instance.player.GetButtonDown("UIPause"))
        {
            GameManager.Instance.OnUnPaused();
        }
    }

    void FixedUpdate()
    {
        Gravity();
    }

    void Gravity()
    {
        // Apply gravity
        ySpeed += Physics.gravity.y * Time.fixedDeltaTime;

        // Exponential decay to the vertical speed
        ySpeed *= decayFactor;

        // Cap the vertical speed at the terminal velocity
        if (ySpeed > terminalVelocity)
        {
            ySpeed = terminalVelocity;
        }
    }

    public void MovePlayer(Vector2 input)
    {
        // Don't move the player if the canMove flag is false
        if (!canMove) return;

        // Calculate movement direction relative to the camera
        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 movement = forward * input.y + right * input.x;
        float magnitude = Mathf.Clamp01(movement.magnitude);

        animator.SetFloat("InputMagnitude", magnitude, 0.05f, Time.deltaTime);

        float speed = magnitude * moveSpeed;
        movement.Normalize();

        velocity = movement * speed;
        velocity.y = ySpeed;

        characterController.Move(velocity * Time.deltaTime);

        if (!inCover)
        {
            coverManager.CoverCheck();
        }

        if (movement != Vector3.zero)
        {
            if (!inCover)
            {
                // Set the IsMoving parameter in the animator to true
                animator.SetBool("IsMoving", true);

                // Calculate the rotation to face the velocity direction (lock x-axis rotation)
                Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);

                // Apply rotation to the transform as well
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            else
            {
                CoverAnimatorHandler(movement);
            }
        }
        else
        {
            if (!inCover)
            {
                animator.SetBool("IsMoving", false);

                if (currentPlayerState == PlayerState.Crouching || currentPlayerState == PlayerState.Crawling)
                {
                    animator.SetBool("IsStanding", false);
                }
                else
                {
                    animator.SetBool("IsStanding", true);
                }
            }
            else
            {
                ResetCover();
            }
        }

    }

    void FirstPerson()
    {
        float leftTrigger = GameInputManager.Instance.player.GetAxisRaw("Lean Left");
        float rightTrigger = GameInputManager.Instance.player.GetAxisRaw("Lean Right");

        // Calculate the combined lean amount
        float leanAmount = (leftTrigger - rightTrigger) * leanMultiplier;
        float tipToeAmount = leftTrigger * rightTrigger * leanMultiplier;

        // Clamp the lean amount within a certain range
        leanAmount = Mathf.Clamp(leanAmount, -maxLeanAmount, maxLeanAmount);
        tipToeAmount = Mathf.Clamp(tipToeAmount, 0, maxLeanAmount / 3);

        // Get the camera's forward and right vectors
        Vector3 cameraRight = firstPersonCamera.transform.right;

        // Project the lean movement onto the horizontal plane defined by the world up vector
        Vector3 leanMovement = Vector3.ProjectOnPlane(cameraRight, Vector3.down).normalized * leanAmount;

        // Calculate lean movement relative to initial position
        Vector3 leanPosition = initialPosition - leanMovement; // Invert the lean movement for camera movement

        if (leftTrigger > 0 && rightTrigger > 0)
        {
            // If both triggers are held, move the camera up
            leanMovement = Vector3.zero;
            Vector3 newCameraPosition = initialCameraPosition + (Vector3.up * tipToeAmount);
            firstPersonVirtualCamera.transform.localPosition = Vector3.Lerp(firstPersonVirtualCamera.transform.localPosition, newCameraPosition, Time.deltaTime * FPRotationSpeed);
        }
        else
        {
            // If only one trigger is held or none, reset camera position smoothly
            firstPersonVirtualCamera.transform.localPosition = Vector3.Lerp(firstPersonVirtualCamera.transform.localPosition, initialCameraPosition, Time.deltaTime * FPRotationSpeed);
        }

        // Apply rotation based on input
        float yRotation = movementInput.x * FPSensitivity;
        xRotation -= movementInput.y * FPSensitivity;

        // Clamp xRotation to prevent looking too far up or down
        switch (currentPlayerState)
        {
            case PlayerState.Standing:
                xRotation = Mathf.Clamp(xRotation, -50, 60);
                break;
            case PlayerState.Crouching:
                xRotation = Mathf.Clamp(xRotation, -50, 45);
                break;
            case PlayerState.Crawling:
                xRotation = Mathf.Clamp(xRotation, -50, 25);
                break;
        }

        // Apply rotations
        transform.Rotate(Vector3.up * yRotation);
        Vector3 newPosition = Vector3.Lerp(transform.position, leanPosition, Time.deltaTime * FPRotationSpeed);
        characterController.Move(newPosition - transform.position);

        firstPersonVirtualCamera.transform.localRotation = Quaternion.Lerp(firstPersonVirtualCamera.transform.localRotation, Quaternion.Euler(xRotation, 0f, 0f), Time.deltaTime * FPRotationSpeed);
    }

    void RecenterCamera(PlayerState playerState)
    {
        switch (playerState)
        {
            case PlayerState.Standing:
                xRotation = 10;
                break;
            case PlayerState.Crouching:
                xRotation = -15f;
                break;
            case PlayerState.Crawling:
                xRotation = -30f;
                break;
        }

        // Ensure the virtual camera is not null before proceeding
        if (firstPersonVirtualCamera != null)
        {
            // Interpolate the position of the virtual camera to its initial position   
            firstPersonCamera.transform.localPosition = Vector3.Lerp(firstPersonVirtualCamera.transform.localPosition, initialCameraPosition, FPSensitivity * Time.deltaTime);
            firstPersonCamera.transform.localRotation = Quaternion.Lerp(firstPersonVirtualCamera.transform.localRotation, initialCameraRotation, FPSensitivity * Time.deltaTime);
        }
    }

    void HandlePlayerState()
    {
        //Enter first person mode if the button is held
        firstPerson = GameInputManager.Instance.player.GetButton("First Person");

        if (GameInputManager.Instance.player.GetButtonDown("First Person"))
        {
            initialPosition = transform.localPosition;
            RecenterCamera(currentPlayerState);

            if(inCover)
            {
                ResetCover();
            }
        }

        if (firstPerson)
        {
            animator.SetBool("IsMoving", false);
            mainCamera.gameObject.SetActive(false);
            mainVirtualCamera.gameObject.SetActive(false);
            firstPersonCamera.gameObject.SetActive(true);
            firstPersonVirtualCamera.gameObject.SetActive(true);
            GameInputManager.Instance.SetFPInput();
        }
        else if(!firstPerson && !inCover)
        {
            mainCamera.gameObject.SetActive(true);
            mainVirtualCamera.gameObject.SetActive(true);
            firstPersonCamera.gameObject.SetActive(false);
            firstPersonVirtualCamera.gameObject.SetActive(false);
            GameInputManager.Instance.DisableFPInput();
        }


        if (!inCover)
        {
            // If the crouch button is held for more than 0.4 seconds
            if (GameInputManager.Instance.player.GetButtonTimedPressDown("Crouch", 0.4f))
            {
                // Hold to crawl
                switch (currentPlayerState)
                {
                    case PlayerState.Standing:
                    case PlayerState.Crouching:
                        Crawl();
                        if (firstPerson)
                        {
                            RecenterCamera(PlayerState.Crawling);
                        }
                        break;
                    case PlayerState.Crawling:
                        // Cannot go from crawling to standing while moving
                        if (movementInput.magnitude == 0f)
                        {
                            Stand();
                        }
                        if (firstPerson)
                        {
                            RecenterCamera(PlayerState.Standing);
                        }
                        break;
                    case PlayerState.Cover:
                        CoverCrouch();
                        break;
                    case PlayerState.CoverCrouch:
                        Cover();
                        break;
                        // Handle other cases if needed
                }
            }
            // If the crouch button is tapped (released within 0.4 seconds)
            else if (GameInputManager.Instance.player.GetButtonTimedPressUp("Crouch", 0, 0.4f))
            {
                // Tap to crouch
                switch (currentPlayerState)
                {
                    case PlayerState.Standing:
                        Crouch();
                        if (firstPerson)
                        {
                            RecenterCamera(PlayerState.Crouching);
                        }
                        break;
                    case PlayerState.Crouching:
                        // Tap to stand
                        Stand();
                        if (firstPerson)
                        {
                            RecenterCamera(PlayerState.Standing);
                        }
                        break;
                    case PlayerState.Crawling:
                        // Tap to crouch
                        Crouch();
                        if (firstPerson)
                        {
                            RecenterCamera(PlayerState.Crawling);
                        }
                        break;
                        // Handle other cases if needed
                }
            }
        }
        if (inCover)
        {
            if (GameInputManager.Instance.player.GetButtonTimedPressUp("Crouch", 0, 0.4f))
            {
                // Tap to crouch
                switch (currentPlayerState)
                {
                    case PlayerState.Cover:
                        CoverCrouch();
                        break;
                    case PlayerState.CoverCrouch:
                        // Tap to stand
                        Cover();
                        break;
                        // Handle other cases if needed
                }
            }
        }
    }


    void Crouch()
    {
        animator.SetBool("IsCrouching", true);
        animator.SetBool("IsStanding", false);
        animator.SetBool("IsCrawling", false);

        currentPlayerState = PlayerState.Crouching;
        // Adjust the character controller's radius and height for crouching
        //characterController.radius = 0.5f;
        //characterController.height = crouchHeight;
        // characterController.center = new Vector3();
        moveSpeed = crouchSpeed;
    }

    void Stand()
    {
        animator.SetBool("IsCrouching", false);
        animator.SetBool("IsStanding", true);
        animator.SetBool("IsCrawling", false);
        // Restore the character controller's radius and height to the original values
        characterController.radius = originalRadius;
        characterController.height = originalHeight;
        moveSpeed = originalSpeed;
        currentPlayerState = PlayerState.Standing;
    }

    void Crawl()
    {
        //Adjust the character controller's radius for crawling
        animator.SetBool("IsCrouching", false);
        animator.SetBool("IsStanding", false);
        animator.SetBool("IsCrawling", true);
        moveSpeed = crawlingSpeed;
        currentPlayerState = PlayerState.Crawling;
    }

    internal void Cover()
    {
        // Set animator bools
        animator.SetBool("IsCover", true);
        animator.SetBool("IsStanding", false);
        animator.SetBool("IsMoving", false);
        animator.SetBool("IsCrawling", false);
        animator.SetBool("IsCoverCrouch", false);

        GameInputManager.Instance.SetCoverInput();

        //What happens when flattened against a wall
        moveSpeed = wallFlattenSpeed;

        currentPlayerState = PlayerState.Cover;
    }

    internal void CoverCrouch()
    {
        Debug.Log("Crouching in cover");

        //Logic for crouch when flattened against a wall
        animator.SetBool("IsCover", true);
        animator.SetBool("IsStanding", false);
        animator.SetBool("IsMoving", false);
        animator.SetBool("IsCrawling", false);
        animator.SetBool("IsCoverCrouch", true);

        //What happens when flattened against a wall
        moveSpeed = wallFlattenSpeed;

        currentPlayerState = PlayerState.CoverCrouch;
    }

    //RESET EVERYTHING RELATED TO COVER
    internal void ResetCover()
    {
        Debug.Log("Resetting Cover");

        if (currentPlayerState == PlayerState.Cover)
        {
            Stand();
        }
        else if (currentPlayerState == PlayerState.CoverCrouch)
        {
            Crouch();
        }

        animator.SetBool("IsCoverCrouch", false);
        animator.SetBool("IsCover", false);
        inCover = false;
        canPeek = false;
        GameInputManager.Instance.DisableCoverInput();
        transform.rotation = Quaternion.Euler(0,-transform.rotation.y,0);
        coverManager.closeToLeftEdge = false;
        coverManager.closeToRightEdge = false;
    }



    void CoverAnimatorHandler(Vector3 move)
    {
        float xMovement = Mathf.Clamp(move.x, -1, 1);
        float zMovement = Mathf.Clamp(move.z, -1, 1);

        float leftTrigger = GameInputManager.Instance.player.GetAxisRaw("Peek Left");
        float rightTrigger = GameInputManager.Instance.player.GetAxisRaw("Peek Right");

        // If the triggers are not being pressed, reset the peeking booleans
        if (leftTrigger <= 0f)
        {
            animator.SetBool("LeftPeek", false);
        }
        if (rightTrigger <= 0f)
        {
            animator.SetBool("RightPeek", false);
        }

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 0.3f))
        {
            Vector3 wallNormal = hit.normal;
            Vector3 wallRight = Vector3.Cross(wallNormal, Vector3.up);

            // Project the movement vector onto the wall plane
            Vector3 moveOnWallPlane = move - Vector3.Dot(move, wallNormal) * wallNormal;

            // Check the direction of the movement relative to the wall's right and up vectors
            float dotProductX = Vector3.Dot(moveOnWallPlane, wallRight);
            float dotProductZ = Vector3.Dot(moveOnWallPlane, Vector3.Cross(wallRight, Vector3.up));

            // Set the animator's "MovementX" and "MovementY" parameters based on the dot products
            animator.SetFloat("MovementX", dotProductZ, 0.05f, Time.deltaTime);
            animator.SetFloat("MovementY", dotProductX, 0.05f, Time.deltaTime);
        }

        if(canPeek)
        {
            if (coverManager.closeToLeftEdge)
            {
                if (coverManager.coverNormal == Vector3.left)  //MOVING INTO COVER FROM BELOW
                {
                    animator.SetBool("LeftPeek", leftTrigger > 0f);
                    animator.SetFloat("LeftTrigger", leftTrigger, 0.05f, Time.deltaTime);
                }
                if (coverManager.coverNormal == Vector3.right) //MOVING INTO COVER FROM ABOVE
                {
                    animator.SetBool("LeftPeek", rightTrigger > 0f);
                    animator.SetFloat("LeftTrigger", rightTrigger, 0.05f, Time.deltaTime);
                }
                if (coverManager.coverNormal == Vector3.forward)//MOVING INTO COVER FROM THE LEFTSIDE
                {
                    animator.SetBool("LeftPeek", leftTrigger > 0f);
                    animator.SetFloat("LeftTrigger", leftTrigger, 0.05f, Time.deltaTime);
                }
                if (coverManager.coverNormal == Vector3.back) //MOVING INTO COVER FROM THE RIGHTSIDE
                {
                    animator.SetBool("LeftPeek", leftTrigger > 0f);
                    animator.SetFloat("LeftTrigger", leftTrigger, 0.05f, Time.deltaTime);
                }
            }
            if (coverManager.closeToRightEdge)
            {
                if (coverManager.coverNormal == Vector3.left)  //MOVING INTO COVER FROM BELOW
                {
                    animator.SetBool("RightPeek", rightTrigger > 0f);
                    animator.SetFloat("RightTrigger", rightTrigger, 0.05f, Time.deltaTime);
                }
                if (coverManager.coverNormal == Vector3.right) //MOVING INTO COVER FROM ABOVE
                {
                    animator.SetBool("RightPeek", leftTrigger > 0f);
                    animator.SetFloat("RightTrigger", leftTrigger, 0.05f, Time.deltaTime);
                }
                if (coverManager.coverNormal == Vector3.forward)//MOVING INTO COVER FROM THE LEFTSIDE
                {
                    animator.SetBool("RightPeek", rightTrigger > 0f);
                    animator.SetFloat("RightTrigger", rightTrigger, 0.05f, Time.deltaTime);
                }
                if (coverManager.coverNormal == Vector3.back) //MOVING INTO COVER FROM THE RIGHTSIDE
                {
                    animator.SetBool("RightPeek", rightTrigger > 0f);
                    animator.SetFloat("RightTrigger", rightTrigger, 0.05f, Time.deltaTime);
                }
            }
        }
        if(animator.GetBool("LeftPeek") || animator.GetBool("RightPeek"))
        {
            currentPlayerState = PlayerState.Peeking;
        }
        else
        {
            if(animator.GetBool("IsCover"))
            {
                if(!animator.GetBool("IsCoverCrouch"))
                {
                    currentPlayerState = PlayerState.Cover;
                }
                else
                {
                    currentPlayerState = PlayerState.CoverCrouch;
                }   
            }
        }
    }

    // Event handler to disable player movement
    void DisablePlayerMovement()
    {
        movementInput = Vector2.zero;
        animator.SetFloat("InputMagnitude", 0, 0.05f, Time.deltaTime);
        animator.SetBool("IsMoving", false);
        canMove = false;
    }

    IEnumerator FallDelay()
    {
        animator.ResetTrigger("IsFalling");
        animator.SetTrigger("HasLanded");
        isFalling = false;
        characterController.stepOffset = originalStepOffset;
        characterController.radius = originalRadius;
        yield return new WaitForSeconds(0.7f);
        disableInput = false;
    }

}