using Assets.Scripts.GameData;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using WordSearchBattleShared.Models;

public class PlayerListManager : MonoBehaviour
{
    [SerializeField] private GameDataObject _gameDataObject;
    [SerializeField] private Transform _playerListObject;


    private void Awake()
    {
        _gameDataObject.OnPlayerListChanged.AddListener(PlayerListChanged);
    }

    private void OnEnable()
    {
        CreateFromList();
    }

    private void OnDisable()
    {
        DestroyChildren();
    }

    private void DestroyChildren()
    {
        foreach (Transform child in _playerListObject.transform)
            Destroy(child.gameObject);
    }

    private void CreateFromList()
    {
        foreach (var player in _gameDataObject._playerList)
            CreatePlayerInList(player);
    }

    private void PlayerListChanged(PlayerInfo? player, bool join)
    {
        if (player == null)
        {
            DestroyChildren();
            CreateFromList();

            return;
        }

        if (join)
            CreatePlayerInList(player.Value);
        else
            RemovePlayer(player.Value);
    }

    private void RemovePlayer(PlayerInfo player)
    {
        var trans = _playerListObject.transform.Find(player.PlayerId.ToString());
        if (trans != null)
            Destroy(trans.gameObject);
    }

    private void CreatePlayerInList(PlayerInfo player)
    {
        GameObject newChild = new(player.PlayerId.ToString());
        newChild.transform.SetParent(_playerListObject, false);

        var tmp = newChild.AddComponent<TextMeshProUGUI>();
        tmp.text = player.PlayerName;
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
    }
}
