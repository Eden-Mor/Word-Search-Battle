using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WordSearchBattleShared.Enums;

namespace WordSearchBattleAPI.Models
{
    public class GameSession
    {
        public GameSession() { }
        public GameSession(GameSessionStatus statusCode, string roomCode)
        {
            GameSessionStatusCode = statusCode;
            RoomCode = roomCode;
        }

        [Key]
        public int GameSessionId { get; set; }
        public GameSessionStatus GameSessionStatusCode { get; set; }
        public string? RoomCode { get; set; }
        public string? LetterGrid { get; set; }
        public string[]? WordList { get; set; }

    }

    public class PlayerGameSession
    {
        [Key]
        public int GameSessionId { get; set; }
        public int PlayerId { get; set; }
    }

    public class Player
    {
        [Key]
        public int PlayerId { get; set; }
        public string? Name { get; set; }
        public int GamesPlayed { get; set; }
        public int WinCount { get; set; }
    }

    public class WordListItem
    {
        [Key]
        public int WordListId { get; set; }
        public int GameSessionId { get; set; }
        public string? Word { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public DirectionEnum Direction { get; set; }
    }
}
