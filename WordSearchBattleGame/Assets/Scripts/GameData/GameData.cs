using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.GameData
{
    public class GameDataObject : MonoBehaviour
    {
        [SerializeField] public List<string> _wordList;
        [SerializeField] public char[,] _letterGrid;
        [SerializeField] public string _roomCode;

        public void SetRoomCode(string roomCode) 
            => _roomCode = roomCode;
    }
}
