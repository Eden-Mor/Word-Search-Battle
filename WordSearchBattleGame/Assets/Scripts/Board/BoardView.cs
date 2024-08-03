using System;
using System.Linq;
using TMPro;
using UnityEngine;
using WordSearchBattle.Scripts;

[Serializable]
public class BoardView : GameView
{
    [SerializeField] private Sprite _player0Image;
    [SerializeField] private Sprite _playerXImage;
    [SerializeField] private TextMeshProUGUI _gameWonWinnerText;
    [SerializeField] private TextMeshProUGUI _currentTurnText;
    [SerializeField] private GameObject _startGameButton;
    [SerializeField] private TextMeshProUGUI _roomCodeText;
    [SerializeField] private TextMeshProUGUI _playerJoinedText;

    private BoardTileView[] _tiles;

    public void Initialize(BoardTileView[] tileViews)
    {
        //_tiles = tileViews;
        
        //ResetBoard();
        _startGameButton.SetActive(true);
    }

    public override void AddPlayerJoinedText(string text)
    {
        _playerJoinedText.text += text + "\n";
    }

    private void ResetBoard()
    {
        //foreach (var boardTileView in _tiles)
        //{
        //    boardTileView.SetSprite(null);
        //}
        
        _startGameButton.gameObject.SetActive(false);
        //_gameWonWinnerText.enabled = false;
        //_currentTurnText.enabled = false;
    }
    
    public override void StartGame(PlayerType playerType)
    {
        ResetBoard();
        //_currentTurnText.enabled = true;
        //SetCurrentTurn(playerType);
    }

    public override void SetTileSign(PlayerType player, BoardTilePosition tilePosition)
    {
        var tile = _tiles.FirstOrDefault(tile => tile.Column == tilePosition.Column && tile.Row == tilePosition.Row);
        if(tile == null)
            throw new NullReferenceException($"Can't find tile in position X: {tilePosition.Row} Y: {tilePosition.Column}");

        tile.SetSprite(player == PlayerType.PlayerO ? _player0Image : _playerXImage);
    }

    public override void ChangeTurn(PlayerType player)
    {
        SetCurrentTurn(player);
    }

    public override void GameWon(PlayerType player)
    {
        GameEnd();
        _gameWonWinnerText.text = player == PlayerType.PlayerO ? "Player 0 Won!" : "Player X Won!";
    }

    public override void GameTie()
    {
        GameEnd();
        _gameWonWinnerText.text = "Game Tie!";
    }

    private void GameEnd()
    {
        _startGameButton.gameObject.SetActive(true);
        
        _currentTurnText.enabled = false;
        _gameWonWinnerText.enabled = true;
    }

    private void SetCurrentTurn(PlayerType player)
    {
        _currentTurnText.text = player == PlayerType.PlayerO ? "Player 0 Turn" : "Player X Turn";
    }
}
