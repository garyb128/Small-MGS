using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Reference to the player's transform
    public float height = 10f; // Height of the camera above the player
    public float distance = 10f; // Distance of the camera from the player
    public float rotationSpeed = 5f; // Speed of camera rotation
    public Vector3 initialRotation = new Vector3(40f, 90f, 0f); // Initial rotation of the camera

    void Start()
    {
        // Set the initial rotation of the camera
        transform.eulerAngles = initialRotation;
    }

    void Update()
    {
        // if (target == null)
        // {
        //     Debug.LogWarning("Target (Player) not assigned for camera follow!");
        //     return;
        // }

        // // Set the camera's position directly above the player
        // Vector3 newPosition = target.position + Vector3.up * height;
        // transform.position = newPosition;

        // // Look at the player from above with rotation
        // Quaternion targetRotation = Quaternion.Euler(initialRotation);
        // transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // // Optional: You can adjust the distance by moving the camera backward
        // transform.Translate(Vector3.back * distance);
    }
}
