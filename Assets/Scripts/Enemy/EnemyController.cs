using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;
using JSAM;
using Unity.VisualScripting;
using Rewired.Demos;
using UnityEngine.Audio;

public class EnemyController : MonoBehaviour
{
    public enum EnemyState
    {
        Patrolling,
        Standing,
        Alerted,
        Surprised,
        Investigating,
        FinishedInvestigating,
        Chasing,
        Searching,
        Sleeping,
        KnockedOut
        // Add more states as needed
    }

    // State variables
    public EnemyState startingState;
    public EnemyState currentState;

    // Position and rotation
    Vector3 startingPosition;
    Quaternion startingRotation;
    Vector3 lastKnownPlayerPosition; // Store the last known player position.

    // Delays and timers
    public float surprisedDelay = 2f; // How long to remain surprised for
    public float investigateDelay = 2f; // How long to investigate for
    public float chaseCooldownTimer = 3f; // Set the chase cooldown time
    public float searchTimer = 5f; // Set the duration for searching

    // Flags
    bool isLooking;
    bool hasReachedDestination = false;
    Vector3 randomDestination;

    // Patrol points
    public List<GameObject> patrolPoints;

    // Character controller and movement input
    CharacterController characterController;
    float magnitude; // Movement vector

    // Speed-related variables
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float crouchSpeed = 2.5f;
    float originalSpeed;
    float originalStepOffset; // Original step offset of the character controller
    float ySpeed = 0f; // Initial vertical speed
    float terminalVelocity = 9.6f; // Unity units per second (adjust as needed)
    float decayFactor = 0.995f; // Adjust this value (close to 1) for gradual decay

    // Field of view and navigation
    FieldOfView fov;
    NavMeshAgent agent;

    // Patrol point index
    int currentPatrolPointIndex;

    // Animator and reaction object
    Animator animator;
    public GameObject enemyReaction; // Reaction to what is happening

    private void Start()
    {
        agent = GetComponentInChildren<NavMeshAgent>();
        agent.speed = moveSpeed;
        fov = GetComponentInChildren<FieldOfView>();
        animator = GetComponentInChildren<Animator>();

        if (startingState == EnemyState.Patrolling && patrolPoints.Count > 0)
        {
            agent.SetDestination(patrolPoints[0].transform.position);//Always walk to the waypoint
            animator.SetBool("IsMoving", true);
        }

        currentState = startingState;

        // Store the initial position and rotation of this enemy for states like standing or sleeping
        startingPosition = transform.position;
        startingRotation = transform.rotation;
    }

    private void Update()
    {
        // Check for player spotted and update state accordingly
        if (fov.spottedPlayer)
        {
            lastKnownPlayerPosition = fov.playerPosition; // Update the last known player position
        }

        //Set input magnitude
        magnitude = agent.velocity.magnitude;
        magnitude = Mathf.Clamp01(magnitude);
        animator.SetFloat("Move",magnitude);

        if(magnitude <= 0.25f)
        {
            animator.SetBool("IsMoving",false);
        }
        else
        {
            animator.SetBool("IsMoving", true);
        }

        switch (currentState)
        {
            case EnemyState.Patrolling:
                PatrolBehavior();
                break;
            case EnemyState.Standing:
                StandingBehavior();
                break;
            case EnemyState.Chasing:
                ChaseBehavior();
                break;
            case EnemyState.Searching:
                SearchBehavior();
                break;
            case EnemyState.Alerted:
                AlertedBehaviour();
                break;
            case EnemyState.Surprised:
                SurprisedBehaviour();
                break;
            case EnemyState.Investigating:
                InvestigatingBehaviour();
                break;
            case EnemyState.FinishedInvestigating:
                FinishedInvestigatingBehaviour();
                break;
        }
    }

    void FixedUpdate()
    {
       
    }

