using Rewired;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInputManager : MonoBehaviour
{
    public static GameInputManager Instance { get; private set; }
    public int playerId = 0;
    [HideInInspector] public Player player;

    // void OnEnable()
    // {
    //    SceneManager.sceneLoaded += OnSceneLoaded;
    // }

    // void OnDisable()
    // {
    //     SceneManager.sceneLoaded -= OnSceneLoaded;
    // }

    // void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    // {
        
    // }

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
            player = ReInput.players.GetPlayer(playerId);
        }
    }

    public void SetUIInput()
    {
        player.controllers.maps.SetMapsEnabled(false, "Default"); // Disable gameplay maps
        player.controllers.maps.SetMapsEnabled(true, "UI"); // Enable UI maps
    }

    public void SetGameplayInput()
    {
        player.controllers.maps.SetMapsEnabled(false, "UI"); // Disable UI maps
        player.controllers.maps.SetMapsEnabled(true, "Default"); // Enable gameplay maps
    }

    public void SetFPInput()
    {
        player.controllers.maps.SetMapsEnabled(true, "FirstPerson"); // Disable FP MAP
    }

    public void DisableFPInput()
    {
        player.controllers.maps.SetMapsEnabled(false, "FirstPerson"); // Disable FP MAP
    }

    public void SetCoverInput()
    {
        player.controllers.maps.SetMapsEnabled(true, "Cover"); // Disable FP MAP
    }

    public void DisableCoverInput()
    {
        player.controllers.maps.SetMapsEnabled(false, "Cover"); // Disable FP MAP
    }
}
