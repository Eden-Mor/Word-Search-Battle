using System;
using UnityEngine;

public class CanvasStateController : MonoBehaviour
{
    public enum GameState
    {
        Landing = 0,
        GameSettings = 1,
        ServerSelect = 2,
        RoomSetup = 3,
        Game = 4
    }

    public GameObject landing;
    public GameObject gameSettings;
    public GameObject serverSelect;
    public GameObject roomSetup;
    public GameObject game;

    private static CanvasStateController _instance = null;

    void Awake()
    {
        if (_instance != null)
            return;

        _instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    void OnDestroy()
        => _instance = null;

    void Start()
        => DisplayLanding();

    private static void SetActiveState(GameState state)
    {
        var instance = Instance();

        instance.landing.SetActive(state == GameState.Landing);
        instance.gameSettings.SetActive(state == GameState.GameSettings);
        instance.serverSelect.SetActive(state == GameState.ServerSelect);
        instance.roomSetup.SetActive(state == GameState.RoomSetup);
        instance.game.SetActive(state == GameState.Game);
    }

    public static bool Exists() => _instance != null;

    public static CanvasStateController Instance()
    {
        if (!Exists())
            throw new Exception("CanvasStateController could not find the CanvasStateController object. Please ensure you have added CanvasStateController script to one of the objects.");
        
        return _instance;
    }

    public static void DisplayLanding()
        => SetActiveState(GameState.Landing);

    public static void DisplayGameSettings()
        => SetActiveState(GameState.GameSettings);

    public static void DisplayServerSelect()
        => SetActiveState(GameState.ServerSelect);

    public static void DisplayRoomSetup()
        => SetActiveState(GameState.RoomSetup);

    public static void DisplayGame()
        => SetActiveState(GameState.Game);
}