    private void PatrolBehavior()
    {
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            SetNextPatrolPoint();
        }
    }

    private void StandingBehavior()
    {
        // Standing behavior code goes here
        if (transform.position != startingPosition)
        {
            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(transform.position, startingPosition, NavMesh.AllAreas, path);

            if (path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(path);
            }
            else
            {
                Debug.LogWarning("Unable to find a valid path to the starting position.");
            }
        }

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            // Convert Quaternion to Vector3 for position
            Vector3 targetDirection = startingRotation * Vector3.forward;

            // Calculate the rotation needed to face the starting rotation
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            // Smoothly rotate towards the target rotation
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 3f);
        }
    }

    private void ChaseBehavior()
    {
        // Chase the player using the position from FieldOfView
        if (fov.playerPosition != null)
        {
            agent.SetDestination(fov.playerPosition);
        }

        // If player is not spotted, start cooldown
        if (!fov.spottedPlayer)
        {
            chaseCooldownTimer -= Time.deltaTime;

            if (chaseCooldownTimer <= 0f)
            {
                currentState = EnemyState.Searching;
                chaseCooldownTimer = 3f; // Reset the chase cooldown timer
            }
        }
        else
        {
            // Reset cooldown timer when player is spotted again
            chaseCooldownTimer = 3f;
        }
    }


    void AlertedBehaviour()
    {

    }

    void SurprisedBehaviour()
    {
        agent.isStopped = true;

        if (fov.spottedPlayer)
        {
            transform.rotation.SetLookRotation(lastKnownPlayerPosition);
        }

        StartCoroutine(ReactionDelay(surprisedDelay));
    }

    void InvestigatingBehaviour()
    {
        //GO TOWARDS POSITION OF SOUND OR PLAYER IF SPOTTED
        if (fov.playerPosition != null)
        {
            agent.SetDestination(lastKnownPlayerPosition);

            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                agent.isStopped = true;

                StartCoroutine(ReactionDelay(surprisedDelay));

                currentState = EnemyState.FinishedInvestigating;

                StartCoroutine(ReactionDelay(investigateDelay/2));

                HandleSearchFinish();
            }
        }
    }

    void FinishedInvestigatingBehaviour()
    {
        //just trigger reaction again
       // enemyReaction.SetActive(true);
    }

    void SearchBehavior()
    {
        if (fov.viewRadius <= 0f)
        {
            Debug.LogError("View radius is not set. Please set a positive value for fov.viewRadius.");
            return;
        }

        if (hasReachedDestination)
        {
            // Add a random chance to look around
            if (Random.Range(0f, 1f) < 1f) // Adjust the chance as needed (here, 0.3 is 30% chance)
            {
                LookAround();
            }
            else
            {
                // Calculate a new random point within the view radius
                Vector2 randomPointInRadius = Random.insideUnitCircle.normalized * fov.viewRadius;
                randomDestination = lastKnownPlayerPosition + new Vector3(randomPointInRadius.x, 0f, randomPointInRadius.y);
                hasReachedDestination = false;
            }
        }

        if (Vector3.Distance(transform.position, lastKnownPlayerPosition) <= fov.viewRadius)
        {
            agent.SetDestination(randomDestination);

            if (agent.remainingDistance <= agent.stoppingDistance && !hasReachedDestination)
            {
                hasReachedDestination = true;

                // Start the search timer
                searchTimer -= Time.deltaTime;
                if (searchTimer <= 0f)
                {
                    HandleSearchFinish(); // Call the function to handle after finishing searching
                }
            }
        }
        else
        {
            hasReachedDestination = false; // Reset the flag when leaving the destination
            currentState = startingState; // Return to patrolling or standing after searching
            searchTimer = 5f; // Reset the search timer
        }
    }

    private void LookAround()
    {
        if (!isLooking)
        {
            isLooking = true;
            StartCoroutine(RotateTowardsTarget(1f));
        }
    }
    private IEnumerator RotateTowardsTarget(float duration)
    {
        float startAngleY = transform.eulerAngles.y;
        float angle1, angle2;

        // Randomly choose to look left or right first, then back
        int randDir = Random.Range(0, 2);
        if (randDir == 0)
        {
            angle1 = startAngleY - 90f;
            angle2 = startAngleY + 90f;
        }
        else
        {
            angle1 = startAngleY + 90f;
            angle2 = startAngleY - 90f;
        }

        // If the object is facing a wall, adjust the angles to turn away
        if (IsFacingWall())
        {
            angle1 += 180f;
            angle2 += 180f;
        }

        // Ensure angles are within 0-360 degrees range
        angle1 = NormalizeAngle(angle1);
        angle2 = NormalizeAngle(angle2);

        // Rotate to the first angle
        yield return StartCoroutine(RotateOverTime(startAngleY, angle1, duration));

        // Rotate to the second angle
        yield return StartCoroutine(RotateOverTime(angle1, angle2, duration));

        // Calculate a new random point within the view radius after rotation is done
        Vector2 randomPointInRadius = Random.insideUnitCircle.normalized * fov.viewRadius;
        randomDestination = lastKnownPlayerPosition + new Vector3(randomPointInRadius.x, 0f, randomPointInRadius.y);
        hasReachedDestination = false;

        isLooking = false; // Set the flag to false
    }

    private IEnumerator RotateOverTime(float startAngle, float endAngle, float duration)
    {
        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            transform.eulerAngles = new Vector3(
                transform.eulerAngles.x,
                Mathf.LerpAngle(startAngle, endAngle, timeElapsed / duration),
                transform.eulerAngles.z
            );
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        // Ensure the final rotation is exactly the end angle
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, endAngle, transform.eulerAngles.z);
    }

    private float NormalizeAngle(float angle)
    {
        angle = angle % 360f;
        if (angle < 0f)
        {
            angle += 360f;
        }
        return angle;
    }

    bool IsFacingWall()
    {
        RaycastHit hit;
        int layerMask = 1 << LayerMask.NameToLayer("Wall"); // Replace "Wall" with your layer's name

        // Perform a raycast forward from the object's position
        if (Physics.Raycast(transform.position, transform.forward, out hit, 1f, layerMask)) // 2f is the distance to check for the wall
        {
            // If the raycast hits something on the "Wall" layer, it's facing a wall
            return true;
        }
        return false;
    }

    private void HandleSearchFinish()
    {
        agent.isStopped = false;

        // Additional logic for after finishing searching
        switch (startingState)
        {
            case EnemyState.Patrolling:
                ReturnToClosestWaypoint();
                break;
            case EnemyState.Standing:
                ReturnToStartingPosition();
                break;
                // Add more cases as needed
        }
    }

    private void ReturnToClosestWaypoint()
    {
        // Add logic to find and set the closest waypoint as the destination
        if (patrolPoints.Count > 0)
        {
            float closestDistance = float.MaxValue;
            int closestWaypointIndex = 0;

            for (int i = 0; i < patrolPoints.Count; i++)
            {
                float distance = Vector3.Distance(transform.position, patrolPoints[i].transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestWaypointIndex = i;
                }
            }

            agent.SetDestination(patrolPoints[closestWaypointIndex].transform.position);
        }
    }

    private void ReturnToStartingPosition()
    {
        // Set the starting position as the destination
        agent.SetDestination(startingPosition);
    }

    private void SetNextPatrolPoint()
    {
        if (patrolPoints.Count == 0)
            return;

        currentPatrolPointIndex = (currentPatrolPointIndex + 1) % patrolPoints.Count;
        agent.SetDestination(patrolPoints[currentPatrolPointIndex].transform.position);
    }

    public void SetPatrollingState(bool patrol)
    {
        if (patrol)
        {
            currentState = EnemyState.Patrolling;
        }
        else
        {
            currentState = EnemyState.Standing;
            agent.ResetPath();
        }
    }


    //Simple coroutine where the delay between certains states occurs
    IEnumerator ReactionDelay(float delay)
    {
        enemyReaction.gameObject.SetActive(true);

        switch (currentState)
        {
            case EnemyState.Surprised:
                AudioManager.PlaySound(DefaultLibrarySounds.EnemySurprisedSounds);
                break;
        }

        yield return new WaitForSeconds(delay);

        enemyReaction.gameObject.SetActive(false);

        if (currentState == EnemyState.Surprised)
        {
            agent.isStopped = false;
            currentState = EnemyState.Investigating;
        }
        else if (currentState == EnemyState.Investigating)
        {
            currentState = EnemyState.FinishedInvestigating;
            HandleSearchFinish();
        }
    }

}
