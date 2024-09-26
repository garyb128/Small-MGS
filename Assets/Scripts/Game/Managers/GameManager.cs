using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        MissionInProgress,
        MissionComplete,
        GameOver,
        Paused,
    }

    public enum GameMode
    {
        MainMenu,
        Story,
        VR
    }

    public GameState currentGameState;
    public GameMode currentGameMode;

    [SerializeField] GameObject playerController;//Reference to playercontroller in the scene

    [SerializeField] List<GameObject> enemies = new List<GameObject>();//Reference to all enemies in scene

    // Define a delegate type for game state changes
    public delegate void GameStateChangeAction();

    // Define events based on the delegate type
    public static event GameStateChangeAction OnMissionCompleteEvent;
    public static event GameStateChangeAction OnGameOverEvent;
    public static event GameStateChangeAction OnPauseEvent;
    public static event GameStateChangeAction OnUnPausedEvent;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset your singleton's values here
        currentGameState = GameState.MissionInProgress;
        playerController = GameObject.FindGameObjectWithTag("Player");
        enemies = GameObject.FindGameObjectsWithTag("Enemy").ToList();
    }
    

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void MissionComplete()
    {
        // Trigger mission complete actions (e.g., display message, update score)
        Debug.Log("Mission Complete!");
        // Prevent further updates after mission is complete
        currentGameState = GameState.MissionComplete;
        //Invoke event
        OnMissionCompleteEvent?.Invoke();
        //Change map to UI
        GameInputManager.Instance.SetUIInput();
        //Freeze time
        //Time.timeScale = 0f;
    }

    public void GameOver()
    {
        // Trigger mission failed actions (e.g., display message, reset level)
        Debug.Log("Mission Failed!");
        // Prevent further updates after mission has failed
        currentGameState = GameState.GameOver;
        //Invoke event
        OnGameOverEvent?.Invoke();
        Time.timeScale = 0f;
        //Change map to UI
        GameInputManager.Instance.SetUIInput();
    }

    public void OnPause()
    {
        //Game has been paused
        Debug.Log("Game paused");
        currentGameState = GameState.Paused;
        OnPauseEvent?.Invoke();
        Time.timeScale = 0f;
        GameInputManager.Instance.SetUIInput();
    }

    public void OnUnPaused()
    {
        //Game has been unpaused
        Debug.Log("Game Unpaused");
        currentGameState = GameState.MissionInProgress;
        OnUnPausedEvent?.Invoke();
        Time.timeScale = 1f;
        GameInputManager.Instance.SetGameplayInput();
    }

    public void RestartGame()
    {
        // Get the current scene name
        string sceneName = SceneManager.GetActiveScene().name;
        // Reload the scene
        SceneManager.LoadScene(sceneName);
        currentGameState = GameState.MissionInProgress;
    }

    public void QuitGame()
    {
        Application.Quit();//For now quits, but will program in a proper exit o menu later on
    }
}
