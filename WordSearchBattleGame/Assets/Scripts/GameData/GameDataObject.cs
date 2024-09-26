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
        [SerializeField] public List<PlayerInfo> _playerList = new();
        public UnityEvent<bool> OnServerListChanged;
        public UnityEvent<PlayerInfo?, bool> OnPlayerListChanged;

        public UnityEvent<string> RoomCodeChanged;
        public Dictionary<string, int> ServerList { get; set; } = new();

        public void ServerListChanged(bool resultOk, Dictionary<string, int> serverRes)
        {
            ServerList = serverRes;
            OnServerListChanged?.Invoke(resultOk);
        }

        public void ChangePlayerList(List<PlayerInfo> playerList)
        {
            _playerList = playerList;
            OnPlayerListChanged?.Invoke(null, true);
        }

        public void AddPlayer(PlayerJoinedInfo player)
        {
            var playerInfo = new PlayerInfo
            {
                PlayerId = player.PlayerId,
                PlayerName = player.PlayerName,
            };

            _playerList.Add(playerInfo);
            OnPlayerListChanged?.Invoke(playerInfo, true);
        }

        public void RemovePlayer(PlayerInfo player)
        {
            var playerIndex = _playerList.FindIndex(0, x => x.PlayerId == player.PlayerId);
            if (playerIndex < 0)
                return;

            _playerList.RemoveAt(playerIndex);
            OnPlayerListChanged?.Invoke(player, false);
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
