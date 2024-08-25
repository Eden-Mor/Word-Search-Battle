using System.Collections.Generic;
using UnityEngine;
using WordSearchBattleShared.Models;

namespace Assets.Scripts.GameData
{
    public class GameDataObject : MonoBehaviour
    {
        [SerializeField] public List<string> _wordList;
        [SerializeField] public char[,] _letterGrid;
        [SerializeField] public string _roomCode;
        [SerializeField] public string _playerName;
        [SerializeField] public List<PlayerInfo> _playerList;

        public void SetRoomCode(string roomCode)
            => _roomCode = roomCode;

        public void SetPlayerName(string playerName)
            => _playerName = playerName;
    }
}
