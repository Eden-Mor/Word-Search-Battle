using System.ComponentModel.DataAnnotations;
using WordSearchBattleShared.Enums;

namespace WordSearchBattleAPI.Models
{
    public class GameSession
    {
        public GameSession() { }
        public GameSession(GameSessionStatus statusCode, int ownerId, string roomCode)
        {
            GameSessionStatusCode = statusCode;
            OwnerPlayerId = ownerId;
            RoomCode = roomCode;
        }
        [Key]
        public int GameSessionId { get; set; }
        public GameSessionStatus GameSessionStatusCode { get; set; }
        public int OwnerPlayerId { get; set; }
        public string? RoomCode { get; set; }
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
        public int EndX { get; set; }
        public int EndY { get; set; }
    }
}
