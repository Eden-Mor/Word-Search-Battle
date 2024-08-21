using System.Drawing;
using WordSearchBattleShared.Enums;

namespace WordSearchBattleAPI.Models
{


    public class SessionData
    {
        public SocketDataType DataType { get; set; }
        public string? Data { get; set; }

        public override string ToString()
            => $"DataType: {DataType}, Data: {Data}";
    }

    public class GameSettingsItem
    {
        public int WordCount { get; set; } = 0;
        public string Theme { get; set; } = string.Empty;

        public override string ToString()
            => $"Words: {WordCount}, Theme: {Theme}";
    }

    public class GameStartItem
    {
        public List<string>? WordList { get; set; }
        public string? LetterGrid { get; set; }
        public List<PlayerInfo>? PlayerList { get; set; }
    }

    public class EndDataItem(List<PlayerResultInfo> playerResultInfos)
    {
        public List<PlayerResultInfo> PlayerResultList { get; set; } = playerResultInfos;
    }

    public class WordItem
    {
        public string? Word { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public DirectionEnum Direction { get; set; }
        public string? PlayerName { get; set; }
        public KnownColor? Color { get; set; }
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
        public string PlayerName { get; set; } = string.Empty;
        public KnownColor ColorEnum { get; set; } = KnownColor.Transparent;
    }

    public class PlayerResultInfo : PlayerInfo
    {
        public int WordsCorrect { get; set; } = 0;
    }

    public class JoinRequestInfo
    {
        public string? PlayerName { get; set; }
        public string? RoomCode { get; set; }
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
