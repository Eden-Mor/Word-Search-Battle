using System;
using System.Collections.Generic;
using System.Drawing;
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
    public struct GameSettingsItem
    {
        public int WordCount;
        public string Theme;
    }

    [Serializable]
    public struct GameStartItem
    {
        public List<string> WordList;
        public string LetterGrid;
        public List<PlayerInfo> PlayerList;
    }

    [Serializable]
    public struct WordItem
    {
        public string Word;
        public int StartX;
        public int StartY;
        public DirectionEnum Direction;
        public int PlayerId;
        public KnownColor Color;
    }

    [Serializable]
    public struct ColorPickerItem
    {
        public int PlayerId;
        public KnownColor OldColor;
        public KnownColor NewColor;
    }

    [Serializable]
    public struct PlayerJoinedInfo
    {
        public bool IsJoined;
        public int PlayerCount;
        public string PlayerName;
        public int PlayerId;
    }

    [Serializable]
    public struct PlayerInfo
    {
        public int PlayerId;
        public string PlayerName;
        public KnownColor ColorEnum;
    }

    [Serializable]
    public struct PlayerResultInfo
    {
        public string PlayerName;
        public KnownColor ColorEnum;
        public int WordsCorrect;
        public int PlayerId;
    }

    public struct EndData
    {
        public List<PlayerResultInfo> PlayerResultList;
    }

    [Serializable]
    public struct JoinRequestInfo
    {
        public string PlayerName;
        public string RoomCode;
    }

    public enum SocketDataType
    {
        Error = 0,
        Start = 1,
        End = 2,
        WordCompleted = 3,
        PlayerJoined = 4,
        ColorChanged = 5,
        PlayerDetails = 6
    }
}
