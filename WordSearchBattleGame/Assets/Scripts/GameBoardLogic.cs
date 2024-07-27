using UnityEngine;
using WordSearchBattle.Scripts;
using System;
using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.API;
using Assets.Scripts.Models;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GameData;
using Assets.Scripts.Board; // Linq is used extensively to check for a win and also in other locations, the commands are very compact and easier to understand than without Linq in my opinion

/// <summary>
/// Eden Mor 02/01/2024
/// <see cref="GameBoardLogic"/> is the class used to display the Tic-Tac-Toe game, by handling the touch events using the provided GameAPI
/// </summary>
public class GameBoardLogic
{
    public const string PLAYER_PREFS = "player_prefs";

    private bool _isGameOver = true;
    private BoardGridSize _boardGridSize;

    private int _rows;
    private int _columns;

    private PlayerType _startingPlayer = PlayerType.PlayerO;
    private PlayerType _currentPlayer;

    private GameView _gameView;
    private UserActionEvents _userActionEvents;
    private GameApiService _gameAPI;
    private GameDataObject _gameDataObject;
    private GridManager _gridManager;
    private WordListManager _wordListManager;
    private List<KeyValuePair<PlayerType, BoardTilePosition>> _playerMoveList = new();

    private string _savedMemoryJsonData = string.Empty;

    public GameBoardLogic(GameView gameView, UserActionEvents userActionEvents, GameApiService gameAPI, GameDataObject gameDataObject, GridManager gridManager, WordListManager wordListManager)
    {
        _gameView = gameView;
        _userActionEvents = userActionEvents;
        _gameAPI = gameAPI;
        _gameDataObject = gameDataObject;
        _gridManager = gridManager;
        _wordListManager = wordListManager;
    }

    public void Initialize()
    {
        _gameAPI.StartCoroutine(_gameAPI.GetRandomWordSearchCoroutine(SetupGame));

        // Subscribe to user action events
        _userActionEvents.TileClicked += UserActionEvents_TileClicked;
        _userActionEvents.StartGameClicked += UserActionEvents_StartGameClicked;

        // Set grid size based on params
        _boardGridSize = new(_rows, _columns);

        _gridManager.actionOnWordSelect = CheckWordResult;
    }

    private void CheckWordResult(string value)
    {
        if (!_gameDataObject._wordList.Contains(value))
            return;

        _wordListManager.RemoveWordFromList(value);
        //Success!

    }

    private void SetupGame()
    {
        if (_gameDataObject._wordList.Count <= 0)
            return;

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

        _gameView = null;
        _userActionEvents = null;
    }

    private void UserActionEvents_StartGameClicked()
    {
        // It's the next game, let the other player start
        _gameView.ChangeTurn(_startingPlayer);
        _currentPlayer = _startingPlayer;

        _gameView.StartGame(_startingPlayer);

        // Reset the game state
        _isGameOver = false;
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