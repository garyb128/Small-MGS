using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VR_UIContainer : MonoBehaviour
{
    [Header("Screen References")]
    public GameObject missionOverviewScreen;
    public GameObject gameOverScreen;
    public GameObject pauseScreen;
    public GameObject missionCompleteScreen;

    [Header("Button References")]
    public Button pauseResumeButton;
    public Button pauseQuitButton;
    public Button missionFailedRetryButton;
    public Button missionFailedQuitButton;
    public Button missionCompleteRetryButton;
    public Button missionCompleteQuitButton;

    private void Start()
    {
        // Add onClick listeners to the buttons
        pauseResumeButton.onClick.AddListener(UIManager.Instance.UnPauseGame);
        pauseQuitButton.onClick.AddListener(GameManager.Instance.QuitGame);
        missionFailedRetryButton.onClick.AddListener(GameManager.Instance.RestartGame);
        missionFailedQuitButton.onClick.AddListener(GameManager.Instance.QuitGame);
        missionCompleteRetryButton.onClick.AddListener(GameManager.Instance.RestartGame);
        missionCompleteQuitButton.onClick.AddListener(GameManager.Instance.QuitGame);
    }
}
