using System.Drawing;
using WordSearchBattleShared.Enums;

namespace WordSearchBattleAPI.Models
{


    public class SessionData
    {
        public SocketDataType DataType { get; set; }
        public string? Data { get; set; }
    }

    public class WordItem
    {
        public string? Word { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public DirectionEnum Direction { get; set; }
        public KnownColor? Color { get; set; }
        public string? PlayerName { get; set; }
    }

    public class ColorPickerItem
    {
        public KnownColor? OldColor { get; set; }
        public KnownColor NewColor { get; set; }
    }

    public class PlayerJoinedInfo
    {
        public bool IsJoined { get; set; }
        public int PlayerCount { get; set; }
        public string? PlayerName { get; set; }
    }

    public class PlayerInfo
    {
        public int WordsCorrect { get; set; } = 0;
        public string? PlayerName { get; set; }
        public string? RoomCode { get; set; }
        public KnownColor? ColorEnum { get; set; }
    }

    public enum SocketDataType
    {
        Error = 0,
        Start = 1,
        End = 2,
        WordCompleted = 3,
        PlayerJoined = 4,
        ColorChanged = 5
    }
}
