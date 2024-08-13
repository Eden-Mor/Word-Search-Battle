using System;
using System.Linq;
using TMPro;
using UnityEngine;
using WordSearchBattle.Scripts;

[Serializable]
public class BoardView : GameView
{
    [SerializeField] private TextMeshProUGUI _gameWonWinnerText;
    [SerializeField] private GameObject _startGameButton;
    [SerializeField] private TextMeshProUGUI _roomCodeText;
    [SerializeField] private TextMeshProUGUI _playerJoinedText;

    public void Initialize()
    {
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
    
    public override void StartGame()
    {
        ResetBoard();
    }


    public override void GameWon()
    {
        GameEnd();
    }

    public override void GameLost()
    {
        GameEnd();
    }

    public override void GameTie()
    {
        GameEnd();
        _gameWonWinnerText.text = "Game Tie!";
    }

    private void GameEnd()
    {
        _startGameButton.gameObject.SetActive(true);
        _gameWonWinnerText.enabled = true;
    }
}
