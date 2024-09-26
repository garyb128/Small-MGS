using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDetect : MonoBehaviour
{
    public string playerTag = "Player"; // Set this to the tag of the player GameObject

    // Start is called before the first frame update
    void Start()
    {
        // Subscribe to the GameManager events
        GameManager.OnMissionCompleteEvent += OnMissionComplete;
    }

    // OnDestroy is called when the script instance is being destroyed
    void OnDestroy()
    {
        // Unsubscribe from the GameManager events
        GameManager.OnMissionCompleteEvent -= OnMissionComplete;
    }

    void Update()
    {
        // Check if any GameObject with the player tag is overlapping with the goal's collider
        Collider[] overlappingColliders = Physics.OverlapCapsule(transform.position, transform.position + Vector3.up * GetComponent<CapsuleCollider>().height, GetComponent<CapsuleCollider>().radius, ~(1 << LayerMask.NameToLayer("Ignore Raycast")));

        foreach (Collider collider in overlappingColliders)
        {
            if (collider.transform.root.CompareTag(playerTag))
            {
                // A GameObject with the player tag has entered the goal's collider
                Debug.Log("Player entered the goal!");
                GameManager.Instance.MissionComplete();
                // Perform any desired actions here (e.g., trigger a win condition, play a sound, etc.)
            }
        }
    }

    // Handler for the mission complete event
    void OnMissionComplete()
    {
        // Disable this script
        enabled = false;
    }
}
