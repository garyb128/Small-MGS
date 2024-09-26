using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Rendering;

public class PatrolPointsEditor : EditorWindow
{
    private bool isPlacingPoints;
    private EnemyController currentEnemy;
    private List<GameObject> patrolPoints = new List<GameObject>();
    int patrolPointCounter;

    [MenuItem("Tools/Patrol Points Editor")]
    private static void OpenWindow()
    {
        PatrolPointsEditor window = GetWindow<PatrolPointsEditor>();
        window.Show();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        patrolPoints.Clear();
        patrolPointCounter = 1;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlID);

        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0 && e.alt)
        {
            // Consume the mouse event to prevent Unity from selecting other objects
            e.Use();

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Vector3 mousePosition = hit.point;

                if (isPlacingPoints)
                {
                    CreatePatrolPoint(mousePosition);
                    SceneView.RepaintAll();

                    Debug.Log("Patrol Point created at: " + mousePosition);
                }
            }
        }

        DrawPatrolPointPreview(sceneView);
    }

    private void OnGUI()
    {
        GUILayout.Label("Hold 'Alt' and click in the Scene view to place a patrol point.");

        GUILayout.BeginHorizontal();
        currentEnemy = EditorGUILayout.ObjectField("Enemy", currentEnemy, typeof(EnemyController), true) as EnemyController;

        EditorGUI.BeginDisabledGroup(currentEnemy == null);
        if (GUILayout.Button(isPlacingPoints ? "Stop Placing Points" : "Place Patrol Points"))
        {
            isPlacingPoints = !isPlacingPoints;
            if (!isPlacingPoints && currentEnemy != null)
            {
                // When stopping placing points, copy the points to the enemy's list
                currentEnemy.patrolPoints = new List<GameObject>(patrolPoints);
            }
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.EndHorizontal();

        // Display a list of patrol points with their locations
        if (currentEnemy != null && currentEnemy.patrolPoints != null)
        {
            GUILayout.Label("Patrol Points:");
            for (int i = 0; i < currentEnemy.patrolPoints.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Point {i + 1}:");
                currentEnemy.patrolPoints[i] = EditorGUILayout.ObjectField(currentEnemy.patrolPoints[i], typeof(GameObject), true) as GameObject;

                // Display the location of the patrol point
                if (currentEnemy.patrolPoints[i] != null)
                {
                    Vector3 pointLocation = currentEnemy.patrolPoints[i].transform.position;
                    GUILayout.Label($"Location: ({pointLocation.x:F2}, {pointLocation.y:F2}, {pointLocation.z:F2})");
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        GUILayout.FlexibleSpace();
    }

    private void CreatePatrolPoint(Vector3 position)
    {
        // Ensure there is a selected enemy to assign patrol points
        if (currentEnemy == null)
        {
            Debug.LogError("No enemy selected for patrol points.");
            return;
        }

        // Find or create the parent object with a unique number based on the enemy's name
        string parentName = "Patrol Points " + currentEnemy.name;
        GameObject patrolPointsParent = GameObject.Find(parentName);
        if (patrolPointsParent == null)
        {
            patrolPointsParent = new GameObject(parentName);
        }

        // Create the new patrol point as a child of the uniquely named parent object
        GameObject patrolPoint = new GameObject($"Patrol Point {patrolPointCounter}");
        patrolPoint.transform.position = position;
        patrolPoint.transform.parent = patrolPointsParent.transform;

        // Add the new patrol point to the enemy's list if it's not already there
        if (!currentEnemy.patrolPoints.Contains(patrolPoint))
        {
            currentEnemy.patrolPoints.Add(patrolPoint);
        }

        // Increment the counter for the next patrol point
        patrolPointCounter++;

        // Repaint the editor window to update the list of waypoints
        Repaint();
    }


    private void DrawPatrolPointPreview(SceneView sceneView)
    {
        if (isPlacingPoints)
        {
            Handles.color = Color.yellow;
            foreach (GameObject point in patrolPoints)
            {
                Handles.SphereHandleCap(0, point.transform.position, Quaternion.identity, 0.5f, EventType.Repaint);
            }
        }
    }

    // Add this method to your PatrolPointsEditor class
    void OnDrawGizmos()
    {
        if (currentEnemy != null && patrolPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (GameObject point in patrolPoints)
            {
                // Draw a small sphere at the patrol point's position
                Gizmos.DrawSphere(point.transform.position, 0.5f);
            }
        }
    }

}
