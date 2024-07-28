using System;
using TMPro;
using UnityEngine;
using WordSearchBattle.Scripts;

[Serializable]
public class BoardEvents : UserActionEvents
{
    public override event Action StartGameClicked;
    public override event Action<BoardTilePosition> TileClicked;
    public override event Action LoginToSocketClicked;

    [SerializeField]
    private TMP_Dropdown _gameStateSourceDropdown;
    
    private GameStateSource GetStateSource()
    {
        var stateSourceStr = _gameStateSourceDropdown.options[_gameStateSourceDropdown.value].text;
        if (!Enum.TryParse<GameStateSource>(stateSourceStr, out var stateSource))
            throw new ArgumentNullException($"Exception parsing {stateSourceStr} into {nameof(GameStateSource)}");
        
        return stateSource;
    }

    public void OnStartGameClicked() 
        => StartGameClicked?.Invoke();

    public void OnLoginToSocketClicked()
        => LoginToSocketClicked?.Invoke();

    public void OnTileClicked(BoardTileView tileView) 
        => TileClicked?.Invoke(new BoardTilePosition(tileView.Row, tileView.Column));

}
