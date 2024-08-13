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


public class GameBoardLogic
{
    public const string PLAYER_PREFS = "player_prefs";

    private bool _isGameOver = true;
    private PlayerType _startingPlayer = PlayerType.PlayerO;
    private PlayerType _currentPlayer;

    private GameView _gameView;
    private UserActionEvents _userActionEvents;
    private GameApiService _gameAPI;
    private GameDataObject _gameDataObject;
    private GridManager _gridManager;
    private WordListManager _wordListManager;
    private GameClient _gameClient;
    private HighlightManager _highlightManager;
    private List<KeyValuePair<PlayerType, BoardTilePosition>> _playerMoveList = new();

    private string _savedMemoryJsonData = string.Empty;

    public GameBoardLogic(GameView gameView,
                          UserActionEvents userActionEvents,
                          GameApiService gameAPI,
                          GameDataObject gameDataObject,
                          GridManager gridManager,
                          WordListManager wordListManager,
                          GameClient _GameClient,
                          HighlightManager highlightManager)
    {
        _gameView = gameView;
        _userActionEvents = userActionEvents;
        _gameAPI = gameAPI;
        _gameDataObject = gameDataObject;
        _gridManager = gridManager;
        _wordListManager = wordListManager;
        _gameClient = _GameClient;
        _highlightManager = highlightManager;
    }

    public void Initialize()
    {
        // Subscribe to user action events
        _userActionEvents.TileClicked += UserActionEvents_TileClicked;
        _userActionEvents.StartGameClicked += UserActionEvents_StartGameClicked;
        _userActionEvents.LoginToSocketClicked += UserActionEvents_LoginClicked;

        _gridManager.OnWordSelect = CheckWordResult;
        _gameClient.OnGameStart = SetupGameFromString;
        _gameClient.OnPlayerJoined = OnPlayerJoined;
        _gameClient.OnWordComplete = MarkWord;
    }

    private void MarkWord(WordItem item)
    {
        _wordListManager.MarkWordCompleted(item.Word, item.PlayerName);
        Position start = new() { X = item.StartX, Y = item.StartY };

        var length = item.Word.Length - 1;

        var end = PositionHelper.GetEndPosition(start, length, item.Direction);


        _highlightManager.CreateHighlightBar(start,
                                             end,
                                             size: _gridManager.rows,
                                             60f);
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
                                                 60f,
                                                 Color.green);
        }


        if (!_gameDataObject._wordList.Contains(value.Word))
            return;

        value.PlayerName = _gameClient.playerJoinInfo.PlayerName;

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
        // Unsubscribe from user action events
        _userActionEvents.TileClicked -= UserActionEvents_TileClicked;
        _userActionEvents.StartGameClicked -= UserActionEvents_StartGameClicked;
        _userActionEvents.LoginToSocketClicked -= UserActionEvents_LoginClicked;

        _gameView = null;
        _userActionEvents = null;
    }

    private void UserActionEvents_StartGameClicked()
    {
        //_gameAPI.StartCoroutine(_gameAPI.GetRandomWordSearchCoroutine(SetupGameFromString));

        _gameClient.SendGameStart();

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

    private void SetupGameFromString(string apires)
    {
        apires = apires.Replace("Item1", "item1").Replace("Item2", "item2");
        var result = JsonUtility.FromJson<WordSearchBoardModel>(apires);

        _gameDataObject._wordList = result.item1;
        _gameDataObject._letterGrid = ConvertToCharArray(result.item2, '|');

        SetupGame();
    }

    private async void UserActionEvents_LoginClicked()
    {
        _gameClient.playerJoinInfo.RoomCode = _gameDataObject._roomCode;
        _gameClient.playerJoinInfo.PlayerName = string.IsNullOrEmpty(_gameDataObject._playerName) ? "default" : _gameDataObject._playerName;

        await _gameClient.ConnectToServerAsync();
    }

    private void UserActionEvents_TileClicked(BoardTilePosition tilePos)
    {
        // If the game is over, don't allow the tiles to be clicked
        if (_isGameOver)
            return;

        // Add the current move to the list of moves both players have made
        _playerMoveList.Add(new KeyValuePair<PlayerType, BoardTilePosition>(_currentPlayer, tilePos));
        _gameView.SetTileSign(_currentPlayer, tilePos);

        // Clear the move list and set game over so that tiles cannot be clicked
        //if (gameWon || gameTie)
        //{
        //    _isGameOver = true;
        //    _playerMoveList.Clear();

        //    if (gameWon)
        //        _gameView.GameWon(_currentPlayer);
        //    else if (gameTie)
        //        _gameView.GameTie();
        //}

        // Swap current player
        //_currentPlayer.Swap();
        _gameView.ChangeTurn(_currentPlayer);
    }
}