using System;
using System.Drawing;
using TMPro;
using UnityEngine;
using WordSearchBattle.Scripts;

[Serializable]
public class BoardEvents : UserActionEvents
{
    public override event Action StartGameClicked;
    public override event Action<bool> LoginToSocketClicked;
    public override event Action<KnownColor> ColorPicked;

    public void OnStartGameClicked() 
        => StartGameClicked?.Invoke();

    public void OnLoginToSocketClicked(bool local)
        => LoginToSocketClicked?.Invoke(local);

    public void OnColorPicked(KnownColor color)
        => ColorPicked?.Invoke(color);
}
