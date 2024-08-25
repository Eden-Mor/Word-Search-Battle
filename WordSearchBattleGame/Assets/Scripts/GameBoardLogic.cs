using UnityEngine;
using WordSearchBattle.Scripts;
using Assets.Scripts.API;
using System.Collections.Generic;
using Assets.Scripts.GameData;
using Assets.Scripts.Board;
using System.Text;
using WordSearchBattleShared.Models;
using WordSearchBattleShared.API;
using WordSearchBattleShared.Helpers;
using Assets.Helpers;
using System;
using System.Drawing;
using System.Linq;


public class GameBoardLogic
{
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


    public void Initialize()
    {
        // Subscribe to user action events
        _userActionEvents.StartGameClicked += UserActionEvents_StartGameClicked;
        _userActionEvents.LoginToSocketClicked += UserActionEvents_LoginClicked;
        _userActionEvents.ColorPicked += UserActionEvents_ColorPicked;

        _gridManager.OnWordSelect = CheckWordResult;

        _gameClient.OnGameStart = SetupGameFromString;
        _gameClient.OnPlayerJoined = OnPlayerJoined;
        _gameClient.OnWordComplete = MarkWord;
        _gameClient.OnColorPicked = ColorPicked;
        _gameClient.OnGameComplete = GameComplete;
    }

    private void GameComplete()
    {
        ShowHideColorMenu(true);
    }

    private void ColorPicked(ColorPickerItem item)
    {
        var isSelf = item.PlayerId == _gameClient.PlayerDetails.PlayerId;
        _colorPickerManager.ColorChosen(item.NewColor, isSelf);
        if (item.OldColor != KnownColor.Transparent)
            _colorPickerManager.ColorUnChosen(item.OldColor);
    }

    private void ShowHideColorMenu(bool show)
        => _colorPickerManager.ShowHideMenu(show);

    private void UserActionEvents_ColorPicked(KnownColor color) 
        => _gameClient.SendColorPickRequest(color);

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
        StringBuilder sb = new();
        sb.Append(info.PlayerName + " ");
        sb.Append(info.IsJoined ? "Joined" : "Left");
        sb.Append(" Count: " + info.PlayerCount);
        _gameView.AddPlayerJoinedText(sb.ToString());
    }


    private void CheckWordResult(WordItem value)
    {
        if (false) //DEBUG HIGHLIGHTS
        {
            Position start = new() { X = value.StartX, Y = value.StartY };

            var length = value.Word.Length - 1;

            var end = PositionHelper.GetEndPosition(start, length, value.Direction);

            _highlightManager.CreateHighlightBar(start,
                                                 end,
                                                 size: _gridManager.rows,
                                                 System.Drawing.Color.Green.ToUnityColor());
        }


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
    }

    public void DeInitialize()
    {
        _gameView = null;
        _userActionEvents = null;
    }

    private void UserActionEvents_StartGameClicked()
    {
        //_gameAPI.StartCoroutine(_gameAPI.GetRandomWordSearchCoroutine(SetupGameFromString));

        GameSettingsItem gameSettingsItem = new()
        {
            WordCount = 0,
            Theme = "test"
        };

        _gameClient.SendGameStart(gameSettingsItem);

        //// It's the next game, let the other player start
        //_gameView.ChangeTurn(_startingPlayer);
        //_currentPlayer = _startingPlayer;

        //_gameView.StartGame(_startingPlayer);

        //// Reset the game state
        //_isGameOver = false;
    }


    public static char[,] ConvertToCharArray(string input, char separator)
    {
        var sections = input.Split(separator);
        int numRows = sections.Length;
        int numCols = sections[0].Length;

        char[,] charArray = new char[numRows, numCols];

        for (int i = 0; i < numRows; i++)
            for (int j = 0; j < numCols; j++)
                charArray[i, j] = sections[i][j];

        return charArray;
    }

    private void SetupGameFromString(GameStartItem gameStartInfo)
    {
        ShowHideColorMenu(false);

        _gameDataObject._wordList = gameStartInfo.WordList;
        _gameDataObject._letterGrid = ConvertToCharArray(gameStartInfo.LetterGrid, '|');
        _gameDataObject._playerList = gameStartInfo.PlayerList;

        SetupGame();
    }

    private void UserActionEvents_LoginClicked(bool local)
    {
        _gameClient.playerJoinInfo.RoomCode = _gameDataObject._roomCode;
        _gameClient.playerJoinInfo.PlayerName = string.IsNullOrEmpty(_gameDataObject._playerName) ? "default" : _gameDataObject._playerName;

        _gameClient.ConnectToServer(local);
    }

}