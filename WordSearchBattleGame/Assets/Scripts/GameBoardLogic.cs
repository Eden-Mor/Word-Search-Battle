using UnityEngine;
using WordSearchBattle.Scripts;
using Assets.Scripts.API;
using Assets.Scripts.GameData;
using Assets.Scripts.Board;
using System.Text;
using WordSearchBattleShared.Models;
using WordSearchBattleShared.API;
using WordSearchBattleShared.Helpers;
using Assets.Helpers;
using System.Drawing;
using System.Linq;


public class GameBoardLogic
{
    #region Constructor
    private GameView _gameView;
    private UserActionEvents _userActionEvents;
    private GameApiService _gameAPI;
    private GameDataObject _gameDataObject;
    private GridManager _gridManager;
    private WordListManager _wordListManager;
    private GameClient _gameClient;
    private HighlightManager _highlightManager;
    private ColorPickerManager _colorPickerManager;

    public GameBoardLogic(GameView gameView,
                          UserActionEvents userActionEvents,
                          GameApiService gameAPI,
                          GameDataObject gameDataObject,
                          GridManager gridManager,
                          WordListManager wordListManager,
                          GameClient _GameClient,
                          HighlightManager highlightManager,
                          ColorPickerManager colorPickerManager)
    {
        _gameView = gameView;
        _userActionEvents = userActionEvents;
        _gameAPI = gameAPI;
        _gameDataObject = gameDataObject;
        _gridManager = gridManager;
        _wordListManager = wordListManager;
        _gameClient = _GameClient;
        _highlightManager = highlightManager;
        _colorPickerManager = colorPickerManager;
    }

#endregion

    public void Initialize()
    {
        // Subscribe to user action events
        _userActionEvents.LoginToSocketClicked += UserActionEvents_LoginClicked;
        _userActionEvents.ColorPicked += UserActionEvents_ColorPicked;

        _gridManager.OnWordSelect = CheckWordResult;

        _gameClient.OnGameStart = SetupGameFromString;
        _gameClient.OnPlayerJoined = OnPlayerJoined;
        _gameClient.OnPlayerLeft = PlayerLeft;
        _gameClient.OnWordComplete = MarkWord;
        _gameClient.OnColorPicked = ColorPicked;
        _gameClient.OnSocketClose.AddListener(OnSocketClosed); 
    }

    private void OnSocketClosed()
    {
        _colorPickerManager?.ClearColors();
    }


    private void UserActionEvents_ColorPicked(KnownColor color)
        => _gameClient.SendColorPickRequest(color);


    private void ColorPicked(ColorPickerItem item)
    {
        var isSelf = item.PlayerId == _gameClient.PlayerDetails.PlayerId;
        _colorPickerManager.ColorChosen(item.NewColor, isSelf);
        _colorPickerManager.ColorUnChosen(item.OldColor);
    }

    private void MarkWord(WordItem item)
    {
        var player = _gameDataObject._playerList.First(x => x.PlayerId == item.PlayerId);
        _wordListManager.MarkWordCompleted(item.Word, player.PlayerName);
        Position start = new() { X = item.StartX, Y = item.StartY };

        var length = item.Word.Length - 1;
        var end = PositionHelper.GetEndPosition(start, length, item.Direction);

        var color = System.Drawing.Color.FromKnownColor(item.Color);
        _highlightManager.CreateHighlightBar(start,
                                             end,
                                             size: _gridManager.rows,
                                             color.ToUnityColor());
    }


    private void OnPlayerJoined(PlayerJoinedInfo info)
    {
        _gameDataObject.AddPlayer(info);
    }

    private void PlayerLeft(PlayerInfo info)
    {
        _gameDataObject.RemovePlayer(info);
        _colorPickerManager.ColorUnChosen(info.ColorEnum);
    }

    private void CheckWordResult(WordItem value)
    {
        //Uncomment to show debug green highlights.
        //Position start = new() { X = value.StartX, Y = value.StartY };
        //var length = value.Word.Length - 1;
        //var end = PositionHelper.GetEndPosition(start, length, value.Direction);
        //_highlightManager.CreateHighlightBar(start, end, size: _gridManager.rows, System.Drawing.Color.Green.ToUnityColor());

        if (!_gameDataObject._wordList.Contains(value.Word))
            return;

        value.PlayerId = _gameClient.PlayerDetails.PlayerId;
        _gameClient.SendWordFound(value);
    }


    private void SetupGame()
    {
        if (_gameDataObject._wordList.Count <= 0)
            return;

        _highlightManager.ResetHighlights();

        var size = (int)Mathf.Sqrt(_gameDataObject._letterGrid.Length);
        _gridManager.columns = size;
        _gridManager.rows = size;
        _gridManager.CreateGrid();

        _wordListManager.PopulateList(_gameDataObject._wordList);

        CanvasStateController.DisplayGame();
    }

    private void SetupGameFromString(GameStartItem gameStartInfo)
    {
        _gameDataObject._wordList = gameStartInfo.WordList;
        _gameDataObject._letterGrid = GridArrayHelper.ConvertToCharArray(gameStartInfo.LetterGrid, '|');
        _gameDataObject.ChangePlayerList(gameStartInfo.PlayerList);

        SetupGame();
    }

    private void UserActionEvents_LoginClicked(bool local)
    {
        _gameClient.CreateRoom(local);
    }

}