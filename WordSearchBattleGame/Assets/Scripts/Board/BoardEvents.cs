using System;
using TMPro;
using UnityEngine;
using WordSearchBattle.Scripts;

[Serializable]
public class BoardEvents : UserActionEvents
{
    public override event Action StartGameClicked;
    public override event Action LoginToSocketClicked;

    public void OnStartGameClicked() 
        => StartGameClicked?.Invoke();

    public void OnLoginToSocketClicked()
        => LoginToSocketClicked?.Invoke();


}
