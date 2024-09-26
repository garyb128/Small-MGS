using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Rewired;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // Reference to UI elements
    public GameObject vrCanvas;

    GameObject currentCanvas;

    private void OnEnable()
    {
        // Subscribe to the GameManager events
        GameManager.OnMissionCompleteEvent += ShowMissionCompleteScreen;
        GameManager.OnGameOverEvent += ShowGameOverScreen;
        GameManager.OnPauseEvent += PauseGame;
        GameManager.OnUnPausedEvent += UnPauseGame;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Unsubscribe from the GameManager events
        GameManager.OnMissionCompleteEvent -= ShowMissionCompleteScreen;
        GameManager.OnGameOverEvent -= ShowGameOverScreen;
        GameManager.OnPauseEvent -= PauseGame;
        GameManager.OnUnPausedEvent -= UnPauseGame;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset your singleton's values here
        // Set initial timescale so code can execute
        Time.timeScale = 1f;

        switch(GameManager.Instance.currentGameMode)
        {
            case GameManager.GameMode.MainMenu:
            break;
            case GameManager.GameMode.Story:
            break;
            case GameManager.GameMode.VR:
            currentCanvas = Instantiate(vrCanvas);
            //Change map to Player
            GameInputManager.Instance.SetUIInput();
            Time.timeScale = 0f; // Freeze time
            break;
        }
    }

    void OnSceneUnloaded(Scene scene, LoadSceneMode mode)
    {
        // Reset your singleton's values here
        // Set initial timescale so code can execute
        Time.timeScale = 1f;

        switch (GameManager.Instance.currentGameMode)
        {
            case GameManager.GameMode.MainMenu:
                break;
            case GameManager.GameMode.Story:
                break;
            case GameManager.GameMode.VR:
                currentCanvas = Instantiate(vrCanvas);
                // Initially hide the restart button
                GameInputManager.Instance.SetUIInput();
                Time.timeScale = 0f; // Freeze time
                break;
        }
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

    void Update()
    {
        if(GameInputManager.Instance.player.GetButtonDown("UISubmit"))
        {
            ConfirmSelection();
        }
    }

    public void ConfirmSelection()
    {
        // Check if the current game mode is the one you want the submit action to work in
        if (GameManager.Instance.currentGameMode == GameManager.GameMode.VR)
        {
            // Your submit logic here
            if (currentCanvas.GetComponent<VR_UIContainer>().missionOverviewScreen.activeSelf)
            {
                GameInputManager.Instance.SetGameplayInput();
                currentCanvas.GetComponent<VR_UIContainer>().missionOverviewScreen.SetActive(false);
                Time.timeScale = 1f; // Resume time
            }
        }
    }

    //Pause menu show
    public void PauseGame()
    {
        if(!currentCanvas.GetComponent<VR_UIContainer>().missionOverviewScreen.activeSelf)
        {
            currentCanvas.GetComponent<VR_UIContainer>().pauseScreen.SetActive(true);
            EventSystem.current.SetSelectedGameObject(vrCanvas.GetComponent<VR_UIContainer>().pauseScreen.GetComponentInChildren<Button>().gameObject);
        }
    }

    public void UnPauseGame()
    {
        if (!currentCanvas.GetComponent<VR_UIContainer>().missionOverviewScreen.activeSelf)
        {
            currentCanvas.GetComponent<VR_UIContainer>().pauseScreen.SetActive(false);
        }
    }

    // Call this method to show the game over screen
    public void ShowGameOverScreen()
    {
        currentCanvas.GetComponent<VR_UIContainer>().gameOverScreen.SetActive(true);
        EventSystem.current.SetSelectedGameObject(currentCanvas.GetComponent<VR_UIContainer>().gameOverScreen.GetComponentInChildren<Button>().gameObject);
    }

    public void ShowMissionCompleteScreen()
    {
        currentCanvas.GetComponent<VR_UIContainer>().missionCompleteScreen.SetActive(true);
        EventSystem.current.SetSelectedGameObject(currentCanvas.GetComponent<VR_UIContainer>().missionCompleteScreen.GetComponentInChildren<Button>().gameObject);
    }
}
