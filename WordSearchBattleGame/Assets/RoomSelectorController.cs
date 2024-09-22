using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class RoomSelectorController : MonoBehaviour
{
    public TextMeshProUGUI roomCodeTxt;
    public TextMeshProUGUI playerCountTxt;
    private Action<string> OnJoinClicked;


    public void Initialize(string roomCode, int playerCount, int maxPlayerCount, Action<string> onJoinClicked)
    {
        OnJoinClicked = onJoinClicked;
        roomCodeTxt.text = roomCode;
        playerCountTxt.text = $"( {playerCount} / {maxPlayerCount} )";
    }

    public void JoinClicked() 
        => OnJoinClicked.Invoke(roomCodeTxt.text);

}
