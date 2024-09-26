using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{   
    //Reference to the enemy controller
    EnemyController enemyController;

    // The radius within which targets are considered visible
    public float viewRadius;

    // The angle of the field of view
    [Range(0, 360)]
    public float viewAngle;

    // The layers to consider when detecting targets and obstacles
    public LayerMask targetMask;
    public LayerMask obstacleMask;

    // A list to store the visible targets
    public Vector3 playerPosition;
    public bool spottedPlayer;

    // How far the player has to be within the radius to be spotted
    public float viewThreshold = 10f;

    // Start is called before the first frame update
    void Start()
    {
        // Start the coroutine to continuously find targets with a delay
        //StartCoroutine("FindTargetsWithDelay", 0.2f);
        enemyController = transform.root.GetComponentInChildren<EnemyController>();
    }

    void OnEnable()
    {
        // Subscribe to the GameManager events
        GameManager.OnMissionCompleteEvent += OnMissionComplete;
        GameManager.OnGameOverEvent += OnGameOver;
    }

    void OnDisable()
    {
        // Unsubscribe from the GameManager events
        GameManager.OnMissionCompleteEvent -= OnMissionComplete;
        GameManager.OnGameOverEvent -= OnGameOver;
    }

    // Handlers for the events
    void OnMissionComplete()
    {
        // Code to execute when the mission is complete
        // For example, stop finding targets
        StopCoroutine("FindTargetsWithDelay");
        // Additional code for when the mission is complete...
    }

    void OnGameOver()
    {
        // Code to execute when the game is over
        // For example, stop finding targets
        StopCoroutine("FindTargetsWithDelay");
        // Additional code for when the game is over...
    }

    void Update()
    {
        LookForTargets();
    }

    // // Coroutine to periodically find visible targets with a delay
    // IEnumerator FindTargetsWithDelay(float delay)
    // {
    //     while (true)
    //     {
    //         // Wait for the specified delay
    //         yield return new WaitForSeconds(delay);

    //         // Call the function to find visible targets
    //         LookForTargets();
    //     }
    // }

    // Function to find visible targets within the field of view
    void LookForTargets()
    {
        spottedPlayer = false;

        // Find all colliders within the view radius on the target layer
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, ~(1 << LayerMask.NameToLayer("Ignore Raycast")));

        // Iterate through each collider in the view radius
        foreach (Collider col in targetsInViewRadius)
        {
            // Check if the collider's GameObject is on the target layer
            int layerMask = 1 << col.transform.root.gameObject.layer;
            if ((layerMask & targetMask) != 0)
            {
                // Get the target's transform
                Transform target = col.transform;

                // Calculate the direction from the current position to the target
                Vector3 dirToTarget = (target.position - transform.position).normalized;

                // Check if the target is within the view angle
                if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
                {
                    // Calculate the distance to the target
                    float dstToTarget = Vector3.Distance(transform.position, target.position);
                    
                    // Check if there are no obstacles between the current position and the target
                    if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                    {
                        // The target is on the target layer, within the view radius, and there are no obstacles
                        playerPosition = target.position;
                        spottedPlayer = true;
                        
                        if (dstToTarget> viewThreshold)
                        {
                            Debug.Log("Spotted player, investigating");
                            enemyController.currentState = EnemyController.EnemyState.Surprised;
                        }   
                        else
                        {
                            Debug.Log("spotted by enemy!!");
                            enemyController.currentState = EnemyController.EnemyState.Alerted;
                            if (GameManager.Instance.currentGameMode == GameManager.GameMode.VR)
                            {
                                GameManager.Instance.GameOver();
                            }
                        }



                    }
                }
            }
        }
    }

    // Function to calculate the direction vector from a given angle
    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        // Adjust the angle based on whether it is global or local
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }

        // Calculate the direction vector from the adjusted angle
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
