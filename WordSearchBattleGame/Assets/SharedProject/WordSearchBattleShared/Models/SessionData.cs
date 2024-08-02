using System;
using WordSearchBattleShared.Enums;

namespace WordSearchBattleShared.Models
{
    [Serializable]
    public struct SessionData
    {
        public SocketDataType DataType;
        public string Data;
    }

    [Serializable]
    public struct WordItem
    {
        public string Word;
        public int StartX;
        public int StartY;
        public DirectionEnum Direction;
        public string PlayerName;
    }

    [Serializable]
    public struct PlayerJoinInfo
    {
        public string PlayerName;
        public string RoomCode;
    }

    [Serializable]
    public struct PlayerJoinedInfo
    {
        public bool IsJoined;
        public int PlayerCount;
        public string PlayerName;
    }

    public enum SocketDataType
    {
        Error = 0,
        Start = 1,
        End = 2,
        WordCompleted = 3,
        PlayerJoined = 4,
    }
}
