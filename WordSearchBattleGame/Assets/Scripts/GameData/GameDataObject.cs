using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using WordSearchBattleShared.Models;

namespace Assets.Scripts.GameData
{
    public class GameDataObject : MonoBehaviour
    {
        //NEEDS TO BE CHANGED TO SERIALIZEDOBJECT

        [SerializeField] public List<string> _wordList;
        [SerializeField] public char[,] _letterGrid;
        [SerializeField] public string _roomCode;
        [SerializeField] public string _playerName;
        [SerializeField] public List<PlayerInfo> _playerList;
        public UnityEvent<bool> OnServerListChanged;

        public UnityEvent<string> RoomCodeChanged;
        public Dictionary<string, int> ServerList { get; set; } = new();

        public void ServerListChanged(bool resultOk, Dictionary<string,int> serverRes)
        {
            ServerList = serverRes;
            OnServerListChanged?.Invoke(resultOk);
        }

        public void SetRoomCode(string roomCode)
        {
            _roomCode = roomCode;
            RoomCodeChanged?.Invoke(roomCode);
        }

        public void SetPlayerName(string playerName)
            => _playerName = playerName;
    }
}
